
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
	public ESiteDistribution Distribution { get; set; }
	public Vector2Int MapDimensions { get; set; }

	public List<Vector3> InitialSites { get; set; }

	public Vector2Int MapPadding { get; set; }

	public int TargetNumSites { get; set; }	
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
	public ESiteDistribution SiteDistributionMode { get; set; }
	public Vector2Int MapDimensions { get; set; }

	public List<Vector3> InitialSites { get; set; }

	public Vector2Int MapPadding { get; set; }

	public int TargetNumSites { get; set; }

	public List<Vector3> GeneratedSites {  get; private set; }

	public SiteData GeneratedData;

	public SiteGenerator()
	{
		MapDimensions = new Vector2Int(512, 256);
		SiteDistributionMode = ESiteDistribution.RANDOM_MIRRORED;
		MapPadding = new Vector2Int(25, 25);
		TargetNumSites = 100;
		GeneratedData = new SiteData();
	}

	public void Init(SiteGeneratorConfig InConfig)
	{
		SiteDistributionMode = InConfig.Distribution;
		MapDimensions = InConfig.MapDimensions;
		InitialSites = InConfig.InitialSites;
		MapPadding = InConfig.MapPadding;
		TargetNumSites = InConfig.TargetNumSites;
	}

	public SiteData GenerateSiteDistribution()
	{
		switch (SiteDistributionMode) 
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
		List<Vector3> Sites = MapUtils.GenerateRandomPointsMirrored2D(
			TargetNumSites, MapDimensions, MapPadding.x, ref LeftSites, ref RightSites, ref EdgeMap);

		GeneratedData.GeneratedLeftSites = LeftSites;
		GeneratedData.GeneratedRightSites = RightSites;
		GeneratedData.SiteEdgeMapping = EdgeMap;
		GeneratedData.GeneratedSites = Sites;

		foreach (var elem in InitialSites)
		{
			GeneratedData.GeneratedSites.Add(elem);
		}

		return GeneratedData;
	}

	public SiteData GenerateRandomSites()
	{
		List<Vector3> Sites = MapUtils.GenerateRandomPoints2D(TargetNumSites, MapDimensions, MapPadding.x);
		GeneratedData.GeneratedSites = Sites;

		return GeneratedData;
	}

	public SiteData GeneratePoissonDistributedSites()
	{
		float Radius = MapUtils.DetermineRadiusForPoissonDisc(MapDimensions, TargetNumSites);
		List<Vector3> Sites = MapUtils.GetPoissonDistributedPoints2D
		(
			MapDimensions, Radius, TargetNumSites, MapPadding.x, InitialSites, TargetNumSites * 10
		);

		GeneratedData.GeneratedSites = Sites;

		return GeneratedData;
	}

	public SiteData GenerateMirroredPoissonDistributedSites()
	{
		float Radius = MapUtils.DetermineRadiusForPoissonDisc(MapDimensions, TargetNumSites);
		List<Vector3> Sites =MapUtils.GetPoissonDistributedPoints2D
		(
			MapDimensions, Radius, TargetNumSites, MapPadding.x, InitialSites, TargetNumSites * 10
		);

		GeneratedData.GeneratedSites = Sites;

		return GeneratedData;
	}
}