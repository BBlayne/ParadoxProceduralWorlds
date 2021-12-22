using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;
using Jobberwocky.GeometryAlgorithms.Source.API;
using Jobberwocky.GeometryAlgorithms.Source.Core;
using Jobberwocky.GeometryAlgorithms.Source.Parameters;

public static class TextureGenerator
{
    public static string AppPath = Application.dataPath + "/Temp/";

    private static string shaderPath = "Shaders/TextureUtilShader";
    private static string DrawPixelsToTexShaderPath = "Shaders/DrawPixelsToTexture";
    private static string noShaderMsg = "Could not find the compute shader. Did you move/rename any of the files?";

    private static void SetShaderVars
    (
        ComputeShader InShader,
        Vector4[] InColours,
        int InKernel
    )
    {
        InShader.SetVectorArray("colours", InColours);
    }

    private static int MAX_POINTS = 25;

    private struct RegionFace
    {
        public Coord Site;
        public int Size;
    }

    private struct RegionBound
    {
        public int minX;
        public int maxX;
        public int minY;
        public int maxY;
    }

    public static Texture DrawPixelsToTexture(Texture inSrc, List<Vector3> pixels, Color inColour)
    {
        // our shader
        ComputeShader shader = Resources.Load(DrawPixelsToTexShaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        // our kernel
        int kernel = shader.FindKernel("DrawPixelsToTexture");

        // our input and output textures
        //shader.SetTexture(kernel, "baseTex", inSrc);
        shader.SetTexture(kernel, "resultTex", inSrc);

        // now we're going to do rounds of kernal stuff
        int[] reses = { inSrc.width, inSrc.height };

        // resolution of the output texture
        shader.SetInts("reses", reses);

        // the colour to draw our new pixels
        shader.SetVector("inColour", inColour);

        // length of our compute buffer
        shader.SetInt("inPixelsLength", pixels.Count);

        // setup our structured buffers
        ComputeBuffer pixelBuf = new ComputeBuffer(pixels.Count, 12);
        pixelBuf.SetData(pixels);
        shader.SetBuffer(kernel, "inPixels", pixelBuf);

        shader.Dispatch(kernel, Mathf.CeilToInt(inSrc.width / 16f), Mathf.CeilToInt(inSrc.height / 16f), 1);

        pixelBuf.Release();

        return inSrc;
    }

    public static RenderTexture GetScaledRenderTextureGPU(Texture inSrc, int inNewWidth, int inNewHeight)
    {
        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        int kernel = shader.FindKernel("ScaleImageNN");

        // final result render texture
        RenderTexture outRTex = new RenderTexture(inNewWidth, inNewHeight, 0);
        outRTex.enableRandomWrite = true;
        outRTex.Create();

        // now we're going to do rounds of kernal stuff
        int[] srcResInts = { inSrc.width, inSrc.height };
        int[] dstResInts = { inNewWidth, inNewHeight };        

        shader.SetInts("srcImageReses", srcResInts);
        shader.SetInts("dstImageReses", dstResInts);        

        // set the texture we're writing to
        shader.SetTexture(kernel, "scaledTex", outRTex);
        shader.SetTexture(kernel, "unscaledTex", inSrc);

        shader.Dispatch(kernel, Mathf.CeilToInt(inNewWidth / 16f), Mathf.CeilToInt(inNewHeight / 16f), 1);

        return outRTex;
    }

    public static RenderTexture GetScaledRenderTextureGL(Texture2D inSrc, int inNewWidth, int inNewHeight, FilterMode inFMode)
    {
        Rect texR = new Rect(0, 0, inNewWidth, inNewHeight);

        //We need the source texture in VRAM because we render with it
        inSrc.filterMode = inFMode;
        inSrc.Apply(true);

        RenderTexture rtt = new RenderTexture(inNewWidth, inNewHeight, 32);
        //Set the RTT in order to render to it
        Graphics.SetRenderTarget(rtt);

        //Setup 2D matrix in range 0..1, so nobody needs to care about sized
        GL.LoadPixelMatrix(0, 1, 1, 0);

        //Then clear & draw the texture to fill the entire RTT.
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        Graphics.DrawTexture(new Rect(0, 0, 1, 1), inSrc);

        inSrc.Resize(inNewWidth, inNewHeight);

        inSrc.ReadPixels(texR, 0, 0);

        inSrc.Apply();

        return rtt;
    }

    public static RenderTexture GetRegionSilhouette
    (
        int inGridWidth,
        int inGridHeight,
        Color inSilhouetteColour,
        ref Region inRegion
    )
    {
        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        // In this shader, we colour the mask texture white
        // for all pixels in the current region, leaving it
        // black by default.
        int kernel = shader.FindKernel("FillRegionSilhouette");

        // final result render texture
        RenderTexture outRTex = new RenderTexture(inGridWidth, inGridHeight, 0);
        outRTex.enableRandomWrite = true;
        outRTex.Create();

        // now we're going to do rounds of kernal stuff
        // first round is initializing our masking layer.
        int[] resInts = { inGridWidth, inGridHeight };

        shader.SetVector("silhouetteColour", inSilhouetteColour);
        shader.SetInts("silhouetteReses", resInts);
        shader.SetInt("silhouetteRegionCoordSize", inRegion.Coords.Count);

        // setup our structured buffers
        // Passing in our array of coordinates comprising our region, a Coord is 2 ints (4 bytes each)
        ComputeBuffer coordBuffer = new ComputeBuffer(inRegion.Coords.Count, 8);
        coordBuffer.SetData(inRegion.Coords);
        shader.SetBuffer(kernel, "silhouetteRegionCoords", coordBuffer);

        // set the texture we're writing to
        shader.SetTexture(kernel, "silhouetteTex", outRTex);

        shader.Dispatch(kernel, Mathf.CeilToInt(inGridWidth / 16f), Mathf.CeilToInt(inGridHeight / 16f), 1);

        coordBuffer.Release();

        return outRTex;
    }

    public static RenderTexture DrawDelaunayRegion
    (
        int inGridWidth,
        int inGridHeight,
        ref Region inRegion,
        RegionGenerator.RegionDebugInfo inDebugInfo
    )
    {
        return new RenderTexture(0, 0, 0);
    }

    public static RenderTexture FillVoronoiRegion
    (
        int inGridWidth,
        int inGridHeight,
        ref Region inRegion,
        RegionGenerator.RegionDebugInfo inDebugInfo
    ) 
    {
        return new RenderTexture(0, 0, 0);
    }

    public static RenderTexture FillVoronoiRegion
    (
        int inGridWidth,
        int inGridHeight,
        ref Region inRegion,
        TriangleNet.Topology.DCEL.Face[] inRegionFaces
    )
    {
        int regionWidth = 0;
        int regionHeight = 0;

        // Random colours for each voronoi face
        Color[] seedColours = new Color[inRegionFaces.Length];

        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        for (int i = 0; i < inRegionFaces.Length; ++i)
        {
            seedColours[i] = UnityEngine.Random.ColorHSV(0, 1, 0, 1, 0.1f, 1, 1, 1);
        }

        // final result render texture
        RenderTexture outRTex = new RenderTexture(inGridWidth, inGridHeight, 0);
        outRTex.enableRandomWrite = true;
        outRTex.Create();

        // masking layer, because we're just doing 1 region
        RenderTexture maskRTex = new RenderTexture(inGridWidth, inGridHeight, 0);
        maskRTex.enableRandomWrite = true;
        maskRTex.Create();

        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        // now we're going to do rounds of kernal stuff
        // first round is initializing our masking layer.
        int[] resInts = { inGridWidth, inGridHeight };
        int[] boundsInts = { regionWidth, regionHeight };
        int[] boundsOriginInts = { inRegion.MinX, inRegion.MinY };

        // In this shader, we colour the mask texture white
        // for all pixels in the current region, leaving it
        // black by default.
        int kernel = shader.FindKernel("InitMaskLayer");

        shader.SetTexture(kernel, "Result", outRTex);
        shader.SetTexture(kernel, "Mask", maskRTex);
        shader.SetInts("reses", resInts);
        shader.SetInts("bounds", boundsInts);
        shader.SetInts("boundsOrigin", boundsOriginInts);

        shader.SetInt("coordsSize", inRegion.Coords.Count);

        /* Buffers */

        // Passing in our array of coordinates comprising our region, a Coord is 2 ints (4 bytes each)
        ComputeBuffer coordBuffer = new ComputeBuffer(inRegion.Coords.Count, 8);
        coordBuffer.SetData(inRegion.Coords);
        shader.SetBuffer(kernel, "coords", coordBuffer);

        // passing in our array of colours for each voronoi face
        ComputeBuffer colourBuffer = new ComputeBuffer(seedColours.Length, 16);
        colourBuffer.SetData(seedColours);
        shader.SetBuffer(kernel, "seedColours", colourBuffer);

        // Setup our faces and edges to check
        RegionFace[] regionFaces = new RegionFace[inRegionFaces.Length];

        Coord[] faceVertices = new Coord[inRegionFaces.Length * MAX_POINTS];

        for (int i = 0; i < inRegionFaces.Length; ++i)
        {
            RegionFace currentFace = new RegionFace();

            int firstEdgeID = inRegionFaces[i].Edge.ID;
            var tempEdge = inRegionFaces[i].Edge;

            int count = 0;
            // iterate through all the points making up the voronoi hull
            do
            {
                faceVertices[i * MAX_POINTS + count] = new Coord
                (
                    Mathf.RoundToInt((float)tempEdge.Origin.X), Mathf.RoundToInt((float)tempEdge.Origin.Y)
                );

                tempEdge = tempEdge.Next;
                count++;
            }
            while (tempEdge != null && firstEdgeID != tempEdge.ID);

            Coord site = new Coord(
                Mathf.RoundToInt((float)inRegionFaces[i].generator.X), 
                Mathf.RoundToInt((float)inRegionFaces[i].generator.Y)
            );

            currentFace.Site = site;
            currentFace.Size = count;
            regionFaces[i] = currentFace;
        }

        // I want to encode the vertex information of each face into the compute shader
        // can't use arrays in my structs because not 'blittable'
        // instead I'll separate pass in a '2D' array of Faces by Coords
        // number of faces * fixed max number of coords per face
        ComputeBuffer faceVertexBuffer = new ComputeBuffer(inRegionFaces.Length * MAX_POINTS, 8);
        faceVertexBuffer.SetData(faceVertices);
        shader.SetBuffer(kernel, "faceVertices", faceVertexBuffer);

        // A RegionFace has a Coord as a field (2 ints), and an int (4 bytes)
        ComputeBuffer faceBuffer = new ComputeBuffer(regionFaces.Length, (4*2) + 4);
        faceBuffer.SetData(regionFaces);
        shader.SetBuffer(kernel, "faces", faceBuffer);

        RegionBound[] bounds = new RegionBound[1];
        bounds[0].minX = int.MaxValue;
        bounds[0].minY = int.MaxValue;

        // Each RegionBound struct is 4 ints and 4 bytes each, there is 1 RegionBound in array
        ComputeBuffer regionBoundsBuffer = new ComputeBuffer(bounds.Length, 4 * 4);
        regionBoundsBuffer.SetData(bounds);        
        shader.SetBuffer(kernel, "regionbounds", regionBoundsBuffer);

        shader.Dispatch(kernel, Mathf.CeilToInt(inGridWidth / 16f), Mathf.CeilToInt(inGridHeight / 16f), 1);

        regionBoundsBuffer.GetData(bounds);

        Debug.Log("Min X: " + bounds[0].minX + ", Max X: " +
            bounds[0].maxX + ", Min Y: " + bounds[0].minY + ", Max Y: " + bounds[0].maxY);

        inRegion.MinX = bounds[0].minX;
        inRegion.MaxX = bounds[0].maxX;
        inRegion.MinY = bounds[0].minY;
        inRegion.MaxY = bounds[0].maxY;

        regionBoundsBuffer.Release();
        coordBuffer.Release();
        colourBuffer.Release();
        faceBuffer.Release();
        faceVertexBuffer.Release();

        return maskRTex;
    }

    public static RenderTexture GetRandomColourLandRegionsTexture
    (
        int InWidth,
        int InHeight,
        Vector4[] InColours,
        byte[] InSilhouetteMap,
        int[] InLabelGrid
    )
    {
        RenderTexture retTex = new RenderTexture(InWidth, InHeight, 0);
        retTex.enableRandomWrite = true;
        retTex.Create();

        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        int[] resInts = { InWidth, InHeight };

        int kernel = shader.FindKernel("ColourLandTexture");
        shader.SetTexture(kernel, "Result", retTex);
        shader.SetInts("reses", resInts);

        //int Vec4Stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4));
        // Vec4 stride: 16
        ComputeBuffer colourBuffer = new ComputeBuffer(InColours.Length, 16);
        colourBuffer.SetData(InColours);
        shader.SetBuffer(kernel, "colours", colourBuffer);

        // Color32 stride: 4
        ComputeBuffer silhouetteColourBuffer = new ComputeBuffer(InSilhouetteMap.Length, 4);
        silhouetteColourBuffer.SetData(InSilhouetteMap);
        shader.SetBuffer(kernel, "silhouetteMap", silhouetteColourBuffer);

        ComputeBuffer labelGridBuffer = new ComputeBuffer(InLabelGrid.Length, 4);
        labelGridBuffer.SetData(InLabelGrid);
        shader.SetBuffer(kernel, "LabelGrid", labelGridBuffer);

        shader.Dispatch(kernel, Mathf.CeilToInt(InWidth / 16f), Mathf.CeilToInt(InHeight / 16f), 1);

        silhouetteColourBuffer.Release();
        colourBuffer.Release();
        labelGridBuffer.Release();

        return retTex;
    }

    public static RenderTexture GetRandomColourRegionsTexture
    (
        int InWidth,
        int InHeight,
        Vector4[] InColours,
        int[] InLabelGrid
    )
    {
        RenderTexture retTex = new RenderTexture(InWidth, InHeight, 0);
        retTex.enableRandomWrite = true;
        retTex.Create();

        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }        

        int[] resInts = { InWidth, InHeight };

        int kernel = shader.FindKernel("ColourTexture");
        shader.SetTexture(kernel, "Result", retTex);
        shader.SetInts("reses", resInts);

        ComputeBuffer colourBuffer = new ComputeBuffer(InColours.Length, 16);
        colourBuffer.SetData(InColours);
        shader.SetBuffer(kernel, "colours", colourBuffer);

        ComputeBuffer labelGridBuffer = new ComputeBuffer(InLabelGrid.Length, 4);
        labelGridBuffer.SetData(InLabelGrid);
        shader.SetBuffer(kernel, "LabelGrid", labelGridBuffer);

        shader.Dispatch(kernel, Mathf.CeilToInt(InWidth / 16f), Mathf.CeilToInt(InHeight / 16f), 1);

        colourBuffer.Release();
        labelGridBuffer.Release();

        return retTex;
    }

    public struct ThreadedSaveTextureAsPNGJob : IJob
    {
        public NativeArray<byte> result;

        [ReadOnly]
        public NativeArray<byte> pixels;
        
        public UnityEngine.Experimental.Rendering.GraphicsFormat texFormat;
        public int width;
        public int height;

        public void Execute()
        {
            // Encode texture into PNG        
            byte[] bytes = ImageConversion.EncodeArrayToPNG(
                pixels.ToArray(),
                texFormat,
                (uint)width,
                (uint)height
            );

            result.CopyFrom(bytes);
        }
    }

    public struct GetPNGSizeJob : IJob
    {
        public NativeArray<int> result;

        [ReadOnly]
        public NativeArray<byte> pixels;

        public UnityEngine.Experimental.Rendering.GraphicsFormat texFormat;
        public int width;
        public int height;

        public void Execute()
        {
            // Encode texture into PNG        
            byte[] bytes = ImageConversion.EncodeArrayToPNG(
                pixels.ToArray(),
                texFormat,
                (uint)width,
                (uint)height
            );

            result[0] = bytes.Length;
        }
    }

    public static Texture2D CreateTexture2D(RenderTexture InRTX, int InWidth, int InHeight)
    {
        Texture2D texture = new Texture2D(InWidth, InHeight, TextureFormat.RGBA32, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = InRTX;
        texture.ReadPixels(new Rect(0, 0, InWidth, InHeight), 0, 0);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        RenderTexture.active = null;
        return texture;
    }

    public static Texture2D CreateTexture2D(RenderTexture InRTX)
    {
        Texture2D texture = new Texture2D(InRTX.width, InRTX.height, TextureFormat.RGBA32, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = InRTX;
        texture.ReadPixels(new Rect(0, 0, InRTX.width, InRTX.height), 0, 0);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        RenderTexture.active = null;
        return texture;
    }

    private static async Task SaveTextureAsPngAsync(Texture2D texture, string path, string InName)
    {
        byte[] bytes = ImageConversion.EncodeArrayToPNG(
            texture.GetRawTextureData(),
            texture.graphicsFormat,
            (uint)texture.width,
            (uint)texture.height
        );

        using (var filestream = new FileStream(path + InName, FileMode.Create))
        {
            await filestream.WriteAsync(bytes, 0, bytes.Length);
        }

        await Task.Delay(1);
    }

    public static async Task SaveTextureAsPng(Texture2D texture, string path, string InName)
    {
        byte[] data = texture.GetRawTextureData();
        UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat = texture.graphicsFormat;
        int width = texture.width;
        int height = texture.height;

        await Task.Run(async () =>
        {
            byte[] bytes = ImageConversion.EncodeArrayToPNG(
                data,
                graphicsFormat,
                (uint)width,
                (uint)height
            );

            using (var filestream = new FileStream(path + InName, FileMode.Create))
            {
                await filestream.WriteAsync(bytes, 0, bytes.Length);
            }
        });
    }

    public static void ThreadedSaveTextureAsPNG(Texture2D texture, string InName)
    {
        // Because I don't know the result of EncodeArrayToPNG's length
        // i need to do this twice, first to get the length and then to
        // save the result


        GetPNGSizeJob getPNGSize = new GetPNGSizeJob();
        getPNGSize.width = texture.width;
        getPNGSize.height = texture.height;
        getPNGSize.pixels = texture.GetRawTextureData<byte>();
        getPNGSize.texFormat = texture.graphicsFormat;
        NativeArray<int> sizeResult = new NativeArray<int>(1, Allocator.TempJob);
        getPNGSize.result = sizeResult;

        JobHandle getSizeHandle = getPNGSize.Schedule();
        getSizeHandle.Complete();

        NativeArray<byte> result = new NativeArray<byte>(sizeResult[0], Allocator.TempJob);

        ThreadedSaveTextureAsPNGJob savePNGJob = new ThreadedSaveTextureAsPNGJob();        

        savePNGJob.width = texture.width;
        savePNGJob.height = texture.height;
        savePNGJob.pixels = texture.GetRawTextureData<byte>();
        savePNGJob.texFormat = texture.graphicsFormat;        
        savePNGJob.result = result;

        JobHandle handle = savePNGJob.Schedule();
        handle.Complete();

        File.WriteAllBytes(Application.dataPath + "/Temp/" + InName + ".png", result.ToArray());

        sizeResult.Dispose();
        result.Dispose();
    }

    public static IEnumerator AsyncSaveTextureAsPNG(Texture2D texture, string name)
    {
        yield return new WaitForEndOfFrame();

        // Encode texture into PNG        
        byte[] bytes = ImageConversion.EncodeArrayToPNG(
            texture.GetRawTextureData(), 
            texture.graphicsFormat,
            (uint)texture.width,
            (uint)texture.height
        );

        File.WriteAllBytes(Application.dataPath + "/Temp/" + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + Application.dataPath + "/Temp/" + name + ".png");
    }

    public static void SaveTextureAsPNG(Texture2D texture, string name)
    {
        // Encode texture into PNG        
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Temp/" + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + Application.dataPath + "/Temp/" + name + ".png");
    }
}
