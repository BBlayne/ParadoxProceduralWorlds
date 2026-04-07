using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.RayTracingAccelerationStructure;

public class PolygonalNodeGraphGeneratorSettings : IGeneratorSettings
{
	public SiteGeneratorConfig SiteGenerationConfig { get; set; }

	public TriangulationConfig TriangulationConfiguration { get; set; }
}

public class PolygonalNodeGraphGenerator : IGenerator<TNetNodeGraphFactory, TriangleNetTriangulator>
{
	public TNetNodeGraphFactory NodeGraphFactory { get; set; }

	public PolygonalNodeGraphGeneratorSettings MapSettings { get; set; }

	public SiteGenerator SiteGen { get; private set; }

	public SiteGeneratorConfig SiteConfig { get; set; }

	public SiteData GeneratedSiteData { get; private set; }

	public PolygonalNodeGraphGenerator()
	{
		NodeGraphFactory = new TNetNodeGraphFactory();
		SiteGen = new SiteGenerator();
		SiteConfig = new SiteGeneratorConfig();
		GeneratedSiteData = new SiteData();
	}

	public void Init()
	{
		NodeGraphFactory.Configuration = MapSettings.TriangulationConfiguration;
		NodeGraphFactory.GeneratorConfig = MapSettings.SiteGenerationConfig;

		SiteGen.Config = SiteConfig;
		SiteGen.Init();

		GeneratedSiteData = SiteGen.GenerateSiteDistribution();

		NodeGraphFactory.GeneratedSites = GeneratedSiteData;

		NodeGraphFactory.Init();
	}

	public INodeGraph Generate()
	{
		if (MapSettings == null)
		{
			Debug.LogWarning("MapSettings is not valid.");
			return null;
		}			

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
