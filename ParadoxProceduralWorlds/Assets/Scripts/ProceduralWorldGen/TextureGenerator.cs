using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;
using TMPro;
using Jobberwocky.GeometryAlgorithms.Source.API;
using Jobberwocky.GeometryAlgorithms.Source.Core;
using Jobberwocky.GeometryAlgorithms.Source.Parameters;

public static class TextureGenerator
{
    public static string AppPath = Application.dataPath + "/../ExportedImages/";

    private static string shaderPath = "Shaders/TextureUtilShader";
    private static string DrawPixelsToTexShaderPath = "Shaders/DrawPixelsToTexture";
    private static string noShaderMsg = "Could not find the compute shader. Did you move/rename any of the files?";

    private static void SetShaderVars
    (
        ComputeShader InShader,
        Vector4[] InColours,
        int InKernel
    )
    {
        InShader.SetVectorArray("colours", InColours);
    }

    private static int MAX_POINTS = 25;

    private struct RegionFace
    {
        public Coord Site;
        public int Size;
    }

    private struct Edge
    {
        public Vector2 StartPoint;
        public Vector2 EndPoint;
    }

    private struct RegionBound
    {
        public int minX;
        public int maxX;
        public int minY;
        public int maxY;
    }

    public static Texture DrawPixelsToTexture(Texture inSrc, List<Vector3> pixels, Color inColour)
    {
        // our shader
        ComputeShader shader = Resources.Load(DrawPixelsToTexShaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        // our kernel
        int kernel = shader.FindKernel("DrawPixelsToTexture");

        // our input and output textures
        //shader.SetTexture(kernel, "baseTex", inSrc);
        shader.SetTexture(kernel, "resultTex", inSrc);

        // now we're going to do rounds of kernal stuff
        int[] reses = { inSrc.width, inSrc.height };

        // resolution of the output texture
        shader.SetInts("reses", reses);

        // the colour to draw our new pixels
        shader.SetVector("inColour", inColour);

        // length of our compute buffer
        shader.SetInt("inPixelsLength", pixels.Count);

        // setup our structured buffers
        ComputeBuffer pixelBuf = new ComputeBuffer(pixels.Count, 12);
        pixelBuf.SetData(pixels);
        shader.SetBuffer(kernel, "inPixels", pixelBuf);

        shader.Dispatch(kernel, Mathf.CeilToInt(inSrc.width / 16f), Mathf.CeilToInt(inSrc.height / 16f), 1);

        pixelBuf.Release();

        return inSrc;
    }

    public static RenderTexture GetScaledRenderTextureGPU(Texture inSrc, int inNewWidth, int inNewHeight)
    {
        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        int kernel = shader.FindKernel("ScaleImageNN");

        // final result render texture
        RenderTexture outRTex = new RenderTexture(inNewWidth, inNewHeight, 0);
        outRTex.enableRandomWrite = true;
        outRTex.Create();

        // now we're going to do rounds of kernal stuff
        int[] srcResInts = { inSrc.width, inSrc.height };
        int[] dstResInts = { inNewWidth, inNewHeight };        

        shader.SetInts("srcImageReses", srcResInts);
        shader.SetInts("dstImageReses", dstResInts);        

        // set the texture we're writing to
        shader.SetTexture(kernel, "scaledTex", outRTex);
        shader.SetTexture(kernel, "unscaledTex", inSrc);

        shader.Dispatch(kernel, Mathf.CeilToInt(inNewWidth / 16f), Mathf.CeilToInt(inNewHeight / 16f), 1);

        return outRTex;
    }

    public static RenderTexture GetScaledRenderTextureGL(Texture2D inSrc, int inNewWidth, int inNewHeight, FilterMode inFMode)
    {
        Rect texR = new Rect(0, 0, inNewWidth, inNewHeight);

        //We need the source texture in VRAM because we render with it
        inSrc.filterMode = inFMode;
        inSrc.Apply(true);

        RenderTexture rtt = new RenderTexture(inNewWidth, inNewHeight, 32);
        //Set the RTT in order to render to it
        Graphics.SetRenderTarget(rtt);

        //Setup 2D matrix in range 0..1, so nobody needs to care about sized
        GL.LoadPixelMatrix(0, 1, 1, 0);

        //Then clear & draw the texture to fill the entire RTT.
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        Graphics.DrawTexture(new Rect(0, 0, 1, 1), inSrc);

        inSrc.Reinitialize(inNewWidth, inNewHeight);

        inSrc.ReadPixels(texR, 0, 0);

        inSrc.Apply();

        return rtt;
    }

    public static RenderTexture GetRegionSilhouette
    (
        int inGridWidth,
        int inGridHeight,
        Color inSilhouetteColour,
        ref Region inRegion
    )
    {
        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        // In this shader, we colour the mask texture white
        // for all pixels in the current region, leaving it
        // black by default.
        int kernel = shader.FindKernel("FillRegionSilhouette");

        // final result render texture
        RenderTexture outRTex = new RenderTexture(inGridWidth, inGridHeight, 0);
        outRTex.enableRandomWrite = true;
        outRTex.Create();

        // now we're going to do rounds of kernal stuff
        // first round is initializing our masking layer.
        int[] resInts = { inGridWidth, inGridHeight };

        shader.SetVector("silhouetteColour", inSilhouetteColour);
        shader.SetInts("silhouetteReses", resInts);
        shader.SetInt("silhouetteRegionCoordSize", inRegion.Coords.Count);

        // setup our structured buffers
        // Passing in our array of coordinates comprising our region, a Coord is 2 ints (4 bytes each)
        ComputeBuffer coordBuffer = new ComputeBuffer(inRegion.Coords.Count, 8);
        coordBuffer.SetData(inRegion.Coords);
        shader.SetBuffer(kernel, "silhouetteRegionCoords", coordBuffer);

        // set the texture we're writing to
        shader.SetTexture(kernel, "silhouetteTex", outRTex);

        shader.Dispatch(kernel, Mathf.CeilToInt(inGridWidth / 16f), Mathf.CeilToInt(inGridHeight / 16f), 1);

        coordBuffer.Release();

        return outRTex;
    }

    public static RenderTexture BlitDelaunayMapToRT(UnityEngine.Mesh InMesh, Vector2Int InMapSizes, Material InMat)
    {
        RenderTexture outRTex = new RenderTexture(InMapSizes.x, InMapSizes.y, 0);

        Nothke.Utils.RTUtils.BeginPixelRendering(outRTex);
        {
            GL.Clear(true, true, Color.black);
            GL.wireframe = true;

            Nothke.Utils.RTUtils.DrawMesh(outRTex, InMesh, InMat, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
        }
        Nothke.Utils.RTUtils.EndRendering(outRTex);

        GL.wireframe = false;

        return outRTex;
    }

    public static RenderTexture BlitTextToTexture(TMP_Text InTextObj, Vector3 InPosition, Vector2Int InMapSizes, float InSize)
    {
        RenderTexture outRTex = new RenderTexture(InMapSizes.x, InMapSizes.y, 0);

        Nothke.Utils.RTUtils.BeginOrthoRendering(outRTex);
        {
            GL.Clear(true, true, Color.clear);

            Nothke.Utils.RTUtils.DrawTMPText(outRTex, InTextObj, InPosition, InSize);
        }
        Nothke.Utils.RTUtils.EndRendering(outRTex);

        GL.wireframe = false;

        return outRTex;
    }

    public static RenderTexture BlitMeshToRT(Mesh InMesh, Vector2Int InMapSizes, Material InMat, bool InWireframe, bool bIsTransparentBG)
    {
        RenderTexture outRTex = new RenderTexture(InMapSizes.x, InMapSizes.y, 0, RenderTextureFormat.ARGB32);
        GL.wireframe = false;
        Nothke.Utils.RTUtils.BeginPixelRendering(outRTex);
        {
            GL.Clear(true, true, bIsTransparentBG ? Color.clear : Color.black);
            if (InWireframe)
            {
                GL.wireframe = true;
            }            

            Nothke.Utils.RTUtils.DrawMesh(outRTex, InMesh, InMat, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
        }
        Nothke.Utils.RTUtils.EndRendering(outRTex);

        GL.wireframe = false;

        return outRTex;
    }

    public static RenderTexture BlitMeshToRT(Mesh InMesh, Vector2Int InMapSizes, Material InMat)
    {
        RenderTexture outRTex = new RenderTexture(InMapSizes.x, InMapSizes.y, 0);

        Nothke.Utils.RTUtils.BeginPixelRendering(outRTex);
        {
            GL.Clear(true, true, Color.black);
            GL.wireframe = true;

            Nothke.Utils.RTUtils.DrawMesh(outRTex, InMesh, InMat, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
        }
        Nothke.Utils.RTUtils.EndRendering(outRTex);

        GL.wireframe = false;

        return outRTex;
    }

    public static RenderTexture DrawTextureOutline(Texture InTex)
    {
        RenderTexture OutRTex = new RenderTexture(InTex.width, InTex.height, 0);
        OutRTex.enableRandomWrite = true;
        OutRTex.Create();

        // Shader Stuff
        ComputeShader Shader = Resources.Load(shaderPath) as ComputeShader;
        if (Shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }        

        int Kernel = Shader.FindKernel("DrawTextureOutline");

        // Set Variables/Uniforms
        int[] resInts = { InTex.width, InTex.height };
        Shader.SetInts("TexDims", resInts);
        Shader.SetTexture(Kernel, "OutlineTexInput", InTex);
        Shader.SetTexture(Kernel, "OutlineTexOutput", OutRTex);

        Shader.Dispatch(Kernel, Mathf.CeilToInt(InTex.width / 16f), Mathf.CeilToInt(InTex.height / 16f), 1);

        return OutRTex;
    }

    public static RenderTexture ThickenOutlinesInTexture(Texture InTex)
    {
        RenderTexture OutRTex = new RenderTexture(InTex.width, InTex.height, 0);
        OutRTex.enableRandomWrite = true;
        OutRTex.Create();

        // Shader Stuff
        ComputeShader Shader = Resources.Load(shaderPath) as ComputeShader;
        if (Shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        int Kernel = Shader.FindKernel("ThickenLineTexture");

        // Set Variables/Uniforms
        int[] resInts = { InTex.width, InTex.height };
        Shader.SetInts("ThickenLineTextureInputDims", resInts);
        Shader.SetTexture(Kernel, "ThickenLnTexInput", InTex);
        Shader.SetTexture(Kernel, "ThickenLnTexOutput", OutRTex);

        Shader.Dispatch(Kernel, Mathf.CeilToInt(InTex.width / 16f), Mathf.CeilToInt(InTex.height / 16f), 1);

        return OutRTex;
    }

    public static RenderTexture DrawDalaunayMap(UnityEngine.Mesh InMesh, int InMapWidth, int InMapHeight)
    {
        // final result render texture
        RenderTexture outRTex = new RenderTexture(InMapWidth, InMapHeight, 0);

        if (InMesh.vertexCount <= 3)
        {
            return outRTex;
        }

        // Derive list of edges
        List<Edge> Edges = new List<Edge>();
        int[] MeshIndices = InMesh.GetIndices(0);

        Vector3[] triangleVertices = new Vector3[3];
        for (int i = 0; i < MeshIndices.Length; i += 3)
        {
            for (var j = 0; j < 3; j++)
            {
                triangleVertices[j] = InMesh.vertices[MeshIndices[i + j]];
            }

            for (int k = 0; k < triangleVertices.Length; k++)
            {
                var startVertex = triangleVertices[k];
                var endVertex = triangleVertices[(k + 1) % triangleVertices.Length];

                Edge edge;
                edge.StartPoint = new Vector2(Mathf.Round(startVertex.x), Mathf.Round(startVertex.y));
                edge.EndPoint = new Vector2(Mathf.Round(endVertex.x), Mathf.Round(endVertex.y));

                Edges.Add(edge);
            }
        }

        outRTex.enableRandomWrite = true;
        outRTex.Create();

        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        int[] resInts = { InMapWidth, InMapHeight };
        int[] Sizes = { InMesh.vertexCount, Edges.Count };
        Debug.Log("Edges: " + Edges.Count);

        int kernel = shader.FindKernel("DrawDelaunayTexture");

        shader.SetInts("Dims", resInts);
        shader.SetInts("Sizes", Sizes);
        shader.SetTexture(kernel, "DelaunayTexOutput", outRTex);

        // Passing in our array of coordinates comprising the vertices of our delaunay triangulation
        // a Coord is 2 ints (4 bytes each)
        int Stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3)); // should be 12
        Debug.Log("Vertex (Vector3) Stride: " + Stride);
        ComputeBuffer PointsBuffer = new ComputeBuffer(InMesh.vertexCount, Stride);
        PointsBuffer.SetData(InMesh.vertices);
        shader.SetBuffer(kernel, "Points", PointsBuffer);

        // Need to pass in array of edges, an edge is two points, which is:
        // 2 ints (4 bytes each) * two, should be 16
        int EdgeStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Edge)); // should be 16
        Debug.Log("Edge Stride: " + EdgeStride);
        ComputeBuffer EdgesBuffer = new ComputeBuffer(Edges.Count, EdgeStride);
        EdgesBuffer.SetData(Edges.ToArray());
        shader.SetBuffer(kernel, "Edges", EdgesBuffer);

        shader.Dispatch(kernel, Mathf.CeilToInt(InMapWidth / 16f), Mathf.CeilToInt(InMapHeight / 16f), 1);

        PointsBuffer.Release();
        EdgesBuffer.Release();

        return outRTex;
    }

    public static RenderTexture DrawDelaunayRegion
    (
        int inGridWidth,
        int inGridHeight,
        ref Region inRegion,
        RegionGenerator.RegionDebugInfo inDebugInfo
    )
    {
        return new RenderTexture(0, 0, 0);
    }

    public static RenderTexture FillVoronoiRegion
    (
        int inGridWidth,
        int inGridHeight,
        ref Region inRegion,
        RegionGenerator.RegionDebugInfo inDebugInfo
    ) 
    {
        return new RenderTexture(0, 0, 0);
    }

    public static RenderTexture FillVoronoiRegion
    (
        int inGridWidth,
        int inGridHeight,
        ref Region inRegion,
        TriangleNet.Topology.DCEL.Face[] inRegionFaces
    )
    {
        int regionWidth = 0;
        int regionHeight = 0;

        // Random colours for each voronoi face
        Color[] seedColours = new Color[inRegionFaces.Length];

        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        for (int i = 0; i < inRegionFaces.Length; ++i)
        {
            seedColours[i] = UnityEngine.Random.ColorHSV(0, 1, 0, 1, 0.1f, 1, 1, 1);
        }

        // final result render texture
        RenderTexture outRTex = new RenderTexture(inGridWidth, inGridHeight, 0);
        outRTex.enableRandomWrite = true;
        outRTex.Create();

        // masking layer, because we're just doing 1 region
        RenderTexture maskRTex = new RenderTexture(inGridWidth, inGridHeight, 0);
        maskRTex.enableRandomWrite = true;
        maskRTex.Create();

        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        // now we're going to do rounds of kernal stuff
        // first round is initializing our masking layer.
        int[] resInts = { inGridWidth, inGridHeight };
        int[] boundsInts = { regionWidth, regionHeight };
        int[] boundsOriginInts = { inRegion.MinX, inRegion.MinY };

        // In this shader, we colour the mask texture white
        // for all pixels in the current region, leaving it
        // black by default.
        int kernel = shader.FindKernel("InitMaskLayer");

        shader.SetTexture(kernel, "Result", outRTex);
        shader.SetTexture(kernel, "Mask", maskRTex);
        shader.SetInts("reses", resInts);
        shader.SetInts("bounds", boundsInts);
        shader.SetInts("boundsOrigin", boundsOriginInts);

        shader.SetInt("coordsSize", inRegion.Coords.Count);

        /* Buffers */

        // Passing in our array of coordinates comprising our region, a Coord is 2 ints (4 bytes each)
        ComputeBuffer coordBuffer = new ComputeBuffer(inRegion.Coords.Count, 8);
        coordBuffer.SetData(inRegion.Coords);
        shader.SetBuffer(kernel, "coords", coordBuffer);

        // passing in our array of colours for each voronoi face
        ComputeBuffer colourBuffer = new ComputeBuffer(seedColours.Length, 16);
        colourBuffer.SetData(seedColours);
        shader.SetBuffer(kernel, "seedColours", colourBuffer);

        // Setup our faces and edges to check
        RegionFace[] regionFaces = new RegionFace[inRegionFaces.Length];

        Coord[] faceVertices = new Coord[inRegionFaces.Length * MAX_POINTS];

        for (int i = 0; i < inRegionFaces.Length; ++i)
        {
            RegionFace currentFace = new RegionFace();

            int firstEdgeID = inRegionFaces[i].Edge.ID;
            var tempEdge = inRegionFaces[i].Edge;

            int count = 0;
            // iterate through all the points making up the voronoi hull
            do
            {
                faceVertices[i * MAX_POINTS + count] = new Coord
                (
                    Mathf.RoundToInt((float)tempEdge.Origin.X), Mathf.RoundToInt((float)tempEdge.Origin.Y)
                );

                tempEdge = tempEdge.Next;
                count++;
            }
            while (tempEdge != null && firstEdgeID != tempEdge.ID);

            Coord site = new Coord(
                Mathf.RoundToInt((float)inRegionFaces[i].generator.X), 
                Mathf.RoundToInt((float)inRegionFaces[i].generator.Y)
            );

            currentFace.Site = site;
            currentFace.Size = count;
            regionFaces[i] = currentFace;
        }

        // I want to encode the vertex information of each face into the compute shader
        // can't use arrays in my structs because not 'blittable'
        // instead I'll separate pass in a '2D' array of Faces by Coords
        // number of faces * fixed max number of coords per face
        ComputeBuffer faceVertexBuffer = new ComputeBuffer(inRegionFaces.Length * MAX_POINTS, 8);
        faceVertexBuffer.SetData(faceVertices);
        shader.SetBuffer(kernel, "faceVertices", faceVertexBuffer);

        // A RegionFace has a Coord as a field (2 ints), and an int (4 bytes)
        ComputeBuffer faceBuffer = new ComputeBuffer(regionFaces.Length, (4*2) + 4);
        faceBuffer.SetData(regionFaces);
        shader.SetBuffer(kernel, "faces", faceBuffer);

        RegionBound[] bounds = new RegionBound[1];
        bounds[0].minX = int.MaxValue;
        bounds[0].minY = int.MaxValue;

        // Each RegionBound struct is 4 ints and 4 bytes each, there is 1 RegionBound in array
        ComputeBuffer regionBoundsBuffer = new ComputeBuffer(bounds.Length, 4 * 4);
        regionBoundsBuffer.SetData(bounds);        
        shader.SetBuffer(kernel, "regionbounds", regionBoundsBuffer);

        shader.Dispatch(kernel, Mathf.CeilToInt(inGridWidth / 16f), Mathf.CeilToInt(inGridHeight / 16f), 1);

        regionBoundsBuffer.GetData(bounds);

        Debug.Log("Min X: " + bounds[0].minX + ", Max X: " +
            bounds[0].maxX + ", Min Y: " + bounds[0].minY + ", Max Y: " + bounds[0].maxY);

        inRegion.MinX = bounds[0].minX;
        inRegion.MaxX = bounds[0].maxX;
        inRegion.MinY = bounds[0].minY;
        inRegion.MaxY = bounds[0].maxY;

        regionBoundsBuffer.Release();
        coordBuffer.Release();
        colourBuffer.Release();
        faceBuffer.Release();
        faceVertexBuffer.Release();

        return maskRTex;
    }

    public static RenderTexture GetRandomColourLandRegionsTexture
    (
        int InWidth,
        int InHeight,
        Vector4[] InColours,
        byte[] InSilhouetteMap,
        int[] InLabelGrid
    )
    {
        RenderTexture retTex = new RenderTexture(InWidth, InHeight, 0);
        retTex.enableRandomWrite = true;
        retTex.Create();

        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }

        int[] resInts = { InWidth, InHeight };

        int kernel = shader.FindKernel("ColourLandTexture");
        shader.SetTexture(kernel, "Result", retTex);
        shader.SetInts("reses", resInts);

        //int Vec4Stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4));
        // Vec4 stride: 16
        ComputeBuffer colourBuffer = new ComputeBuffer(InColours.Length, 16);
        colourBuffer.SetData(InColours);
        shader.SetBuffer(kernel, "colours", colourBuffer);

        // Color32 stride: 4
        ComputeBuffer silhouetteColourBuffer = new ComputeBuffer(InSilhouetteMap.Length, 4);
        silhouetteColourBuffer.SetData(InSilhouetteMap);
        shader.SetBuffer(kernel, "silhouetteMap", silhouetteColourBuffer);

        ComputeBuffer labelGridBuffer = new ComputeBuffer(InLabelGrid.Length, 4);
        labelGridBuffer.SetData(InLabelGrid);
        shader.SetBuffer(kernel, "LabelGrid", labelGridBuffer);

        shader.Dispatch(kernel, Mathf.CeilToInt(InWidth / 16f), Mathf.CeilToInt(InHeight / 16f), 1);

        silhouetteColourBuffer.Release();
        colourBuffer.Release();
        labelGridBuffer.Release();

        return retTex;
    }

    public static RenderTexture GetRandomColourRegionsTexture
    (
        int InWidth,
        int InHeight,
        Vector4[] InColours,
        int[] InLabelGrid
    )
    {
        RenderTexture retTex = new RenderTexture(InWidth, InHeight, 0);
        retTex.enableRandomWrite = true;
        retTex.Create();

        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return null;
        }        

        int[] resInts = { InWidth, InHeight };

        int kernel = shader.FindKernel("ColourTexture");
        shader.SetTexture(kernel, "Result", retTex);
        shader.SetInts("reses", resInts);

        ComputeBuffer colourBuffer = new ComputeBuffer(InColours.Length, 16);
        colourBuffer.SetData(InColours);
        shader.SetBuffer(kernel, "colours", colourBuffer);

        ComputeBuffer labelGridBuffer = new ComputeBuffer(InLabelGrid.Length, 4);
        labelGridBuffer.SetData(InLabelGrid);
        shader.SetBuffer(kernel, "LabelGrid", labelGridBuffer);

        shader.Dispatch(kernel, Mathf.CeilToInt(InWidth / 16f), Mathf.CeilToInt(InHeight / 16f), 1);

        colourBuffer.Release();
        labelGridBuffer.Release();

        return retTex;
    }

    public struct ThreadedSaveTextureAsPNGJob : IJob
    {
        public NativeArray<byte> result;

        [ReadOnly]
        public NativeArray<byte> pixels;
        
        public UnityEngine.Experimental.Rendering.GraphicsFormat texFormat;
        public int width;
        public int height;

        public void Execute()
        {
            // Encode texture into PNG        
            byte[] bytes = ImageConversion.EncodeArrayToPNG(
                pixels.ToArray(),
                texFormat,
                (uint)width,
                (uint)height
            );

            result.CopyFrom(bytes);
        }
    }

    public struct GetPNGSizeJob : IJob
    {
        public NativeArray<int> result;

        [ReadOnly]
        public NativeArray<byte> pixels;

        public UnityEngine.Experimental.Rendering.GraphicsFormat texFormat;
        public int width;
        public int height;

        public void Execute()
        {
            // Encode texture into PNG        
            byte[] bytes = ImageConversion.EncodeArrayToPNG(
                pixels.ToArray(),
                texFormat,
                (uint)width,
                (uint)height
            );

            result[0] = bytes.Length;
        }
    }

    // The First Texture of InTextures is the one all the textures are being merged into
    // It is the "Bottom" of the texture stack.
    public static Texture2D MergeTextures(params Texture[] InTextures)
    {
        if (InTextures == null || InTextures.Length == 0)
        {
            return null;
        }

        int oldQuality = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(5);

        RenderTexture RenderTex = RenderTexture.GetTemporary(
            InTextures[0].width,
            InTextures[0].height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(InTextures[0], RenderTex);
        RenderTexture Previous = RenderTexture.active;
        RenderTexture.active = RenderTex;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, InTextures[0].width, InTextures[0].height, 0);
        for (int i = 1; i < InTextures.Length; i++)
        {
            Graphics.DrawTexture(new Rect(0, 0, InTextures[0].width, InTextures[0].height), InTextures[i]);
        }            
        GL.PopMatrix();

        Texture2D OutTex = new Texture2D(InTextures[0].width, InTextures[0].height, TextureFormat.RGBA32, false);
        OutTex.ReadPixels(new Rect(0, 0, RenderTex.width, RenderTex.height), 0, 0);
        OutTex.filterMode = FilterMode.Point;
        OutTex.wrapMode = TextureWrapMode.Clamp;
        OutTex.Apply();
        RenderTexture.active = Previous;
        RenderTexture.ReleaseTemporary(RenderTex);
        QualitySettings.SetQualityLevel(oldQuality);

        return OutTex;
    }

    public static RenderTexture MergeTexturesToRenderTexture(params Texture[] InTextures)
    {
        if (InTextures == null || InTextures.Length == 0)
        {
            return null;
        }

        int oldQuality = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(5);

        RenderTexture RenderTex = RenderTexture.GetTemporary(
            InTextures[0].width,
            InTextures[0].height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(InTextures[0], RenderTex);
        RenderTexture Previous = RenderTexture.active;
        RenderTexture.active = RenderTex;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, InTextures[0].width, InTextures[0].height, 0);
        for (int i = 1; i < InTextures.Length; i++)
        {
            Graphics.DrawTexture(new Rect(0, 0, InTextures[0].width, InTextures[0].height), InTextures[i]);
        }
        GL.PopMatrix();

        RenderTexture OutRTex = new RenderTexture(InTextures[0].width, InTextures[0].height, 0);
        Graphics.Blit(RenderTex, OutRTex);
        RenderTexture.active = Previous;
        RenderTexture.ReleaseTemporary(RenderTex);
        QualitySettings.SetQualityLevel(oldQuality);

        return OutRTex;
    }

    public static Texture2D OverlayTwoTextures(RenderTexture InSrcA, RenderTexture InSrcB)
    {
        RenderTexture RenderTex = RenderTexture.GetTemporary(
            InSrcB.width,
            InSrcB.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(InSrcB, RenderTex);
        RenderTexture Previous = RenderTexture.active;
        RenderTexture.active = RenderTex;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, InSrcB.width, InSrcB.height, 0);
        // Add Loop Here Later
        Graphics.DrawTexture(new Rect(0, 0, InSrcB.width, InSrcB.height), InSrcA);
        GL.PopMatrix();

        Texture2D OutTex = new Texture2D(InSrcB.width, InSrcB.height, TextureFormat.RGBA32, false);
        OutTex.ReadPixels(new Rect(0, 0, InSrcA.width, InSrcA.height), 0, 0);
        OutTex.filterMode = FilterMode.Point;
        OutTex.wrapMode = TextureWrapMode.Clamp;
        OutTex.Apply();
        RenderTexture.active = Previous;

        return OutTex;
    }

    public static Texture2D CreateTexture2D(RenderTexture InRTX, int InWidth, int InHeight)
    {
        Texture2D texture = new Texture2D(InWidth, InHeight, TextureFormat.RGBA32, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = InRTX;
        texture.ReadPixels(new Rect(0, 0, InWidth, InHeight), 0, 0);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        RenderTexture.active = null;
        return texture;
    }

    public static Texture2D CreateTexture2D(RenderTexture InRTX)
    {
        Texture2D texture = new Texture2D(InRTX.width, InRTX.height, TextureFormat.RGBA32, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = InRTX;
        texture.ReadPixels(new Rect(0, 0, InRTX.width, InRTX.height), 0, 0);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.minimumMipmapLevel = 0;
        texture.Apply(false);
        RenderTexture.active = null;
        return texture;
    }

    private static async Task SaveTextureAsPngAsync(Texture2D texture, string path, string InName)
    {
        byte[] bytes = ImageConversion.EncodeArrayToPNG(
            texture.GetRawTextureData(),
            texture.graphicsFormat,
            (uint)texture.width,
            (uint)texture.height
        );

        using (var filestream = new FileStream(path + InName, FileMode.Create))
        {
            await filestream.WriteAsync(bytes, 0, bytes.Length);
        }

        await Task.Delay(1);
    }

    public static async Task SaveTextureAsPng(Texture2D texture, string path, string InName)
    {
        byte[] data = texture.GetRawTextureData();
        UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat = texture.graphicsFormat;
        int width = texture.width;
        int height = texture.height;

        await Task.Run(async () =>
        {
            byte[] bytes = ImageConversion.EncodeArrayToPNG(
                data,
                graphicsFormat,
                (uint)width,
                (uint)height
            );

            using (var filestream = new FileStream(path + InName, FileMode.Create))
            {
                await filestream.WriteAsync(bytes, 0, bytes.Length);
            }
        });
    }

    public static void ThreadedSaveTextureAsPNG(Texture2D texture, string InName)
    {
        // Because I don't know the result of EncodeArrayToPNG's length
        // i need to do this twice, first to get the length and then to
        // save the result


        GetPNGSizeJob getPNGSize = new GetPNGSizeJob();
        getPNGSize.width = texture.width;
        getPNGSize.height = texture.height;
        getPNGSize.pixels = texture.GetRawTextureData<byte>();
        getPNGSize.texFormat = texture.graphicsFormat;
        NativeArray<int> sizeResult = new NativeArray<int>(1, Allocator.TempJob);
        getPNGSize.result = sizeResult;

        JobHandle getSizeHandle = getPNGSize.Schedule();
        getSizeHandle.Complete();

        NativeArray<byte> result = new NativeArray<byte>(sizeResult[0], Allocator.TempJob);

        ThreadedSaveTextureAsPNGJob savePNGJob = new ThreadedSaveTextureAsPNGJob();        

        savePNGJob.width = texture.width;
        savePNGJob.height = texture.height;
        savePNGJob.pixels = texture.GetRawTextureData<byte>();
        savePNGJob.texFormat = texture.graphicsFormat;        
        savePNGJob.result = result;

        JobHandle handle = savePNGJob.Schedule();
        handle.Complete();

        File.WriteAllBytes(Application.dataPath + "/Temp/" + InName + ".png", result.ToArray());

        sizeResult.Dispose();
        result.Dispose();
    }

    public static IEnumerator AsyncSaveTextureAsPNG(Texture2D texture, string name)
    {
        yield return new WaitForEndOfFrame();

        // Encode texture into PNG        
        byte[] bytes = ImageConversion.EncodeArrayToPNG(
            texture.GetRawTextureData(), 
            texture.graphicsFormat,
            (uint)texture.width,
            (uint)texture.height
        );

        File.WriteAllBytes(AppPath + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + AppPath + name + ".png");
    }

    public static void SaveTextureAsPNG(Texture2D texture, string name)
    {
        // Encode texture into PNG        
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(AppPath + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + AppPath + name + ".png");
    }

    public static List<Color> GenerateHSVColours(int InTotalPoints, Vector2Int InHue, Vector2Int InSat, Vector2Int InVal, int InHueOffset = 30)
    {
        List<Color> colors = new List<Color>();

        (int, int) HueRange = (InHue.x, InHue.y);
        (int, int) SatRange = (InSat.x, InSat.y);
        (int, int) ValRange = (InVal.x, InVal.y); // brightness

        // I recommend introducing some randomness in the generation so you don't end up with a TOO obviously generated result
        float HueJitter = 0.05f;
        float SatJitter = 0.05f;
        float ValJitter = 0.05f;

        int SatCount = 10; // Number of saturation bands to generate.  Must be greater than 1 to avoid a div/0 error.
        int ValCount = 10; // Number of value bands to generate.  Must be greater than 1 to avoid a div/0 error.

        // (With Hue = 10) This will generate 1000 colors (10*10*10).  To set (approximate) number of colors directly, use cubic root of n for each.
        //int hueCount = 10; // Number of hues to generate. Must be greater than 1 to avoid a div/0 error.

        // making hueCount closer to the number of sites
        // will be slightly more if there is a remainder.
        int HueCount = Mathf.CeilToInt((float)InTotalPoints / (ValCount * SatCount));

        if (HueCount < 10) HueCount = 10;

        for (int i = 0; i < HueCount; i++)
        {
            for (int j = 0; j < SatCount; j++)
            {
                for (int k = 0; k < ValCount; k++)
                {
                    var Hue = Mathf.Lerp(HueRange.Item1, HueRange.Item2, ((float)i / (HueCount - 1)) + UnityEngine.Random.Range(-HueJitter, HueJitter));
                    var Sat = Mathf.Lerp(SatRange.Item1, SatRange.Item2, ((float)j / (SatCount - 1)) + UnityEngine.Random.Range(-SatJitter, SatJitter));
                    // This gives a fully linear distribution, however people don't see brightness linearly.  
                    // Recommend using gamma scaling instead.  This is left as an exercise to the reader :)
                    var Val =
                        Mathf.Lerp(ValRange.Item1,
                                   ValRange.Item2,
                                   ((float)k / (ValCount - 1)) + UnityEngine.Random.Range(-ValJitter, ValJitter));

                    int HueRounded = Mathf.FloorToInt(Hue);
                    float HueRounder = Val % 1;
                    Hue = MapUtils.Mod(HueRounded + InHueOffset, 360) + HueRounder;
                    colors.Add(Color.HSVToRGB(Hue / 360, Sat / 100, Val / 100));
                }
            }
        }

        List<Color> temp = new List<Color>();
        float amt = (float)colors.Count / InTotalPoints;
        for (int i = 0; i < InTotalPoints; i++)
        {
            int index = Mathf.RoundToInt(i * amt);
            temp.Add(colors[index]);
        }

        return temp;
    }

    public static Texture2D GenerateContinentalTextureMap(int InNuMCells, int[] InCellByID, List<Color> InColours)
    {
        Texture2D OutTex = new Texture2D(InNuMCells, 1, TextureFormat.RGBA32, false);
        OutTex.filterMode = FilterMode.Point;
        OutTex.wrapMode = TextureWrapMode.Clamp;

        List<Color> CellColours = new List<Color>();

        for (int i = 0; i < InNuMCells; i++)
        {
            CellColours.Add(InColours[InCellByID[i]]);
        }

        OutTex.SetPixels(CellColours.ToArray());
        OutTex.Apply();

        SaveMapAsPNG("ContinentalTextureMap", OutTex);

        return OutTex;
    }

    public static Texture2D GenerateTectonicPlateTextureMap(int InNuMCells, int[] InCellByID, List<Color> InColours)
    {
        Texture2D OutTex = new Texture2D(InNuMCells, 1, TextureFormat.RGBA32, false);
        OutTex.filterMode = FilterMode.Point;
        OutTex.wrapMode = TextureWrapMode.Clamp;

        List<Color> CellColours = new List<Color>();

        for (int i = 0; i < InNuMCells; i++)
        {
            CellColours.Add(InColours[InCellByID[i]]);
        }

        OutTex.SetPixels(CellColours.ToArray());
        OutTex.Apply(false);

        SaveMapAsPNG("TectonicPlateTextureMap", OutTex);

        return OutTex;
    }

    public static Texture2D GenerateBiColourElevationTextureMap(int InNumCells, ProceduralWorlds.MapCell[] InMapCells)
    {
        int NumGradients = 100;

        Texture2D OutTex = new Texture2D(InNumCells, 1, TextureFormat.RGBA32, false);
        OutTex.filterMode = FilterMode.Point;
        OutTex.wrapMode = TextureWrapMode.Clamp;

        Vector2Int LandHues = new Vector2Int(30, 330);
        Vector2Int LandSaturation = new Vector2Int(0, 1);
        Vector2Int LandBrightness = new Vector2Int(99, 100);

        List<Color> LandElevationColours = GenerateHSVColours(NumGradients, LandHues, LandSaturation, LandBrightness, 0);

        Vector2Int WaterHues = new Vector2Int(160, 250);
        Vector2Int WaterSaturation = new Vector2Int(0, 1);
        Vector2Int WaterBrightness = new Vector2Int(0, 1);
        List<Color> WaterElevationColours = GenerateHSVColours(NumGradients, WaterHues, WaterSaturation, WaterBrightness, 0);
        List<Color> CellColours = new List<Color>();
        for (int i = 0; i < InNumCells; i++)
        {
            if (InMapCells[i].RegionType == ERegionType.Land)
            {
                CellColours.Add(LandElevationColours[Mathf.RoundToInt(InMapCells[i].Elevation * NumGradients)]);
            }
            else
            {
                CellColours.Add(WaterElevationColours[Mathf.RoundToInt(InMapCells[i].Elevation * NumGradients)]);
            }
        }

        OutTex.SetPixels(CellColours.ToArray());
        OutTex.Apply();

        return OutTex;
    }

    public static Texture2D GenerateRandomColourTexture(int InNumColours, Vector2Int Hues, Vector2Int Saturation, Vector2Int Brightness)
    {
        Texture2D OutTex = new Texture2D(InNumColours, 1, TextureFormat.RGBA32, false);

        List<Color> Colours = GenerateHSVColours(InNumColours, Hues, Saturation, Brightness, 0);
        Colours.Shuffle();
        OutTex.SetPixels(Colours.ToArray());
        OutTex.Apply();

        return OutTex;
    }

    // Below is probably faster to do on the GPU?
    public static Texture2D GenerateRandomColourTexture(int InNumColours)
    {
        Texture2D OutTex = new Texture2D(InNumColours, 1, TextureFormat.RGBA32, false);

        Vector2Int Hues = new Vector2Int(30, 330);
        Vector2Int Saturation = new Vector2Int(80, 100);
        Vector2Int Brightness = new Vector2Int(70, 100);

        List<Color> Colours = GenerateHSVColours(InNumColours, Hues, Saturation, Brightness, 0);
        OutTex.SetPixels(Colours.ToArray());
        OutTex.Apply();

        return OutTex;
    }

    async public static void SaveMapAsPNG(string InFileName, Texture2D InTex)
    {
        if (InTex != null)
        {
            // attempting C# aync functionality
            await TextureGenerator.SaveTextureAsPng(
                InTex,
                AppPath,
                InFileName + ".png"
            );
        }
    }

    async public static void SaveMapAsPNG(string InFileName, RenderTexture InTex)
    {
        if (InTex != null)
        {
            // attempting C# aync functionality
            await TextureGenerator.SaveTextureAsPng(
                TextureGenerator.CreateTexture2D(InTex),
                AppPath,
                InFileName + ".png"
            );
        }
    }

    public static Material GetUnlitTextureMaterial()
    {
        return Resources.Load("Materials/UnlitTextureMapMaterial", typeof(Material)) as Material;
    }

    public static Material GetUnlitMaterial()
    {
        return Resources.Load("Materials/UnlitMapMaterial", typeof(Material)) as Material;
    }
}
