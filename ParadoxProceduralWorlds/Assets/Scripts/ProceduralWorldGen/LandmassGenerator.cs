using DataStructures.ViliWonka.KDTree;
using System.Collections.Generic;
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

public class LandmassGenerator : IMapGenerator<PolygonalNodeGraph>
{
	PolygonalNodeGraph NodeGraph;

	public Mesh CellMesh {  get; private set; }

	public bool DebugMapEnabled {  get; set; }

	public MapSetting MapSettings { get; set; }

	public KDTree VSiteKDTree = null;

	public LandmassGenerator()
	{

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



		return InGraph;
	}

	void GenerateContinents(int InNumContinents, int InPadding, float LandWaterRatio)
	{

	}

	/*
	 * Functions for Continents and Plates are similar atm, but might change later,
	 * no need to code golf it to just one function when might customize it more later.
	 */
	int[] DetermineInitialContinentCells(int InNumContinents, int InPadding)
	{
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

		int[] CellsToFill = CellGroupFloodFill(InitialPlatePoints.ToList(), PlateSizes);
	}

	/*
	 * Functions for Continents and Plates are similar atm, but might change later,
	 * no need to code golf it to just one function when might customize it more later.
	 */
	int[] DetermineInitialTectonicPlatePoints(int InNumPlates, Vector2Int InPadding, bool bIsRandom)
	{
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
	/// <returns></returns>
	public int[] CellGroupFloodFill(List<int> InInitialCells, List<int> InTargetSizes)
	{
		/*
		 * Basic idea is we want to randomly fill the continents so they form interesting
		 * shapes without being too random or too uniform.
		 * 
		 * For additional customization we can also specify a maximum or target size per group
		 */
		if (NodeGraph == null)
			return null;

		int DebugIterationStep = 2;

		// Colours for Visualization purposes aimed for sufficient contrast
		Vector2Int Hues = new Vector2Int(30, 330);
		Vector2Int Saturation = new Vector2Int(99, 100);
		Vector2Int Brightness = new Vector2Int(99, 100);
		List<Color> DebugColours = TextureGenerator.GenerateHSVColours(InInitialCells.Count + 1, Hues, Saturation, Brightness);
		DebugColours[0] = Color.black;

		int MaxCells = NodeGraph.GetNumCells();
		if (MaxCells <= 0)
		{
			Debug.LogError("Error: Map is invalid, Cell count <= 0");
			return null;
		}

		int NumCellGroups = InInitialCells.Count;
		HashSet<int> ClosedList = new HashSet<int>();
		Heap<PriorityVCell> Frontier = new Heap<PriorityVCell>(MaxCells);
		int[] CellsToBeFilled = new int[MaxCells];
		int[] GroupCellCounter = new int[NumCellGroups];

		for (int i = 0; i < NumCellGroups; i++)
		{
			Frontier.Add(new PriorityVCell(InInitialCells[i], InInitialCells[i], i));
			CellsToBeFilled[InInitialCells[i]] = i + 1;
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
			int CurrentCellGroupID = CellsToBeFilled[CurrentCell.CellParentIndex];
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
			CellsToBeFilled[CurrentFaceIndex] = CurrentCellGroupID;
			if (GroupCellCounter[CurrentCellGroupID - 1] >= InTargetSizes[CurrentCellGroupID - 1])
			{
				continue;
			}

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
			if (DebugMapEnabled && (Iteration % DebugIterationStep == 0))
			{
				if (CellMesh != null)
				{
					MapUtils.RenderPolygonalMap("DebugContinents" + Iteration, CellMesh,
						TextureGenerator.GenerateContinentalTextureMap(MaxCells, CellsToBeFilled, DebugColours),
						TextureGenerator.GetUnlitTextureMaterial()
					);
				}
			}

			Iteration++;
		}

		return CellsToBeFilled;
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