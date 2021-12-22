using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Voronoi;

public static class RegionDebug
{
    public static Texture DebugDrawConstainedDelaunayRegion(List<Vector3> pixels, Texture srcTex, Color inColour)
    {
        return TextureGenerator.DrawPixelsToTexture(srcTex, pixels, inColour);
    }

    public static Texture2D DisplayRegionEdgePixels(List<Vector3> edgePixels, Texture2D inScaledRegion, Color inEdgeColour)
    {
        Unity.Collections.NativeArray<Color32> pixels = inScaledRegion.GetRawTextureData<Color32>();

        foreach (var pixel in edgePixels)
        {
            pixels[(int)pixel.y * inScaledRegion.width + (int)pixel.x] = inEdgeColour;
        }

        inScaledRegion.Apply();

        return inScaledRegion;
    }

    public static List<Vector3> DebugConstrainedDelaunayMeshToPixels(Mesh inDelaunayMesh, Mesh inHull2DMesh)
    {
        List<Vector3> pixels = new List<Vector3>();

        // delaunay mesh
        var indices = inDelaunayMesh.GetIndices(0);
        var vertices = inDelaunayMesh.vertices;

        var triangleVertices = new Vector3[3];
        for (var i = 0; i < indices.Length; i += 3)
        {
            for (var j = 0; j < 3; j++)
            {
                pixels.Add(vertices[indices[i + j]]);
                triangleVertices[j] = vertices[indices[i + j]];
            }

            pixels.AddRange(DrawWorldShape.DrawLine2D(triangleVertices[0], triangleVertices[1]));
            pixels.AddRange(DrawWorldShape.DrawLine2D(triangleVertices[1], triangleVertices[2]));
            pixels.AddRange(DrawWorldShape.DrawLine2D(triangleVertices[2], triangleVertices[0]));
        }

        // boundary
        for (int i = 0; i < inHull2DMesh.vertices.Length; i++)
        {
            var startvertex = inHull2DMesh.vertices[i];
            var endvertex = inHull2DMesh.vertices[(i + 1) % inHull2DMesh.vertices.Length];
            pixels.AddRange(DrawWorldShape.DrawLine2D(startvertex, endvertex));
        }

        return pixels;
    }

    public static Texture2D DebugRegionSites(RegionGenerator.RegionDebugInfo inRegionDebugInfo, Texture2D inWorldTexture)
    {
        Unity.Collections.NativeArray<Color32> colourData = inWorldTexture.GetRawTextureData<Color32>();

        List<Coord> points = new List<Coord>();

        // draw sites
        foreach (var face in inRegionDebugInfo.regionVoronoiGraph.Faces)
        {
            colourData[(int)face.generator.Y * inWorldTexture.width + (int)face.generator.X] = new Color32(255, 0, 0, 255);

            if (((int)face.generator.Y < (inWorldTexture.height - 1) && (int)face.generator.Y > 1) &&
                ((int)face.generator.X < (inWorldTexture.width - 1) && (int)face.generator.X > 1))
            {
                colourData[((int)face.generator.Y + 1) * inWorldTexture.width + (int)face.generator.X] = new Color32(255, 0, 0, 255);
                colourData[((int)face.generator.Y - 1) * inWorldTexture.width + (int)face.generator.X] = new Color32(255, 0, 0, 255);
                colourData[(int)face.generator.Y * inWorldTexture.width + (int)face.generator.X - 1] = new Color32(255, 0, 0, 255);
                colourData[(int)face.generator.Y * inWorldTexture.width + (int)face.generator.X + 1] = new Color32(255, 0, 0, 255);
            }

            var temp = face.Edge;
            var startEdge = face.Edge;
            while (temp.Next != null && temp.Next != startEdge)
            {
                colourData[(int)temp.Origin.Y * inWorldTexture.width + (int)temp.Origin.X] = new Color32(255, 255, 0, 255);
                points = DrawWorldShape.DrawLine
                (
                    new Coord((int)temp.Origin.X, (int)temp.Origin.Y),
                    new Coord((int)temp.Next.Origin.X, (int)temp.Next.Origin.Y)
                );

                foreach (Coord pixel in points)
                {
                    colourData[pixel.y * inWorldTexture.width + pixel.x] = new Color32(255, 0, 0, 255);
                }

                temp = temp.Next;
            }

            // repeat once more for last edge between penultimate point and origin point
            if (temp.Next != null)
            {
                points = DrawWorldShape.DrawLine
                (
                    new Coord((int)temp.Origin.X, (int)temp.Origin.Y),
                    new Coord((int)temp.Next.Origin.X, (int)temp.Next.Origin.Y)
                );

                foreach (Coord pixel in points)
                {
                    colourData[pixel.y * inWorldTexture.width + pixel.x] = new Color32(255, 0, 0, 255);
                }
            }

        }

        // draw inputs...?
        foreach (var point in inRegionDebugInfo.mesh.vertices)
        {
            colourData[((int)point.Value.Y) * inWorldTexture.width + (int)point.Value.X] = new Color32(0, 255, 0, 255);
            if (((int)point.Value.Y < (inWorldTexture.height - 1) && (int)point.Value.Y > 1) &&
                ((int)point.Value.X < (inWorldTexture.width - 1) && (int)point.Value.X > 1))
            {
                colourData[((int)point.Value.Y + 1) * inWorldTexture.width + (int)point.Value.X] = new Color32(0, 255, 0, 255);
                colourData[((int)point.Value.Y - 1) * inWorldTexture.width + (int)point.Value.X] = new Color32(0, 255, 0, 255);
                colourData[((int)point.Value.Y) * inWorldTexture.width + (int)point.Value.X - 1] = new Color32(0, 255, 0, 255);
                colourData[((int)point.Value.Y) * inWorldTexture.width + (int)point.Value.X + 1] = new Color32(0, 255, 0, 255);
            }
        }

        inWorldTexture.Apply();

        SaveMapAsPNG("DebugRegionSites", inWorldTexture);

        return inWorldTexture;
    }

    static async void SaveMapAsPNG(string InFileName, Texture2D InTex)
    {
        if (InTex != null)
        {
            // attempting C# aync functionality
            await TextureGenerator.SaveTextureAsPng(
                InTex,
                Application.dataPath + "/Temp/",
                InFileName + ".png"
            );
        }
    }
}
