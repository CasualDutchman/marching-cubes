using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Block {

    public int data;
    public Vector3 positionInChunk;
    public Chunk chunk;
    public int code;

    public Block(int d, Vector3 pos, Chunk c) {
        data = d;
        positionInChunk = pos;
        chunk = c;
    }

    public Vector3 GetWorldPosition() {
        return new Vector3(chunk.GetPosition().x * chunk.worldObj.maxChunkSize + positionInChunk.x, positionInChunk.y, chunk.GetPosition().y * chunk.worldObj.maxChunkSize + positionInChunk.z);
    }


}
