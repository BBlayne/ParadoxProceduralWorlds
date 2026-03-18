using System.Collections;
using System.Collections.Generic;

// Either an edge that forms part of a boundary of a node,
// or links two nodes together
public interface INodeEdge
{
	public int ID { get; set; }
	public INode Start { get; set; }
	public INode End { get; set; }
}

public interface INodeData
{

}

public interface INode
{
	public INodeData Data { get; set; }
	public List<INode> GetNeighbours();
	public List<INode> Neighbours { get; set; }
}

public interface INodeGraph
{

}

public interface INodeGraphFactory<TTriangulator> where TTriangulator : ITriangulator
{
	public TTriangulator Triangulator { get; set; }
	public INodeGraph GenerateNodeGraph();
}