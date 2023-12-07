using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ImprovedPerlinNoiseProject;

public class HeightMapGenerator
{
    private ShapeGenerator worldShapeGenerator = null;

    public HeightMapGenerator(ShapeGenerator _shapeGen)
    {
        worldShapeGenerator = _shapeGen;
    }

    // Gets the height map, most of the work
    // is done by ShapeGenerator
    // currently only gets the first one
    public Texture2D GenerateHeightMap()
    {
        return worldShapeGenerator.GetLayerElevationTexture(0);
    }
}
