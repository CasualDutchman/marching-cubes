using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Block {

    public struct MarchingCube {
        public int code;
        public int color;
    }

    public int data;
    public Vector3 positionInChunk;
    public Chunk chunk;
    public int code;
    public int colorCode;

    public MarchingCube[,,] marchingcubes = new MarchingCube[2, 2, 2];

    public Block(int d, Vector3 pos, Chunk c) {
        data = d;
        positionInChunk = pos;
        chunk = c;
    }

    public void SetMarchingCode(int x, int y, int z, int code, int color) {
        marchingcubes[x, y, z].code = code;
        marchingcubes[x, y, z].color = color;
    }

    public Vector3 GetWorldPosition() {
        return new Vector3(chunk.GetPosition().x * chunk.GetWorldObj().maxChunkSize + positionInChunk.x, positionInChunk.y, chunk.GetPosition().y * chunk.GetWorldObj().maxChunkSize + positionInChunk.z);
    }


}
