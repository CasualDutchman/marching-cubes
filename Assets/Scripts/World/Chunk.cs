using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[System.Serializable]
public class Chunk {

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    Mesh mesh;

    bool meshReady = false;

    public World worldObj;
    Vector2 position;
    Block[,,] blocks;
    GameObject chunkObject;

    public bool dirty = true;

    Thread generationThread;

    public Chunk() { }

    public Chunk(World world, Vector2 pos, Transform parent) {
        worldObj = world;
        position = pos;

        blocks = new Block[worldObj.maxChunkSize, worldObj.maxHeight, worldObj.maxChunkSize];

        chunkObject = new GameObject("chunk " + position.x + "/" + position.y);
        chunkObject.transform.position = new Vector3(position.x * worldObj.maxChunkSize, 0, position.y * worldObj.maxChunkSize) * worldObj.sizeOfBlock;
        chunkObject.transform.SetParent(parent);
        chunkObject.layer = LayerMask.NameToLayer("Terrain");

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshCollider = chunkObject.AddComponent<MeshCollider>();

        Generate();

        SetVisible(false);
    }

    public void UpdateChunk() {
        if (chunkObject.activeSelf) {
            if (meshReady) {
                Debug.Log("done");

                mesh = new Mesh();
                mesh.name = chunkObject.name;

                mesh.vertices = verts.ToArray();
                mesh.uv = uvs.ToArray();
                mesh.triangles = tris.ToArray();

                verts.Clear();
                uvs.Clear();
                tris.Clear();

                mesh.RecalculateNormals();

                meshFilter.mesh = mesh;
                meshCollider.sharedMesh = mesh;

                meshRenderer.material = Resources.Load<Material>("Material/WorldMaterial");

                mesh = null;

                meshReady = false;

                generationThread.Abort();
                generationThread = null;
            }
        }
    }

    public bool InView(Vector2 playerPos) {
        Vector2 centerPos = new Vector2(chunkObject.transform.position.x, chunkObject.transform.position.z) + new Vector2(worldObj.maxChunkSize / 2.0f, worldObj.maxChunkSize / 2.0f);
        return Vector2.Distance(centerPos, playerPos) < worldObj.renderRange * worldObj.maxChunkSize;
    }

    public bool IsVisible() {
        return chunkObject.activeSelf;
    }

    public void SetVisible(bool b) {
        chunkObject.SetActive(b);

        if (dirty && b) {
            generationThread = new Thread(GenerateMesh);
            generationThread.Start();

            //GenerateMesh();

            dirty = false;
        }  
    }

    void Generate() {
        for (int x = 0; x < worldObj.maxChunkSize; x++) {
            for (int y = 0; y < worldObj.maxHeight; y++) {
                for (int z = 0; z < worldObj.maxChunkSize; z++) {
                    float perlin = worldObj.noise.Value(Mathf.FloorToInt(position.x * worldObj.maxChunkSize + x), Mathf.FloorToInt(position.y * worldObj.maxChunkSize + z));

                    int i = Mathf.FloorToInt(worldObj.maxHeight * perlin);

                    FillData(y == 0 ? 1 : (y <= i ? 1 : 0), x, y, z);
                }
            }
        }
    }

    void FillData(int i, int x, int y, int z) {
        blocks[x, y, z] = new Block(i, new Vector3(x, y, z), this);
    }

    public void UpdateMesh() {
        generationThread = new Thread(GenerateMesh);
        generationThread.Start();
    }

    void GenerateMesh() {
        CreateInfo();

        SetMarchingCubes(mesh);
        meshReady = true;
    }

    private struct blockdata {
        public int posX;
        public int posY;
        public int posZ;
        public int data;
    }

    void CreateInfo() {
        Block block;
        Block[,,] neighbours = new Block[3, 3, 3];

        int code = 0;

        for (int x = 0; x < worldObj.maxChunkSize; x++) {
            for (int z = 0; z < worldObj.maxChunkSize; z++) {
                for (int y = 0; y < worldObj.maxHeight; y++) {
                    block = GetBlock(x, y, z);

                    int i = 0;
                    GetNeighbourBlocks(neighbours, block, out i);

                    if (i == 0) {
                        block.code = 0;
                        continue;
                    }

                    if (i == 25) {
                        block.code = 255;
                        continue;
                    }

                    Vector3 center = GetWorldPosition() + block.positionInChunk;

                    for (int y2 = 0; y2 < 2; y2++) {
                        for (int z2 = 0; z2 < 2; z2++) {
                            for (int x2 = 0; x2 < 2; x2++) {
                                int j = 0;

                                if (neighbours[x2, y2, z2] != null) {
                                    j = neighbours[x2, y2, z2].data;
                                }

                                if (center.y + y2 <= 0) {
                                    j = 1;
                                }

                                code <<= 1;
                                code |= j;
                            }
                        }
                    }

                    block.code = code;
                    code = 0;
                }
            }
        }
    }

    public Vector2 GetPosition() {
        return position;
    }

    public Vector3 GetWorldPosition() {
        return new Vector3(position.x * worldObj.maxChunkSize, 0, position.y * worldObj.maxChunkSize);
    }

    public Block GetBlock(int posX, int posY, int posZ) {
        return blocks[posX, posY, posZ];
    }

    public void GetNeighbourBlocks(Block[,,] neighbours, Block block) {
        int i = 0;
        GetNeighbourBlocks(neighbours, block, out i);
    }

    public void GetNeighbourBlocks(Block[,,] neighbours, Block block, out int i) {
        i = 0;

        //save previousely checked items, for optimazation
        Chunk prevCheckedChunk = null;
        int prevCheckCunkPosX = (int)position.x;
        int prevCheckCunkPosY = (int)position.y;

        for (int y = -1; y <= 1; y++) {
            for (int z = -1; z <= 1; z++) {
                for (int x = -1; x <= 1; x++) {

                    int checkX = (int)block.positionInChunk.x + x;
                    int checkY = (int)block.positionInChunk.y + y;
                    int checkZ = (int)block.positionInChunk.z + z;

                    int chunkX = (int)position.x;
                    int chunkY = (int)position.y;

                    Chunk checkChunk = this;

                    if(checkY < 0 || checkY >= worldObj.maxHeight) {
                        continue;
                    }

                    if (checkX < 0) {
                        chunkX--;
                        checkX = worldObj.maxChunkSize - 1;
                    }

                    if (checkX >= worldObj.maxChunkSize) {
                        chunkX++;
                        checkX = 0;
                    }

                    if (checkZ < 0) {
                        chunkY--;
                        checkZ = worldObj.maxChunkSize - 1;
                    }

                    if (checkZ >= worldObj.maxChunkSize) {
                        chunkY++;
                        checkZ = 0;
                    }

                    if (chunkX != position.x || chunkY != position.y) {
                        if (chunkX == prevCheckCunkPosX && chunkY == prevCheckCunkPosY) {
                            checkChunk = prevCheckedChunk;
                        } 
                        else {
                            checkChunk = worldObj.GetChunkDirectly(chunkX, chunkY);
                            prevCheckedChunk = checkChunk;
                            prevCheckCunkPosX = chunkX;
                            prevCheckCunkPosY = chunkY;
                        }
                    }

                    Block neighbour = checkChunk.GetBlock(checkX, checkY, checkZ);
                    
                    neighbours[x + 1, y + 1, z + 1] = neighbour;
                    
                    if(neighbour.data == 1) {
                        i++;
                    }
                }
            }
        }
    }

    List<Vector3> verts = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    List<int> tris = new List<int>();

    void SetMarchingCubes(Mesh mesh) {

        Vector3[] activeMarchingInfo = new Vector3[]{
                        new Vector3(0.5f, 0, 0),
                        new Vector3(0, 0, 0.5f),
                        new Vector3(1f, 0, 0.5f),
                        new Vector3(0.5f, 0, 1f),

                        new Vector3(0, 0.5f, 0),
                        new Vector3(1f, 0.5f, 0),
                        new Vector3(0, 0.5f, 1f),
                        new Vector3(1f, 0.5f, 1f),

                        new Vector3(0.5f, 1f, 0),
                        new Vector3(0, 1f, 0.5f),
                        new Vector3(1f, 1f, 0.5f),
                        new Vector3(0.5f, 1f, 1f)
                    };

        for (int y = 0; y < worldObj.maxHeight ; y++) {
            for (int z = 0; z < worldObj.maxChunkSize ; z++) {
                for (int x = 0; x < worldObj.maxChunkSize ; x++) {
                    Vector3 marchingCubeBegin = GetWorldPosition() + (new Vector3(x, y, z) * worldObj.sizeOfBlock);

                    int code = blocks[x, y, z].code;

                    if (code == 255 || code == 0) {
                        continue;
                    }

                    int triCount = tris.Count;

                    //System.Func needed to keep all local variables and don't make it messy
                    Func<int[], bool> AddFaces = delegate (int[] vert) {

                        for (int i = 0; i < vert.Length; i++) {
                            verts.Add((marchingCubeBegin + (activeMarchingInfo[vert[i]]) * worldObj.sizeOfBlock) - GetWorldPosition() + new Vector3(0, -0.5f, 0));
                            tris.Add(triCount + (Mathf.FloorToInt(i / 3) * 3) + (2 - (i % 3)));
                            uvs.Add(new Vector2(0, 0));
                        }
                        return true;
                    };

                    //All possible combinations of the block
                    switch (code) {
                        case 15: AddFaces(new int[] { 0, 1, 4 }); break;
                        case 16: AddFaces(new int[] { 2, 7, 3 }); break;
                        case 32: AddFaces(new int[] { 1, 3, 6 }); break;
                        case 48: AddFaces(new int[] { 1, 2, 7, 1, 7, 6}); break;
                        case 51: AddFaces(new int[] { 1, 2, 10, 1, 10, 9 }); break;
                        case 64: AddFaces(new int[] { 2, 0, 5 }); break;
                        case 80: AddFaces(new int[] { 0, 5, 3, 3, 5, 7}); break;
                        case 85: AddFaces(new int[] { 3, 0, 8, 3, 8, 11 }); break;
                        //case 192: AddFaces(new int[] { }); break;
                        case 112: AddFaces(new int[] { 0, 5, 1, 1, 5, 6, 5, 7, 6 }); break;
                        //case 113: AddFaces(new int[] { 1, 7, 2 }); break;
                        case 128: AddFaces(new int[] { 0, 1, 4 }); break;
                        case 160: AddFaces(new int[] { 0, 3, 4, 4, 3, 6}); break;
                        case 170: AddFaces(new int[] { 0, 3, 11, 0, 11, 8 }); break;
                        case 176: AddFaces(new int[] { 4, 0, 2, 4, 2, 7, 4, 7, 6 }); break;
                        //case 178: AddFaces(new int[] { 0, 3, 6 }); break;
                        case 192: AddFaces(new int[] { 4, 5, 2, 4, 2, 1 }); break;
                        case 204: AddFaces(new int[] { 2, 1, 9, 2, 9, 10}); break;
                        case 208: AddFaces(new int[] { 3, 1, 7, 7, 1, 4, 7, 4, 5 }); break;
                        //case 212: AddFaces(new int[] { 0, 5, 3 }); break;
                        case 224: AddFaces(new int[] { 4, 5, 6, 6, 5, 2, 6, 2, 3 }); break;
                        //case 232: AddFaces(new int[] { 1, 2, 4 }); break;
                        case 240: AddFaces(new int[] { 4, 5, 7, 4, 7, 6 }); break;
                        case 241: AddFaces(new int[] { 4, 5, 6, 6, 5, 10, 6, 10, 11 }); break;
                        case 242: AddFaces(new int[] { 4, 5, 7, 4, 7, 11, 4, 11, 9 }); break;
                        case 243: AddFaces(new int[] { 4, 5, 10, 4, 10, 9 }); break;
                        case 244: AddFaces(new int[] { 7, 6, 4, 7, 4, 8, 7, 8, 10 }); break;
                        case 245: AddFaces(new int[] { 4, 8, 6, 6, 8, 11}); break;
                        case 247: AddFaces(new int[] { 4, 8, 9 }); break;
                        case 248: AddFaces(new int[] { 9, 8, 5, 9, 5, 6, 6, 5, 7 }); break;
                        case 250: AddFaces(new int[] { 5, 7, 11, 5, 11, 8 }); break;
                        case 251: AddFaces(new int[] { 5, 10, 8 }); break;
                        case 252: AddFaces(new int[] { 7, 6, 9, 7, 9, 10 }); break;
                        case 253: AddFaces(new int[] { 6, 9, 11 }); break;
                        case 254: AddFaces(new int[] { 7, 11, 10 }); break;
                    }
                }
            }
        }
    }
}
