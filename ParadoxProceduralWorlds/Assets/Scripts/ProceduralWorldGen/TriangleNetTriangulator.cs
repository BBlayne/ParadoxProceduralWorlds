using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMesh = TriangleNet.Mesh;
using THalfEdge = TriangleNet.Topology.DCEL.HalfEdge;
using TEdge = TriangleNet.Geometry.Edge;
using TFace = TriangleNet.Topology.DCEL.Face;
using TQualityOptions = TriangleNet.Meshing.QualityOptions;
using TVertex = TriangleNet.Geometry.Vertex;
using TPolygon = TriangleNet.Geometry.Polygon;
using TTriangle = TriangleNet.Topology.Triangle;
using TSubSegment = TriangleNet.Topology.SubSegment;

// The TriangleNet Oriented Triangle
using TOTriangle = TriangleNet.Topology.Otri;
using TriangleNet.Geometry;
using TConstraintOptions = TriangleNet.Meshing.ConstraintOptions;
using TriangleNet.Voronoi;
using TriangleNet.Topology;
//using DTMesh = TriangleNet.Meshing.

/*
 * Goal: An implementation of the ITriangulator interface.
 * 
 * A class that implements a ITriangulator provides a Triangulator service.
 * 
 * I.e: It does the things we expect any Triangulation Library such as 
 * TriangleNet to do, or TriangleCPP, etc and so on. Given a set of 
 * points, get a Delaunay Triangulation and a Voronoi graph diagram.
 * 
 * In short this class acts as a wrapper for a Triangle library, in 
 * this case TriangleNet.
 * 
 * Plan is for common data to be easily accessible without annoying
 * Unity3D vis a vis C# data type conversions like double to float.
 * 
*/

public struct TNetConfig
{
	public bool bIsConforming;

	public int SmoothingInterations;

	public TNetConfig(bool bInIsConformingDelaunay, int InSmoothingIterations)
	{
		bIsConforming = bInIsConformingDelaunay;
		SmoothingInterations = InSmoothingIterations;
	}
}

public class TriangleNetTriangulator : ITriangulator
{
	TNetConfig TheTriangleConfiguration;

	public TMesh Triangulation { get; private set; }

	public BoundedVoronoi TNetVoronoiTesselation { get; private set; }

	public List<Vector3> Sites { get; set; }

	public TriangleNetTriangulator(TNetConfig InTriNetConfig)
	{
		TheTriangleConfiguration = InTriNetConfig;
	}

	public void GenerateTriangulationFromPoints(Vector3[] InPoints)
	{
		Triangulation = GenerateDelaunayMesh(InPoints);
	}

	// Will replace existing Delaunay Triangulation if any and generate
	// a Voronoi Diagram from it.
	public void GenerateVoronoiTesselationFromPoints(Vector3[] InPoints)
	{
		Triangulation = GenerateDelaunayMesh(InPoints);
		TNetVoronoiTesselation = GetBoundedVoronoiFromTriangulation(Triangulation);
	}


	TMesh GenerateDelaunayMesh(Vector3[] InPoints)
	{
		TConstraintOptions ConstraintOptions = new TConstraintOptions()
		{
			ConformingDelaunay = TheTriangleConfiguration.bIsConforming
		};

		TPolygon DelaneyShape = new TPolygon();

		foreach (Vector3 PointV3 in InPoints)
		{
			TVertex Vertex = new TVertex(PointV3.x, PointV3.y);
			DelaneyShape.Add(Vertex);
		}

		DelaneyShape.Bounds();

		TMesh TriangleMesh = (TMesh)DelaneyShape.Triangulate(ConstraintOptions);

		TriangleNet.Smoothing.SimpleSmoother SimpleSmoother = new TriangleNet.Smoothing.SimpleSmoother();
		if (TriangleMesh != null)
		{
			SimpleSmoother.Smooth(TriangleMesh, TheTriangleConfiguration.SmoothingInterations);
		}

		return TriangleMesh;
	}

	StandardVoronoi GetStandardVoronoiFromTriangulation(TMesh InTriangleMesh)
	{
		return new StandardVoronoi(InTriangleMesh);
	}

	BoundedVoronoi GetBoundedVoronoiFromTriangulation(TMesh InTriangleMesh)
	{
		return new BoundedVoronoi(InTriangleMesh);
	}

	public void Triangulate()
	{
		if (Sites != null)
		{
			GenerateVoronoiTesselationFromPoints(Sites.ToArray());
		}
	}
}
