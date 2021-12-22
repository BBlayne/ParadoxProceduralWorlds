using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataStructures.ViliWonka.Heap;

/*
 * Scaled SubRegion Detection:
 * 
 * Using a scaled down (1/16th scale) map and using a modified flood detection to divide the region
 * into N approximately equal sized subregions each with their own color.
 * 
 * The next step is then to form a contour of the points forming a convex hull of the subregion to
 * generate a polygon representing the subregion for the original source map to be coloured in the GPU.
 * 
 * This should avoid the issue of exclaves/enclaves as its formed based on the resulting flood-fill 
 * instead of applying a polygon onto an arbitrary and random section of the underlying landmass.
 */

public class SubRegion
{
    public float influence = 25;
    public List<Vector3> ownedCoords = new List<Vector3>();
    //public HashSet<Vector3> ownedCoords = new HashSet<Vector3>();
    //public Queue<Vector3> coordQueue = new Queue<Vector3>();
    public MinHeap<Vector3> coordPQ = new MinHeap<Vector3>();

    public Color colour;

    public float radius;
}

public class SubregionGenerator
{
    private float GetDistance(Vector3 coordA, Vector3 coordB)
    {
        int dstX = Mathf.Abs((int)(coordA.x - coordB.x));
        int dstY = Mathf.Abs((int)(coordA.y - coordB.y));

        if (dstX > dstY)
        {
            return 14 * dstY + 10 * (dstX - dstY);
        }

        return 14 * dstX + 10 * (dstY - dstX);
    }

    public Texture2D GetSubRegionsFromRegion
    (
        Texture2D inScaledRegionMap, 
        Texture2D inScaledHeightMap, 
        List<Vector3> inSubRegionSites
    )
    {        
        
        // colour range goes from 0 to 255; our height map ranges from 140 to 250ish.
        Unity.Collections.NativeArray<Color32> pixels = inScaledRegionMap.GetRawTextureData<Color32>();

        Texture2D outputTex = new Texture2D(inScaledRegionMap.width, inScaledRegionMap.height, TextureFormat.RGBA32, false);

        int scaledSize = inScaledRegionMap.width;

        Random.InitState((int)System.DateTime.Now.Ticks);

        Unity.Collections.NativeArray<Color32> outputPixels = outputTex.GetRawTextureData<Color32>();

        List<SubRegion> completedSubRegions = new List<SubRegion>();
        List<SubRegion> ExpectedSubRegions = new List<SubRegion>();
        HashSet<Vector3> SampledPixels = new HashSet<Vector3>();

        for (int i = 0; i < inSubRegionSites.Count; ++i)
        {
            SubRegion subRegion = new SubRegion();
            subRegion.colour = Random.ColorHSV(0, 1, 0, 1, 0.1f, 1, 1, 1);
            subRegion.coordPQ.PushObj(inSubRegionSites[i], 0);
            subRegion.radius = 55;
            ExpectedSubRegions.Add(subRegion);
        }

        while (completedSubRegions.Count < inSubRegionSites.Count)
        {
            for (int i = 0; i < ExpectedSubRegions.Count; ++i)
            {
                // each sites gets a turn to pick a pixel from its P.Queue
                // as long as queue is non-empty (thus points to examine) and available influence is
                // more than the cost of the cheapest coordinate in the queue, proceed.
                while (ExpectedSubRegions[i].coordPQ.Count > 0 && 
                       ExpectedSubRegions[i].coordPQ.HeadValue <= ExpectedSubRegions[i].influence)
                {
                    float currentCost = ExpectedSubRegions[i].coordPQ.HeadValue;
                    Vector3 current = ExpectedSubRegions[i].coordPQ.PopObj();

                    // check if this coordinate was already sampled, is not "water"
                    if (!SampledPixels.Contains(current) && 
                        pixels[(int)current.y * scaledSize + (int)current.x].a != 0)
                    {
                        // subtract our cost if its a valid coordinate to explore
                        ExpectedSubRegions[i].influence -= currentCost;
                        // add coordinate to hashset of already checked coordinates
                        SampledPixels.Add(current);
                        // add coordinate to list of owned coordinates
                        ExpectedSubRegions[i].ownedCoords.Add(current);
                        outputPixels[(int)current.y * scaledSize + (int)current.x] = ExpectedSubRegions[i].colour;
                        //
                        Vector3 left = new Vector3(current.x - 1, current.y, 0);
                        Vector3 right = new Vector3(current.x + 1, current.y, 0);
                        Vector3 up = new Vector3(current.x, current.y + 1, 0);
                        Vector3 down = new Vector3(current.x, current.y - 1, 0);

                        // might have strange results since they don't update when radius changes
                        float leftCost = GetDistance(ExpectedSubRegions[i].ownedCoords[0], left);
                        if (leftCost > ExpectedSubRegions[i].radius)
                        {
                            leftCost *= 4;
                        }

                        float rightCost = GetDistance(ExpectedSubRegions[i].ownedCoords[0], right);
                        if (rightCost > ExpectedSubRegions[i].radius)
                        {
                            rightCost *= 4;
                        }

                        float upCost = GetDistance(ExpectedSubRegions[i].ownedCoords[0], up);
                        if (upCost > ExpectedSubRegions[i].radius)
                        {
                            upCost *= 4;
                        }

                        float downCost = GetDistance(ExpectedSubRegions[i].ownedCoords[0], down);
                        if (downCost > ExpectedSubRegions[i].radius)
                        {
                            downCost *= 4;
                        }

                        ExpectedSubRegions[i].coordPQ.PushObj(left, leftCost);
                        ExpectedSubRegions[i].coordPQ.PushObj(right, rightCost);
                        ExpectedSubRegions[i].coordPQ.PushObj(up, upCost);
                        ExpectedSubRegions[i].coordPQ.PushObj(down, downCost);
                    }
                }

                // increase amount of available influence each turn
                if (ExpectedSubRegions[i].coordPQ.Count > 0)
                {
                    if (ExpectedSubRegions[i].influence < ExpectedSubRegions[i].coordPQ.HeadValue)
                    {
                        ExpectedSubRegions[i].influence += 25;
                    }                    
                }
                else
                {
                    completedSubRegions.Add(ExpectedSubRegions[i]);
                    ExpectedSubRegions.Remove(ExpectedSubRegions[i]);
                }                
            }
        }

        outputTex.Apply();

        return outputTex;
    }
}
