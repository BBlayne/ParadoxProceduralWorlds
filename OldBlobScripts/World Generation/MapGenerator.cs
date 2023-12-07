using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using ImprovedPerlinNoiseProject;
using System.IO;
using System.Linq;

public class MapGenerator : MonoBehaviour
{

    public enum VIEW_MODES
    {
        HEIGHT_MAP,
        BLENDED_HEIGHT_MAP,
        FALL_OFF
    }

    public enum WRAP_MODES
    {
        NONE,
        WRAP_HORIZONTAL
    }

    [HideInInspector]
    public bool shapeSettingsFoldout;

    public ShapeSettings worldShapeSettings = null;

    ShapeGenerator worldShapeGenerator = new ShapeGenerator();
    //ShapeGenerator[] shapeGenerators;

    [SerializeField, Range(1, 5632)]
    public int Width = 256;
    [SerializeField, Range(1, 2048)]
    public int Height = 256;

    public bool useScaledHeightMap = true;
    [Range(0.0f, 1f)]
    public float cutoffThreshold = 0.0f;

    WorldMinMax blendedMinMax = new WorldMinMax();

    public bool UpdateDisplay = true;

    public Vector3 factors = new Vector3(2.2f, 3.2f, 2);

    public float frequency = 10;

    public float[,] finalHeightMap = null;

    public RESOLUTIONS resolution = RESOLUTIONS.W256_H256;

    public VIEW_MODES ViewMode = VIEW_MODES.HEIGHT_MAP;
    public FalloffGenerator.FalloffModes falloffMode = FalloffGenerator.FalloffModes.SQUARE;
    [SerializeField]
    public bool useFalloff = false;

    MeshRenderer mapRenderer;

    private GPUPerlinNoise m_perlin;

    // Burst compile really important. Makes 1 second operations 10 milliseconds. It's amazing.
    [BurstCompile]
    public struct MapChunkEvaluateJob : IJobParallelFor
    {
        // This is needed in order to determine X and Y coordinates from index.
        [ReadOnly] public int sizeX;

        [ReadOnly] public float f;

        [ReadOnly] public int offsetX;
        [ReadOnly] public int offsetY;

        // This is the "collapsedArray", the 2d array collapsed into a 1d array.
        // The [WriteOnly] is not as nessisary as [ReadOnly] in jobs but it might help on the behind the scenes Unity optimization.
        [WriteOnly] public NativeArray<float> unscaled_array;

        public void Execute(int index)
        {
            float amplitude = 1.0f;
            float sum = 0;
            float lacunarity = 3.25f;
            float frequency = 0.0025f;
            float gain = 1.025f; // also persistence
            // Previously: Unity.Mathematics.noise.snoise(new Vector2(index, i));
            // Index = X value. i = Y value.
            // We can obtain these values from the index using sizeX
            // Use Unity.Mathematics.float2, it's optimized for Jobs.
            // I dont know if snoise needs a float2 or int2 can be used and you can skip the casting to int.
            // octave layers
            for (int i = 0; i < 5; i++)
            {
                float x = ((index+offsetX) % (sizeX + offsetX)) * frequency;
                float y = ((index + offsetY) / (sizeX + offsetY)) * frequency;

                float value = Unity.Mathematics.noise.snoise(new Unity.Mathematics.float2(x, y));
                sum += value * amplitude;
                frequency *= lacunarity;
                amplitude *= gain;
            }

            unscaled_array[index] = sum;
        }
    }

    // Burst compile really important. Makes 1 second operations 10 milliseconds. It's amazing.
    [BurstCompile]
    public struct MapEvaluateJob : IJobParallelFor
    {
        // This is needed in order to determine X and Y coordinates from index.
        [ReadOnly] public int sizeX;

        [ReadOnly] public float f;

        // This is the "collapsedArray", the 2d array collapsed into a 1d array.
        // The [WriteOnly] is not as nessisary as [ReadOnly] in jobs but it might help on the behind the scenes Unity optimization.
        [WriteOnly] public NativeArray<float> unscaled_array;

        public void Execute(int index)
        {
            float amplitude = 1.0f;
            float sum = 0;
            float lacunarity = 3.25f;
            float frequency = 0.0025f;
            float gain = 1.025f; // also persistence
            // Previously: Unity.Mathematics.noise.snoise(new Vector2(index, i));
            // Index = X value. i = Y value.
            // We can obtain these values from the index using sizeX
            // Use Unity.Mathematics.float2, it's optimized for Jobs.
            // I dont know if snoise needs a float2 or int2 can be used and you can skip the casting to int.
            // octave layers
            for (int i = 0; i < 5; i++)
            {
                float x = (index % sizeX) * frequency;
                float y = (index / sizeX) * frequency;

                float value = Unity.Mathematics.noise.snoise(new Unity.Mathematics.float2(x, y));
                sum += value * amplitude;
                frequency *= lacunarity;
                amplitude *= gain;
            }

            unscaled_array[index] = sum;
        }
    }

    // Burst compile really important. Makes 1 second operations 10 milliseconds. It's amazing.
    [BurstCompile]
    public struct MapChunkNormalizeJob : IJobParallelFor
    {

        // This is the "collapsedArray", the 2d array collapsed into a 1d array.
        // The [WriteOnly] is not as nessisary as [ReadOnly] in jobs 
        // but it might help on the behind the scenes Unity optimization.
        // Set this to [NativeDisableParallelForRestriction]. Allows you to now read and write to the same array.
        [NativeDisableParallelForRestriction] public NativeArray<float> scaled_array;

        [ReadOnly] public NativeReference<float> myMin, myMax;

        public void Execute(int index)
        {
            //scaled_array[index] = (unscaled_array[index] - min) / (max - min);
            scaled_array[index] = 
                Unity.Mathematics.math.unlerp(myMin.Value, myMax.Value, scaled_array[index]) * 2 - 1;
        }
    }

    [BurstCompile]
    private struct ComputeMinMax : IJob
    {
        // [ReadOnly] an [WriteOnly] tags dont really matter in IJobs but it's a good habit to get into.
        [ReadOnly] public NativeArray<float> unscaledArray;

        // Read and write from these. It's an IJob, not paralleled so eh.
        // Save memory space.
        public NativeReference<float> min, max;

        public void Execute()
        {
            min.Value = float.MaxValue;
            max.Value = float.MinValue;

            for (var index = 0; index < unscaledArray.Length; index++)
            {
                var value = unscaledArray[index];
                if (value < min.Value) min.Value = value;
                if (value > max.Value) max.Value = value;
            }
        }
    }

    public float[] PopulateArray1D(int sizeX, int sizeY)
    {
        // Try to use .TempJob allocation instead of persistant. Keeps unintentional memory leaks minimized.
        var heights = new NativeArray<float>(sizeX * sizeY, Allocator.TempJob);

        // When creating a single sized NativeArray for job output, use NativeReference.
        // It's the same thing but better terminology.
        var min = new NativeReference<float>(Allocator.TempJob);
        var max = new NativeReference<float>(Allocator.TempJob);

        JobHandle handle = new MapEvaluateJob
        {
            sizeX = sizeX,
            unscaled_array = heights,
            f = frequency
            // This inlines everything. It initializes the parallel threads and stops the main thread 
            // until it's done.
        }.Schedule(heights.Length, 1);

        handle = new ComputeMinMax
        {
            unscaledArray = heights,
            min = min,
            max = max
        }.Schedule(handle);        

        new MapChunkNormalizeJob
        {
            scaled_array = heights,
            myMin = min,
            myMax = max
        }.Schedule(heights.Length, 1, handle).Complete();

        var outputArray = heights.ToArray();

        // DO NOT FORGET TO DEALLOCATE THE NATIVE ARRAY WHEN YOU ARE DONE!!!!!!
        heights.Dispose();
        min.Dispose();
        max.Dispose();

        return outputArray;
    }

    public float[] PopulateSubArray1D(int sizeX, int sizeY, int offsetX, int offsetY)
    {
        // Try to use .TempJob allocation instead of persistant. Keeps unintentional memory leaks minimized.
        var heights = new NativeArray<float>(sizeX * sizeY, Allocator.TempJob);

        // When creating a single sized NativeArray for job output, use NativeReference.
        // It's the same thing but better terminology.
        var min = new NativeReference<float>(Allocator.TempJob);
        var max = new NativeReference<float>(Allocator.TempJob);

        JobHandle handle = new MapChunkEvaluateJob
        {
            sizeX = sizeX,
            offsetX = offsetX,
            offsetY = offsetY,
            unscaled_array = heights,
            f = frequency
            // This inlines everything. It initializes the parallel threads and stops the main thread 
            // until it's done.
        }.Schedule(heights.Length, 1);

        handle = new ComputeMinMax
        {
            unscaledArray = heights,
            min = min,
            max = max
        }.Schedule(handle);

        new MapChunkNormalizeJob
        {
            scaled_array = heights,
            myMin = min,
            myMax = max
        }.Schedule(heights.Length, 1, handle).Complete();

        var outputArray = heights.ToArray();

        // DO NOT FORGET TO DEALLOCATE THE NATIVE ARRAY WHEN YOU ARE DONE!!!!!!
        heights.Dispose();
        min.Dispose();
        max.Dispose();

        return outputArray;
    }

    // Start is called before the first frame update
    void Start()
    {
        /*
        // Stopwatch diagnostic template, saved for later
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();            
        sw.Stop();
        Debug.Log("Multithreaded job system PopulateArray1D took " + 
            (float)sw.ElapsedMilliseconds / 1000 + " seconds...");
        */

        // Most of the above should be in Initialize()?
        Initialize();

        // Generate the maps
        Generate();
    }

    private void SetAspectRatio(int width, int height)
    {
        // update aspect ratio to pass to shader
        float aspect = (float)Width / Height;
        Vector2 m_scale = new Vector2(1, 1);

        if (aspect > 1)
        {
            m_scale.x = aspect;
        }
        else
        {
            m_scale.y = (float)Height / Width;
        }
    }

    private void SetCamera(int width, int height)
    {
        if ((float)Screen.width / Screen.height < (float)width / height)
        {
            Camera.main.orthographicSize = (float)width / 2 * (float)Screen.height / Screen.width;
        }
        else
        {
            Camera.main.orthographicSize = (float)height / 2;
        }
        Camera.main.orthographicSize = Camera.main.orthographicSize * 10;
    }

    private void Update()
    {       
        //mapRenderer.transform.localScale = new Vector3(-Width, 1, -Height);
        //SetCamera(Width, Height);

        // Save map/noise/etc to texture for debugging purposes
        // for production use this will be a button
        //SaveMapToTexture(Width, Height);
    }

    private void SaveTextureToFile(Texture2D tex, string filename)
    {
        Debug.Log("Saving texture...");

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Temp/" + filename + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " +
            Application.dataPath + "/Temp/" + filename + ".png");
    }

    public void Initialize()
    {
        // Get the mesh we are rendering our output to
        mapRenderer = transform.Find("MapObject2").GetComponent<MeshRenderer>();

        // Set our resolution
        //System.Tuple<int, int> res = WorldMapUtils.SetResolution(resolution);
        //Width  = res.Item1;
        //Height = res.Item2;

        if (worldShapeSettings == null)
        {
            Debug.Log("Error, no shape settings");
        }
        else
        {
            Width = worldShapeSettings.Width;
            Height = worldShapeSettings.Height;
            // Camera Stuff
            SetCamera(worldShapeSettings.Width, worldShapeSettings.Height);

            // set map display plane to scale of the texture
            // -width/-height because Unity is weird.
            //mapRenderer.transform.localScale = new Vector3(-Width, 1, -Height);
            mapRenderer.transform.localScale = new Vector3(-(float)Width / 100, 1, -(float)Height / 100);


            worldShapeGenerator = new ShapeGenerator();
            worldShapeGenerator.UpdateSettings(worldShapeSettings);

            /*
            shapeGenerators = new ShapeGenerator[worldShapeSettings.noiseLayers.Length];
            for (int i = 0; i < worldShapeSettings.noiseLayers.Length; i++)
            {
                shapeGenerators[i] = new ShapeGenerator();
                shapeGenerators[i].UpdateWorldDimensions(Width, Height);
                shapeGenerators[i].UpdateSettings(worldShapeSettings);
            }
            */
        }
    }

    public void UpdateSettings()
    {
        // Set our resolution
        //System.Tuple<int, int> res = WorldMapUtils.SetResolution(resolution);
        //Width = res.Item1;
        //Height = res.Item2;

        if (worldShapeSettings != null)
        {
            Width = worldShapeSettings.Width;
            Height = worldShapeSettings.Height;

            // set map display plane to scale of the texture
            // -width/-height because Unity is weird.
            //mapRenderer.transform.localScale = new Vector3(-Width, 1, -Height);
            mapRenderer.transform.localScale = new Vector3(-(float)Width / 100, 1, -(float)Height / 100);
            worldShapeGenerator.UpdateSettings(worldShapeSettings);

            SetCamera(Width, Height);
        }
    }

    /*
     * Generate the underlying "maps" that determine the world map.
     * Height Maps. Which determine the general landmasses (water, land, islands, mountains, etc).
     * Heat map (for climate with height)
     * Terrain (Colour) maps based on height and climate.
     * Tree map (based on climate)
     * world normal map.
     * 
     * The only one probably not here is rivers, which needs voronoi.
     * 
     * Height map might need additional passes with different noise modes
     * blended together to get appropriate results.
     * 
     */
    public void Generate()
    {
        int numNoiseLayers = worldShapeSettings.noiseLayers.Length;

        // loop through noise layers, create height maps
        // based on layer settings, blend them together
        // return final height map.
        Texture2D previousTex =
            new Texture2D(worldShapeSettings.Width, worldShapeSettings.Height, TextureFormat.ARGB32, false);
        previousTex.filterMode = FilterMode.Point;
        previousTex.wrapMode = TextureWrapMode.Clamp;
        previousTex.Apply();

        for (int i = numNoiseLayers - 1; i >= 0; i--)
        {
            // generate height maps based on settings
            // load specific material
            var nl = worldShapeSettings.noiseLayers[i];
            Texture2D currentTex = worldShapeGenerator.GetLayerElevationTexture(i);
            Material currentBlendMaterial = nl.blendMaterial;
            if (numNoiseLayers == 1 || i == numNoiseLayers - 1)
            {
                currentBlendMaterial = WorldMapUtils.LoadBlendMaterial(NoiseMapUtils.BLEND_MODES.NORMAL);
                currentBlendMaterial.SetFloat("_Opacity", 0);
            }
            else
            {
                currentBlendMaterial.SetFloat("_Opacity", 1);
            }
                

            currentBlendMaterial.SetTexture("_MainTex", currentTex);
            currentBlendMaterial.SetTexture("_SecondTex", previousTex);
            currentBlendMaterial.SetFloat("_StrengthA", 1);
            currentBlendMaterial.SetFloat("_StrengthB", 1);
            currentBlendMaterial.SetFloat("_Contrast", 1);

            previousTex = WorldMapUtils.BlitMaterialToTexture(currentBlendMaterial,
                                                                worldShapeSettings.Width,
                                                                worldShapeSettings.Height);


            SaveTextureToFile(currentTex, "current");
        }

        mapRenderer.sharedMaterial.SetTexture("_BaseMap", previousTex);
        SaveTextureToFile(previousTex, "final");
    }
    
    public Texture2D UpdateMapDisplay(float[] map, int _width, int _height)
    {
        Texture2D tex = TextureGenerator.TextureFromHeightMap(map, _width, _height);

        mapRenderer.sharedMaterial.mainTexture = tex;
        mapRenderer.sharedMaterial.SetTexture("_BaseMap", tex);
        mapRenderer.transform.localScale = new Vector3(-(float)_width / 100, 1, -(float)_height / 100);

        return tex;
    }

    public void OnShapeSettingsUpdated()
    {
        //Initialize();
        //Generate();
    }

    private void OnValidate()
    {
        //Initialize();
        //Generate();
    }
}
