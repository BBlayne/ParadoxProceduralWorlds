using System.Collections;
using System.Collections.Generic;

public interface ICellData
{

}

public interface IMapCell
{
    ICellData CellData { get; }
    int CellID { get; }
    List<IMapCell> GetNeighbours(bool bIsWrappableMap = false);
}

public interface IGraphMap
{
    int GetMapCellCount();
    List<IMapCell> GetMapCells();
    IMapCell GetMapCell(int InIndex);
    List<IMapCell> GetMapCellNeighbours(IMapCell InCell, bool bIsWrappableMap = false);
    List<IMapCell> GetMapCellNeighbours(int InCellIndex, bool bIsWrappableMap = false);
}
