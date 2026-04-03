

using System.Collections.Generic;
using UnityEngine;

public static class DebugMapUtils
{
	public static void GetArrowMeshV2
	(
		float InStemWidth, 
		float InTipWidth, 
		float InTipLength, 
		float InStemLength,
		Vector3 InPosition,
		float InRotationAngle,
		ref List<Vector3> InOutVertices,
		ref List<int> InOutTriangles,
		ref List<Vector2> InOutUVs,
		ref int InOutVertexIndex
	)
	{
		// Generates an Arrow that's centered near the approximate center of gravity, 
		// Instead of being centered about the origin of the stem.

		// setup
		// Stem Setup
		Vector3 StemOrigin = Vector3.zero;
		float StemHalfWidth = InStemWidth / 2;
		float StemHalfLength = InStemLength / 2;

		// compute our initial vertex positions
		Vector3 p0 = StemOrigin + (StemHalfWidth * Vector3.down) - (StemHalfLength * Vector3.right);
		Vector3 p1 = StemOrigin + (StemHalfWidth * Vector3.up) - (StemHalfLength * Vector3.right);
		Vector3 p2 = p0 + (InStemLength * Vector3.right);
		Vector3 p3 = p1 + (InStemLength * Vector3.right);

		// rotate them relative to the origin
		p0 = Quaternion.Euler(0, 0, InRotationAngle) * p0;
		p1 = Quaternion.Euler(0, 0, InRotationAngle) * p1;
		p2 = Quaternion.Euler(0, 0, InRotationAngle) * p2;
		p3 = Quaternion.Euler(0, 0, InRotationAngle) * p3;

		// translate them to their position
		p0 += InPosition;
		p1 += InPosition;
		p2 += InPosition;
		p3 += InPosition;

		// Add Stem Points to Vertex List
		InOutVertices.Add(p0);
		InOutVertices.Add(p1);
		InOutVertices.Add(p2);
		InOutVertices.Add(p3);

		// Stem Triangles
		InOutTriangles.Add(InOutVertexIndex + 0);
		InOutTriangles.Add(InOutVertexIndex + 1);
		InOutTriangles.Add(InOutVertexIndex + 3);

		InOutTriangles.Add(InOutVertexIndex + 0);
		InOutTriangles.Add(InOutVertexIndex + 3);
		InOutTriangles.Add(InOutVertexIndex + 2);

		// Tip setup
		Vector3 TipOrigin = StemHalfLength * Vector3.right;
		float TipHalfWidth = InTipWidth / 2;

		// Set Tip Point positions
		Vector3 p4 = TipOrigin + (TipHalfWidth * Vector3.up);
		Vector3 p5 = TipOrigin + (TipHalfWidth * Vector3.down);
		Vector3 p6 = TipOrigin + (InTipLength * Vector3.right);

		// rotate them
		p4 = Quaternion.Euler(0, 0, InRotationAngle) * p4;
		p5 = Quaternion.Euler(0, 0, InRotationAngle) * p5;
		p6 = Quaternion.Euler(0, 0, InRotationAngle) * p6;

		// translate them
		p4 += InPosition;
		p5 += InPosition;
		p6 += InPosition;

		InOutVertices.Add(p4);
		InOutVertices.Add(p5);
		InOutVertices.Add(p6);

		// tip triangle
		InOutTriangles.Add(InOutVertexIndex + 4);
		InOutTriangles.Add(InOutVertexIndex + 6);
		InOutTriangles.Add(InOutVertexIndex	+ 5);

		InOutUVs.Add(Vector2.zero);
		InOutUVs.Add(Vector2.up);
		InOutUVs.Add(Vector2.right);
		InOutUVs.Add(Vector2.one);

		InOutVertexIndex += 7;
	}
}
