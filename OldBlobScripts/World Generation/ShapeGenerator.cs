using ImprovedPerlinNoiseProject;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/*
 * The ShapeGenerator class generates the final and intermediate "shapes"
 * that will be converted into a height map (or some other kind of map)
 * the shapes originate from a GPU driven noise algorithm via a shader.
 */
public class ShapeGenerator
{
    ShapeSettings shapeSettings;
    INoiseFilter[] noiseFilters;

    private GPUPerlinNoise m_perlin;

    private Material FalloffMaterial = null;
    public Material WorldMapMaterial = null;

    private bool useFalloff = true;

    Texture2D blankTex = null;

    RenderTexture currentWorldRT = null;

    List<int> ActiveLayers = new List<int>();

    public void InitSettings(ShapeSettings _shapeSettings)
    {
        this.shapeSettings = _shapeSettings;
        if (shapeSettings == null)
        {
            Debug.Log("Somehow null w_shape_settings?");
            return;
        }

        if (shapeSettings.noiseLayers == null)
        {
            Debug.Log("Somehow null noise layers?");
            return;
        }

        blankTex = new Texture2D(_shapeSettings.Width, _shapeSettings.Height, TextureFormat.RGBA32, false);
        currentWorldRT = RenderTexture.GetTemporary(_shapeSettings.Width, _shapeSettings.Height);

        FalloffMaterial = Resources.Load("Materials/Blend Materials/RoundedFalloff", typeof(Material)) as Material;
        if (FalloffMaterial == null)
        {
            Debug.Log("Error, RoundedFalloff not found.");
        }

        SetNoiseFilters(_shapeSettings);

        UpdateNoise();

        // register to events
        UIHandler.OnSeedSettingsChanged += HandleSeedSettingsChanged;
        UIHandler.OnNoiseSettingsUpdated += UpdateSettings;
        // to do maybe attach something to UIHandler.OnBlendSettingsUpdate and so on
        UIHandler.OnBlendModeChanged += HandleBlendModeChanged;
    }

    void UpdateNoise()
    {
        // Retrieve which noise layers are enabled
        GetActiveNoiseLayers();
        // update blend settings
        UpdateBlendSettings();
    }

    void GetActiveNoiseLayers()
    {
        // In UpdateBlendSettings if a layer isn't enabled we still wish to blend
        // the current active layer with the layers *below* the inactive layer,
        // skipping the inactive layer for blending purposes.
        // we acquire the indexes of the active layers to loop through
        // if no layers are inactive then there should be a 1:1
        // correspondence between active layers and their indexes
        ActiveLayers.Clear();
        for (int i = 0; i < shapeSettings.noiseLayers.Length; i++)
        {
            if (shapeSettings.noiseLayers[i].enabled)
            {
                ActiveLayers.Add(i);
            }
        }
    }

    public void SetNoiseFilters(ShapeSettings _shapeSettings)
    {
        this.noiseFilters = new INoiseFilter[_shapeSettings.noiseLayers.Length];

        for (int i = 0; i < noiseFilters.Length; i++)
        {
            this.noiseFilters[i] = 
                NoiseFilterFactory.CreateNoiseFilter(_shapeSettings.noiseLayers[i].noiseSettings, _shapeSettings.Width, _shapeSettings.Height);
            
            NoiseMapUtils.BLEND_MODES blend_mode = _shapeSettings.noiseLayers[i].blend_mode;

            _shapeSettings.noiseLayers[i].blendMaterial = WorldMapUtils.LoadBlendMaterial(blend_mode);

            // hrm does this do anything?
            _shapeSettings.noiseLayers[i].tex = this.noiseFilters[i].Evaluate();
            // remember to comment this out
            WorldMapUtils.SaveTextureToFile(_shapeSettings.noiseLayers[i].tex, "shapesettings_" + i + "_tex");
        }
    }

    public void UpdateSettings(int _layerID)
    {
        // Update the material settings from the now updated noisesettings/shapesettings.
        // Update world shape settings with new noise settings at the specified layer
        shapeSettings.UpdateNoiseLayer(_layerID, shapeSettings.noiseLayers[_layerID].noiseSettings);
        UpdateSettings(_layerID, shapeSettings);
        UpdateBlendSettings();
    }

    public void HandleSeedSettingsChanged(int _layerID)
    {
        // Update the material settings from the now updated noisesettings/shapesettings.
        NoiseFilterFactory.UpdateNoiseFilterSeed(noiseFilters[_layerID], shapeSettings.noiseLayers[_layerID].noiseSettings.noiseSeedSettings.Seed);
        //UpdateBlendSettings();
    }

    public void HandleNoiseFilterChanged(int _layerID)
    {
        // todo

    }

    public void HandleBlendModeChanged(int _layerID)
    {
        // todo
        UpdateBlendSettings();
    }

    public void UpdateSettings(ShapeSettings _nuShapeSettings)
    {
        // Only reupdate everything is a layer was added/deleted
        if (noiseFilters.Length != _nuShapeSettings.noiseLayers.Length)
        {
            this.shapeSettings = _nuShapeSettings;
            SetNoiseFilters(_nuShapeSettings);
        }
        else
        {
            // layers are the same
            for (int i = 0; i < noiseFilters.Length; i++)
            {
                this.noiseFilters[i].UpdateSettings(_nuShapeSettings.noiseLayers[i].noiseSettings);
                this.noiseFilters[i].UpdateDimensions(_nuShapeSettings.Width, _nuShapeSettings.Height);
            }
        }
    }

    public void UpdateSettings(int _layerID, ShapeSettings _nuShapeSettings)
    {
        // update the material settings
        this.noiseFilters[_layerID].UpdateSettings(_nuShapeSettings.noiseLayers[_layerID].noiseSettings);
    }

    public void UpdateSettings(int _layerID, NoiseSettings _noiseSettings)
    {
        // this has zero references and is unclear to me why I wrote this.
        // seems to include checks for when filter type or seed settings are changed
        // in order to prompt the recreation of the relevant noise filters and reinitialize noise settings/seeds.
        if (shapeSettings.noiseLayers[_layerID].noiseSettings.filterType != _noiseSettings.filterType)
        {
            // replace noisefilter if there has been a change in selected noisefilter for that layer
            noiseFilters[_layerID] = NoiseFilterFactory.CreateNoiseFilter(_noiseSettings, shapeSettings.Width, shapeSettings.Height);
        }
        else if (shapeSettings.noiseLayers[_layerID].noiseSettings.noiseSeedSettings.Seed != _noiseSettings.noiseSeedSettings.Seed)
        {
            // update seed information if the seed has been changed for that layer
            NoiseFilterFactory.UpdateNoiseFilterSeed(noiseFilters[_layerID], _noiseSettings.noiseSeedSettings.Seed);
        }

        // Update world shape settings with new noise settings at the specified layer
        shapeSettings.UpdateNoiseLayer(_layerID, _noiseSettings);
        UpdateSettings(_layerID, shapeSettings);
    }

    // todo
    // this function might not be necessary and can be handled in ShapeSettings when the change first occurs?
    public void UpdateBlendMode(int _noiseLayerID, int _nuBlendMode)
    {
        shapeSettings.noiseLayers[_noiseLayerID].blend_mode = (NoiseMapUtils.BLEND_MODES)_nuBlendMode;
        // Load new material
        shapeSettings.noiseLayers[_noiseLayerID].blendMaterial = WorldMapUtils.LoadBlendMaterial(shapeSettings.noiseLayers[_noiseLayerID].blend_mode);
        // Update
        UpdateBlendSettings();
    }



    // TODO: Check if Blend Settings are right
    public void UpdateBlendSettings()
    {
        RenderTexture.ReleaseTemporary(currentWorldRT);
        currentWorldRT = RenderTexture.GetTemporary(shapeSettings.Width, shapeSettings.Height);
        // re adding in the previous code to compute blend layers
        for (int i = ActiveLayers.Count - 1; i >= 0; i--)
        {
            int currentLayerIndex = ActiveLayers[i];
            RenderTexture local_rtx = noiseFilters[currentLayerIndex].Render();
            shapeSettings.noiseLayers[currentLayerIndex].blendMaterial.SetTexture("_TexA", local_rtx);
            shapeSettings.noiseLayers[currentLayerIndex].blendMaterial.SetTexture("_TexB", currentWorldRT);

            Graphics.Blit(null, currentWorldRT, shapeSettings.noiseLayers[currentLayerIndex].blendMaterial);

            RenderTexture.ReleaseTemporary(local_rtx);
        }

        /*
        for (int i = ActiveLayers.Count - 1; i >= 0; i--)
        {          


            if (shapeSettings.noiseLayers[ActiveLayers[i]].blend_mode == NoiseMapUtils.BLEND_MODES.NORMAL || i == (noiseFilters.Length - 1))
            {

                rtex = RenderTexture.GetTemporary(shapeSettings.Width, shapeSettings.Height);
                shapeSettings.noiseLayers[ActiveLayers[i]].blendMaterial.SetTexture("_TexA", noiseFilters[ActiveLayers[i]].Render());
                shapeSettings.noiseLayers[ActiveLayers[i]].blendMaterial.SetTexture("_TexB", rtex);

                Graphics.Blit(null, rtex, shapeSettings.noiseLayers[ActiveLayers[i]].blendMaterial);
            }
            else
            {
                shapeSettings.noiseLayers[ActiveLayers[i]].blendMaterial.SetTexture("_TexA", noiseFilters[ActiveLayers[i]].Render());
                shapeSettings.noiseLayers[ActiveLayers[i]].blendMaterial.SetTexture("_TexB", rtex);

                Graphics.Blit(null, rtex, shapeSettings.noiseLayers[ActiveLayers[i]].blendMaterial);
            }
        }
        */

        //RenderTexture temp_rtx = noiseFilters[0].Render();
        //shapeSettings.noiseLayers[0].blendMaterial.SetTexture("_TexA", temp_rtx);
        //shapeSettings.noiseLayers[0].blendMaterial.SetTexture("_TexB", currentWorldRT);

        //Graphics.Blit(null, currentWorldRT, shapeSettings.noiseLayers[0].blendMaterial);

        UpdateWorldMapMaterial(currentWorldRT);

        //RenderTexture.ReleaseTemporary(temp_rtx);
        
    }

    public void UpdateWorldMapMaterial(Texture _rtex)
    {
        WorldMapMaterial.SetTexture("_TexA", _rtex);
    }

    /*
     * Iterate through all active noise layers and return
     * only the topmost layer with all transformations
     * i.e layered blend effects applied.
     * 
     * currently seems incomplete
     * todo complete this
     */
    public void UpdateWorldMapMaterial()
    {
        if (useFalloff)
        {
            //Texture tex = RenderTexture.GetTemporary(shapeSettings.Width, shapeSettings.Height);
            //FalloffMaterial.SetTexture("_MainTex", currentWorldRT);
            //UpdateFalloffSettings(shapeSettings.FalloffFactors);
            //Graphics.Blit(null, tex as RenderTexture, FalloffMaterial);
            //WorldMapMaterial.SetTexture("_TexA", currentWorldRT);
            // confirmed this is the issue (releasing render texture nulls material)
            //RenderTexture.ReleaseTemporary(currentWorldRT);
            //RenderTexture.ReleaseTemporary(tex as RenderTexture);
        }

        WorldMapMaterial.SetTexture("_TexA", blankTex);
        /*
        for (int i = 0; i < shapeSettings.noiseLayers.Length; i++)
        {
            if (shapeSettings.noiseLayers[i].enabled)
            {
                if (useFalloff)
                {
                    Texture tex = RenderTexture.GetTemporary(shapeSettings.Width, shapeSettings.Height);
                    Graphics.Blit(null, tex as RenderTexture, shapeSettings.noiseLayers[i].blendMaterial);
                    FalloffMaterial.SetTexture("_MainTex", tex);
                    UpdateFalloffSettings(shapeSettings.FalloffFactors);
                    RenderTexture.ReleaseTemporary(tex as RenderTexture);

                }
                else
                {
                    WorldMapMaterial = shapeSettings.noiseLayers[i].blendMaterial;
                }            
                return;
            }
        }
        */
    }

    public void ToggleFalloff(bool isEnabled)
    {
        this.useFalloff = isEnabled;

        UpdateWorldMapMaterial();
    }

    public Material GetCurrentWorldMaterial()
    {
        return WorldMapMaterial;
    }

    public void UpdateFalloffSettings(Vector2 power)
    {
        if (FalloffMaterial != null)
        {
            FalloffMaterial.SetFloat("StrengthA", power.x);
            FalloffMaterial.SetFloat("StrengthB", power.y);
        }
    }

    public INoiseFilter GetNoiseLayerSettings(int noiseLayer)
    {
        if (noiseFilters.Length > 0)
        {
            if (shapeSettings.noiseLayers[noiseLayer].enabled)
            {
                return noiseFilters[noiseLayer];
            }
        }
        return null;
    }

    public Texture2D GetLayerElevationTexture(int layerID)
    {
        if (layerID < shapeSettings.noiseLayers.Length)
        {
            if (shapeSettings.noiseLayers[layerID].enabled)
            {
                return noiseFilters[layerID].Evaluate();
            }
        }

        return new Texture2D(shapeSettings.Width, shapeSettings.Height); // blank texture
    }

    public Color[] GetLayerElevationValues(int layerID)
    {
        Color[] values = new Color[shapeSettings.Width * shapeSettings.Height];
        if (shapeSettings.noiseLayers[layerID].enabled)
        {
            values = noiseFilters[layerID].Evaluate().GetPixels();
        }

        return values;
    }



    private void HandleFalloffPowerChanged(Vector2 value)
    {
        shapeSettings.FalloffFactors = value;
        UpdateFalloffSettings(shapeSettings.FalloffFactors);
    }

    private void HandleNoiseLayerSettingsUpdated(int _layerID)
    {
        UpdateSettings(_layerID);

        //UpdateMapDisplay();
    }

    private void HandleNoiseLayerSeedChanged(int _layerID)
    {

    }
}
