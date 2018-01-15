using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise {

    /// <summary>
    /// Get a 2 dimesional float array for the heightmap using perlin noise and octaces
    /// </summary>
    public static float[,] GetMap(int size, float posX, float posY, int seedInput, int octavesInput) {
        float[,] heightmap = new float[size, size];

        Random.InitState(seedInput);

        int octaves = octavesInput;
        float scale = 1f + (Random.value * 0.5f);

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {

                float height = 0;

                for (int i = 0; i < octaves; i++) {

                    float offsetX = (i * 10);
                    float offsetY = -(i * 10);

                    float multiplier = (0.025f * (i * 2)) * Random.value;

                    height += Mathf.PerlinNoise(offsetX + (posX + x) * multiplier * scale, offsetY + (posY + y) * multiplier * scale);
                }

                heightmap[x, y] = height / octaves;
            }
        }

        return heightmap;
    }
}
