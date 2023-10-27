using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonalMapGeneratorSettings : IGeneratorSettings
{
    public string SeedString = "Blayne";
    [Range(10, 60)]
    public int NumPoissonSamples = 30;
}

public class PolygonMapCell : IMapCell
{
    public PolygonMapCell() { }

    public ICellData CellData { get; }

    public int CellID { get; }

    public List<IMapCell> GetNeighbours(bool bIsWrappableMap = false)
    {
        return new List<IMapCell>();
    }
}

public class PolygonalGraphMap : IGraphMap
{
    List<IMapCell> mapCells;

    public PolygonalGraphMap()
    {
        mapCells = new List<IMapCell>();
    }

    public int GetMapCellCount()
    {
        return 0;
    }
    public List<IMapCell> GetMapCells()
    {
        return null;
    }
    public IMapCell GetMapCell(int InIndex)
    {
        return null;
    }
    public List<IMapCell> GetMapCellNeighbours(IMapCell InCell, bool bIsWrappableMap = false)
    {
        return null;
    }
    public List<IMapCell> GetMapCellNeighbours(int InCellIndex, bool bIsWrappableMap = false)
    {
        return null;
    }
}

public class PolygonalGraphMapGenerator : IGenerator
{
    public IGraphMap Generate(IGeneratorSettings InSettings)
    {
        return null;
    }

    public IGraphMap Generate(IGeneratorSettings InSettings, IGraphMap InGraphMap)
    {
        return null;
    }
}
