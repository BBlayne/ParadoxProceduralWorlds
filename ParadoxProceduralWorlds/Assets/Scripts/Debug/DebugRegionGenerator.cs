using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugRegionGenerator : MonoBehaviour
{
    RegionGenerator.RegionDebugInfo regionDebugInfo = null;

    private void OnEnable()
    {
        WorldGenerator.regionsGeneratedDelegate += RegionGenerated;
    }

    private void OnDisable()
    {
        WorldGenerator.regionsGeneratedDelegate -= RegionGenerated;
    }

    private void RegionGenerated(RegionGenerator.RegionDebugInfo inRegionDebugInfo)
    {
        regionDebugInfo = inRegionDebugInfo;
    }

    private void OnDrawGizmos()
    {
        if (regionDebugInfo != null && regionDebugInfo.regionVoronoiGraph != null)
        {
            TriangleNet.Voronoi.StandardVoronoi voronoiGraph = regionDebugInfo.regionVoronoiGraph;
            foreach (var face in voronoiGraph.Faces)
            {

            }
        }
    }
}
