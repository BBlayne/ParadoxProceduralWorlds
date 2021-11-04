using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;

public static class TextureGenerator
{
    public static string AppPath = Application.dataPath + "/Temp/";

    private static string shaderPath = "Shaders/TextureUtilShader";
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

        File.WriteAllBytes(Application.dataPath + "/Temp/" + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + Application.dataPath + "/Temp/" + name + ".png");
    }

    public static void SaveTextureAsPNG(Texture2D texture, string name)
    {
        // Encode texture into PNG        
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Temp/" + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + Application.dataPath + "/Temp/" + name + ".png");
    }
}
