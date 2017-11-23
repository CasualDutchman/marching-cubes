using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldBackup : MonoBehaviour {

    public static WorldBackup instance;

    public int seed;

    public Transform target;
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

    Thread generationThread;
    public bool doneGenerating = false;
    bool generating = false;

    bool InitialLevelLoaded = false;

    void Awake() {
        instance = this;
    }

    void Start() {
        
    }

    void Update() {
        //convert position of player to Vector2 and Vector2 of currently visiting chunk
        playerPosition = new Vector2(target.position.x, target.position.z);
        playerChunkPosition = new Vector2(Mathf.FloorToInt(playerPosition.x / maxChunkSize), Mathf.FloorToInt(playerPosition.y / maxChunkSize));

        //only generate when going into a new chunk
        if (playerChunkPosition != prevPlayerChunkPosition) {
            UpdateChunks(); //this makes all the new chunks, because they don't excist yet;
            UpdateChunks(); //this creates the mesh for all the chunks in view
        }

        prevPlayerChunkPosition = playerChunkPosition;
    }


    void FixedUpdate() {
        foreach (Chunk c in chunksSeen) {
            if (!chunksToUpdate.Contains(c)) {
                chunksToUpdate.Remove(c);
            }
        }

        //when thread is done generating for chunk -> reset
        if (doneGenerating) {
            chunksToUpdate[0].FinalizeChunk();

            chunksToUpdate.RemoveAt(0);

            doneGenerating = false;
            generating = false;
        }

        //when nog generating mesh for chunk and list has new chunks to update, update
        if(chunksToUpdate.Count > 0 && !generating) {

            chunksToUpdate[0].GetChunkObject().SetActive(true);
            if (chunksToUpdate[0].dirty) {
                generationThread = new Thread(chunksToUpdate[0].GenerateMesh);
                generationThread.Start();
            }
            generating = true;

        }

        //InitialLevelLoaded will be true when all initial chunks are generated
        if (!InitialLevelLoaded && chunksToUpdate.Count == 0) {
            if (chunks.Count > 0) {
                Debug.Log("yey");
                InitialLevelLoaded = true;
            }
        }
    } 

    void UpdateChunks() {
        //when chunks are not in view, make them invisible
        foreach (Chunk chunk in chunksSeen) {
            if(!chunk.InView(playerPosition))
                chunk.GetChunkObject().SetActive(false);
        }
        chunksSeen.Clear();

        for (int xOffset = -renderRange - 1; xOffset <= renderRange + 1; xOffset++) {
            for (int yOffset = -renderRange - 1; yOffset <= renderRange + 1; yOffset++) {
                Vector2 viewedChunkPos = new Vector2(playerChunkPosition.x + xOffset, playerChunkPosition.y + yOffset);

                if (chunks.ContainsKey(viewedChunkPos)) {
                    if (!chunks[viewedChunkPos].InLoadingArea(playerPosition)) {
                        //print("Destroy");
                        DestroyImmediate(chunks[viewedChunkPos].GetChunkObject());
                        chunks.Remove(viewedChunkPos);
                        continue;
                    }

                    //make visible when not visible
                    if (chunks[viewedChunkPos].InView(playerPosition) && !chunks[viewedChunkPos].IsVisible()){
                        chunksToUpdate.Add(chunks[viewedChunkPos]);
                    }

                    //already visible chunks add to list, so they won't get updated
                    if (chunks[viewedChunkPos].IsVisible()) {
                        chunksSeen.Add(chunks[viewedChunkPos]); 
                    }
                }
                else {
                    //add new chunk to Dictionary
                    Chunk c = new Chunk(null /*this*/, viewedChunkPos, transform);
                    chunks.Add(viewedChunkPos, c);
                }
            }
        }
    }

    /// <summary>
    /// Change a block's data at a given position in world space
    /// </summary>
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

        foreach (Block neighbour in neighbours) {
            if (neighbour != null) {
                if (!chunksToUpdate.Contains(neighbour.chunk)) {
                    neighbour.chunk.dirty = true;
                    chunksToUpdate.Add(neighbour.chunk);
                }
            }
        }
    }

    /// <summary>
    /// Get Chunk based on world position
    /// </summary>
    public Chunk GetChunk(Vector3 worldPos) {
        int posX = Mathf.FloorToInt(worldPos.x / maxChunkSize);
        int posY = Mathf.FloorToInt(worldPos.z / maxChunkSize);

        return GetChunk(posX, posY);
    }

    /// <summary>
    /// Get Chunk based on chunk's position, with check
    /// </summary>
    public Chunk GetChunk(int posX, int posY) {
        if (chunks.ContainsKey(new Vector2(posX, posY))) {
            return chunks[new Vector2(posX, posY)];
        }
        return null;
    }

    /// <summary>
    /// Get Chunk based on chunk's position, without check
    /// </summary>
    public Chunk GetChunkDirectly(int posX, int posY) {
        return chunks[new Vector2(posX, posY)];
    }

    /// <summary>
    /// Get block based on world position, need to find chunk
    /// </summary>
    public Block GetBlock(Vector3 worldPos) {
        Chunk chunk = GetChunk(worldPos);

        if (chunk == null)
            return null;

        return GetBlock(worldPos, chunk);
    }

    /// <summary>
    /// Get block based on world position, already knowing chunk
    /// </summary>
    public Block GetBlock(Vector3 worldPos, Chunk chunk) {
        int posX = Mathf.FloorToInt(worldPos.x - (chunk.GetPosition().x * maxChunkSize));
        int posY = Mathf.FloorToInt(worldPos.y);
        int posZ = Mathf.FloorToInt(worldPos.z - (chunk.GetPosition().y * maxChunkSize));

        return chunk.GetBlock(posX, posY, posZ);
    }
}
