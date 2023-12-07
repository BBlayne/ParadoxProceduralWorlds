using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class VoronoiSettings : ScriptableObject
{
    public int SmoothingIterations = 10;
    public int Seed = 0;
    public bool useRandomSeed = false;
    public bool Relax = true;

}
