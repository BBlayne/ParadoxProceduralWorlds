using UnityEngine;
using TVertex = TriangleNet.Geometry.Vertex;

public static class Vector3Extensions
{
	public static Vector3 ToTVertex(this TVertex v)
	{
		return new Vector3((float)v.X, (float)v.Y, 0.0f);
	}
	
	public static Vector3 FromTVertex(this Vector3 _, TVertex v)
	{
		return new Vector3((float)v.X, (float)v.Y, 0.0f);
	}
}