

public class TNetNodeGraphFactory : INodeGraphFactory<TriangleNetTriangulator>
{
	public TriangleNetTriangulator Triangulator { get; set; }

	public INodeGraph GenerateNodeGraph()
	{
		if (Triangulator == null)
			return null;



		return null;
	}
}