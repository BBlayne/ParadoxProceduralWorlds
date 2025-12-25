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
	public IGraphMap TriangulatedGraph {  get; }
	public IGraphMap VoronoiGraph { get; }
	public IGraphMap GenerateTriangulatedGraph();
	public IGraphMap GenerateVoronoiGraph(bool IsBounded = true);
}
