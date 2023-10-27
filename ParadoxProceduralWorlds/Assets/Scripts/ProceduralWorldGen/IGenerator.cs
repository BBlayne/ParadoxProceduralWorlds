using System.Collections;
using System.Collections.Generic;

public interface IGeneratorSettings
{

}

public interface IGenerator
{
    IGraphMap Generate(IGeneratorSettings InSettings);
    IGraphMap Generate(IGeneratorSettings InSettings, IGraphMap InGraphMap);
}
