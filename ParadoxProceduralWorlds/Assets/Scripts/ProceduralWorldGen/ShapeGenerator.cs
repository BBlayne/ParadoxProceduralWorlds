using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;

/*
 * This class is for taking a Height Map and generating
 * "shape" data roughly corresponding to geographic features.
 * 
 * Desired:
 * - A list of all landmasses and their size in pixels.
 * - A list of all water masses such as lakes, oceans and their size.
 * - The pixels that make up the "border" of each mass.
 * 
 * Need to use Flood fill algorithm to scan the whole image;
 * I can probably do this okay-ish on the CPU, it'll lag but 
 * doesn't need to be real time; but putting on the GPU would 
 * be faster. 
 * 
 * GPU Solution is best probably for colouring the provinces
 * later based off of relaxed voronoi partitioning; but for
 * my Object Oriented purposes I need CPU based solution, 
 * perhaps I can improve performance with multithreading.
 */

public enum ELandType
{
    Lake,
    InlandSea,
    Ocean,
    Island,
    Continent
}

public enum EProvinceTerrainType
{
    Farmland,
    Plains,
    Desert,
    Tundra,
    Jungle,
    Hill,
    Mountain
}

// A regions type is either Land or Water (Sea/Ocean).
public enum ERegionType
{
    Land,
    Water
}

public class WorldShape
{
    public List<Region> Regions;
    public int[] Labels;
}

public struct Coord
{
    public int x;
    public int y;

    public Coord(int InX, int InY)
    {
        x = InX;
        y = InY;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)2166136261;
            hash = (hash * 16777619) ^ x.GetHashCode();
            hash = (hash * 16777619) ^ y.GetHashCode();
            return hash;
        }
    }

    //public static Coord operator+ (Coord a, Coord b)
    //{
    //    Coord coord = new Coord(a.x + b.x, a.y + b.y);
    //    return coord;
    //}

    //public static Coord operator -(Coord a, Coord b)
    //{
    //    Coord coord = new Coord(a.x - b.x, a.y - b.y);
    //    return coord;
    //}
}

public struct Region
{
    public int RegionID;
    public int Label;
    public ERegionType RegionType;
    public Color RegionColour;
    public List<Coord> coords;
}

public class ShapeGenerator
{
    public int currentLabel = 0;

    private int guessMaxRegions = 20000;

    private int totalLandPixels = 0;
    private int totalWaterPixels = 0;

    //HashSet<int> RegionIds = new HashSet<int>();

    public LabelNode[] labelNodes; // initialize when needed, to idk, 20,000?

    public int[] LabelledWorldGrid;

    bool[] isRegion;

    public struct LabelNode
    {
        public int parentIndex;
        public int rank;
    }

    // 
    public int Find(int InNodeX)
    {
        int root = InNodeX;
        while (labelNodes[root].parentIndex != root)
        {
            root = labelNodes[root].parentIndex;
        }

        while (labelNodes[InNodeX].parentIndex != root)
        {
            int parent = labelNodes[InNodeX].parentIndex;
            labelNodes[InNodeX].parentIndex = root;
            InNodeX = parent;
        }

        return root;
    }

    private void Union(int InNodeX, int InNodeY)
    {
        InNodeX = Find(InNodeX);
        InNodeY = Find(InNodeY);

        if (InNodeX == InNodeY) return;

        if (labelNodes[InNodeX].rank < labelNodes[InNodeY].rank)
        {
            int tmp = InNodeX;
            InNodeX = InNodeY;
            InNodeY = tmp;
        }

        labelNodes[InNodeY].parentIndex = InNodeX;

        if (labelNodes[InNodeX].rank == labelNodes[InNodeY].rank)
        {
            labelNodes[InNodeX].rank = labelNodes[InNodeX].rank + 1;
        }
    }

    public int[] DetectAllRegions(Texture2D InMap)
    {        
        NativeArray<Color32> map = InMap.GetRawTextureData<Color32>();

        int maxWidth = InMap.width;
        int maxHeight = InMap.height;

        int WxH = maxWidth * maxHeight;
        int[] labelMap = new int[WxH];
        int[] newLabelMap = new int[WxH];

        LabelledWorldGrid = newLabelMap;

        labelNodes = new LabelNode[guessMaxRegions];
        isRegion = new bool[guessMaxRegions];

        /* Performance Metrics (Init & Start) */
        var timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        /* ********************************** */

        labelMap[0] = currentLabel;

        labelNodes[currentLabel].parentIndex = currentLabel;

        // prefill the bottom row and leftmost column as currently it is 
        // assumed the edges are all ocean and they are all trivially connected.
        // this might not always be true but thats for future me to solve.
        // we add these coordinates to the current label and current region
        // bottom row
        for (int x = 1; x < maxWidth; ++x)
        {
            labelMap[x] = currentLabel;
        }
                
        // leftmost column
        for (int y = 1; y < maxHeight; ++y)
        {
            labelMap[y * maxWidth] = currentLabel;
        }

        for (int y = 1; y < maxHeight; ++y)
        {
            int SameRow = y * maxWidth;
            int BelowRow = (y - 1) * maxWidth;
            for (int x = 1; x < maxWidth; ++x)
            {
                // check neighbours
                // check if West tile is the same colour as the current pixel
                if (map[SameRow + x - 1].r == map[SameRow + x].r)
                {
                    labelMap[SameRow + x] = labelMap[SameRow + x - 1];

                    if (map[BelowRow + x].r == map[SameRow + x].r &&
                        labelMap[BelowRow + x] != labelMap[SameRow + x - 1])
                    {
                        Union(labelMap[SameRow + x], labelMap[BelowRow + x]);
                    }
                }
                // check if South tile is the same colour as the current pixel
                else if (map[BelowRow + x].r == map[SameRow + x].r)
                {
                    labelMap[SameRow + x] = labelMap[BelowRow + x];
                }
                else
                {
                    // increment current label id
                    ++currentLabel;

                    // create new label with new id
                    labelNodes[currentLabel].parentIndex = currentLabel;

                    // set the label to the label map
                    labelMap[SameRow + x] = currentLabel;
                }
            }
        }

        /* Performance Metrics (Stop & Print) */
        timer.Stop();
        System.TimeSpan duration = timer.Elapsed;
        string timeElapsedMsg = "Time taken to get all pixels labeled: " + duration.ToString(@"m\:ss\.fff");
        Debug.Log(timeElapsedMsg);
        /* ********************************** */

        for (int i = 0; i < WxH; ++i)
        {
            newLabelMap[i] = Find(labelMap[i]);
            isRegion[newLabelMap[i]] = true;
        }

        return newLabelMap;
    }

    public List<Region> GetRegions(Texture2D InHeightMap)
    {
        NativeArray<Color32> map = InHeightMap.GetRawTextureData<Color32>();

        int width = InHeightMap.width;
        int height = InHeightMap.height;

        int size = width * height;        

        float widthInverse = (float)1 / width;

        List<Region> Regions = new List<Region>();

        int[] LabelledMap = DetectAllRegions(InHeightMap);

        int max_regions = currentLabel + 1; // is set after DetectAllRegions

        // optimizations?
        Coord[] coords = new Coord[size];
        Region[] reserveRegions = new Region[max_regions];

        List<Coord>[] coordLists = new List<Coord>[max_regions];
        List<int> regionIds = new List<int>();

        /* Performance Metrics (Init & Start) */
        var timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        /* ********************************** */

        for (int i = 0; i < max_regions; ++i)
        {
            if (isRegion[i])
            {
                // this is wong vvv
                coordLists[i] = new List<Coord>();
                regionIds.Add(i);
            }
        }

        int regionCount = 0;
        for (int i = 0; i < size; ++i)
        {
            coords[i].x = i % width;
            coords[i].y = (int)(i * widthInverse);

            coordLists[LabelledMap[i]].Add(coords[i]);   
        }

        for (int i = 0; i < regionIds.Count; ++i)
        {
            reserveRegions[regionCount].RegionID = regionCount; // shifting IDs to 0 indexed
            reserveRegions[regionCount].RegionColour = Random.ColorHSV(0, 1, 0, 1, 0.1f, 1, 1, 1);
            reserveRegions[regionCount].coords = coordLists[regionIds[i]];
            Regions.Add(reserveRegions[regionCount]);
            ++regionCount;
        }

        for (int i = 0; i < Regions.Count; ++i)
        {
            Coord samplePixel = Regions[i].coords[0];
            Region currentRegion = Regions[i];
            if (map[samplePixel.y * width + samplePixel.x].r != 0)
            {
                totalLandPixels += Regions[i].coords.Count;                
                currentRegion.RegionType = ERegionType.Land;
            }
            else
            {
                totalWaterPixels += Regions[i].coords.Count;
                currentRegion.RegionType = ERegionType.Water;
            }

            currentRegion.Label = LabelledMap[samplePixel.y * width + samplePixel.x];
            Regions[i] = currentRegion;
        }

        /* Performance Metrics (Stop & Print) */
        timer.Stop();
        System.TimeSpan duration = timer.Elapsed;
        string timeElapsedMsg = "Time taken to get all region coords: " + duration.ToString(@"m\:ss\.fff");
        Debug.Log(timeElapsedMsg);
        /* ********************************** */

        Debug.Log("Land tile count: " + totalLandPixels);
        Debug.Log("Water tile count: " + totalWaterPixels);

        float landPercentage = (float)totalLandPixels / (totalLandPixels + totalWaterPixels);
        Debug.Log(
            "Land makes up " + 
            100 * ((float)totalLandPixels / (totalLandPixels + totalWaterPixels)) + 
            "% of the world's surface."
        );

        Debug.Log(
            "Water makes up " +
            100 * (1 - landPercentage) +
            "% of the world's surface."
        );

        return Regions;
    }
}
