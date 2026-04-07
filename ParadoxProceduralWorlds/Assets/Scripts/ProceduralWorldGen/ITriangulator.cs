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
	public Vector2Int TextureDimensions { get; set; }

	public bool VoronoiRelaxationEnabled { get; set; }

	public int NumRelaxationIterations { get; set; }

	public TriangulationConfig(bool InIsConformingDelaunay, int InSmoothingIterations)
	{
		IsConforming = InIsConformingDelaunay;
		NumSmoothingIterations = InSmoothingIterations;
		MapDimensions = new Vector2Int(512, 512);
		TextureDimensions = new Vector2Int(512, 512);
		VoronoiRelaxationEnabled = true;
		NumRelaxationIterations = 5;
	}
}