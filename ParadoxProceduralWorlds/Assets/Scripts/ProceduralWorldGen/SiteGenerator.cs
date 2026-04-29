
using System.Collections.Generic;
using UnityEngine;

public enum ESiteDistribution
{
	RANDOM,
	POISSON,
	RANDOM_MIRRORED,
	POISSON_MIRRORED
}

public struct SiteGeneratorConfig
{
	public ESiteDistribution SiteDistributionMode { get; set; }
	public Vector2Int MapDimensions { get; set; }

	public Vector2Int TextureDimensions { get; set; }

	public List<Vector3> InitialSites { get; set; }

	public Vector2Int MapPadding { get; set; }

	public int TargetNumSites { get; set; }	

	public string SeedString { get; set; }

	public int NumPoissonSamples { get; set; }

	public bool UseRandomSeed { get; set; }

	public float Bias { get; set; } 
}

public struct SiteData
{
	public List<Vector3> GeneratedSites {  get; set; }
	public Dictionary<int, int> SiteEdgeMapping {  get; set; }
	public List<Vector3> GeneratedLeftSites {  get; set; }
	public List<Vector3> GeneratedRightSites {  get; set; }
}

/*
 * Class for generating random locations for generating maps, i.e poisson disc
 * 
 */

public class SiteGenerator
{
	public SiteGeneratorConfig Config { get; set; }

	public SiteData GeneratedData;

	System.Random PRNG = null;

	string seed = "";

	public SiteGenerator()
	{
		Config = new SiteGeneratorConfig();
		GeneratedData = new SiteData();
	}

	public void Init()
	{
		seed = Config.SeedString;
		if (Config.UseRandomSeed)
		{
			seed = System.DateTime.Now.Ticks.ToString();
		}

		PRNG = new System.Random(seed.GetHashCode());

		Debug.Log("Initializing PRNG with string: " + seed + ", and hashcode: " + seed.GetHashCode());
	}

	public SiteData GenerateSiteDistribution()
	{
		switch (Config.SiteDistributionMode) 
		{
			case ESiteDistribution.RANDOM:
				return GenerateRandomSites();
			case ESiteDistribution.POISSON:
				return GeneratePoissonDistributedSites();
			case ESiteDistribution.RANDOM_MIRRORED:
				return GenerateRandomMirroredSites();
			case ESiteDistribution.POISSON_MIRRORED:
				return GenerateMirroredPoissonDistributedSites();
		}

		return GeneratedData;
	}

	public SiteData GenerateRandomMirroredSites()
	{
		List<Vector3> LeftSites = new List<Vector3>();
		List<Vector3> RightSites = new List<Vector3>();
		Dictionary<int, int> EdgeMap = new Dictionary<int, int>();
		List<Vector3> Sites = MapUtils.GenerateRandomPointsMirrored2D
		(
			Config.TargetNumSites,
			Config.TextureDimensions,
			Config.MapDimensions, 
			Config.MapPadding, 
			ref LeftSites, 
			ref RightSites, 
			ref EdgeMap
		);

		GeneratedData.GeneratedLeftSites = LeftSites;
		GeneratedData.GeneratedRightSites = RightSites;
		GeneratedData.SiteEdgeMapping = EdgeMap;
		GeneratedData.GeneratedSites = Sites;

		foreach (var elem in Config.InitialSites)
		{
			GeneratedData.GeneratedSites.Add(elem);
		}

		return GeneratedData;
	}

	public SiteData GenerateRandomSites()
	{
		List<Vector3> Sites = MapUtils.GenerateRandomPoints2D
		(
			Config.TargetNumSites, 
			Config.MapDimensions, 
			Config.MapPadding.x
		);
		GeneratedData.GeneratedSites = Sites;

		return GeneratedData;
	}

	public SiteData GeneratePoissonDistributedSites()
	{
		float Radius = MapUtils.DetermineRadiusForPoissonDisc
		(
			Config.MapDimensions, 
			Config.TargetNumSites
		);

		List<Vector3> Sites = MapUtils.GetPoissonDistributedPoints2D
		(
			Config.MapDimensions, 
			Radius, 
			Config.TargetNumSites, 
			Config.MapPadding.x, 
			Config.InitialSites, 
			Config.TargetNumSites * 10
		);

		GeneratedData.GeneratedSites = Sites;

		return GeneratedData;
	}

	public SiteData GenerateMirroredPoissonDistributedSites()
	{
		float Radius = MapUtils.DetermineRadiusForPoissonDisc(Config.MapDimensions, Config.TargetNumSites);
		List<Vector3> Sites =MapUtils.GetPoissonDistributedPoints2D
		(
			Config.MapDimensions, 
			Radius, 
			Config.TargetNumSites, 
			Config.MapPadding.x, 
			Config.InitialSites, 
			Config.TargetNumSites * 10
		);

		GeneratedData.GeneratedSites = Sites;

		return GeneratedData;
	}
}