using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise {

    public int seed;
    public float scale = 0.5f;

    public Noise() {
        seed = Random.Range(0, int.MaxValue);
    }

    public Noise(int _seed) {
        seed = _seed;
    }


    public static float[,] GetMap(int size, float posX, float posY) {
        //Random.InitState(seed);

        float[,] heightmap = new float[size, size];

        int octaves = 6;
        float scale = 0.5f;

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {

                float height = 0;

                for (int i = 0; i < octaves; i++) {

                    float offsetX = (i * 10);
                    float offsetY = -(i * 10);

                    float multiplier = (0.025f * (i * 2));

                    height += Mathf.PerlinNoise(offsetX + (posX + x) * multiplier * scale, offsetY + (posY + y) * multiplier * scale);
                }
                /*
                float perlin1 = Mathf.PerlinNoise((posX + x) * 0.02f, (posY + y) * 0.02f);
                float perlin2 = Mathf.PerlinNoise(100 + (posX + x) * 0.05f, -100 + (posY + y) * 0.05f);
                float perlin3 = Mathf.PerlinNoise(-100 + (posX + x) * 0.1f, 100 + (posY + y) * 0.1f);
                float val = (perlin1 + perlin2 + perlin3) / 3.0f;
                */


                heightmap[x, y] = height / octaves;
            }
        }

        return heightmap;
    }
}
