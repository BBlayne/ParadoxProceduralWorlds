using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Voronoi;
using UnityEngine;
using TConstraintOptions = TriangleNet.Meshing.ConstraintOptions;
using TEdge = TriangleNet.Geometry.Edge;
using TFace = TriangleNet.Topology.DCEL.Face;
using THalfEdge = TriangleNet.Topology.DCEL.HalfEdge;
using TMesh = TriangleNet.Mesh;
// The TriangleNet Oriented Triangle
using TOTriangle = TriangleNet.Topology.Otri;
using TPolygon = TriangleNet.Geometry.Polygon;
using TQualityOptions = TriangleNet.Meshing.QualityOptions;
using TSubSegment = TriangleNet.Topology.SubSegment;
using TTriangle = TriangleNet.Topology.Triangle;
using TVertex = TriangleNet.Geometry.Vertex;
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

public class TriangleNetTriangulator : ITriangulator
{
	public TriangulationConfig Configuration { get; set; }

	public TMesh Triangulation { get; private set; }

	public BoundedVoronoi TNetVoronoiTesselation { get; private set; }

	public List<Vector3> Sites { get; set; }

	public TriangleNetTriangulator() 
	{ 
		
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

		Mesh DelaunayUnityMesh = GenerateDelaunayUnityMesh(Triangulation);
		Mesh VoronoiUnityMesh = GenerateUnityMeshFromTNetVoronoi(TNetVoronoiTesselation);
		RenderTexture VoronoiGraphRTex = MapUtils.RenderPolygonalWireframeMap(
			Configuration.TextureDimensions, 
			VoronoiUnityMesh, 
			TextureGenerator.GetUnlitMaterial(), 
			Color.white
		);
		MapUtils.SaveMapAsPNG("Test_VoronoiGraphRTex", VoronoiGraphRTex);

		RenderTexture DelaunayGraphRTex = MapUtils.RenderPolygonalWireframeMap(
			Configuration.TextureDimensions, 
			DelaunayUnityMesh, 
			TextureGenerator.GetUnlitMaterial(), 
			Color.white
		);
		MapUtils.SaveMapAsPNG("Test_DelaunayGraphRTex", DelaunayGraphRTex);
	}


	TMesh GenerateDelaunayMesh(Vector3[] InPoints)
	{
		TConstraintOptions ConstraintOptions = new TConstraintOptions()
		{
			ConformingDelaunay = Configuration.IsConforming
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
			SimpleSmoother.Smooth(TriangleMesh, Configuration.NumSmoothingIterations);
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

	public static Mesh GenerateUnityMeshFromTNetVoronoi(VoronoiBase InVor)
	{
		Mesh OutVorMesh = new Mesh();

		List<IEdge> Edges = InVor.Edges.ToList();
		List<Vector3> MeshVertices = new List<Vector3>();
		List<int> MeshIndices = new List<int>();
		int IndexCounter = 0;
		for (int i = 0; i < Edges.Count; i++)
		{
			Vector3 PointP = new Vector3
			(
				(float)InVor.Vertices[Edges[i].P0].x,
				(float)InVor.Vertices[Edges[i].P0].y,
				0
			);

			Vector3 PointQ = new Vector3
			(
				(float)InVor.Vertices[Edges[i].P1].x,
				(float)InVor.Vertices[Edges[i].P1].y,
				0
			);

			MeshVertices.Add(PointP);
			MeshVertices.Add(PointQ);
			MeshIndices.Add(IndexCounter++);
			MeshIndices.Add(IndexCounter++);
		}
		OutVorMesh.vertices = MeshVertices.ToArray();
		OutVorMesh.SetIndices(MeshIndices, MeshTopology.Lines, 0);

		return OutVorMesh;
	}

	public Mesh GenerateDelaunayUnityMesh(TMesh triangleNetMesh, TQualityOptions options = null)
	{
		if (options != null)
		{
			triangleNetMesh.Refine(options);
		}

		Mesh mesh = new Mesh();
		var triangleNetVerts = triangleNetMesh.Vertices.ToList();

		var triangles = triangleNetMesh.Triangles;

		Vector3[] verts = new Vector3[triangleNetVerts.Count];
		int[] trisIndex = new int[triangles.Count * 3];

		for (int i = 0; i < verts.Length; i++)
		{
			verts[i] = new Vector3((float)triangleNetVerts[i].x, (float)triangleNetVerts[i].y, 0);
			Debug.Log("Coord: x: " + triangleNetVerts[i].x + ", y: " + triangleNetVerts[i].y);
		}

		int k = 0;

		foreach (var triangle in triangles)
		{
			for (int i = 2; i >= 0; i--)
			{
				trisIndex[k] = triangleNetVerts.IndexOf(triangle.GetVertex(i));
				k++;
			}
		}

		mesh.vertices = verts;
		mesh.triangles = trisIndex;

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		return mesh;
	}
}
