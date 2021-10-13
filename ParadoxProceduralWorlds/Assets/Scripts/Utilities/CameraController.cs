using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public int OrthographicSizeModifier = 10;

    private int Width = 512;
    private int Height = 512;    

    private Vector2 m_scale = new Vector2(1, 1);

    private static CameraController _instance;

    public CameraController Instance 
    {
        get {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<CameraController>();

                if (_instance == null)
                {
                    GameObject _cameraController = new GameObject("CameraController");
                    _cameraController.transform.position = new Vector3();
                    _cameraController.transform.rotation = Quaternion.identity;
                    _instance = _cameraController.AddComponent<CameraController>();
                }
            }

            return _instance;
        }
    }


    public void SetResolution(int InWidth, int InHeight)
    {
        Width = InWidth;
        Height = InHeight;
        UpdateAspectAndScale();
        UpdateCamera();
    }

    private void UpdateAspectAndScale()
    {
        float aspect = (float)Width / Height;
        m_scale.x = 1;
        m_scale.y = 1;

        if (aspect > 1)
        {
            m_scale.x = aspect;
        }
        else
        {
            m_scale.y = (float)Height / Width;
        }
    }

    private void UpdateCamera()
    {
        if ((float)Screen.width / Screen.height < (float)Width / Height)
        {
            Camera.main.orthographicSize = (float)Width / 2 * (float)Screen.height / Screen.width;
        }
        else
        {
            Camera.main.orthographicSize = (float)Height / 2;
        }
        Camera.main.orthographicSize = Camera.main.orthographicSize * OrthographicSizeModifier;
    }
}
