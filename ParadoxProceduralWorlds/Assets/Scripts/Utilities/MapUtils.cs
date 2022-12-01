using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TriangleNet.Voronoi;
using TMesh = TriangleNet.Mesh;
using THalfEdge = TriangleNet.Topology.DCEL.HalfEdge;
using TFace = TriangleNet.Topology.DCEL.Face;
using System;

public class Priority : IHeapItem<Priority>
{
    public int Value;
    public int Rank;
    int MyHeapIndex;

    public Priority(int InValue, int InRank) 
    { 
        Value = InValue;
        Rank = InRank;
    }
    public int HeapIndex
    {
        get
        {
            return MyHeapIndex;
        }
        set
        {
            MyHeapIndex = value;
        }
    }

    public int CompareTo(Priority InOtherPriority)
    {
        return -Rank.CompareTo(InOtherPriority.Rank);
    }
}

public static class IListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

public class Heap<T> where T : IHeapItem<T>
{

    T[] items;
    int currentItemCount;

    public Heap(int maxHeapSize)
    {
        items = new T[maxHeapSize];
    }

    public void Add(T item)
    {
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }

    public T RemoveFirst()
    {
        T firstItem = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return firstItem;
    }

    public void UpdateItem(T item)
    {
        SortUp(item);
    }

    public int Count
    {
        get
        {
            return currentItemCount;
        }
    }

    public bool Contains(T item)
    {
        return Equals(items[item.HeapIndex], item);
    }

    void SortDown(T item)
    {
        while (true)
        {
            int childIndexLeft = item.HeapIndex * 2 + 1;
            int childIndexRight = item.HeapIndex * 2 + 2;
            int swapIndex = 0;

            if (childIndexLeft < currentItemCount)
            {
                swapIndex = childIndexLeft;

                if (childIndexRight < currentItemCount)
                {
                    if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
                    {
                        swapIndex = childIndexRight;
                    }
                }

                if (item.CompareTo(items[swapIndex]) < 0)
                {
                    Swap(item, items[swapIndex]);
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }

    void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;

        while (true)
        {
            T parentItem = items[parentIndex];
            if (item.CompareTo(parentItem) > 0)
            {
                Swap(item, parentItem);
            }
            else
            {
                break;
            }

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    void Swap(T itemA, T itemB)
    {
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;
        int itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }
}

public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex
    {
        get;
        set;
    }
}

public static class MapUtils
{
    public static List<Vector3> GenerateSiteDistribution
    (
        ProceduralWorlds.ESiteDistribution InSiteDistribution,
        int InTargetNumberSites,
        Vector2Int InMapDims,
        int InPadding,
        int InRadius,
        List<Vector3> InSupplementSites
    )
    {
        switch (InSiteDistribution)
        {
        case ProceduralWorlds.ESiteDistribution.POISSON:
            return MapUtils.GetPoissonDistributedPoints2D(
                InMapDims,
                InRadius,
                30,
                InPadding,
                InSupplementSites,
                InTargetNumberSites
            );
        case ProceduralWorlds.ESiteDistribution.RANDOM:
        default:
            return MapUtils.GenerateRandomPoints2D(InTargetNumberSites, InMapDims, InPadding);
        }
    }

    public static List<Vector3> GenerateSiteDistribution
    (
        ProceduralWorlds.ESiteDistribution InSiteDistribution,
        int InTargetNumberSites,
        Vector2Int InMapDims,
        int InPadding,
        List<Vector3> InSupplementSites
    )
    {
        switch (InSiteDistribution)
        {
        case ProceduralWorlds.ESiteDistribution.POISSON:
            return MapUtils.GetPoissonDistributedPoints2D(
                InMapDims,
                MapUtils.DetermineRadiusForPoissonDisc(InMapDims, InTargetNumberSites),
                30,
                InPadding,
                InSupplementSites,
                InTargetNumberSites
            );
        case ProceduralWorlds.ESiteDistribution.RANDOM:
        default:
            return MapUtils.GenerateRandomPoints2D(InTargetNumberSites, InMapDims, InPadding);
        }
    }

    public static int DetermineRadiusForPoissonDisc(Vector2Int InMapSizes, int InSites)
    {
        int Area = InMapSizes.x * InMapSizes.y;
        float N = (float)Area / InSites;
        float MN = Mathf.Round(Mathf.Sqrt(N));
        return Mathf.RoundToInt(MN / 2) + Mathf.RoundToInt(MN / 4);
    }

    private static bool IsValid
    (
        Vector3 InCandidate, Vector2 InMapSizes, float InCellSize, float InRadius, List<Vector3> InPoints, int[,] InGrid, int InPadding
    )
    {
        if (InCandidate.x >= InPadding && InCandidate.x < (InMapSizes.x - InPadding) && InCandidate.y >= InPadding && InCandidate.y < (InMapSizes.y - InPadding))
        {
            int CellX = (int)(InCandidate.x / InCellSize);
            int CellY = (int)(InCandidate.y / InCellSize);
            int SearchStartX = Mathf.Max(0, CellX - 2);
            int SearchEndX = Mathf.Min(CellX + 2, InGrid.GetLength(0) - 1);
            int SearchStartY = Mathf.Max(0, CellY - 2);
            int SearchEndY = Mathf.Min(CellY + 2, InGrid.GetLength(1) - 1);

            for (int x = SearchStartX; x <= SearchEndX; x++)
            {
                for (int y = SearchStartY; y <= SearchEndY; y++)
                {
                    int PointIndex = InGrid[x, y] - 1;
                    if (PointIndex != -1)
                    {
                        float SqrDist = (InCandidate - InPoints[PointIndex]).sqrMagnitude;
                        if (SqrDist < InRadius * InRadius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }

    public static List<Vector3> GetPoissonDistributedPoints2D
    (
        Vector2Int InMapSizes,
        float InRadius,
        int numMaxSamples,
        int InPadding,
        Vector3 InInitialPoint,
        List<Vector3> InPoints,
        int InMaxPoints = int.MaxValue
    )
    {
        float CellSize = InRadius / Mathf.Sqrt(2);

        int[,] Grid = new int[Mathf.CeilToInt(InMapSizes.x / CellSize), Mathf.CeilToInt(InMapSizes.y / CellSize)];
        List<Vector3> OutPoints = new List<Vector3>();
        List<Vector3> SpawnPoints = new List<Vector3>();

        SpawnPoints.Add(InInitialPoint);

        for (int i = 0; i < InPoints.Count; i++)
        {
            OutPoints.Add(InPoints[i]);
        }

        while (SpawnPoints.Count > 0 && OutPoints.Count < InMaxPoints)
        {
            int SpawnIndex = UnityEngine.Random.Range(0, SpawnPoints.Count);
            Vector3 SpawnCenter = SpawnPoints[SpawnIndex];
            bool bCandidateAccepted = false;

            for (int i = 0; i < numMaxSamples; i++)
            {
                float Angle = UnityEngine.Random.value * Mathf.PI * 2;
                Vector3 Direction = new Vector3(Mathf.Sin(Angle), Mathf.Cos(Angle), 0);
                Vector3 Candidate = SpawnCenter + Direction * UnityEngine.Random.Range(InRadius, 2 * InRadius);
                if (IsValid(Candidate, InMapSizes, CellSize, InRadius, OutPoints, Grid, InPadding))
                {
                    OutPoints.Add(Candidate);
                    SpawnPoints.Add(Candidate);
                    Grid[(int)(Candidate.x / CellSize), (int)(Candidate.y / CellSize)] = OutPoints.Count;
                    bCandidateAccepted = true;
                    break;
                }
            }

            if (!bCandidateAccepted)
            {
                SpawnPoints.RemoveAt(SpawnIndex);
            }
        }

        return OutPoints;
    }

    public static List<Vector3> GenerateRandomPoints2D(int InNumPoints, Vector2Int InMapSizes, int InPadding)
    {
        List<Vector3> Points = new List<Vector3>();

        for (int i = 0; i < InNumPoints; i++)
        {
            Points.Add
            (
                new Vector3
                (
                    UnityEngine.Random.Range(InPadding, InMapSizes.x - InPadding),
                    UnityEngine.Random.Range(InPadding, InMapSizes.y - InPadding),
                    0
                )
            );
        }

        return Points;
    }

    public static List<Vector3> GetPoissonDistributedPoints2D
    (
        Vector2Int InMapSizes, 
        float InRadius, 
        int numMaxSamples, 
        int InPadding, 
        List<Vector3> InPoints,
        int InMaxPoints = int.MaxValue
    )
    {
        float CellSize = InRadius / Mathf.Sqrt(2);

        int[,] Grid = new int[Mathf.CeilToInt(InMapSizes.x / CellSize), Mathf.CeilToInt(InMapSizes.y / CellSize)];
        List<Vector3> OutPoints = new List<Vector3>();
        List<Vector3> SpawnPoints = new List<Vector3>();

        SpawnPoints.Add(
            new Vector3
            (
                Mathf.CeilToInt(UnityEngine.Random.Range(InPadding, InMapSizes.x - InPadding)),
                Mathf.CeilToInt(UnityEngine.Random.Range(InPadding, InMapSizes.y - InPadding)),
                0
            )
        );

        if (InPoints != null)
        {
            for (int i = 0; i < InPoints.Count; i++)
            {
                OutPoints.Add(InPoints[i]);
            }
        }

        while (SpawnPoints.Count > 0 && OutPoints.Count < InMaxPoints)
        {
            int SpawnIndex = UnityEngine.Random.Range(0, SpawnPoints.Count);
            Vector3 SpawnCenter = SpawnPoints[SpawnIndex];
            bool bCandidateAccepted = false;

            for (int i = 0; i < numMaxSamples; i++)
            {
                float Angle = UnityEngine.Random.value * Mathf.PI * 2;
                Vector3 Direction = new Vector3(Mathf.Sin(Angle), Mathf.Cos(Angle), 0);
                Vector3 Candidate = SpawnCenter + Direction * UnityEngine.Random.Range(InRadius, 2 * InRadius);
                if (IsValid(Candidate, InMapSizes, CellSize, InRadius, OutPoints, Grid, InPadding))
                {
                    OutPoints.Add(Candidate);
                    SpawnPoints.Add(Candidate);
                    Grid[(int)(Candidate.x / CellSize), (int)(Candidate.y / CellSize)] = OutPoints.Count;
                    bCandidateAccepted = true;
                    break;
                }
            }

            if (!bCandidateAccepted)
            {
                SpawnPoints.RemoveAt(SpawnIndex);
            }
        }

        return OutPoints;
    }

    public static int Mod(int a, int n)
    {
        int c = a % n;
        if ((c < 0 && n > 0) || (c > 0 && n < 0))
        {
            c += n;
        }
        return c;
    }
}
