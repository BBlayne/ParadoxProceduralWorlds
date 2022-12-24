using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ContinentInfo
{
    public int NumCells;
}

public enum EPlateDirections
{
    NORTH = 0,
    NORTHEAST,
    EAST,
    SOUTHEAST,
    SOUTH,
    SOUTHWEST,
    WEST,
    NORTHWEST,
    NUM_DIRECTIONS
}

public struct Region
{
    public int RegionID;
    public int Label;
    public ERegionType RegionType;
    public Color RegionColour;
    public List<Coord> Coords;
    public int MinX;
    public int MaxX;
    public int MinY;
    public int MaxY;
}

public struct Coord
{
    public int x;
    public int y;

    public Coord(int InX, int InY)
    {
        x = InX;
        y = InY;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)2166136261;
            hash = (hash * 16777619) ^ x.GetHashCode();
            hash = (hash * 16777619) ^ y.GetHashCode();
            return hash;
        }
    }
}

// A regions type is either Land or Water (Sea/Ocean).
public enum ERegionType
{
    Land,
    Water
}

public enum ELandType
{
    Lake,
    InlandSea,
    Ocean,
    Island,
    Continent
}

public enum EProvinceTerrainType
{
    Wasteland,
    Farmland,
    Plains,
    Desert,
    Tundra,
    Jungle,
    Hill,
    Mountain
}

// maybe to have a general single type to aggregate world
// data together into for ease of reference?
public class WorldShape
{
    public List<Region> Regions;
    public int[] Labels;
}