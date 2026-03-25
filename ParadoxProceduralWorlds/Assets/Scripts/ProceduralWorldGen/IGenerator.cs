using System.Collections;
using System.Collections.Generic;

public interface IGeneratorSettings
{

}

public interface IGenerator<TNodeGrapher, TTriangulator> 
	where TTriangulator : ITriangulator
	where TNodeGrapher : INodeGraphFactory<TTriangulator>
{
	TNodeGrapher NodeGraphFactory { get; set; }
	INodeGraph Generate();
	INodeGraph Generate(INodeGraph InGraphMap);
}
