using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TriangleNet.Voronoi;
using TMesh = TriangleNet.Mesh;
using THalfEdge = TriangleNet.Topology.DCEL.HalfEdge;
using TFace = TriangleNet.Topology.DCEL.Face;

public static class IListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

public static class MapUtils
{

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
            int SpawnIndex = Random.Range(0, SpawnPoints.Count);
            Vector3 SpawnCenter = SpawnPoints[SpawnIndex];
            bool bCandidateAccepted = false;

            for (int i = 0; i < numMaxSamples; i++)
            {
                float Angle = Random.value * Mathf.PI * 2;
                Vector3 Direction = new Vector3(Mathf.Sin(Angle), Mathf.Cos(Angle), 0);
                Vector3 Candidate = SpawnCenter + Direction * Random.Range(InRadius, 2 * InRadius);
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
                    Random.Range(InPadding, InMapSizes.x - InPadding),
                    Random.Range(InPadding, InMapSizes.y - InPadding),
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
                Mathf.CeilToInt(Random.Range(InPadding, InMapSizes.x - InPadding)),
                Mathf.CeilToInt(Random.Range(InPadding, InMapSizes.y - InPadding)),
                0
            )
        );

        for (int i = 0; i < InPoints.Count; i++)
        {
            OutPoints.Add(InPoints[i]);
        }

        while (SpawnPoints.Count > 0 && OutPoints.Count < InMaxPoints)
        {
            int SpawnIndex = Random.Range(0, SpawnPoints.Count);
            Vector3 SpawnCenter = SpawnPoints[SpawnIndex];
            bool bCandidateAccepted = false;

            for (int i = 0; i < numMaxSamples; i++)
            {
                float Angle = Random.value * Mathf.PI * 2;
                Vector3 Direction = new Vector3(Mathf.Sin(Angle), Mathf.Cos(Angle), 0);
                Vector3 Candidate = SpawnCenter + Direction * Random.Range(InRadius, 2 * InRadius);
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
