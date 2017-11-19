using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class World : MonoBehaviour {

    public int seed;

    public static World instance;

    public Transform target;
    public Transform tester;
    public int renderRange = 5;

    Vector2 playerPosition;
    Vector2 playerChunkPosition;
    Vector2 prevPlayerChunkPosition = new Vector2(100, 100);

    public int maxChunkSize;
    public int maxHeight;
    public float sizeOfBlock = 1.0f;

    Dictionary<Vector2, Chunk> chunks = new Dictionary<Vector2, Chunk>();
    List<Chunk> chunksSeen = new List<Chunk>();
    List<Chunk> chunksToUpdate = new List<Chunk>();
    List<Chunk> updateChunks = new List<Chunk>();

    public Noise noise;

    void Awake() {
        instance = this;
        noise = new Noise(seed);
    }

    void Start() {
        
    }

    void Update() {
        playerPosition = new Vector2(target.position.x, target.position.z);
        playerChunkPosition = new Vector2(Mathf.FloorToInt(playerPosition.x / maxChunkSize), Mathf.FloorToInt(playerPosition.y / maxChunkSize));

        if (playerChunkPosition != prevPlayerChunkPosition) {
            UpdateChunks();//this makes all the new chunks, because they do'nt excist yet;
            UpdateChunks();//this creates the mesh for all the chunks in view
        }

        prevPlayerChunkPosition = playerChunkPosition;

        foreach (Chunk c in updateChunks) {
            c.UpdateChunk();
        }
    }


    void FixedUpdate() {
        foreach (Chunk c in chunksSeen) {
            if (!chunksToUpdate.Contains(c)) {
                chunksToUpdate.Remove(c);
            }
        }

        if(chunksToUpdate.Count > 0) {
            
            chunksToUpdate[0].SetVisible(true);
            chunksToUpdate.RemoveAt(0);
        }
    } 

    void UpdateChunks() {
        foreach (Chunk chunk in chunksSeen) {
            if(!chunk.InView(playerPosition))
                chunk.SetVisible(false);
        }
        chunksSeen.Clear();

        for (int xOffset = -renderRange - 1; xOffset <= renderRange + 1; xOffset++) {
            for (int yOffset = -renderRange - 1; yOffset <= renderRange + 1; yOffset++) {
                Vector2 viewedChunkPos = new Vector2(playerChunkPosition.x + xOffset, playerChunkPosition.y + yOffset);

                if (chunks.ContainsKey(viewedChunkPos)) {
                    if(chunks[viewedChunkPos].InView(playerPosition) && !chunks[viewedChunkPos].IsVisible()){
                        //chunks[viewedChunkPos].SetVisible(true);
                        chunksToUpdate.Add(chunks[viewedChunkPos]);
                        updateChunks.Add(chunks[viewedChunkPos]);
                    }

                    if (chunks[viewedChunkPos].IsVisible()) {
                        chunksSeen.Add(chunks[viewedChunkPos]);
                        
                    }
                }
                else {
                    Chunk c = new Chunk(this, viewedChunkPos, transform);
                    chunks.Add(viewedChunkPos, c);
                }
            }
        }
    }

    public void SetBlock(Vector3 worldPos, int i) {
        worldPos += new Vector3(-1, -1, -1);
        Block block = GetBlock(worldPos);

        if(block.data == i) {
            return;
        }

        if(block.positionInChunk.y <= 0) {
            return;
        }

        block.data = i;

        Block[,,] neighbours = new Block[3, 3, 3];
        block.chunk.GetNeighbourBlocks(neighbours, block);
        List<Chunk> dirtyChunks = new List<Chunk>();

        foreach (Block neighbour in neighbours) {
            if (neighbour != null) {
                if (!dirtyChunks.Contains(neighbour.chunk)) {
                    dirtyChunks.Add(neighbour.chunk);
                }
            }
        }

        foreach (Chunk chunk in dirtyChunks) {
            chunk.UpdateMesh();
        }

        dirtyChunks.Clear();
    }

    [ContextMenu("Test")]
    public void ChangeBlock() {
        SetBlock(tester.position, 0);
    }

    public Chunk GetChunk(Vector3 worldPos) {
        int posX = Mathf.FloorToInt(worldPos.x / maxChunkSize);
        int posY = Mathf.FloorToInt(worldPos.z / maxChunkSize);

        return GetChunk(posX, posY);
    }

    public Chunk GetChunk(int posX, int posY) {
        if (chunks.ContainsKey(new Vector2(posX, posY))) {
            return chunks[new Vector2(posX, posY)];
        }
        return null;
    }

    public Chunk GetChunkDirectly(int posX, int posY) {
        return chunks[new Vector2(posX, posY)];
    }

    public Block GetBlock(Vector3 worldPos) {
        Chunk chunk = GetChunk(worldPos);

        if (chunk == null)
            return null;

        return GetBlock(worldPos, chunk);
    }

    public Block GetBlock(Vector3 worldPos, Chunk chunk) {
        int posX = Mathf.FloorToInt(worldPos.x - (chunk.GetPosition().x * maxChunkSize));
        int posY = Mathf.FloorToInt(worldPos.y);
        int posZ = Mathf.FloorToInt(worldPos.z - (chunk.GetPosition().y * maxChunkSize));

        return chunk.GetBlock(posX, posY, posZ);
    }
}
