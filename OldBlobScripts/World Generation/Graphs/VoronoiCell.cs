using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using System.Linq;

public struct CellData
{
    public Color cellColour;
    public bool isLand;
    public bool isBranch;
    public HashSet<VoronoiCell> friends;
    public HashSet<VoronoiCell> enemies;
    public HashSet<VoronoiCell> landCellNeighbours;
    public HashSet<VoronoiCell> seaCellNeighbours;
}

public class VoronoiCell : GraphNode<CellData> {
    public Vertex SiteCoord = new Vertex(0, 0);
    public HashSet<IGraphNode> Neighbours;
    public TileBlob parent = null;
    public int ID = -1;

    public VoronoiCell(Vertex site)
    {
        Neighbours = new HashSet<IGraphNode>();
        this.SiteCoord = site;
        this.GraphData = new CellData();
        this.GraphData.isBranch = false;
        this.GraphData.friends = new HashSet<VoronoiCell>();
        this.GraphData.enemies = new HashSet<VoronoiCell>();
        this.GraphData.seaCellNeighbours = new HashSet<VoronoiCell>();
        this.GraphData.landCellNeighbours = new HashSet<VoronoiCell>();
        if (this.SiteCoord != null)
            this.ID = SiteCoord.ID;
    }

    public override void AddNeighbour(IGraphNode neighbour)
    {
        Neighbours.Add(neighbour as VoronoiCell);
    }

    public override HashSet<IGraphNode> GetNeighbours()
    {
        return Neighbours;
    }

    public override void SetParent(IGraphNode parentToSet)
    {
        parent = parentToSet as TileBlob;
    }

    public override IGraphNode GetParent()
    {
        return parent;
    }

    public override bool Equals(object obj)
    {
        var cell = obj as VoronoiCell;
        return cell != null &&
               ID == cell.ID;
    }

    public override int GetHashCode()
    {
        return 1213502048 + ID.GetHashCode();
    }
}
