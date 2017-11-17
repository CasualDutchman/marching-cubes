using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour {

    public int seed;

    public static World instance;

    public Transform target;
    public int renderRange = 5;

    Vector2 playerPosition;
    Vector2 playerChunkPosition;

    public int maxChunkSize;
    public int maxHeight;

    Dictionary<Vector2, Chunk> chunks = new Dictionary<Vector2, Chunk>();
    List<Chunk> chunksSeen = new List<Chunk>();

    public Noise noise;

    public int shown = 0;
    public int goal = 1;

    public bool gotit {
        get {
            return shown >= goal;
        }
    }

    void Awake() {
        instance = this;
        noise = new Noise(seed);
    }

    void Start() {

    }

    void Update() {
        playerPosition = new Vector2(target.position.x, target.position.z);
        playerChunkPosition = new Vector2(Mathf.FloorToInt(playerPosition.x / maxChunkSize), Mathf.FloorToInt(playerPosition.y / maxChunkSize));

        UpdateChunks();    
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
                        chunks[viewedChunkPos].SetVisible(true);
                    }

                    if (chunks[viewedChunkPos].IsVisible()) {
                        chunksSeen.Add(chunks[viewedChunkPos]);
                    }
                }
                else {
                    chunks.Add(viewedChunkPos, new Chunk(this, viewedChunkPos, transform));
                }
            }
        }
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
