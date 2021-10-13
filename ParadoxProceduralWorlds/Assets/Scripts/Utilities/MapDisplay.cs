using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapDisplay : MonoBehaviour
{
    [SerializeField]
    private RawImage _mapDisplayImgTarget = null;

    public RawImage MapDisplayImgTarget {
        get {
            return _mapDisplayImgTarget;
        }

        set {
            _mapDisplayImgTarget = value;
        }
    }
}
