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


	public float Value(int posX, int posY) {
        Random.InitState(seed);

        float perlin1 = Mathf.PerlinNoise(posX * 0.1f, 50 + posY * 0.1f);
        float perlin2 = Mathf.PerlinNoise(100 + posX * (Random.value * 0.1f), -100 + posY * (Random.value * 0.1f));
        float val = (perlin1 + perlin2) / 2.0f;

        return val;
    }
}
