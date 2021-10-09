using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public static class TextureGenerator
{
    public static Texture2D CreateTexture2D(RenderTexture InRTX, int InWidth, int InHeight)
    {
        Texture2D texture = new Texture2D(InWidth, InHeight, TextureFormat.RGB24, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = InRTX;
        texture.ReadPixels(new Rect(0, 0, InWidth, InHeight), 0, 0);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        return texture;
    }

    public static void SaveTextureAsPNG(Texture2D texture, string name)
    {
        // Encode texture into PNG        
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Temp/" + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + Application.dataPath + "/Temp/" + name + ".png");
    }
}
