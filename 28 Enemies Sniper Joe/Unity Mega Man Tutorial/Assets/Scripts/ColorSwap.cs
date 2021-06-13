using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/*
 * Comes from this article with some minor changes by me
 * https://gamedevelopment.tutsplus.com/tutorials/how-to-use-a-shader-to-dynamically-swap-a-sprites-colors--cms-25129
 */

public class ColorSwap : MonoBehaviour
{
    SpriteRenderer mSpriteRenderer;

    Texture2D mColorSwapTex;
    Color[] mSpriteColors;

    void Awake()
    {
        // sprite renderer of gameobject this script is attached to
        mSpriteRenderer = GetComponent<SpriteRenderer>();

        InitColorSwapTex();
    }

    public void InitColorSwapTex()
    {
        Texture2D colorSwapTex = new Texture2D(256, 1, TextureFormat.RGBA32, false, false);
        colorSwapTex.filterMode = FilterMode.Point;

        for (int i = 0; i < colorSwapTex.width; ++i)
            colorSwapTex.SetPixel(i, 0, new Color(0.0f, 0.0f, 0.0f, 0.0f));

        colorSwapTex.Apply();

        mSpriteRenderer.material.SetTexture("_SwapTex", colorSwapTex);

        mSpriteColors = new Color[colorSwapTex.width];
        mColorSwapTex = colorSwapTex;
    }

    public static Color ColorFromInt(int c, float alpha = 1.0f)
    {
        int r = (c >> 16) & 0x000000FF;
        int g = (c >> 8) & 0x000000FF;
        int b = c & 0x000000FF;

        Color ret = ColorFromIntRGB(r, g, b);
        ret.a = alpha;

        return ret;
    }

    public static Color ColorFromIntRGB(int r, int g, int b)
    {
        return new Color((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f, 1.0f);
    }

    public void SwapColors(List<int> indexes, List<Color> colors)
    {
        for (int i = 0; i < indexes.Count; ++i)
        {
            mSpriteColors[indexes[i]] = colors[i];
            mColorSwapTex.SetPixel(indexes[i], 0, colors[i]);
        }
        mColorSwapTex.Apply();
    }

    public void SwapColor(int index, Color color)
    {
        mSpriteColors[index] = color;
        mColorSwapTex.SetPixel(index, 0, color);
    }

    public void ApplyColor()
    {
        mColorSwapTex.Apply();
        // save the color swap texture to local storage for debugging
        //byte[] data = mColorSwapTex.EncodeToPNG();
        //File.WriteAllBytes(Application.dataPath + "/../ColorSwapTexture.png", data);
    }
}
