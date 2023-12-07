using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGraphNode
{
    void AddNeighbour(IGraphNode neighbourToAdd);
    HashSet<IGraphNode> GetNeighbours();

    void SetParent(IGraphNode parentToSet);
    IGraphNode GetParent();
}
