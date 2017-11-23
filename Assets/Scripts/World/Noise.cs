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


	public static float Value(int seed, int posX, int posY) {
        //Random.InitState(seed);

        //System.Random rng = new System.Random(seed);

        float perlin1 = Mathf.PerlinNoise(posX * 0.1f, 50 + posY * 0.1f);
        float perlin2 = Mathf.PerlinNoise(100 + posX * 0.1f, -100 + posY * 0.1f);
        float val = (perlin1 + perlin2) / 2.0f;

        return val;
    }
}
