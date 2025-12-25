using UnityEngine;

public class TNetTriangulator : ITriangulator
{
	private IGraphMap triangulatedGraph = null;
	private IGraphMap voronoiGraph = null;
	IGraphMap ITriangulator.TriangulatedGraph => throw new System.NotImplementedException();

	IGraphMap ITriangulator.VoronoiGraph => throw new System.NotImplementedException();

	public IGraphMap GenerateTriangulatedGraph()
	{
		throw new System.NotImplementedException();
	}

	public IGraphMap GenerateVoronoiGraph(bool IsBounded = true)
	{ 
		throw new System.NotImplementedException(); 
	}
}
