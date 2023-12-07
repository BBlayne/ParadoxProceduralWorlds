using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldMinMax
{
    private readonly object _padlock = new object();

    public float Min { get; private set; }
    public float Max { get; private set; }

    public WorldMinMax()
    {
        Min = float.MaxValue;
        Max = float.MinValue;
    }

    public void AddValue(float v)
    {
        lock(_padlock)
        {
            if (v > Max)
            {
                Max = v;
            }

            if (v < Min)
            {
                Min = v;
            }
        }
    }
}
