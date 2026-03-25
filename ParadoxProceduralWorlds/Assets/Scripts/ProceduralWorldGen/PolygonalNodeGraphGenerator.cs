using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonalNodeGraphGeneratorSettings : IGeneratorSettings
{
	public string SeedString = "Blayne";
	[Range(10, 60)]
	public int NumPoissonSamples = 30;

	[Range(1, 100)]
	public Vector2Int Padding = new Vector2Int(10, 10);

	[Range(128, 5000)]
	public Vector2Int MapSize = new Vector2Int();

	public List<Vector3> InitialSites = new List<Vector3>();

	[Range(0, 20)]
	public int NumSmoothingIterations = 10;

	public ESiteDistribution SiteDistributionMode { get; set; } = ESiteDistribution.RANDOM_MIRRORED;
}

public class PolygonalNodeGraphGenerator : IGenerator<TNetNodeGraphFactory, TriangleNetTriangulator>
{
	public TNetNodeGraphFactory NodeGraphFactory { get; set; }

	public PolygonalNodeGraphGeneratorSettings MapSettings { get; set; }

	SiteGenerator SiteGen = new SiteGenerator();

	public PolygonalNodeGraphGenerator()
	{
	}

	public void Init()
	{
		SiteGen.MapDimensions = MapSettings.MapSize;
		SiteGen.MapPadding = MapSettings.Padding;
		SiteGen.InitialSites = MapSettings.InitialSites;
		SiteGen.SiteDistributionMode = MapSettings.SiteDistributionMode;

		TriangulationConfig Configuration = new TriangulationConfig();
		Configuration.NumSmoothingIterations = MapSettings.NumSmoothingIterations;

		SiteData GeneratedData = GenerateSites(MapSettings.NumPoissonSamples);
		Configuration.Sites = GeneratedData.GeneratedSites;
		Configuration.IsConforming = true;

		NodeGraphFactory.Configuration = Configuration;
		NodeGraphFactory.GeneratedSites = GeneratedData;
	}

	public INodeGraph Generate()
	{
		return NodeGraphFactory.GenerateNodeGraph();
	}

	public INodeGraph Generate(INodeGraph InGraphMap)
	{
		return null;
	}

	public SiteData GenerateSites(int NumSamples)
	{
		return SiteGen.GeneratedData;
	}
}
