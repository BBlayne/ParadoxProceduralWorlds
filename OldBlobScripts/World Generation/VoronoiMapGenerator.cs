using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Tools;
using System.IO;
using System.Linq;
using System.Timers;
using System.Diagnostics;


public class VoronoiMapGenerator
{
    public enum SITE_MODES
    {
        UNIFORM,
        OFFSET,
        CIRCLE,
        RANDOM
    }

    public List<Color> colors = new List<Color>();

    public bool relax = true;
    public int iterations = 10;
    public int seed = 0;
    public bool useRandomSeed = false;
    public float minHeight = 0;
    public float fillPercent = 100;

    public int minSat = 0;
    public int maxSat = 100;

    public int minHue = 0;
    public int maxHue = 360;

    public int minBright = 0;
    public int maxBright = 100;

    System.Random prng = null;

    private int currentVSiteID = 0;

    public int Width;
    public int Height;

    List<Vertex> verts = new List<Vertex>();
    List<Edge> edges = new List<Edge>();

    // stored sites
    public List<Vertex> Sites = new List<Vertex>();

    Polygon poly = null;

    public TriangleNet.Mesh mesh = null;

    public TriangleNet.Voronoi.StandardVoronoi voronoi = null;

    public TriangleQuadTree triangleQuadTree = null;

    public VoronoiMapGenerator(int width, int height) {
        this.Width = width;
        this.Height = height;
    }

    public void Init_Triangle()
    {
        if (poly == null)
            poly = new Polygon();
        else
        {
            poly = null;
            poly = new Polygon();
        }

        mesh = null;
        voronoi = null;
        triangleQuadTree = null;
    }

    public void Init()
    {
        if (useRandomSeed)
        {
            seed = System.DateTime.Now.GetHashCode();
            prng = new System.Random(seed);
        }

        prng = new System.Random(seed);
    }

    public void GenerateDelaneyMesh(List<Vertex> sites)
    {
        Vertex cornerA = new Vertex(0, 0);
        Vertex cornerB = new Vertex(Width - 1, 0);
        Vertex cornerC = new Vertex(0, Height - 1);
        Vertex cornerD = new Vertex(Width - 1, Height - 1);

        List<Vertex> m_sites = new List<Vertex>(sites);
        m_sites.Add(cornerA);
        m_sites.Add(cornerB);
        m_sites.Add(cornerC);
        m_sites.Add(cornerD);

        foreach (Vertex vertToAdd in m_sites)
        {
            poly.Add(vertToAdd);
        }

        // ConformingDelaunay is false by default; this leads to ugly long polygons at the edges
        // because the algorithm will try to keep the mesh convex
        TriangleNet.Meshing.ConstraintOptions options =
            new TriangleNet.Meshing.ConstraintOptions() { ConformingDelaunay = true };

        mesh = (TriangleNet.Mesh)poly.Triangulate(options);

        if (relax)
        {
            TriangleNet.Smoothing.SimpleSmoother simpleSmoother = new TriangleNet.Smoothing.SimpleSmoother();
            simpleSmoother.Smooth(mesh, iterations);
        }

        // init
        verts = new List<Vertex>(mesh.Vertices);
        edges = new List<Edge>(mesh.Edges);

        triangleQuadTree = new TriangleQuadTree(mesh);
    }

    private void InitializeDelaneyMesh(SITE_MODES mode, int numPoints, int _margin)
    {
        Vertex cornerA = new Vertex(0, 0);
        Vertex cornerB = new Vertex(Width - 1, 0);
        Vertex cornerC = new Vertex(0, Height - 1);
        Vertex cornerD = new Vertex(Width - 1, Height - 1);

        poly.Add(cornerA);
        poly.Add(cornerB);
        poly.Add(cornerC);
        poly.Add(cornerD);

        List<Vertex> sites = GenerateSites(mode, numPoints, _margin);
        List<Vertex> sites2 = new List<Vertex>();
        //sites2.Add(new Vertex(Width / 2, Height / 2));

        foreach (Vertex vertToAdd in sites)
        {
            poly.Add(vertToAdd);
        }

        //poly.Add(new Contour(sites2));

        // ConformingDelaunay is false by default; this leads to ugly long polygons at the edges
        // because the algorithm will try to keep the mesh convex
        TriangleNet.Meshing.ConstraintOptions options =
            new TriangleNet.Meshing.ConstraintOptions() { ConformingDelaunay = true };

        mesh = (TriangleNet.Mesh)poly.Triangulate(options);

        if (relax)
        {
            TriangleNet.Smoothing.SimpleSmoother simpleSmoother = new TriangleNet.Smoothing.SimpleSmoother();
            simpleSmoother.Smooth(mesh, iterations);
        }

        // init
        verts = new List<Vertex>(mesh.Vertices);
        edges = new List<Edge>(mesh.Edges);

    }

    public void GenerateVoronoiDiagram()
    {
        voronoi = new TriangleNet.Voronoi.StandardVoronoi(mesh);

        UnityEngine.Debug.Log("Voronoi Face Count: " + voronoi.Faces.Count);
        UnityEngine.Debug.Log("(Mesh) Vertice Count: " + mesh.Vertices.Count);
    }

    public List<Vertex> GenerateSites(SITE_MODES mode, int numPoints, int _margin)
    {
        return GenerateOffsetSites(numPoints, _margin);
    }

    public List<Vertex> GenerateOffsetSites(int numPoints, int _margin)
    {
        List<Vertex> sites = new List<Vertex>();
        // sqrt(100) = 10
        //
        int pointLength = Mathf.RoundToInt(Mathf.Sqrt(numPoints));

        //pointLength = Mathf.RoundToInt(pointLength * density);
        int _width = Width;
        int _height = Height;
        float _rowOffset = 0;

        float spacing_ecks = (float)_width / (pointLength + 1);
        float spacing_why = (float)_height / (pointLength + 1);

        for (int y = 0; y < pointLength; y++)
        {
            int _pointLength = pointLength;
            if (y % 2 == 0)
            {
                _rowOffset = 0;
            }
            else
            {
                _rowOffset = spacing_ecks / 2;
                _pointLength = pointLength - 1;
            }

            for (int x = 0; x < _pointLength; x++)
            {
                Vertex vertToAdd =
                    new Vertex((x * spacing_ecks) + spacing_ecks + _rowOffset,
                               (y * spacing_why) + spacing_why);


                vertToAdd.X = vertToAdd.X + prng.Next(-10, 10);
                vertToAdd.Y = vertToAdd.Y + prng.Next(-10, 10);
                sites.Add(vertToAdd);
            }
        }

        return sites;
    }

    List<Vertex> GenerateRandomSiteDistribution(int numPoints, int _margin)
    {
        List<Vertex> sites = new List<Vertex>();

        for (int i = 0; i < numPoints; i++)
        {
            sites.Add(new Vertex(
                prng.Next(_margin, Width - _margin - 1),
                prng.Next(_margin, Height - _margin - 1)));
        }

        /*
        float inner_percent = innerPercent / 100;

        int inner_ecks_a = (xsize / 2) - Mathf.RoundToInt(xsize * inner_percent);
        int inner_ecks_b = (xsize / 2) + Mathf.RoundToInt(xsize * inner_percent);

        int inner_why_a = (ysize / 2) - Mathf.RoundToInt(ysize * inner_percent);
        int inner_why_b = (ysize / 2) + Mathf.RoundToInt(ysize * inner_percent);

        // less random
        for (int i = 0; i < numInnerPoints; i++)
        {
            sites.Add(new Vertex(Random.Range(inner_ecks_a, inner_ecks_b),
                                   Random.Range(inner_why_a, inner_why_b)));
        }
        */

        return sites;
    }
}

