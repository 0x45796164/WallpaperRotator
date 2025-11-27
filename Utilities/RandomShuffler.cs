using System;

namespace WallpaperRotator.Utilities;

public static class RandomShuffler
{
    public static void Shuffle(int[] indices)
    {
        // Fisher-Yates shuffle
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
    }
}
