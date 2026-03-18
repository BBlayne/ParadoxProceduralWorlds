using System.Collections;
using System.Collections.Generic;

public interface IGeneratorSettings
{

}

public interface IGenerator
{
	INodeGraph Generate(IGeneratorSettings InSettings);
	INodeGraph Generate(IGeneratorSettings InSettings, INodeGraph InGraphMap);
}
