using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum TileLayers
{
    LAND = 0,
    WATER,
    UNKNOWN
};

public struct TileData
{
    public bool isLand;
    public TileLayers LayerID;
}

/*
 * A TileBlob represents a "tile" during world map generations
 * which contains one or more voronoi cells.
 * 
 * All cells inside a tileblob form a connected graph.
 * 
 * A tileblob has an ID which corresponds to it's assigned
 * "colour" for map representation purposes.
 * 
 * A TileBlob probably has neighbouring TileBlobs which it
 * is adjacent to, forming a sort of supergraph.
 * 
 */
public class TileBlob : GraphNode<TileData>
{
    private static int _id = -1;

    public int ID { get; } = -1;

    public readonly Color TileColour;

    public HashSet<IGraphNode> Neighbours;
    List<IGraphNode> Children;
    public HashSet<IGraphNode> BorderCells;
    IGraphNode parent = null;
    public Vector2 center = new Vector2(0, 0);

    public TileBlob(Color colour)
    {
        _id++;
        this.ID = _id;
        TileColour = colour;
        Neighbours = new HashSet<IGraphNode>();
        GraphData = new TileData();
        BorderCells = new HashSet<IGraphNode>();
        Children = new List<IGraphNode>();
    }

    public void RecalculateCenter()
    {
        // for each child cell calculate
        // center point which is average of all
        // cells within the blob.
        center = Vector2.zero;
        foreach (VoronoiCell node in Children.Cast<VoronoiCell>().ToList())
        {
            Vector2 node_point = new Vector2((float)node.SiteCoord.X, (float)node.SiteCoord.Y);
            center += node_point;            
        }

        center /= Children.Count;
    }

    public override void AddNeighbour(IGraphNode tileblob)
    {
        Neighbours.Add(tileblob as TileBlob);
    }

    public override HashSet<IGraphNode> GetNeighbours()
    {
        return Neighbours;
    }

    public void AddChildNode(IGraphNode child)
    {
        Children.Add(child);
        child.SetParent(this);
    }

    public List<IGraphNode> GetChildren()
    {
        return Children;
    }

    public override void SetParent(IGraphNode parentToSet)
    {
        parent = parentToSet;
    }

    public override IGraphNode GetParent()
    {
        return parent;
    }

    public override bool Equals(object obj)
    {
        var blob = obj as TileBlob;
        return blob != null &&
               ID == blob.ID;
    }

    public override int GetHashCode()
    {
        return 1603364281 + ID.GetHashCode();
    }
}
