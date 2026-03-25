using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * A Triangulator is similar to a Generator but is a wrapper for the underlying 
 * library being used to create graph maps, such as a delaunay triangulation
 * or its Voronoi graph.
 * 
 * This way we can decouple the Graph Map generation from the underlying
 * libraries being utilized.
 */

public interface ITriangulator
{
	public TriangulationConfig Configuration { get; set; }
	public List<Vector3> Sites { get; set; }
	// Generate a delaunay/voronoi triangulation/tesselation
	void Triangulate();
}

public interface ITriangulationConfig
{

}

public struct TriangulationConfig : ITriangulationConfig
{
	public bool IsConforming { get; set; }
	public int NumSmoothingIterations { get; set; }

	public Vector2Int MapDimensions { get; set; }

	public List<Vector3> Sites { get; set; }

	public TriangulationConfig(bool InIsConformingDelaunay, int InSmoothingIterations)
	{
		IsConforming = InIsConformingDelaunay;
		NumSmoothingIterations = InSmoothingIterations;
		Sites = new List<Vector3>();
		MapDimensions = new Vector2Int();
	}
}