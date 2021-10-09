using UnityEngine;
using System.Collections;
using System.IO;

namespace ImprovedPerlinNoiseProject
{
    public class IPN_GPU_2Dv2 : MonoBehaviour
    {

        public NOISE_STLYE m_style = NOISE_STLYE.FBM;

        public int m_seed = 0;

        public float m_frequency = 1.0f;

        public float m_lacunarity = 2.0f;

        public int m_octaves = 4;

        public float m_gain = 0.5f;
        public Vector2 m_scale = new Vector2(1, 1);
        public Vector2 m_offset = new Vector2(0, 0);

        private Renderer m_renderer;

        private GPUPerlinNoise m_perlin;

        public int width = 1408;
        public int height = 512;

        void Start()
        {
            m_perlin = new GPUPerlinNoise(m_seed);

            m_perlin.LoadResourcesFor2DNoise();

            m_renderer = GetComponent<Renderer>();

            m_renderer.material.SetTexture("_PermTable1D", m_perlin.PermutationTable1D);
            m_renderer.material.SetTexture("_Gradient2D", m_perlin.Gradient2D);

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

        public void SaveTextureFromShader()
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(null, renderTexture, m_renderer.material);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(Vector2.zero, new Vector2(width, height)), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/Temp/FromShader.png", bytes);
            Debug.Log
            (
                "Writing " + width + " by " + height + " image to disk at " + 
                Application.dataPath + "/Temp/FromShader.png"
            );
        }

        void Update()
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

            m_renderer.material.SetFloat("_Frequency", m_frequency);
            m_renderer.material.SetFloat("_Lacunarity", m_lacunarity);
            m_renderer.material.SetFloat("_Gain", m_gain);
            m_renderer.material.SetFloat("_NoiseStyle", (float)m_style);
            m_renderer.material.SetVector("_Scale", m_scale);
            m_renderer.material.SetVector("_Offset", m_offset);
            m_renderer.material.SetInteger("_Octaves", m_octaves);

            m_renderer.transform.localScale = new Vector3((float)width / 10, 1, (float)height / 10);
            if ((float)Screen.width / Screen.height < (float)width / height)
            {
                Camera.main.orthographicSize = (float)width / 2 * (float)Screen.height / Screen.width;
            }
            else
            {
                Camera.main.orthographicSize = (float)height / 2;
            }

            Camera.main.orthographicSize = Camera.main.orthographicSize;

            // Save();
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SaveTextureFromShader();
            }
        }

    }

}
