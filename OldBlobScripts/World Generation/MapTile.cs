using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
public enum HeightType
{
    DeepWater = 1,
    ShallowWater = 2,
    Shore = 3,
    Sand = 4,
    Grass = 5,
    Forest = 6,
    Rock = 7,
    Snow = 8
}
*/

public class MapTile
{
    public int HeightTypeID; // corresponds to terraintype element
    public float HeightValue { get; set; }
    public int X, Y;
    public int Bitmask;

    public MapTile Left;
    public MapTile Right;
    public MapTile Top;
    public MapTile Bottom;

    public bool Collidable;
    public bool FloodFilled;

    public MapTile()
    {

    }

    public void UpdateBitmask()
    {
        int count = 0;

        if (Top.HeightTypeID == HeightTypeID)
            count += 1;
        if (Right.HeightTypeID == HeightTypeID)
            count += 2;
        if (Bottom.HeightTypeID == HeightTypeID)
            count += 4;
        if (Left.HeightTypeID == HeightTypeID)
            count += 8;

        Bitmask = count;
    }
}
