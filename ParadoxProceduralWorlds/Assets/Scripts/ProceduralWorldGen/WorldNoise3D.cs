using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class WorldNoise3D
{
	private static int seed_;

	/// <summary> 
	///  The seed for the noise function. Randomized at startup by default.
	/// </summary>
	public static int seed {
		get {
			return seed_;
		}
		set {
			seed_ = value;
			UnityEngine.Random.InitState(value);
			SetupNoise();
		}
	}

	/* gradients & perm tables */
	private static int[][] grad3 = {new int[]{1,1,0}, new int[]{-1,1,0}, new int[]{1,-1,0}, new int[]{-1,-1,0},
		new int[]{1,0,1}, new int[]{-1,0,1}, new int[]{1,0,-1}, new int[]{-1,0,-1},
		new int[]{0,1,1}, new int[]{0,-1,1}, new int[]{0,1,-1}, new int[]{0,-1,-1}};

	private static int[][] grad4 = {new int[]{0,1,1,1}, new int[]{0,1,1,-1},  new int[]{0,1,-1,1},  new int[]{0,1,-1,-1},
		new int[]{0,-1,1,1},new int[] {0,-1,1,-1},new int[] {0,-1,-1,1},new int[] {0,-1,-1,-1},
		new int[]{1,0,1,1}, new int[]{1,0,1,-1},  new int[]{1,0,-1,1},  new int[]{1,0,-1,-1},
		new int[]{-1,0,1,1},new int[] {-1,0,1,-1},new int[] {-1,0,-1,1},new int[] {-1,0,-1,-1},
		new int[]{1,1,0,1}, new int[]{1,1,0,-1},  new int[]{1,-1,0,1},  new int[]{1,-1,0,-1},
		new int[]{-1,1,0,1},new int[] {-1,1,0,-1},new int[] {-1,-1,0,1},new int[] {-1,-1,0,-1},
		new int[]{1,1,1,0}, new int[]{1,1,-1,0},  new int[]{1,-1,1,0},  new int[]{1,-1,-1,0},
		new int[]{-1,1,1,0},new int[] {-1,1,-1,0},new int[] {-1,-1,1,0},new int[] {-1,-1,-1,0}};

	private static int[] p = null;

	private static int[] perm_ = null;
	private static int[] perm {
		get {
			if (perm_ == null)
				SetupNoise();
			return perm_;
		}
		set {
			perm_ = value;
		}
	}
	private static void SetupNoise()
	{
		p = new int[256];
		for (int i = 0; i < 256; i++) p[i] = Mathf.FloorToInt(UnityEngine.Random.value * 256);

		perm_ = new int[512];
		for (int i = 0; i < 512; i++) perm_[i] = p[i & 255];
	}

	private static int[][] simplex = {
		new int[]{0,1,2,3}, new int[]{0,1,3,2}, new int[]{0,0,0,0}, new int[]{0,2,3,1}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{1,2,3,0},
		new int[]{0,2,1,3}, new int[]{0,0,0,0}, new int[]{0,3,1,2}, new int[]{0,3,2,1}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{1,3,2,0},
		new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0},
		new int[]{1,2,0,3}, new int[]{0,0,0,0}, new int[]{1,3,0,2}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{2,3,0,1}, new int[]{2,3,1,0},
		new int[]{1,0,2,3}, new int[]{1,0,3,2}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{2,0,3,1}, new int[]{0,0,0,0}, new int[]{2,1,3,0},
		new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0},
		new int[]{2,0,1,3}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{3,0,1,2}, new int[]{3,0,2,1}, new int[]{0,0,0,0}, new int[]{3,1,2,0},
		new int[]{2,1,0,3}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{0,0,0,0}, new int[]{3,1,0,2}, new int[]{0,0,0,0}, new int[]{3,2,0,1}, new int[]{3,2,1,0}};

	/* GPU Noise */
	static bool needsFakeBuffer = true;

	private static string shaderPath = "Shaders/WorldNoise3D";
	private static string noShaderMsg = "Could not find the noise compute shader. Did you move/rename any of the files?";

	private static void SetShaderVars
	(
		ComputeShader InShader, 
		NoiseSettings InNoiseSettings, 
		bool bNormalize, 
		int InKernel
	)
    {
		// Noise parameters
		InShader.SetInt("octaves", InNoiseSettings.Octaves);
		InShader.SetFloat("persistence", InNoiseSettings.Persistence);		
		InShader.SetFloat("noiseScale", (float)1 / InNoiseSettings.Frequency);
		InShader.SetFloat("lacunarity", InNoiseSettings.Lacunarity);
		InShader.SetVector("offset", InNoiseSettings.Offsets);

		InShader.SetInt("normalize", System.Convert.ToInt32(bNormalize));

		if (needsFakeBuffer)
		{
			ComputeBuffer cb = new ComputeBuffer(1, 4);
			InShader.SetBuffer(InKernel, "float1Array", cb);
			InShader.SetBuffer(InKernel, "float2Array", cb);
			InShader.SetBuffer(InKernel, "float3Array", cb);
			InShader.SetBuffer(InKernel, "float4Array", cb);
			cb.Release();
			needsFakeBuffer = false;
		}
	}

	public static RenderTexture GetNoiseRenderTexture
	(
		int InWidth, 
		int InHeight, 
		NoiseSettings InNoiseSettings, 
		bool bNormalize = true
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

		int kernel = shader.FindKernel("ComputeNoise");
		shader.SetTexture(kernel, "Result", retTex);
		SetShaderVars(shader, InNoiseSettings, bNormalize, kernel);
		shader.SetInts("reses", resInts);

		ComputeBuffer permBuffer = new ComputeBuffer(perm.Length, 4);
		permBuffer.SetData(perm);
		shader.SetBuffer(kernel, "perm", permBuffer);

		shader.Dispatch(kernel, Mathf.CeilToInt(InWidth / 16f), Mathf.CeilToInt(InHeight / 16f), 1);

		permBuffer.Release();

		return retTex;
	}

	/// <summary> 
	/// Uses the GPU to process an array of 1D coordinates for noise and return an array with the noise at 
	/// the specified coordinates.
	/// </summary>
	/// <returns>Float array</returns>
	/// <param name="positions"> Array of coordinates to process. </param>
	/// <param name="noiseScale"> Value to scale the noise coordinates by. </param>
	/// <param name="normalize"> Whether or not to remap the noise from (-1, 1) to (0, 1). </param>
	public static float[] NoiseArrayGPU(float[] positions, NoiseSettings InNoiseSettings, bool bNormalize = true)
    {
		ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
		if (shader == null)
		{
			Debug.LogError(noShaderMsg);
			return null;
		}

		int kernel = shader.FindKernel("ComputeNoiseArray");
		SetShaderVars(shader, InNoiseSettings, bNormalize, kernel);

		ComputeBuffer permBuffer = new ComputeBuffer(perm.Length, 4);
		permBuffer.SetData(perm);
		shader.SetBuffer(kernel, "perm", permBuffer);

		ComputeBuffer posBuffer = new ComputeBuffer(positions.Length, 4);
		posBuffer.SetData(positions);
		shader.SetBuffer(kernel, "float1Array", posBuffer);

		ComputeBuffer outputBuffer = new ComputeBuffer(positions.Length, 4);
		shader.SetBuffer(kernel, "outputArray", outputBuffer);

		shader.Dispatch(kernel, Mathf.CeilToInt(positions.Length / 16f), 1, 1);

		float[] outputData = new float[positions.Length];
		outputBuffer.GetData(outputData);

		permBuffer.Release();
		posBuffer.Release();
		outputBuffer.Release();

		return outputData;
	}
}
