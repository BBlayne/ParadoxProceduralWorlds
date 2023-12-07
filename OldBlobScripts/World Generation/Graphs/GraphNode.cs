using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GraphNode<T> : IGraphNode
{

    public float gCost;
    public float hCost;

    public float fCost {
        get {
            return gCost + hCost;
        }
    }

    public T GraphData;

    public abstract void AddNeighbour(IGraphNode neighbourToAdd);
    public abstract HashSet<IGraphNode> GetNeighbours();

    public abstract void SetParent(IGraphNode parentToSet);
    public abstract IGraphNode GetParent();
}
