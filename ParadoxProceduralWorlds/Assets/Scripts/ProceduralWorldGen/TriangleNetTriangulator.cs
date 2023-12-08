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

public class TNetVertex
{
	public int id;
	public Vector3 Coordinate;

	public int ID
	{
		get => id;
	}

	public TNetVertex(TVertex InOldVertex)
	{
		id = InOldVertex.ID;
		Coordinate = new Vector3((float)InOldVertex.X, (float)InOldVertex.Y);
	}
}

public class TNetTriangle
{
	int id;

	public int ID
	{
		get => id;
	}

	TNetVertex[] vertices = new TNetVertex[3];

	public TNetVertex[] Vertices
	{
		get => vertices;
	}

	TNetOTriangle[] neighbours = new TNetOTriangle[3];

	TNetOTriangle[] Neighbours
	{
		get => neighbours;
	}

	public TNetTriangle()
	{
		// Default Constructor creates a Dummy triangle
		id = -1;

		neighbours[0].Triangle = this;
		neighbours[1].Triangle = this;
		neighbours[2].Triangle = this;
	}

	public TNetTriangle(TTriangle InOldTriangle, TNetVertex[] InAllVertices)
	{
		id = InOldTriangle.ID;

		if (InOldTriangle.GetVertexID(0) >= 0)
		{
			Vertices[0] = InAllVertices[InOldTriangle.GetVertexID(0)];
		}

		if (InOldTriangle.GetVertexID(1) >= 0)
		{
			Vertices[1] = InAllVertices[InOldTriangle.GetVertexID(1)];
		}

		if (InOldTriangle.GetVertexID(2) >= 0)
		{
			Vertices[2] = InAllVertices[InOldTriangle.GetVertexID(2)];
		}
	}

	public TNetVertex GetVertex(int InIndex)
	{
		if (InIndex >= 3)
		{
			return null;
		}
		return Vertices[InIndex];
	}

	// Get the ID of the Vertex using the specific ID
	public int GetVertexID(int InIndex)
	{
		if (InIndex >= 3)
		{
			return -1;
		}
		return Vertices[InIndex].ID;
	}

	public void SetNeighbours(TTriangle[] InOldTriangles, TNetOTriangle[] InOldOTriangles)
	{
		Neighbours[0] = InOldOTriangles[InOldTriangles[id].neighbors[0].Triangle.ID];
		Neighbours[0].Orientation = 0;
		Neighbours[1] = InOldOTriangles[InOldTriangles[id].neighbors[1].Triangle.ID];
		Neighbours[1].Orientation = 1;
		Neighbours[2] = InOldOTriangles[InOldTriangles[id].neighbors[2].Triangle.ID];
		Neighbours[2].Orientation = 2;
	}
}

// Oriented Triangle
public struct TNetOTriangle
{
	TNetTriangle triangle;
	int orientation; // 0,1, or 2

	public TNetTriangle Triangle 
	{ 
		get => triangle; 
		set => triangle = value;
	}

	public int Orientation
	{
		get => orientation;
		set => orientation = value;
	}

	public void Init(TNetTriangle InOriginalTri)
	{
		triangle = InOriginalTri;
	}
}

public class TNetEdge
{
	int p0;
	int p1;

	// first point's index
	public int P0
	{
		get => p0;
	}

	// second point's index
	public int P1
	{
		get => p1;
	}

	public TNetEdge(TEdge InOldEdge)
	{
		p0 = InOldEdge.P0;
		p1 = InOldEdge.P1;
	}
}

public class TNetHalfEdge
{
	public int ID;
	public TNetVertex origin;
	public TNetFace face;
	public TNetHalfEdge twin;
	public TNetHalfEdge next;

	public TNetHalfEdge(THalfEdge InOldHalfedge)
	{

	}
}

/*
 * SubSegments seems to be the edges along the outer boundary.
 * I have no idea how true this is or if there's gaps, but it 
 * seems to check out.
 */
public class TNetSubSegment
{
	int p0;
	int p1;

	TNetOTriangle[] triangles = new TNetOTriangle[2];

	TNetOTriangle[] Triangles
	{
		get => triangles;
	}

	public int P0
	{
		get => p0;
	}

	public int P1
	{
		get => p1;
	}

	public TNetSubSegment(TSubSegment InOldSubSegment, TNetOTriangle[] InTNetOTriangles)
	{
		p0 = InOldSubSegment.P0;
		p1 = InOldSubSegment.P1;
		// There other information about owning triangles 
		// and attached? adjorning? SubSegments but I have 
		// zero idea what they are in terms of a typical 
		// Triangulation.
		// Might be worth it to include the linked tris.
		if (InOldSubSegment.triangles[0].Triangle.ID != -1)
		{
			triangles[0] = InTNetOTriangles[InOldSubSegment.triangles[0].Triangle.ID];
		}

		if (InOldSubSegment.triangles[1].Triangle.ID != -1)
		{
			triangles[1] = InTNetOTriangles[InOldSubSegment.triangles[1].Triangle.ID];
		}
	}
}

public class TNetFace
{
	public int id;
	public TNetVertex Site;
	public TNetHalfEdge Edge;

	public int ID
	{
		get => id;
	}

	public TNetFace(TFace InOldFace)
	{
		id = InOldFace.ID;
	}
}

public class TNetTriangulation
{
	public Rect Bounds;
	public int NumInputVertices = 0;
	public TNetVertex[] Vertices;

	public void SetBounds(Rect InBounds)
	{
		Bounds = InBounds;
	}

	public void SetInputVertices(int InNumInputVertices)
	{
		NumInputVertices = InNumInputVertices;
	}

	public void SetVertices(TNetVertex[] InVertices)
	{
		Vertices = InVertices;
	}
}

public class TNetVoronoiDiagram
{
	public TNetVertex[] Vertices;
	public TNetHalfEdge[] Edges;
	public TNetFace[] Faces;
}

public class TriangleNetTriangulator : ITriangulator
{
	TNetConfig TheTriangleConfiguration;
	TMesh TheTriangulation = null;
	BoundedVoronoi TheVoronoiDiagram = null;
	// Our wrapper versions
	TNetTriangulation TheTNetTriangulation = null;

	public TMesh GetTriangulation
	{
		get => TheTriangulation;
	}

	public BoundedVoronoi GetVoronoiTesselation
	{
		get => TheVoronoiDiagram;
	}

	public TriangleNetTriangulator(TNetConfig InTriNetConfig)
	{
		TheTriangleConfiguration = InTriNetConfig;
	}

	public void GenerateTriangulationFromPoints(List<Vector3> InPoints)
	{
		TheTriangulation = GenerateDelaunayMesh(InPoints);
	}

	// Will replace existing Delaunay Triangulation if any and generate
	// a Voronoi Diagram from it.
	public void GenerateVoronoiTesselationFromPoints(List<Vector3> InPoints)
	{
		TheTriangulation = GenerateDelaunayMesh(InPoints);
		TheVoronoiDiagram = GetBoundedVoronoiFromTriangulation(TheTriangulation);
	}


	TMesh GenerateDelaunayMesh(List<Vector3> InPoints)
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

	// Generate our wrapper class for a TriangleNet triangulation
	void GenerateTNetTriangulation()
	{
		if (TheTriangulation == null)
		{
			return;
		}

		// Initialization
		TheTNetTriangulation = new TNetTriangulation();
		int NumVertices = TheTriangulation.Vertices.Count;
		TNetVertex[] TNetVertices = new TNetVertex[NumVertices];
		TVertex[] OriginalVertices = new TVertex[NumVertices];
		TheTriangulation.Vertices.CopyTo(OriginalVertices, 0);
		for (int i = 0; i < NumVertices; i++)
		{
			TNetVertices[i] = new TNetVertex(OriginalVertices[i]);
		}

		int NumTNetTriangles = TheTriangulation.Triangles.Count;
		TNetTriangle[] TNetTriangles = new TNetTriangle[NumTNetTriangles];
		TNetOTriangle[] TNetOrientedTriangles = new TNetOTriangle[NumTNetTriangles];
		TTriangle[] TTriangles = new TTriangle[NumTNetTriangles];
		TheTriangulation.Triangles.CopyTo(TTriangles, 0);

		TNetTriangle DummyTri = new TNetTriangle();

		for (int i = 0; i < NumTNetTriangles; i++)
		{
			TNetTriangles[i] = new TNetTriangle(TTriangles[i], TNetVertices);
			TNetOrientedTriangles[i].Init(TNetTriangles[i]);
		}

		for (int i = 0; i < NumTNetTriangles; i++)
		{
			TNetTriangles[i].SetNeighbours(TTriangles, TNetOrientedTriangles);
		}

		int NumSegments = TheTriangulation.Segments.Count;
		TNetEdge[] TNetEdges = new TNetEdge[NumSegments];
	}
}
