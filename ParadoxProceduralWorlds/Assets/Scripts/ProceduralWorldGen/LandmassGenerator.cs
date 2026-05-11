using DataStructures.ViliWonka.KDTree;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public struct MapSetting
{
	public string MapName {  get; set; }
	public Vector2Int MapSize {  get; set; }

	public int NumberOfContinents { get; set; }

	public int NumberOfTectonicPlates { get; set; }

	public float LandToWaterRatio { get; set; }
}

/*
 * Note, no gaurantee that continents won't end up adjacent
 * thereby forming a larger continuous landmass. Etc.
 */
public enum EContinentSizeModes
{
	BALANCED, // same sized continents
	MEGACONTINENT, // one supercontinent, everything else are island-like
	EURASIA
};

public enum ELandmassType
{
	CONTINENT,
	TECTONIC
};

public class LandmassGenerator : IMapGenerator<PolygonalNodeGraph>
{
	PolygonalNodeGraph NodeGraph;

	public Mesh CellMesh {  get; private set; }

	public bool DebugMapEnabled {  get; set; }

	public MapSetting MapSettings { get; set; }

	public KDTree VSiteKDTree = null;

	public RenderTexture ContinentRTex;
	public RenderTexture TectonicPlatesRTex;

	const int OCEAN_CONTINENT = 1;
	// expected minimum, for Megacontinent setups the remainder represents large islands.
	const int MIN_LAND_CONTINENTS = 3;

	const float NUDGE_FACTOR = 0.1f;
	int DebugFloodfillIterationStep = 2;

	const int UNASSIGNED = -1;

	public LandmassGenerator()
	{
		MapSetting DefaultMapSettings = new MapSetting();
		DefaultMapSettings.MapName = "TestTectMap";
		DefaultMapSettings.NumberOfTectonicPlates = 5;
		DefaultMapSettings.NumberOfContinents = 3;
		DefaultMapSettings.MapSize = new Vector2Int(600, 600);
		DefaultMapSettings.LandToWaterRatio = 0.20f; // i.e 20% of tiles are land

		MapSettings = DefaultMapSettings;
	}

	public INodeGraph Generate(INodeGraph InGraph)
	{
		NodeGraph = InGraph as PolygonalNodeGraph;

		if (NodeGraph == null) 
		{
			Debug.LogError("Error: NodeGraph parameter not a valid PolygonalNodeGraph...");
			return null;
		}

		Dictionary<int, Vector3> NodeCoords = new Dictionary<int, Vector3>();

		// todo
		GenerateTectonicPlates();
		GenerateContinents();

		// todo - actually assign values to the graph

		return InGraph;
	}

	bool GenerateBalancedContinentSizes(int numLandCells, int numLandContinents, ref List<int> ContinentSizes)
	{
		if (ContinentSizes == null)
			return false;


		for (int i = 0; i < numLandContinents; i++)
		{
			// get the target size of each continent
			ContinentSizes.Add(numLandCells);
		}		

		return true;
	}

	/*
	 * Aim to generate continents in decreasing order of size. E.g:
	 * Asia: 50%
	 * Europe: 10%
	 * Africa: 15%
	 * North America: 12%
	 * South America: 8%
	 * Australia: 5%
	 * 
	 * We'll fix the size of the first continent to 50% and then evenly
	 * divide the remaining 50% to the remaining continents (min 2)
	 * plus some random nudge factor to try to add variety.
	 */
	bool GenerateEurasianContinentSizes(int numLandCells, int numLandContinents, ref List<int> ContinentSizes)
	{
		if (ContinentSizes == null)
			return false;

		numLandContinents = Mathf.Max(numLandContinents, 3);
		int EurasiaSize = Mathf.RoundToInt(numLandCells * 0.5f);
		// get the remaining budget of cells to distribute
		numLandCells -= EurasiaSize;
		int Remainders = numLandContinents - 1; // the remaining continents
		numLandCells = Mathf.RoundToInt((float)numLandCells / Remainders);

		// determine a reasonable range of our nudge that scales with the map size
		// for now something like 10% of the size of our remainder continents
		// minimum 1.
		// We add our nudge factor to try to avoid gaps.
		int Nudge = Mathf.Max(Mathf.RoundToInt(NUDGE_FACTOR * numLandCells), 1);

		ContinentSizes.Add(EurasiaSize);
		for (int i = 0; i < numLandContinents - 1; i++)
		{
			// get the target size of each remaining continent
			ContinentSizes.Add(numLandCells + Random.Range(0, Nudge));
		}		

		return true;
	}

	bool GenerateMegaContinentSizes(int numLandCells, int numLandContinents, ref List<int> ContinentSizes)
	{
		if (ContinentSizes == null)
			return false;

		numLandContinents = Mathf.Max(numLandContinents, 3);
		int EurasiaSize = Mathf.RoundToInt(numLandCells * 0.8f);
		// get the remaining budget of cells to distribute
		numLandCells -= EurasiaSize;
		int Remainders = numLandContinents - 1; // the remaining continents
		numLandCells = Mathf.RoundToInt((float)numLandCells / Remainders);

		// determine a reasonable range of our nudge that scales with the map size
		// for now something like 10% of the size of our remainder continents
		// minimum 1.
		// We add our nudge factor to try to avoid gaps.
		int Nudge = Mathf.Max(Mathf.RoundToInt(NUDGE_FACTOR * numLandCells), 1);

		ContinentSizes.Add(EurasiaSize);
		for (int i = 0; i < numLandContinents - 1; i++)
		{
			// get the target size of each remaining continent
			ContinentSizes.Add(numLandCells + Random.Range(0, Nudge));
		}		

		return true;
	}

	void GenerateContinents()
	{
		Debug.Log("Generating continents..."); 

		// we can represent the ocean as being its own "continent" blob
		int[] InitialContinentPoints = DetermineInitialContinentCells(
			MapSettings.NumberOfContinents + OCEAN_CONTINENT, 
			25
		);		

		/*
			Assign each continent a target size to be the maximum bound
			if target size is equal to map size then each continent tries 
			to be as large as it can until it runs out of room.
		 */
		int numLandCells = Mathf.RoundToInt(MapSettings.LandToWaterRatio * NodeGraph.GetNumFaces());
		int numContinents = MapSettings.NumberOfContinents; // actual number of LAND continents
		List<int> ContinentSizes = new List<int>();
		EContinentSizeModes ContinentSizeMode = EContinentSizeModes.BALANCED;
		int numOceanCells = NodeGraph.GetNumFaces() - numLandCells;
		// have our ocean "continent" be our first entry
		ContinentSizes.Add(numOceanCells);
		switch (ContinentSizeMode)
		{
			case EContinentSizeModes.BALANCED:
			{
				GenerateBalancedContinentSizes(numLandCells, numContinents, ref ContinentSizes);
				break;
			}
			case EContinentSizeModes.EURASIA:
			{
				GenerateEurasianContinentSizes(numLandCells, numContinents, ref ContinentSizes);
				break;
			}
			case EContinentSizeModes.MEGACONTINENT:
			{
				GenerateMegaContinentSizes(numLandCells, numContinents, ref ContinentSizes);
				break;
			}
		}

		// visualize
		Vector2Int Hues = new Vector2Int(30, 330);
		Vector2Int Saturation = new Vector2Int(99, 100);
		Vector2Int Brightness = new Vector2Int(99, 100);
		List<Color> ContinentColours = TextureGenerator.GenerateHSVColours(MapSettings.NumberOfContinents + OCEAN_CONTINENT, Hues, Saturation, Brightness);
		ContinentColours.Shuffle();
		ContinentColours[0] = Color.blue; // ocean

		Mesh ContinentMesh = NodeGraph.GenerateUnityMeshFromGraph(EUnityMeshMode.VORONOI_FILLED);
		ContinentMesh.name = "ContinentMesh";

		// get our array of continent group ids assigned to our cells
		int[] AssignedContinentCells = CellGroupFloodFill(ELandmassType.CONTINENT, InitialContinentPoints.ToList(), ContinentSizes, ContinentColours, ContinentMesh, DebugMapEnabled);		

		Texture2D ContinentTexMap = TextureGenerator.GenerateContinentalTextureMap(NodeGraph.GetNumCells(), AssignedContinentCells, ContinentColours);

		ContinentRTex = MapUtils.RenderPolygonalMap(ContinentMesh, MapSettings.MapSize, ContinentTexMap, TextureGenerator.GetUnlitTextureMaterial(), true);
	}

	/*
	 * Functions for Continents and Plates are similar atm, but might change later,
	 * no need to code golf it to just one function when might customize it more later.
	 */
	int[] DetermineInitialContinentCells(int InNumContinents, int InPadding)
	{
		Debug.Log("Determining initial Continental points..."); 

		// Select a number of sites that are evenly spread
		List<int> OutContinentSites = new List<int>();

		// Get Initial Sample Sites either Randomly or via Poisson Disc Sampling
		int PoissonRadius = MapUtils.DetermineRadiusForPoissonDisc(MapSettings.MapSize, InNumContinents);
		ESiteDistribution PlateSiteDistribution = ESiteDistribution.RANDOM;
		List<Vector3> InitialSamplePoints = MapUtils.GenerateSiteDistribution(
			PlateSiteDistribution,
			InNumContinents,
			MapSettings.MapSize,
			InPadding,
			PoissonRadius,
			null
		);

		foreach (var coord in InitialSamplePoints)
		{
			int ResultCellID = NodeGraph.GetCellIDFromCoordinate(coord);
			if (ResultCellID >= 0)
			{
				OutContinentSites.Add(ResultCellID);
			}
		}

		return OutContinentSites.ToArray();
	}

	void GenerateTectonicPlates()
	{
		Debug.Log("Generating Tectonic Plates..."); 

		int[] InitialPlatePoints = DetermineInitialTectonicPlatePoints(
			MapSettings.NumberOfTectonicPlates, 
			MapSettings.MapSize,
			true
		);

		List<int> PlateSizes = new List<int>();
		for (int i = 0; i < InitialPlatePoints.Length; i++)
		{
			PlateSizes.Add(NodeGraph.GetNumFaces());
		}

		// visualize
		Vector2Int Hues = new Vector2Int(30, 330);
		Vector2Int Saturation = new Vector2Int(99, 100);
		Vector2Int Brightness = new Vector2Int(99, 100);
		List<Color> PlateColours = TextureGenerator.GenerateHSVColours(MapSettings.NumberOfTectonicPlates, Hues, Saturation, Brightness);
		PlateColours.Shuffle();

		Mesh TectPlateMesh = NodeGraph.GenerateUnityMeshFromGraph(EUnityMeshMode.VORONOI_FILLED);
		TectPlateMesh.name = "TectonicPlateMesh";

		// get our array of tectonic plate group ids assigned to our cells
		int[] AssignedTectonicPlateCells = CellGroupFloodFill(ELandmassType.TECTONIC, InitialPlatePoints.ToList(), PlateSizes, PlateColours, TectPlateMesh, false);

		Texture2D PlateTexMap = TextureGenerator.GenerateTectonicPlateTextureMap(NodeGraph.GetNumCells(), AssignedTectonicPlateCells, PlateColours);

		TectonicPlatesRTex = MapUtils.RenderPolygonalMap(TectPlateMesh, MapSettings.MapSize, PlateTexMap, TextureGenerator.GetUnlitTextureMaterial(), true);	
	}

	/*
	 * Functions for Continents and Plates are similar atm, but might change later,
	 * no need to code golf it to just one function when might customize it more later.
	 */
	int[] DetermineInitialTectonicPlatePoints(int InNumPlates, Vector2Int InPadding, bool bIsRandom)
	{
		Debug.Log("Determining initial Tectonic Plate Points..."); 
		// Select a number of sites that are evenly spread
		List<int> OutPlateSites = new List<int>();

		// Get Initial Sample Sites either Randomly or via Poisson Disc Sampling
		int PoissonRadius = MapUtils.DetermineRadiusForPoissonDisc(MapSettings.MapSize, InNumPlates);
		ESiteDistribution PlateSiteDistribution = ESiteDistribution.RANDOM;
		List<Vector3> InitialSamplePoints = MapUtils.GenerateSiteDistribution(
			PlateSiteDistribution,
			InNumPlates,
			MapSettings.MapSize,
			InPadding.x,
			PoissonRadius,
			null
		);

		foreach (var coord in InitialSamplePoints)
		{
			int ResultCellID = NodeGraph.GetCellIDFromCoordinate(coord);
			if (ResultCellID >= 0)
			{
				OutPlateSites.Add(ResultCellID);
			}
		}

		return OutPlateSites.ToArray();
	}

	/// <summary>
	/// Generate N-Groups of Cells from the Graph via Random Flood Fill
	/// </summary>
	/// <param name="InInitialCells">The initial selection of cells to flood fill from</param>
	/// <param name="InTargetSizes">Target size of the passed in groups</param>
	/// <returns>List of Tectonic Plate IDs (starting from 1, 0 is reserved) for each Cell ID</returns>
	public int[] CellGroupFloodFill(ELandmassType InLandtype, List<int> InInitialCells, List<int> InTargetSizes, List<Color> InCellColours, Mesh InCellMesh, bool InDebugEnabled)
	{
		/*
		 * Basic idea is we want to randomly fill the continents so they form interesting
		 * shapes without being too random or too uniform.
		 * 
		 * For additional customization we can also specify a maximum or target size per group
		 */
		if (NodeGraph == null)
			return null;

		string landtype = "Unknown";
		switch (InLandtype)
		{
			case ELandmassType.CONTINENT:
			{
				landtype = "Continent";
				break;
			}
			case ELandmassType.TECTONIC:
			{
				landtype = "Tectonic";
				break;
			}
		}
		Debug.Log("Flood Filling Landmass: " + landtype + " Cells...");

		int MaxCells = NodeGraph.GetNumCells();
		if (MaxCells <= 0)
		{
			Debug.LogError("Error: Map is invalid, Cell count <= 0");
			return null;
		}

		int NumCellGroups = InInitialCells.Count;
		HashSet<int> ClosedList = new HashSet<int>();
		Heap<PriorityVCell> Frontier = new Heap<PriorityVCell>(MaxCells);
		int[] AssignedCells = new int[MaxCells];
		// 
		for (int i = 0; i < MaxCells; i++)
		{
			AssignedCells[i] = UNASSIGNED;
		}

		int[] GroupCellCounter = new int[NumCellGroups];

		// fill our frontier with our initial sites
		for (int i = 0; i < NumCellGroups; i++)
		{
			Frontier.Add(new PriorityVCell(InInitialCells[i], InInitialCells[i], i));
			AssignedCells[InInitialCells[i]] = i;
			ClosedList.Add(InInitialCells[i]);
			GroupCellCounter[i] = 1;
		}

		int Iteration = 0;

		while (Frontier.Count > 0)
		{
			PriorityVCell CurrentCell = Frontier.RemoveFirst();
			if (CurrentCell == null)
			{
				continue;
			}

			int CurrentFaceIndex = CurrentCell.CellIndex;
			int CurrentCellGroupID = AssignedCells[CurrentCell.CellParentIndex];
			// In case we have fake faces inserted by our voronoi library
			// We'll keep popping until we have another valid one.
			while (CurrentFaceIndex < 0)
			{
				PriorityVCell PoppedCell = Frontier.RemoveFirst();
				if (PoppedCell != null)
				{
					CurrentFaceIndex = PoppedCell.CellIndex;
				}
			}

			// Check if the current grouping is full
			AssignedCells[CurrentFaceIndex] = CurrentCellGroupID;
			GroupCellCounter[CurrentCellGroupID]++; // increment cell group
			if (GroupCellCounter[CurrentCellGroupID ] >= InTargetSizes[CurrentCellGroupID])
			{
				continue;
			}

			// add all adjacent cells to the frontier
			VCell CellData = NodeGraph.Cells[CurrentFaceIndex];
			foreach (var NeighbourCell in CellData.Neighbours)
			{
				VCell Neighbour = NeighbourCell as VCell;
				if (Neighbour == null)
					continue;

				if (!ClosedList.Contains(Neighbour.ID))
				{
					int Rank = Random.Range(0, int.MaxValue);
					Frontier.Add(new PriorityVCell(Neighbour.ID, CurrentFaceIndex, Rank));
					ClosedList.Add(Neighbour.ID);
				}
			}

			// Debug Images
			if (InDebugEnabled && (Iteration % DebugFloodfillIterationStep == 0))
			{				
				if (InCellMesh != null)
				{
					Texture2D floodfillDebugTexture = null;
					switch (InLandtype)
					{
						case ELandmassType.CONTINENT:
						{
							Debug.Log("Generating Continental Texture Mapping Texture...");
							floodfillDebugTexture = TextureGenerator.GenerateContinentalTextureMap(MaxCells, AssignedCells, InCellColours);
							break;
						}
						case ELandmassType.TECTONIC:
						{
							Debug.Log("Generating Tectonic Texture Mapping Texture...");
							floodfillDebugTexture = TextureGenerator.GenerateTectonicPlateTextureMap(MaxCells, AssignedCells, InCellColours);
							break;
						}
					}

					string relative_filenames = InCellMesh.name + "Debug/" + InCellMesh.name + Iteration;
					Debug.Log("Rendering Polygonal Map, Iteration: " + Iteration);
					RenderTexture DebugRTex = MapUtils.RenderPolygonalMap(relative_filenames, MapSettings.MapSize, InCellMesh,
						floodfillDebugTexture,
						TextureGenerator.GetUnlitTextureMaterial(),
						InDebugEnabled
					);

					MapUtils.SaveMapAsPNG(relative_filenames, DebugRTex);
				}
			}

			Iteration++;
		}

		return AssignedCells;
	}

	RenderTexture RenderArrows(Vector2Int InMapSize, Mesh InMapMesh, Color InArrowColour, bool InIsDebug)
	{
		RenderTexture ArrowMapRT = null;
		Material MeshMaterial = TextureGenerator.GetUnlitMaterial();
		if (MeshMaterial != null)
		{
			MeshMaterial.SetColor("_Color", InArrowColour);
			ArrowMapRT = TextureGenerator.BlitMeshToRT(InMapMesh, InMapSize, MeshMaterial, false, true);
			if (InIsDebug)
			{
				TextureGenerator.SaveMapAsPNG("RenderArrowMapTestV2", ArrowMapRT);
			}
		}

		return ArrowMapRT;
	}

	Mesh DrawTectonicPlateCellArrows()
	{
		if (NodeGraph == null)
			return null;

		Mesh mesh = new Mesh();
		List<Vector3> ArrowVertices = new List<Vector3>();
		List<int> ArrowTriangleIndices = new List<int>();
		List<Vector2> ArrowUVs = new List<Vector2>();
		int TriangleOffset = 0;
		float ArrowRotation = 0;

		EPlateDirections CellDirection = EPlateDirections.NORTH;
		Vector3 ArrowLocation = Vector3.zero;

		VCell[] Cells = NodeGraph.Cells;
		int NumCells = Cells.Length;

		for (int i = 0; i < NumCells; i++) 
		{
			// get arrow location from node graph
			ArrowLocation = Cells[i].Centroid.Coords;
			switch (CellDirection)
			{
				case EPlateDirections.NORTH:
					ArrowRotation = 90;
					break;
				case EPlateDirections.NORTHEAST:
					ArrowRotation = 45;
					break;
				case EPlateDirections.EAST:
					ArrowRotation = 0;
					break;
				case EPlateDirections.SOUTHEAST:
					ArrowRotation = 315;
					break;
				case EPlateDirections.SOUTH:
					ArrowRotation = 270;
					break;
				case EPlateDirections.SOUTHWEST:
					ArrowRotation = 225;
					break;
				case EPlateDirections.WEST:
					ArrowRotation = 180;
					break;
				case EPlateDirections.NORTHWEST:
					ArrowRotation = 135;
					break;
			}

			DebugMapUtils.GetArrowMeshV2(2, 6, 3, 4, ArrowLocation, ArrowRotation, ref ArrowVertices, ref ArrowTriangleIndices, ref ArrowUVs, ref TriangleOffset);
		}

		mesh.SetVertices(ArrowVertices);
		mesh.SetTriangles(ArrowTriangleIndices, 0);
		mesh.SetUVs(0, ArrowUVs);
		mesh.RecalculateNormals();

		return mesh;
	}
}