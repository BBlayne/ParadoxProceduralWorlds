using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapDisplay : MonoBehaviour
{
    [SerializeField]
    private RawImage _mapDisplayImgTarget = null;

	public Texture2D CurrentDisplayTex = null;

	public Material MapUIMaterial = null;

	public void UpdateMapDisplayTexture(Texture2D tex)
	{
		CurrentDisplayTex = tex;
		if (MapUIMaterial != null) 
		{
			MapUIMaterial.SetTexture("_MainTex", tex);
		}
	}

    public void UpdateMapDisplayRatio(int InWidth, int InHeight)
    {
        if (_mapDisplayImgTarget != null)
        {
            RectTransform RctTform = _mapDisplayImgTarget.gameObject.GetComponent<RectTransform>();
            if (RctTform != null)
            {
                RctTform.sizeDelta = new Vector2(InWidth, InHeight);
            }

			BoxCollider2D box = _mapDisplayImgTarget.gameObject.GetComponent<BoxCollider2D>();
			if (box != null) 
			{
				box.size = new Vector2(InWidth, InHeight);
			}

			//Material mat = _mapDisplayImgTarget.gameObject.GetComponent<RawImage>().defaultMaterial;

        }
    }

    public RawImage MapDisplayImgTarget {
        get {
            return _mapDisplayImgTarget;
        }

        set {
            _mapDisplayImgTarget = value;
        }
    }
}
