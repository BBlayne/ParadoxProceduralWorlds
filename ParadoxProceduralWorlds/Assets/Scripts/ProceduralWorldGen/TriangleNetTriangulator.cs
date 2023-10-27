using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMesh = TriangleNet.Mesh;
using THalfEdge = TriangleNet.Topology.DCEL.HalfEdge;
using TFace = TriangleNet.Topology.DCEL.Face;
using TQualityOptions = TriangleNet.Meshing.QualityOptions;
using TVertex = TriangleNet.Geometry.Vertex;
using TPolygon = TriangleNet.Geometry.Polygon;
using TriangleNet.Geometry;

public class TriangleNetTriangulator : ITriangulator
{
    struct TriNetFace
    {

    }
    public TriangleNetTriangulator(List<Vector3> InPoints)
    {

    }

    TMesh GenerateDelaunayMesh(List<Vector3> InPoints)
    {
        TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions()
        {
            ConformingDelaunay = true
        };

        TPolygon DelaneyShape = new TPolygon();

        foreach (Vector3 PointV3 in InPoints)
        {
            TVertex Vertex = new TVertex(PointV3.x, PointV3.y);
            //sites.Add(Vertex);
            DelaneyShape.Add(Vertex);
        }

        DelaneyShape.Bounds();

        TMesh TriangleMesh = (TMesh)DelaneyShape.Triangulate(options);

        TriangleNet.Smoothing.SimpleSmoother simpleSmoother = new TriangleNet.Smoothing.SimpleSmoother();
        if (TriangleMesh != null)
        {
            simpleSmoother.Smooth(TriangleMesh, 1);
        }

        return TriangleMesh;
    }
}
