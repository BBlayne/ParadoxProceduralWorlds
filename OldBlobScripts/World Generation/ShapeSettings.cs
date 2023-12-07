using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ShapeSettings : ScriptableObject
{
    public NoiseLayer[] noiseLayers = null;

    public int Width = 8192;
    public int Height = 4096;

    public Vector2 FalloffFactors = new Vector2(1, 2); // a,b

    [System.Serializable]
    public class NoiseLayer
    {
        public bool enabled = true;
        public bool useFirstLayerAsMask;
        public NoiseSettings noiseSettings;
        public Texture2D tex;
        public RenderTexture result;
        public Material noiseMaterial;
        public NoiseMapUtils.BLEND_MODES blend_mode;
        public Material blendMaterial;

        public NoiseLayer(NoiseSettings _noiseSettings)
        {
            noiseSettings = _noiseSettings;
            blend_mode = NoiseMapUtils.BLEND_MODES.NORMAL;
        }

        public NoiseLayer()
        {
            noiseSettings = new NoiseSettings();
            blend_mode = NoiseMapUtils.BLEND_MODES.NORMAL;
        }
    }

    public ShapeSettings()
    {
        noiseLayers = new NoiseLayer[1]; // initial noiselayer
        noiseLayers[0] = new NoiseLayer();
    }

    // assign the first noise layer's settings to the default settings of the first noise menu layer's settings
    public void InitializeNoiseLayers(NoiseSettings _noiseSettings)
    {
        noiseLayers = new NoiseLayer[1]; // initial noiselayer
        noiseLayers[0] = new NoiseLayer(_noiseSettings);
    }

    public void AddNoiseLayer(NoiseSettings _nuNoiseSettings)
    {
        int nuNoiseLayerCount = this.noiseLayers.Length + 1;
        NoiseLayer[] oldNoiseLayers = this.noiseLayers;
        this.noiseLayers = new NoiseLayer[nuNoiseLayerCount];

        for (int i = 0; i < nuNoiseLayerCount - 1; i++)
        {
            noiseLayers[i] = oldNoiseLayers[i];
        }

        this.noiseLayers[nuNoiseLayerCount - 1] = new NoiseLayer(_nuNoiseSettings);
    }

    public void UpdateNoiseLayer(int _layerID, NoiseSettings _NuNoiseSettings)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer out of range");
            return;
        }

        noiseLayers[_layerID].noiseSettings = _NuNoiseSettings;
    }

    public void UpdateNoiseLayerStatus(int _layerID, bool status)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer out of range");
            return;
        }

        noiseLayers[_layerID].enabled = status;
    }

    public void UpdateFilterMode(int _layerID, int _value)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer out of range");
            return;
        }

        if (noiseLayers[_layerID].noiseSettings == null)
        {
            Debug.Log("Error, noiseSettings uninitialized");
            return;
        }

        // defaults
        int octaves = 4;
        float frequency = 10;
        float persistence = 1.5f;
        float lacunarity = 2f;
        Vector2 offsets = new Vector2(0, 0);

        switch (noiseLayers[_layerID].noiseSettings.filterType)
        {
            case NoiseSettings.FilterType.BASE:
                octaves = noiseLayers[_layerID].noiseSettings.basicNoiseSettings.numOctaves;
                frequency = noiseLayers[_layerID].noiseSettings.basicNoiseSettings.frequency;
                persistence = noiseLayers[_layerID].noiseSettings.basicNoiseSettings.persistence;
                lacunarity = noiseLayers[_layerID].noiseSettings.basicNoiseSettings.lacunarity;
                offsets = noiseLayers[_layerID].noiseSettings.basicNoiseSettings.offset;
                break;
            case NoiseSettings.FilterType.BILLOWED:
                octaves = noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.numOctaves;
                frequency = noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.frequency;
                persistence = noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.persistence;
                lacunarity = noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.lacunarity;
                offsets = noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.offset;
                break;
            case NoiseSettings.FilterType.RIDGED:
                octaves = noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.numOctaves;
                frequency = noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.frequency;
                persistence = noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.persistence;
                lacunarity = noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.lacunarity;
                offsets = noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.offset;
                break;
            case NoiseSettings.FilterType.PERTURBED:
                octaves = noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.numOctaves;
                frequency = noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.frequency;
                persistence = noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.persistence;
                lacunarity = noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.lacunarity;
                offsets = noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.offset;
                break;
        }

        switch ((NoiseSettings.FilterType)_value)
        {
            case NoiseSettings.FilterType.BASE:
                 noiseLayers[_layerID].noiseSettings.basicNoiseSettings.numOctaves = octaves;
                 noiseLayers[_layerID].noiseSettings.basicNoiseSettings.frequency = frequency;
                 noiseLayers[_layerID].noiseSettings.basicNoiseSettings.persistence = persistence;
                 noiseLayers[_layerID].noiseSettings.basicNoiseSettings.lacunarity = lacunarity;
                 noiseLayers[_layerID].noiseSettings.basicNoiseSettings.offset = offsets;
                break;
            case NoiseSettings.FilterType.BILLOWED:
                noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.numOctaves = octaves;
                noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.frequency = frequency;
                noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.persistence = persistence;
                noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.lacunarity = lacunarity;
                noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.offset = offsets;
                break;
            case NoiseSettings.FilterType.RIDGED:
                noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.numOctaves = octaves;
                noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.frequency = frequency;
                noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.persistence = persistence;
                noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.lacunarity = lacunarity;
                noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.offset = offsets;
                break;
            case NoiseSettings.FilterType.PERTURBED:
                noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.numOctaves = octaves;
                noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.frequency = frequency;
                noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.persistence = persistence;
                noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.lacunarity = lacunarity;
                noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.offset = offsets;
                break;
        }

        noiseLayers[_layerID].noiseSettings.filterType = (NoiseSettings.FilterType)_value;
    }

    public void UpdateNoiseLayerOctaves(int _layerID, int _value)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer index out of range");
            return;
        }

        if (noiseLayers[_layerID].noiseSettings == null)
        {
            Debug.Log("Error, noiseSettings uninitialized");
            return;
        }

        switch (noiseLayers[_layerID].noiseSettings.filterType)
        {
            case NoiseSettings.FilterType.BASE:
                noiseLayers[_layerID].noiseSettings.basicNoiseSettings.numOctaves = _value;
                break;
            case NoiseSettings.FilterType.BILLOWED:
                noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.numOctaves = _value;
                break;
            case NoiseSettings.FilterType.RIDGED:
                noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.numOctaves = _value;
                break;
            case NoiseSettings.FilterType.PERTURBED:
                noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.numOctaves = _value;
                break;
        }
    }

    public void UpdateNoiseLayerFrequency(int _layerID, float _value)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer index out of range");
            return;
        }

        if (noiseLayers[_layerID].noiseSettings == null)
        {
            Debug.Log("Error, noiseSettings uninitialized");
            return;
        }

        switch (noiseLayers[_layerID].noiseSettings.filterType)
        {
            case NoiseSettings.FilterType.BASE:
                noiseLayers[_layerID].noiseSettings.basicNoiseSettings.frequency = _value;
                break;
            case NoiseSettings.FilterType.BILLOWED:
                noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.frequency = _value;
                break;
            case NoiseSettings.FilterType.RIDGED:
                noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.frequency = _value;
                break;
            case NoiseSettings.FilterType.PERTURBED:
                noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.frequency = _value;
                break;
        }
    }

    public void UpdateNoiseLayerPersistence(int _layerID, float _value)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer index out of range");
            return;
        }

        if (noiseLayers[_layerID].noiseSettings == null)
        {
            Debug.Log("Error, noiseSettings uninitialized");
            return;
        }

        switch (noiseLayers[_layerID].noiseSettings.filterType)
        {
            case NoiseSettings.FilterType.BASE:
                noiseLayers[_layerID].noiseSettings.basicNoiseSettings.persistence = _value;
                break;
            case NoiseSettings.FilterType.BILLOWED:
                noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.persistence = _value;
                break;
            case NoiseSettings.FilterType.RIDGED:
                noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.persistence = _value;
                break;
            case NoiseSettings.FilterType.PERTURBED:
                noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.persistence = _value;
                break;
        }
    }

    public void UpdateNoiseLayerLacunarity(int _layerID, float _value)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer index out of range");
            return;
        }

        if (noiseLayers[_layerID].noiseSettings == null)
        {
            Debug.Log("Error, noiseSettings uninitialized");
            return;
        }

        switch (noiseLayers[_layerID].noiseSettings.filterType)
        {
            case NoiseSettings.FilterType.BASE:
                noiseLayers[_layerID].noiseSettings.basicNoiseSettings.lacunarity = _value;
                break;
            case NoiseSettings.FilterType.BILLOWED:
                noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.lacunarity = _value;
                break;
            case NoiseSettings.FilterType.RIDGED:
                noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.lacunarity = _value;
                break;
            case NoiseSettings.FilterType.PERTURBED:
                noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.lacunarity = _value;
                break;
        }
    }

    public void UpdateNoiseLayerOffsets(int _layerID, Vector2 _offsets)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer index out of range");
            return;
        }

        if (noiseLayers[_layerID].noiseSettings == null)
        {
            Debug.Log("Error, noiseSettings uninitialized");
            return;
        }

        switch (noiseLayers[_layerID].noiseSettings.filterType)
        {
            case NoiseSettings.FilterType.BASE:
                noiseLayers[_layerID].noiseSettings.basicNoiseSettings.offset = _offsets;
                break;
            case NoiseSettings.FilterType.BILLOWED:
                noiseLayers[_layerID].noiseSettings.billowedNoiseSettings.offset = _offsets;
                break;
            case NoiseSettings.FilterType.RIDGED:
                noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.offset = _offsets;
                break;
            case NoiseSettings.FilterType.PERTURBED:
                noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.offset = _offsets;
                break;
        }
    }

    public void UpdateNoiseLayerSharpness(int _layerID, float _value)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer index out of range");
            return;
        }

        if (noiseLayers[_layerID].noiseSettings == null)
        {
            Debug.Log("Error, noiseSettings uninitialized");
            return;
        }

        noiseLayers[_layerID].noiseSettings.ridgedNoiseSettings.ridgeSharpness = _value;
    }

    public void UpdateNoiseLayerPerturbation(int _layerID, float _value)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer index out of range");
            return;
        }

        if (noiseLayers[_layerID].noiseSettings == null)
        {
            Debug.Log("Error, noiseSettings uninitialized");
            return;
        }

        noiseLayers[_layerID].noiseSettings.perturbedNoiseSettings.perturbationFactor = _value;
    }

    public void UpdateNoiseLayerSeed(int _layerID, int _value)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer index out of range");
            return;
        }

        if (noiseLayers[_layerID].noiseSettings == null)
        {
            Debug.Log("Error, noiseSettings uninitialized");
            return;
        }

        if (noiseLayers[_layerID].noiseSettings.noiseSeedSettings == null)
        {
            Debug.Log("Error, noiseSeedSettings uninitialized");
            return;
        }

        noiseLayers[_layerID].noiseSettings.noiseSeedSettings.Seed = _value;
    }

    public void UpdateNoiseLayerBlendMode(int _layerID, int _blendMode)
    {
        if (noiseLayers == null)
        {
            Debug.Log("Error, noiseLayers uninitialized");
            return;
        }

        if (_layerID >= noiseLayers.Length)
        {
            Debug.Log("Error, noiselayer index out of range");
            return;
        }

        noiseLayers[_layerID].blend_mode = (NoiseMapUtils.BLEND_MODES)_blendMode;

        // actually update the material
        noiseLayers[_layerID].blendMaterial = WorldMapUtils.LoadBlendMaterial(noiseLayers[_layerID].blend_mode);        
    }
}
