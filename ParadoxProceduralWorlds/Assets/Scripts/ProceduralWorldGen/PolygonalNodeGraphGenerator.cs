using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonalNodeGraphGeneratorSettings : IGeneratorSettings
{
	public string SeedString = "Blayne";
	[Range(10, 60)]
	public int NumPoissonSamples = 30;

	[Range(0, 20)]
	public int NumSmoothingIterations = 10;
}

public class PolygonalNodeGraphGenerator : IGenerator
{
	INodeGraphFactory<ITriangulator> NodeGraphFactory { get; set; }

	public PolygonalNodeGraphGenerator()
	{
		NodeGraphFactory = new TNetNodeGraphFactory() as INodeGraphFactory<ITriangulator>;
	}

	public INodeGraph Generate(IGeneratorSettings InSettings)
	{

		
		return null;
	}

	public INodeGraph Generate(IGeneratorSettings InSettings, INodeGraph InGraphMap)
	{
		return null;
	}

	public List<Vector3> GenerateSites(int NumSamples)
	{
		return new List<Vector3>();
	}
}
