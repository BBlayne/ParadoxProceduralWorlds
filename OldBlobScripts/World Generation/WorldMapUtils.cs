using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public enum SEEDS
{
    RAENIR,
    VANIVER,
    DRAGOON,
    KING_OF_MEN,
    YAMI,
    TAZZZO,
    SAURON,
    HAGBARD,
    MARK,
    BCM,
    STIEF,
    EGO,
    RADEK,
    DANO,
    RANGER,
    MIKE,
    MICH,
    FIMCONTE,
    GRAVES,
    ODDMAN
}

public enum RESOLUTIONS
{
    W128_H128,
    W352_H128,
    W256_H256,
    W512_H512,
    W704_H256,
    W1408_H512
}

public static class WorldMapUtils
{
    public static Tuple<int, int> SetResolution(RESOLUTIONS resolution)
    {
        Tuple<int, int> reses;
        switch (resolution)
        {
            case RESOLUTIONS.W128_H128:
                reses = new Tuple<int, int>(128, 128);
                break;
            case RESOLUTIONS.W256_H256:
                reses = new Tuple<int, int>(256, 256);
                break;
            case RESOLUTIONS.W512_H512:
                reses = new Tuple<int, int>(512, 512);
                break;
            case RESOLUTIONS.W352_H128:
                reses = new Tuple<int, int>(352, 128);
                break;
            case RESOLUTIONS.W704_H256:
                reses = new Tuple<int, int>(704, 256);
                break;
            case RESOLUTIONS.W1408_H512:
                reses = new Tuple<int, int>(1408, 512);
                break;
            default:
                reses = new Tuple<int, int>(256, 256);
                break;
        }

        return reses;
    }

    public static string SelectSeed(SEEDS playerSeed)
    {
        string mPlayerSeed = "";
        switch (playerSeed)
        {
            case SEEDS.RAENIR:
                mPlayerSeed = "Raenir E. Salazar";
                break;
            case SEEDS.BCM:
                mPlayerSeed = "Blue Cheese Moon";
                break;
            case SEEDS.DANO:
                mPlayerSeed = "Danomite";
                break;
            case SEEDS.DRAGOON:
                mPlayerSeed = "Dragoon";
                break;
            case SEEDS.EGO:
                mPlayerSeed = "BurningEgo";
                break;
            case SEEDS.RADEK:
                mPlayerSeed = "Radek";
                break;
            case SEEDS.SAURON:
                mPlayerSeed = "Sauron";
                break;
            case SEEDS.STIEF:
                mPlayerSeed = "SteifCantBeBotheredToSpellhisNameRight";
                break;
            case SEEDS.YAMI:
                mPlayerSeed = "Yami";
                break;
            case SEEDS.KING_OF_MEN:
                mPlayerSeed = "King of Men";
                break;
            case SEEDS.HAGBARD:
                mPlayerSeed = "Hagbard";
                break;
            case SEEDS.MARK:
                mPlayerSeed = "Mark";
                break;
            case SEEDS.VANIVER:
                mPlayerSeed = "Vaniver";
                break;
            case SEEDS.RANGER:
                mPlayerSeed = "Ranger the King under the Mountain";
                break;
            case SEEDS.MIKE:
                mPlayerSeed = "gMike";
                break;
            case SEEDS.MICH:
                mPlayerSeed = "Mich";
                break;
            case SEEDS.FIMCONTE:
                mPlayerSeed = "Fimconte";
                break;
            case SEEDS.GRAVES:
                mPlayerSeed = "Graves";
                break;
            case SEEDS.ODDMAN:
                mPlayerSeed = "Oddman";
                break;
            case SEEDS.TAZZZO:
                mPlayerSeed = "Tazzzo";
                break;
            default:
                mPlayerSeed = Time.time.ToString();
                break;
        }

        return mPlayerSeed;
    }

    public static bool IsInMapRange(int x, int y, int width, int height)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }    

    public static Texture2D BlitMaterialToTexture(Material material2blit, int width, int height)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
        // switch material
        Graphics.Blit(null, renderTexture, material2blit);
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(Vector2.zero, new Vector2(width, height)), 0, 0);
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);
        texture.Apply();

        return texture;
    }

    public static Material GetNoiseMaterial(NoiseSettings.FilterType filterType)
    {
        // Get Material with correct noise
        switch (filterType)
        {
            case NoiseSettings.FilterType.BASE:
                return Resources.Load("Materials/IPN_FBM_2D", typeof(Material)) as Material;
            case NoiseSettings.FilterType.BILLOWED:
                return Resources.Load("Materials/IPN_Billowed_2D", typeof(Material)) as Material;
            case NoiseSettings.FilterType.RIDGED:
                return Resources.Load("Materials/IPN_Ridged_2D", typeof(Material)) as Material;
            case NoiseSettings.FilterType.PERTURBED:
                return Resources.Load("Materials/IPN_Perturbed_2D", typeof(Material)) as Material;
            default:
                return Resources.Load("Materials/IPN_FBM_2D", typeof(Material)) as Material;
        }
    }

    public static Material LoadBlendMaterial(NoiseMapUtils.BLEND_MODES blend_mode)
    {
        switch (blend_mode)
        {
            case NoiseMapUtils.BLEND_MODES.NORMAL:
                return Resources.Load("Materials/Blend Materials/NormalBlendMode", typeof(Material)) as Material;
            case NoiseMapUtils.BLEND_MODES.MULTIPLY:
                return Resources.Load("Materials/Blend Materials/MultiplyBlendMode", typeof(Material)) as Material;
            case NoiseMapUtils.BLEND_MODES.DIVIDE:
                return Resources.Load("Materials/Blend Materials/DivideBlendMode", typeof(Material)) as Material;
            case NoiseMapUtils.BLEND_MODES.OVERLAY:
                return Resources.Load("Materials/Blend Materials/OverlayBlendMode", typeof(Material)) as Material;
            case NoiseMapUtils.BLEND_MODES.DIFFERENCE:
                return Resources.Load("Materials/Blend Materials/DifferenceBlendMode", typeof(Material)) as Material;
            case NoiseMapUtils.BLEND_MODES.ADDITIVE:
                return Resources.Load("Materials/Blend Materials/AdditiveBlendMode", typeof(Material)) as Material;
            case NoiseMapUtils.BLEND_MODES.SUBTRACT:
                return Resources.Load("Materials/Blend Materials/SubtractBlendMode", typeof(Material)) as Material;
            case NoiseMapUtils.BLEND_MODES.SCREEN:
                return Resources.Load("Materials/Blend Materials/ScreenBlendMode", typeof(Material)) as Material;
            default:
                return Resources.Load("Materials/Blend Materials/NormalBlendMode", typeof(Material)) as Material;
        }
    }

    public static void SaveTextureToFile(Texture2D tex, string filename)
    {
        Debug.Log("Saving texture...");
        if (tex == null)
        {
            Debug.Log("Error: " + filename + " is null");
        }

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Temp/" + filename + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " +
            Application.dataPath + "/Temp/" + filename + ".png");
    }
}
