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
using TConstraintOptions = TriangleNet.Meshing.ConstraintOptions;

public struct TriNetConfig
{
	public bool bIsConforming;

	public int SmoothingInterations;

	public TriNetConfig(bool bInIsConformingDelaunay, int InSmoothingIterations)
	{
		bIsConforming = bInIsConformingDelaunay;
		SmoothingInterations = InSmoothingIterations;
	}
}

public class TriangleNetTriangulator : ITriangulator
{

	struct TriNetFace
	{

	}
	public TriangleNetTriangulator(List<Vector3> InPoints, TriNetConfig InTriNetConfig)
	{
		GenerateDelaunayMesh(InPoints, InTriNetConfig);
	}

	TMesh GenerateDelaunayMesh(List<Vector3> InPoints, TriNetConfig InTriNetConfig)
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

		TriangleNet.Smoothing.SimpleSmoother SimpleSmoother = new TriangleNet.Smoothing.SimpleSmoother();
		if (TriangleMesh != null)
		{
			SimpleSmoother.Smooth(TriangleMesh, InTriNetConfig.SmoothingInterations);
		}

		return TriangleMesh;
	}
}
