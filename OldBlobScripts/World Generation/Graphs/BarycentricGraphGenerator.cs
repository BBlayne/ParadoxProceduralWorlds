using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Voronoi;
using System.Linq;

public class BarycentricGraphGenerator
{
    TriangleNet.Mesh mesh;
    public StandardVoronoi voronoi;
    TriangleNet.Tools.TriangleQuadTree quadtree = null;

    public BarycentricGraphGenerator(TriangleNet.Mesh mesh, StandardVoronoi voronoi)
    {
        this.mesh = mesh;
        this.voronoi = voronoi;
        this.quadtree = new TriangleNet.Tools.TriangleQuadTree(mesh);
    }

    public BarycentricGraphGenerator(TriangleNet.Mesh mesh)
    {
        this.mesh = mesh;
        this.voronoi = new StandardVoronoi(mesh);
        this.quadtree = new TriangleNet.Tools.TriangleQuadTree(mesh);
    }

    private Vector2 GetRandomPointFromVertices(List<Vector2> points)
    {
        if (points.Count < 3)
        {
            Debug.Log("Something went wrong");
            return new Vector2();
        }

        float r1 = Random.Range(0.45f, 0.55f);
        float r2 = Random.Range(0.45f, 0.55f);

        if (r1 + r2 > 1)
        {
            r1 = 1 - r1;
            r2 = 1 - r2;
        }

        float a = 1 - r1 - r2;
        float b = r1;
        float c = r2;

        return (a * points[0]) + (b * points[1]) + (c * points[2]);
    }

    public void GenerateBarycentricDualMesh(int width, int height)
    {

        for (int i = 0; i < voronoi.Vertices.Count; i++)
        {
            var currentVert = voronoi.Vertices[i];

            if (currentVert == null)
            {
                Debug.Log("current vert is null at i = " + i);
                continue;
            }

            if (currentVert.X == 0 || currentVert.Y == 0 ||
               currentVert.X == width - 1 || currentVert.Y == height - 1)
            {
                // is an edge on the edge, skip
                continue;
            }

            // get all edges surrounding this vertex
            List<TriangleNet.Topology.DCEL.HalfEdge> edges = 
                currentVert.EnumerateEdges().ToList();

            // compute the centroid by getting the location of the
            // site of each face
            Vector2 centroid = new Vector2();
            foreach (var edge in edges)
            {
                centroid += new Vector2((float)edge.Face.generator.X, 
                                        (float)edge.Face.generator.Y);
            }

            centroid /= edges.Count;

            currentVert.X = centroid.x + Random.Range(-0.5f, 0.5f);
            currentVert.Y = centroid.y + Random.Range(-0.5f, 0.5f);

        }
    }
}
