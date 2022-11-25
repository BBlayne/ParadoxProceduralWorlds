using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapDisplay : MonoBehaviour
{
    [SerializeField]
    private RawImage _mapDisplayImgTarget = null;

    public void UpdateMapDisplayRatio(int InWidth, int InHeight)
    {
        if (_mapDisplayImgTarget != null)
        {
            RectTransform RctTform = _mapDisplayImgTarget.gameObject.GetComponent<RectTransform>();
            if (RctTform != null)
            {
                RctTform.sizeDelta = new Vector2(InWidth, InHeight);
            }            
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
