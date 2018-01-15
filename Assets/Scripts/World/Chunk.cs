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

    List<Vector3> verts = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    List<int> tris = new List<int>();

    World worldObj;
    Vector2 position;
    Block[,,] blocks;
    GameObject chunkObject;

    //true when update for mesh is needed
    public bool dirty = true;
    public List<Block> dirtyBlocks = new List<Block>();

    public Chunk() { }

    public Chunk(World world, Vector2 pos) {
        worldObj = world;
        position = pos;

        //moved it to a coroutine
    }

    /// <summary>
    /// First setup of blocks
    /// </summary>
    public void GenerateBlocks(Transform parent) {
        //create the chunk object in the scene
        chunkObject = new GameObject("chunk " + position.x + "/" + position.y);
        chunkObject.transform.position = new Vector3(position.x * worldObj.maxChunkSize, 0, position.y * worldObj.maxChunkSize) * worldObj.sizeOfBlock;
        chunkObject.transform.SetParent(parent);
        chunkObject.layer = LayerMask.NameToLayer("Terrain");

        chunkObject.SetActive(false);

        //instantiate the block array
        blocks = new Block[worldObj.maxChunkSize, worldObj.maxHeight, worldObj.maxChunkSize];

        float[,] map = Noise.GetMap(worldObj.maxChunkSize, position.x * worldObj.maxChunkSize, position.y * worldObj.maxChunkSize, worldObj.seed, worldObj.octaves);

        for (int x = 0; x < worldObj.maxChunkSize; x++) {
            for (int y = 0; y < worldObj.maxHeight; y++) {
                for (int z = 0; z < worldObj.maxChunkSize; z++) {
                    //height value between 0 and 1 based on x and z
                    float perlin = map[x, z];

                    //top of the height value
                    int i = (int)(worldObj.maxHeight / 3) + (int)((worldObj.maxHeight / 3) * perlin);

                    //top layer = 1; 2 layers below that is 2; below that is 3; air is 0
                    int data = y == 0 ? 2 : (y == i ? 1 : (y < i ? (y < i - 2 ? 3 : 2) : 0));

                    blocks[x, y, z] = new Block(data, new Vector3(x, y, z), this);
                }
            }
        }

        //yield return null;
    }

    /// <summary>
    /// Generate information for the mesh
    /// </summary>
    public void GenerateMesh() {
        CreateInfo();//generate the info for the marching cubes and the color

        SetMarchingCubes(mesh); //create the mesh for the chunk based on the blocks data

        worldObj.doneGenerating = true;
    }

    public void UpdateMesh() {

        Debug.Log("go");

        Block[,,] neighbours = new Block[3, 3, 3];

        int code = 0;

        foreach (Block block in dirtyBlocks) {
            int i = 0;
            GetNeighbourBlocks(neighbours, block, out i);

            if (i == 0) {
                block.code = 0;
                continue;
            }

            if (i == 25) { //maximum neighbours minus itself
                block.code = 255;
                continue;
            }

            Vector3 center = GetWorldPosition() + block.positionInChunk;

            int[] colorMajority = new int[4];

            for (int y2 = 0; y2 < 2; y2++) {
                for (int z2 = 0; z2 < 2; z2++) {
                    for (int x2 = 0; x2 < 2; x2++) {
                        int j = 0;

                        if (neighbours[x2, y2, z2] != null) {
                            j = neighbours[x2, y2, z2].data == 0 ? 0 : 1;
                        }

                        if (j != 0) {
                            colorMajority[neighbours[x2, y2, z2].data]++;
                        }

                        if (center.y + y2 <= 0) {
                            j = 1;
                        }

                        code <<= 1;
                        code |= j;
                    }
                }
            }

            int colorCode = 0;
            int amount = 0;

            for (i = 0; i < colorMajority.Length; i++) {
                if (colorMajority[i] > amount) {
                    colorCode = i;
                    amount = colorMajority[i];
                }
            }

            block.colorCode = colorCode;
            block.code = code;
            code = 0;
        }

        dirtyBlocks.Clear();

        Debug.Log("end");

        SetMarchingCubes(mesh); //create the mesh for the chunk based on the blocks data

        Debug.Log("mesh");

        worldObj.doneGenerating = true;
    }

    /// <summary>
    /// Add all the information to the mesh
    /// </summary>
    public void FinalizeChunk() {
        //add components if needed, otherwise get them
        if (!chunkObject.GetComponent<MeshFilter>()) {
            meshFilter = chunkObject.AddComponent<MeshFilter>();
        }else {
            meshFilter = chunkObject.GetComponent<MeshFilter>();
        }
        if (!chunkObject.GetComponent<MeshRenderer>()) {
            meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        } else {
            meshRenderer = chunkObject.GetComponent<MeshRenderer>();
        }
        if (!chunkObject.GetComponent<MeshCollider>()) {
            meshCollider = chunkObject.AddComponent<MeshCollider>();
        } else {
            meshCollider = chunkObject.GetComponent<MeshCollider>();
        }

        //create and add data to the mesh
        mesh = new Mesh();
        mesh.name = chunkObject.name;

        mesh.vertices = verts.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();

        mesh.RecalculateNormals();

        //set components
        meshFilter.mesh = null;
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;

        meshRenderer.material = Resources.Load<Material>("Material/WorldMaterial");

        //Debug.Log("Chunk Done Loading");

        //delete unnecessary data, it is no longer needed
        verts.Clear();
        uvs.Clear();
        tris.Clear();
        mesh = null;
        meshFilter = null;
        meshRenderer = null;
        meshCollider = null;
    }

    /// <summary>
    /// prepare all blocks with the marching cube code
    /// </summary>
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

                    if (i == 25) { //maximum neighbours minus itself
                        block.code = 255;
                        continue;
                    }

                    Vector3 center = GetWorldPosition() + block.positionInChunk;

                    int[] colorMajority = new int[4];

                    for (int y2 = 0; y2 < 2; y2++) {
                        for (int z2 = 0; z2 < 2; z2++) {
                            for (int x2 = 0; x2 < 2; x2++) {
                                int j = 0;

                                if (neighbours[x2, y2, z2] != null) {
                                    j = neighbours[x2, y2, z2].data == 0 ? 0 : 1;
                                }

                                if (j != 0) {
                                    colorMajority[neighbours[x2, y2, z2].data]++;
                                }

                                if (center.y + y2 <= 0) {
                                    j = 1;
                                }

                                code <<= 1;
                                code |= j;
                            }
                        }
                    }

                    int colorCode = 0;
                    int amount = 0;

                    for (i = 0; i < colorMajority.Length; i++) {
                        if (colorMajority[i] > amount) {
                            colorCode = i;
                            amount = colorMajority[i];
                        }
                    }

                    block.colorCode = colorCode;
                    block.code = code;
                    code = 0;
                }
            }
        }
    }

    /// <summary>
    /// Get all neighbour blocks
    /// </summary>
    public void GetNeighbourBlocks(Block[,,] neighbours, Block block) {
        int i = 0;
        GetNeighbourBlocks(neighbours, block, out i);
    }

    /// <summary>
    /// Get all neighbour blocks
    /// </summary>
    public void GetNeighbourBlocks(Block[,,] neighbours, Block block, out int i) {
        i = 0; //variables to check how many non air blocks it has

        //save previousely checked items, for optimazation
        Chunk prevCheckedChunk = null;
        int prevCheckCunkPosX = (int)position.x;
        int prevCheckCunkPosY = (int)position.y;

        for (int y = -1; y <= 1; y++) {
            for (int z = -1; z <= 1; z++) {
                for (int x = -1; x <= 1; x++) {

                    if (x == 0 && y == 0 && z == 0) {
                        neighbours[1, 1, 1] = block;
                        continue;
                    }

                    //base variables
                    int checkX = (int)block.positionInChunk.x + x;
                    int checkY = (int)block.positionInChunk.y + y;
                    int checkZ = (int)block.positionInChunk.z + z;

                    int chunkX = (int)position.x;
                    int chunkY = (int)position.y;

                    Chunk checkChunk = this;

                    //dont go further is below 0 or above max height level
                    if(checkY < 0 || checkY >= worldObj.maxHeight) {
                        continue;
                    }

                    //change the check variables if needed
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
                        //if the chunk check position is the same as previous, use the previous chunk
                        if (chunkX == prevCheckCunkPosX && chunkY == prevCheckCunkPosY) {
                            checkChunk = prevCheckedChunk;
                        } 
                        else {//check for the chunk in the world
                            checkChunk = worldObj.GetChunk(chunkX, chunkY);
                            prevCheckedChunk = checkChunk;
                            prevCheckCunkPosX = chunkX;
                            prevCheckCunkPosY = chunkY;
                        }
                    }

                    Block neighbour = checkChunk.GetBlock(checkX, checkY, checkZ);

                    neighbours[x + 1, y + 1, z + 1] = neighbour;

                    if (neighbour.data != 0) {
                        i++;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Create information for the mesh
    /// </summary>
    void SetMarchingCubes(Mesh mesh) {

        //attachment points for the marching cubes
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

        //baseline for uv
        Vector2[] uvHelper = new Vector2[] {
            new Vector2(0.05f, 0.05f),
            new Vector2(0.15f, 0.05f),
            new Vector2(0.15f, 0.15f)
        };

        for (int y = 0; y < worldObj.maxHeight; y++) {
            for (int z = 0; z < worldObj.maxChunkSize; z++) {
                for (int x = 0; x < worldObj.maxChunkSize; x++) {
                    Vector3 marchingCubeBegin = GetWorldPosition() + (new Vector3(x, y, z) * worldObj.sizeOfBlock);

                    int code = blocks[x, y, z].code;

                    //if fully surrounded or no data, dont bother adding mesh
                    if (code == 255 || code == 0) {
                        continue;
                    }

                    int triCount = tris.Count;

                    //System.Func needed to keep all local variables and don't make it messy
                    Func<int[], bool> AddFaces = delegate (int[] vert) {

                        for (int i = 0; i < vert.Length; i++) {
                            //add vertice based on the int array
                            verts.Add((marchingCubeBegin + (activeMarchingInfo[vert[i]]) * worldObj.sizeOfBlock) - GetWorldPosition() + new Vector3(0, -0.5f, 0));
                            //always create a triangle clockwise
                            tris.Add(triCount + (Mathf.FloorToInt(i / 3) * 3) + (2 - (i % 3)));
                            //add simple uv triangle based on the colorcode
                            uvs.Add(new Vector2(uvHelper[i % 3].x + (int)(blocks[x, y, z].colorCode % 4) * 0.25f, uvHelper[i % 3].y + (int)(blocks[x, y, z].colorCode / 4) * 0.25f));
                        }
                        return true;
                    };

                    //All possible combinations of the block
                    switch (code) {
                        //case : AddFaces(new int[] {  }); break; // template
                        case 1: AddFaces(new int[] { 11, 7, 10 }); break;
                        case 2: AddFaces(new int[] { 9, 6, 11 }); break;
                        case 3: AddFaces(new int[] { 9, 6, 7, 9, 7, 10 }); break;
                        case 4: AddFaces(new int[] { 10, 5, 8 }); break;
                        case 5: AddFaces(new int[] { 11, 7, 5, 11, 5, 8 }); break;
                        case 6: AddFaces(new int[] { 9, 6, 11, 10, 5, 8 }); break;
                        case 7: AddFaces(new int[] { 6, 8, 9, 6, 5, 8, 5, 6, 7 }); break;
                        case 8: AddFaces(new int[] { 8, 4, 9 }); break;
                        case 9: AddFaces(new int[] { 8, 4, 9, 11, 7, 10 }); break;
                        case 10: AddFaces(new int[] { 8, 4, 6, 8, 6, 11 }); break;
                        case 11: AddFaces(new int[] { 4, 6, 7, 4, 7, 10, 4, 10, 8 }); break;
                        case 12: AddFaces(new int[] { 10, 5, 4, 10, 4, 9 }); break;
                        case 13: AddFaces(new int[] { 7, 5, 4, 7, 4, 9, 7, 9, 11 }); break;
                        case 14: AddFaces(new int[] { 5, 4, 6, 5, 6, 11, 5, 11, 10 }); break;
                        case 15: AddFaces(new int[] { 4, 7, 5, 4, 6, 7 }); break;
                        case 16: AddFaces(new int[] { 2, 7, 3 }); break;
                        case 17: AddFaces(new int[] { 3, 2, 10, 3, 10, 11 }); break;
                        case 18: AddFaces(new int[] { 9, 6, 11, 3, 2, 7 }); break;
                        case 19: AddFaces(new int[] { 9, 2, 10, 9, 6, 3, 9, 3, 2 }); break;
                        case 20: AddFaces(new int[] { 3, 2, 7, 10, 5, 8 }); break;
                        case 21: AddFaces(new int[] { 11, 3, 8, 8, 3, 2, 8, 2, 5 }); break;
                        case 22: AddFaces(new int[] { 3, 2, 7, 10, 5, 8, 9, 6, 11 }); break;
                        case 23: AddFaces(new int[] { 6, 3, 2, 6, 2, 5, 6, 5, 8, 6, 8, 9 }); break;
                        case 24: AddFaces(new int[] { 3, 2, 7, 8, 4, 9 }); break;
                        case 25: AddFaces(new int[] { 8, 4, 9, 11, 3, 2, 11, 2, 10 }); break;
                        case 26: AddFaces(new int[] { 3, 2, 7, 8, 4, 6, 8, 6, 11 }); break;
                        case 27: AddFaces(new int[] { 3, 4, 6, 4, 3, 2, 4, 2, 8, 8, 2, 10 }); break;
                        case 28: AddFaces(new int[] { 3, 2, 7, 10, 5, 9, 5, 4, 9 }); break;
                        case 29: AddFaces(new int[] { 4, 2, 5, 4, 3, 2, 4, 9, 3, 3, 9, 11 }); break;
                        case 30: AddFaces(new int[] { 5, 4, 6, 5, 6, 10, 10, 6, 11, 3, 2, 7 }); break;
                        case 31: AddFaces(new int[] { 6, 5, 4, 6, 2, 5, 6, 3, 2 }); break;
                        case 32: AddFaces(new int[] { 1, 3, 6 }); break;
                        case 33: AddFaces(new int[] { 1, 3, 6, 11, 7, 10 }); break;
                        case 34: AddFaces(new int[] { 1, 3, 11, 1, 11, 9 }); break;
                        case 35: AddFaces(new int[] { 1, 10, 9, 1, 7, 10, 1, 3, 7 }); break;
                        case 36: AddFaces(new int[] { 1, 3, 6, 10, 5, 8 }); break;
                        case 37: AddFaces(new int[] { 1, 3, 6, 11, 7, 8, 7, 5, 8 }); break;
                        case 38: AddFaces(new int[] { 10, 5, 8, 9, 1, 11, 1, 3, 11 }); break;
                        case 39: AddFaces(new int[] { 1, 8, 9, 1, 5, 8, 1, 3, 5, 3, 7, 5 }); break;
                        case 40: AddFaces(new int[] { 1, 3, 6, 8, 4, 9 }); break;
                        case 41: AddFaces(new int[] { 1, 3, 6, 8, 4, 9, 11, 7, 10 }); break;
                        case 42: AddFaces(new int[] { 8, 3, 11, 8, 4, 1, 8, 1, 3 }); break;
                        case 43: AddFaces(new int[] { 4, 10, 8, 4, 7, 10, 4, 3, 7, 4, 1, 3 }); break;
                        case 44: AddFaces(new int[] { 1, 3, 6, 10, 5, 4, 10, 4, 9 }); break;
                        case 45: AddFaces(new int[] { 1, 3, 6, 7, 9, 11, 7, 4, 9, 7, 5, 4 }); break;
                        case 46: AddFaces(new int[] { 1, 5, 4, 1, 3, 5, 5, 3, 10, 10, 3, 11 }); break;
                        case 47: AddFaces(new int[] { 1, 3, 4, 4, 3, 7, 4, 7, 5 }); break;
                        case 48: AddFaces(new int[] { 1, 2, 7, 1, 7, 6 }); break;
                        case 49: AddFaces(new int[] { 1, 2, 10, 1, 10, 11, 1, 11, 6 }); break;
                        case 50: AddFaces(new int[] { 9, 1, 2, 9, 2, 7, 9, 7, 11 }); break;
                        case 51: AddFaces(new int[] { 1, 2, 10, 1, 10, 9 }); break;
                        case 52: AddFaces(new int[] { 10, 5, 8, 6, 1, 7, 1, 2, 7 }); break;
                        case 53: AddFaces(new int[] { 6, 8, 11, 6, 1, 8, 1, 5, 8, 1, 2, 5 }); break;
                        case 54: AddFaces(new int[] { 10, 5, 8, 9, 1, 2, 2, 7, 9, 7, 11, 9 }); break;
                        case 55: AddFaces(new int[] { 9, 1, 2, 9, 2, 5, 9, 5, 8 }); break;
                        case 56: AddFaces(new int[] { 8, 4, 9, 6, 1, 2, 6, 2, 7 }); break;
                        case 57: AddFaces(new int[] { 8, 4, 8, 1, 2, 10, 1, 10, 11, 1, 11, 6 }); break;
                        case 58: AddFaces(new int[] { 1, 2, 4, 4, 2, 8, 8, 2, 7, 8, 7, 11 }); break;
                        case 59: AddFaces(new int[] { 1, 2, 10, 4, 1, 10, 4, 10, 8 }); break;
                        case 60: AddFaces(new int[] { 6, 1, 2, 6, 2, 7, 10, 5, 4, 10, 4, 9 }); break;
                        case 61: AddFaces(new int[] { 11, 6, 9, 4, 1, 2, 4, 2, 5 }); break;
                        case 62: AddFaces(new int[] { 10, 7, 11, 4, 1, 2, 4, 2, 5 }); break;
                        case 63: AddFaces(new int[] { 4, 1, 2, 4, 2, 5 }); break;
                        case 64: AddFaces(new int[] { 2, 0, 5 }); break;
                        case 65: AddFaces(new int[] { 11, 7, 10, 2, 0, 5 }); break;
                        case 66: AddFaces(new int[] { 2, 0, 5, 9, 6, 11 }); break;
                        case 67: AddFaces(new int[] { 2, 0, 5, 9, 6, 7, 9, 7, 10 }); break;
                        case 68: AddFaces(new int[] { 2, 0, 8, 2, 8, 10 }); break;
                        case 69: AddFaces(new int[] { 11, 0, 8, 7, 2, 0, 7, 0, 11 }); break;
                        case 70: AddFaces(new int[] { 9, 6, 11, 10, 2, 0, 10, 0, 8 }); break;
                        case 71: AddFaces(new int[] { 6, 7, 9, 9, 7, 8, 2, 8, 7, 2, 0, 8 }); break;
                        case 72: AddFaces(new int[] { 2, 0, 5, 8, 4, 9 }); break;
                        case 73: AddFaces(new int[] { 8, 4, 9, 11, 7, 10, 2, 0, 5 }); break;
                        case 74: AddFaces(new int[] { 2, 0, 5, 8, 4, 6, 8, 6, 11 }); break;
                        case 75: AddFaces(new int[] { 2, 0, 5, 4, 6, 7, 8, 4, 7, 8, 7, 10 }); break;
                        case 76: AddFaces(new int[] { 10, 2, 9, 2, 0, 9, 0, 4, 9 }); break;
                        case 77: AddFaces(new int[] { 2, 0, 4, 2, 4, 9, 2, 9, 11, 2, 11, 7 }); break;
                        case 78: AddFaces(new int[] { 6, 0, 4, 2, 0, 6, 2, 6, 11, 2, 11, 10 }); break;
                        case 79: AddFaces(new int[] { 6, 7, 4, 7, 2, 4, 2, 4, 0 }); break;
                        case 80: AddFaces(new int[] { 0, 5, 3, 3, 5, 7 }); break;
                        case 81: AddFaces(new int[] { 3, 0, 11, 11, 0, 5, 11, 5, 10 }); break;
                        case 82: AddFaces(new int[] { 9, 6, 11, 7, 3, 0, 7, 0, 5 }); break;
                        case 83: AddFaces(new int[] { 3, 0, 6, 6, 0, 9, 9, 0, 5, 9, 5, 10 }); break;
                        case 84: AddFaces(new int[] { 3, 0, 8, 3, 8, 10, 3, 10, 7 }); break;
                        case 85: AddFaces(new int[] { 3, 0, 8, 3, 8, 11 }); break;
                        case 86: AddFaces(new int[] { 9, 6, 11, 3, 0, 8, 7, 3, 8, 7, 8, 0 }); break;
                        case 87: AddFaces(new int[] { 3, 0, 8, 6, 3, 8, 6, 8, 9 }); break;
                        case 88: AddFaces(new int[] { 8, 4, 9, 7, 3, 0, 7, 0, 5 }); break;
                        case 89: AddFaces(new int[] { 8, 4, 9, 11, 3, 0, 11, 0, 5, 11, 5, 10 }); break;
                        case 90: AddFaces(new int[] { 8, 4, 6, 8, 6, 11, 7, 3, 0, 7, 0, 5 }); break;
                        case 91: AddFaces(new int[] { 8, 5, 10, 6, 3, 0, 6, 0, 4 }); break;
                        case 92: AddFaces(new int[] { 3, 0, 4, 3, 4, 9, 7, 3, 9, 7, 9, 10 }); break;
                        case 93: AddFaces(new int[] { 11, 3, 0, 11, 0, 4, 11, 4, 9 }); break;
                        case 94: AddFaces(new int[] { 6, 3, 0, 6, 0, 4, 10, 7, 11 }); break;
                        case 95: AddFaces(new int[] { 6, 3, 0, 6, 0, 4 }); break;
                        case 96: AddFaces(new int[] { 6, 1, 3, 2, 0, 5 }); break;
                        case 97: AddFaces(new int[] { 6, 1, 3, 2, 0, 5, 11, 7, 10 }); break;
                        case 98: AddFaces(new int[] { 2, 0, 5, 9, 1, 3, 9, 3, 11 }); break;
                        case 99: AddFaces(new int[] { 2, 0, 5, 9, 1, 10, 10, 1, 3, 10, 3, 7 }); break;
                        case 100: AddFaces(new int[] { 6, 1, 3, 10, 2, 0, 10, 0, 8 }); break;
                        case 101: AddFaces(new int[] { 6, 1, 3, 11, 0, 8, 11, 0, 2, 11, 2, 7 }); break;
                        case 112: AddFaces(new int[] { 0, 5, 1, 1, 5, 6, 5, 7, 6 }); break;
                        case 113: AddFaces(new int[] { 1, 0, 6, 6, 0, 5, 6, 5, 10, 6, 10, 11 }); break;
                        case 115: AddFaces(new int[] { 1, 0, 5, 1, 5, 10, 1, 10, 9 }); break;
                        case 116: AddFaces(new int[] { 0, 8, 11, 0, 11, 6, 0, 6, 1 }); break;
                        case 119: AddFaces(new int[] { 0, 8, 1, 8, 9, 1 }); break;
                        case 128: AddFaces(new int[] { 0, 1, 4 }); break;
                        case 136: AddFaces(new int[] { 0, 1, 9, 0, 9, 8 }); break;
                        case 144: AddFaces(new int[] { 4, 0, 1, 7, 3, 2 }); break;
                        case 159: AddFaces(new int[] { 1, 6, 3, 2, 5, 0 }); break;
                        case 160: AddFaces(new int[] { 0, 3, 4, 4, 3, 6 }); break;
                        case 162: AddFaces(new int[] { 0, 3, 11, 0, 11, 9, 0, 9, 4 }); break;
                        case 168: AddFaces(new int[] { 8, 0, 3, 8, 3, 6, 8, 6, 9 }); break;
                        case 170: AddFaces(new int[] { 0, 3, 11, 0, 11, 8 }); break;
                        case 171: AddFaces(new int[] { 8, 0, 3, 8, 3, 7, 8, 7, 10 }); break;
                        case 176: AddFaces(new int[] { 4, 0, 2, 4, 2, 7, 4, 7, 6 }); break;
                        case 178: AddFaces(new int[] { 4, 0, 2, 4, 2, 7, 4, 7, 11, 4, 11, 9 }); break;
                        case 179: AddFaces(new int[] { 2, 10, 9, 2, 9, 4, 2, 4, 0 }); break;
                        case 186: AddFaces(new int[] { 0, 2, 7, 0, 7, 11, 0, 11, 8 }); break;
                        case 187: AddFaces(new int[] { 8, 0, 2, 8, 2, 10 }); break;
                        case 192: AddFaces(new int[] { 4, 5, 2, 4, 2, 1 }); break;
                        case 196: AddFaces(new int[] { 10, 2, 1, 10, 1, 4, 10, 4, 8 }); break;
                        case 200: AddFaces(new int[] { 2, 1, 9, 2, 9, 8, 2, 8, 5 }); break;
                        case 204: AddFaces(new int[] { 2, 1, 9, 2, 9, 10 }); break;
                        case 206: AddFaces(new int[] { 10, 2, 1, 10, 1, 6, 10, 6, 11 }); break;
                        case 208: AddFaces(new int[] { 3, 1, 7, 7, 1, 4, 7, 4, 5 }); break;
                        case 212: AddFaces(new int[] { 3, 1, 7, 7, 1, 4, 7, 4, 8, 7, 8, 10 }); break;
                        case 213: AddFaces(new int[] { 3, 1, 4, 3, 4, 8, 3, 8, 11 }); break;
                        case 220: AddFaces(new int[] { 1, 9, 10, 1, 10, 7, 1, 7, 3 }); break;
                        case 221: AddFaces(new int[] { 9, 3, 1, 9, 11, 3 }); break;
                        case 224: AddFaces(new int[] { 4, 5, 6, 6, 5, 2, 6, 2, 3 }); break;
                        case 232: AddFaces(new int[] { 5, 2, 3, 5, 3, 6, 5, 6, 9, 5, 9, 8 }); break;
                        case 234: AddFaces(new int[] { 3, 11, 8, 3, 8, 5, 3, 5, 2 }); break;
                        case 235: AddFaces(new int[] { 3, 5, 2, 3, 8, 5, 3, 10, 8, 3, 7, 10 }); break;
                        case 236: AddFaces(new int[] { 2, 3, 6, 2, 6, 9, 2, 9, 10 }); break;
                        case 237: AddFaces(new int[] { 2, 9, 11, 2, 11, 7, 2, 6, 9, 2, 3, 6 }); break;
                        case 238: AddFaces(new int[] { 11, 2, 3, 11, 10, 2 }); break;
                        case 239: AddFaces(new int[] { 2, 3, 7 }); break;
                        case 240: AddFaces(new int[] { 4, 5, 7, 4, 7, 6 }); break;
                        case 241: AddFaces(new int[] { 4, 5, 6, 6, 5, 10, 6, 10, 11 }); break;
                        case 242: AddFaces(new int[] { 4, 5, 7, 4, 7, 11, 4, 11, 9 }); break;
                        case 243: AddFaces(new int[] { 4, 5, 10, 4, 10, 9 }); break;
                        case 244: AddFaces(new int[] { 7, 6, 4, 7, 4, 8, 7, 8, 10 }); break;
                        case 245: AddFaces(new int[] { 4, 8, 6, 6, 8, 11 }); break;
                        case 246: AddFaces(new int[] { 4, 7, 11, 4, 11, 9, 4, 10, 7, 4, 8, 10 }); break;
                        case 247: AddFaces(new int[] { 4, 8, 9 }); break;
                        case 248: AddFaces(new int[] { 9, 8, 5, 9, 5, 6, 6, 5, 7 }); break;
                        case 249: AddFaces(new int[] { 6, 5, 10, 6, 10, 11, 5, 6, 9, 5, 9, 8 }); break;
                        case 250: AddFaces(new int[] { 5, 7, 11, 5, 11, 8 }); break;
                        case 251: AddFaces(new int[] { 5, 10, 8 }); break;
                        case 252: AddFaces(new int[] { 7, 6, 9, 7, 9, 10 }); break;
                        case 253: AddFaces(new int[] { 6, 9, 11 }); break;
                        case 254: AddFaces(new int[] { 7, 11, 10 }); break;
                    }

                    if (tris.Count % 3 != 0) {
                        Debug.Log(code);
                    }
                }
            }
        }
    }

    /// <summary>
    /// True when in view
    /// </summary>
    public bool InView(Vector2 playerPos) {
        Vector2 centerPos = new Vector2(chunkObject.transform.position.x, chunkObject.transform.position.z) + new Vector2(worldObj.maxChunkSize * 0.5f, worldObj.maxChunkSize * 0.5f);
        return Vector2.Distance(centerPos, playerPos) < (worldObj.renderRange - 1) * worldObj.maxChunkSize;
    }

    /// <summary>
    /// True when in view
    /// </summary>
    public bool InLoadingArea(Vector2 playerPos) {
        Vector2 centerPos = new Vector2(chunkObject.transform.position.x, chunkObject.transform.position.z) + new Vector2(worldObj.maxChunkSize * 0.5f, worldObj.maxChunkSize * 0.5f);
        return Vector2.Distance(centerPos, playerPos) <= (worldObj.renderRange + 1) * worldObj.maxChunkSize;
    }

    /// <summary>
    /// True when chunkObject is active
    /// </summary>
    public bool IsVisible() {
        return chunkObject.activeSelf;
    }

    /// <summary>
    /// return worldobj
    /// </summary>
    public World GetWorldObj() {
        return worldObj;
    }

    /// <summary>
    /// return chunk position
    /// </summary>
    public Vector2 GetPosition() {
        return position;
    }

    /// <summary>
    /// return chunk GameObject
    /// </summary>
    public GameObject GetChunkObject() {
        return chunkObject;
    }

    /// <summary>
    /// return chunk world position
    /// </summary>
    public Vector3 GetWorldPosition() {
        return new Vector3(position.x * worldObj.maxChunkSize, 0, position.y * worldObj.maxChunkSize);
    }

    /// <summary>
    /// return block based on x, y, z
    /// </summary>
    public Block GetBlock(int posX, int posY, int posZ) {
        return blocks[posX, posY, posZ];
    }
}
