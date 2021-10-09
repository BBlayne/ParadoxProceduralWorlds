using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct SimplexNoiseSettings
{
    public int m_seed;
    public float m_frequency;

    public float m_lacunarity;
    public Vector2 m_offset;
    public float m_waterLevel;
    public float m_gain;
}

public class SimplexGPUNoise2D : MonoBehaviour
{
    public SimplexNoiseSettings m_noiseSettings;

    public Vector2 m_scale = new Vector2(1, 1);


    public RawImage m_outputImage;

    RenderTexture noiseTex = null;

    public int width = 512;
    public int height = 512;

    public int octaves = 4;

    // Start is called before the first frame update
    void Start()
    {
        UpdateAspectAndScale();
        // camera
        UpdateCamera();
    }

    void UpdateAspectAndScale()
    {
        float aspect = (float)width / height;
        m_scale.x = 1;
        m_scale.y = 1;

        if (aspect > 1)
        {
            m_scale.x = aspect;
        }
        else
        {
            m_scale.y = (float)height / width;
        }
    } 

    void UpdateCamera()
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
    void MakeTexture()
    {
        if (noiseTex)
        {
            Destroy(noiseTex);
        }

        NoiseS3D.octaves = octaves;

        noiseTex = NoiseS3D.GetNoiseRenderTexture(width, height, m_noiseSettings);

        if (noiseTex)
        {
            noiseTex.filterMode = FilterMode.Point;

            m_outputImage.texture = noiseTex;

            m_outputImage.rectTransform.sizeDelta = new Vector2(width, height);
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAspectAndScale();
        // camera
        UpdateCamera();

        MakeTexture();
    }


}
