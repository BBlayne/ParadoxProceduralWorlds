using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

/*
 * This class is for taking a Height Map and generating
 * "shape" data roughly corresponding to geographic features.
 * 
 * Desired:
 * - A list of all landmasses and their size in pixels.[done]
 * - A list of all water masses such as lakes, oceans and their size.[done]
 * - The pixels that make up the "border" of each mass.[?]
 * 
 * A form of scanline floodfill algorithm is used, known as connected
 * component labeling. Scans the whole image and breaks down the texture
 * data into components based on a pre-determined sense of connectivity.
 * 
 */
namespace ProceduralWorlds
{
    public class ShapeGenerator
    {
        public int currentLabel = 0;

        private int guessMaxRegions = 20000;

        private int totalLandPixels = 0;
        private int totalWaterPixels = 0;

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
                reserveRegions[regionCount].Coords = coordLists[regionIds[i]];
                Regions.Add(reserveRegions[regionCount]);
                ++regionCount;
            }

            for (int i = 0; i < Regions.Count; ++i)
            {
                Coord samplePixel = Regions[i].Coords[0];
                Region currentRegion = Regions[i];
                if (map[samplePixel.y * width + samplePixel.x].r != 0)
                {
                    totalLandPixels += Regions[i].Coords.Count;                
                    currentRegion.RegionType = ERegionType.Land;
                }
                else
                {
                    totalWaterPixels += Regions[i].Coords.Count;
                    currentRegion.RegionType = ERegionType.Water;
                }

                currentRegion.Label = LabelledMap[samplePixel.y * width + samplePixel.x];

                // getting region bounds, probably should be done earlier
                //int regionSize = currentRegion.Coords.Count;
                //currentRegion.MaxX = int.MinValue;
                //currentRegion.MaxY = int.MinValue;
                //currentRegion.MinX = int.MaxValue;
                //currentRegion.MinY = int.MaxValue;

                //for (int j = 0; j < regionSize; ++j)
                //{
                //    Coord currentCoord = currentRegion.Coords[j];
                //    if (currentCoord.x > currentRegion.MaxX)
                //    {
                //        currentRegion.MaxX = currentCoord.x;
                //    }

                //    if (currentCoord.y > currentRegion.MaxY)
                //    {
                //        currentRegion.MaxY = currentCoord.y;
                //    }

                //    if (currentCoord.x < currentRegion.MinX)
                //    {
                //        currentRegion.MinX = currentCoord.x;
                //    }

                //    if (currentCoord.y < currentRegion.MinY)
                //    {
                //        currentRegion.MinY = currentCoord.y;
                //    }
                //}

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
}
