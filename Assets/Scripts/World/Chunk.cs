using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Chunk {

    public World worldObj;
    Vector2 position;
    //Block[,,] data;
    Dictionary<Vector3, Block> blocks = new Dictionary<Vector3, Block>();
    GameObject chunkObject;

    Dictionary<Vector3, int> marchingInfo = new Dictionary<Vector3, int>();
    Dictionary<Vector3, bool> marching = new Dictionary<Vector3, bool>();

    List<Vector3> meshVerts = new List<Vector3>();
    List<int> meshTris = new List<int>();
    List<Vector2> meshUV = new List<Vector2>();

    public Chunk() { }

    public Chunk(World world, Vector2 pos, Transform parent) {
        worldObj = world;
        position = pos;
        //data = new Block[(int)maxSize.x + 2, (int)maxSize.y, (int)maxSize.z];

        chunkObject = new GameObject("chunk " + position.x + "/" + position.y);
        chunkObject.transform.position = new Vector3(position.x * worldObj.maxChunkSize, 0, position.y * worldObj.maxChunkSize);
        chunkObject.transform.SetParent(parent);

        Generate();

        SetVisible(false);
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

        if (!chunkObject.GetComponent<MeshFilter>() && b) {
            GenerateMesh();
            worldObj.shown++;
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
        //data[x, y, z] = new Block(i, new Vector3(x, y, z)); ;
        blocks.Add(new Vector3(x, y, z), new Block(i, new Vector3(x, y, z), this));
    }

    void GenerateMesh() {
        /*
        if(!worldObj.told)
        for (int x = 0; x < worldObj.maxChunkSize; x++) {
            for (int z = 0; z < worldObj.maxChunkSize; z++) {
                int heighest = -1;
                for (int y = 0; y < worldObj.maxHeight; y++) {
                    if (blocks[new Vector3(x, y, z)].data == 1) {
                        heighest++;
                        if (heighest < worldObj.maxHeight - 1) {
                            //continue;
                        }
                    } 
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.SetParent(chunkObject.transform);
                    go.transform.position = new Vector3((position.x * worldObj.maxChunkSize) + x, heighest, (position.y * worldObj.maxChunkSize) + z);
                    go.transform.localScale = Vector3.one * 0.25f;
                    //break;
                }
            }
        }
        worldObj.told = true;
        //*/

        MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        mesh.name = chunkObject.name;

        CreateMesh(mesh);

        if(!worldObj.gotit)
            SetMarchingCubes(mesh);

        

        

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = Resources.Load<Material>("Material/WorldMaterial");


        marching.Clear();
        marchingInfo.Clear();
    }

    void CreateMesh(Mesh mesh) {
        for (int x = 0; x < worldObj.maxChunkSize; x++) {//check every block in the chunk
            for (int z = 0; z < worldObj.maxChunkSize; z++) {//
                for (int y = 0; y < worldObj.maxHeight; y++) {//
                    Block block = GetBlock(x, y, z);

                    //if (block.data == 0) //block has no data? dont bother looking further
                    ///    continue;

                    SetMarchingInfo(block);
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

    public Block GetBlock(Vector3 pos) {
        return blocks[pos];
    }

    public Block GetBlock(int posX, int posY, int posZ) {
        return blocks[new Vector3(posX, posY, posZ)];
    }

    Dictionary<Vector3, Block> GetNeighbourBlocks(Block block) {
        Vector3 pos = block.positionInChunk;

        Dictionary<Vector3, Block> neighbours = new Dictionary<Vector3, Block>();

        for (int y = -1; y <= 1; y++) {
            for (int z = -1; z <= 1; z++) {
                for (int x = -1; x <= 1; x++) {
                    if (x == 0 && y == 0 && z == 0)
                        continue;

                    int checkX = Mathf.FloorToInt(pos.x + x);
                    int checkY = Mathf.FloorToInt(pos.y + y);
                    int checkZ = Mathf.FloorToInt(pos.z + z);

                    int chunkX = Mathf.FloorToInt(position.x);
                    int chunkY = Mathf.FloorToInt(position.y);

                    Chunk checkChunk = this;

                    if(checkY < 0 || checkY >= worldObj.maxHeight) {
                        /*
                        Vector3 newkey = new Vector3(position.x * worldObj.maxChunkSize + checkX, checkY, position.y * worldObj.maxChunkSize + checkZ);
                        if (!neighbours.ContainsKey(newkey))
                            neighbours.Add(newkey, null);
                        */
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

                    if (chunkX != position.x || chunkY != position.y)
                        checkChunk = worldObj.GetChunk(chunkX, chunkY);

                    Block neighbour = checkChunk.GetBlock(new Vector3(checkX, checkY, checkZ));

                    neighbours.Add(neighbour.GetWorldPosition(), neighbour);
                }
            }
        }

        return neighbours;
    }

    void SetMarchingInfo(Block block) {

        Dictionary<Vector3, Block> neighbours = GetNeighbourBlocks(block);
        Vector3 center = GetWorldPosition() + block.positionInChunk;

        int code = 0;
        int i = 0;

        for (int y = 0; y < 2; y++) {
            for (int z = 0; z < 2; z++) {
                for (int x = 0; x < 2; x++) {

                    Vector3 key = center - (Vector3.one * 0.5f) + new Vector3((float)x * 0.5f, (float)y * 0.5f, (float)z * 0.5f);

                    for (int y2 = 0; y2 < 2; y2++) {
                        for (int z2 = 0; z2 < 2; z2++) {
                            for (int x2 = 0; x2 < 2; x2++) {
                                Vector3 blockLookup = center - Vector3.one + new Vector3(x + x2, y + y2, z + z2);

                                if (!worldObj.gotit)
                                    //Debug.Log(blockLookup);

                                if (blockLookup.x == 0 && blockLookup.y == 0 && blockLookup.z == 0) {
                                    code <<= 1;
                                    code |= block.data;
                                    i++;
                                    continue;
                                }

                                int j = 0;
                                if (neighbours.ContainsKey(blockLookup)) {
                                    j = neighbours[blockLookup].data;
                                }

                                if (blockLookup.y <= 0) {
                                    j = 1;
                                }

                                code <<= 1;
                                code |= j;
                                i++;
                            }
                        }
                    }

                    if (!worldObj.gotit)
                        Debug.Log(code);

                    if (i == 8) {
                        if (!marchingInfo.ContainsKey(key)) {
                            marchingInfo.Add(key, code);
                        } else {
                            marchingInfo[key] = code;
                        }
                    }

                    code = 0;
                    i = 0;
                }
            }
        }

        /* //WORKING CODE
        for (int y = 0; y < 2; y++) {
            for (int z = 0; z < 2; z++) {
                for (int x = 0; x < 2; x++) { 
                    Vector3 blockLookup = center + new Vector3(x, y, z);

                    if (x == 0 && y == 0 && z == 0) {
                        code <<= 1;
                        code |= block.data;
                        i++;
                        continue;
                    }

                    int j = 0;
                    if (neighbours.ContainsKey(blockLookup)) {
                        j = neighbours[blockLookup].data;
                    }

                    if (blockLookup.y <= 0) {
                        j = 1;
                    }

                    code <<= 1;
                    code |= j;
                    i++;
                }
            }
        }

        if(!worldObj.gotit)
            Debug.Log(code);

        if (i == 8) {
            if (!marchingInfo.ContainsKey(center)) {
                marchingInfo.Add(center, code);
            } else {
                marchingInfo[center] = code;
            }
        }

        code = 0;
        i = 0;

        //END WORKING CODE

        /*
        for (int y = -1; y <= 2; y++) {
            for (int z = -1; z <= 1; z++) {
                for (int x = -1; x <= 1; x++) {
                    Vector3 key = center + new Vector3((float)x * 0.5f, (float)y * 0.5f, (float)z * 0.5f);

                    Vector3 blockLookup = center + new Vector3(x, y, z);

                    if (x == 0 && y == 0 && z == 0) {
                        if (!marching.ContainsKey(key)) {
                            marching.Add(key, true);
                        } else {
                            marching[key] = true;
                        }
                        continue;
                    }

                    if (blockLookup.y < 0) {
                        if (!marching.ContainsKey(key)) {
                            marching.Add(key, true);
                        } else {
                            marching[key] = true;
                        }
                        continue;
                    }

                    if (neighbours.ContainsKey(blockLookup)) {
                        if (!marching.ContainsKey(key)) {
                            marching.Add(key, neighbours[blockLookup].data == 1);
                        } else {
                            marching[key] = neighbours[blockLookup].data == 1;
                        }
                    } 
                    else {
                        if (!marching.ContainsKey(key)) {
                            marching.Add(key, false);
                        } else {
                            marching[key] = false;
                        }
                    }
                }
            }
        }

        for (int y = -1; y <= 0; y++) {
            for (int z = -1; z <= 0; z++) {
                for (int x = -1; x <= 0; x++) {
                    Vector3 key = center + new Vector3((float)x * 0.5f, (float)y * 0.5f, (float)z * 0.5f);

                    for (int yInMarch = 0; yInMarch < 2; yInMarch++) {
                        for (int zInMarch = 0; zInMarch < 2; zInMarch++) {
                            for (int xInMarch = 0; xInMarch < 2; xInMarch++) {

                                Vector3 newkey = key + new Vector3((float)xInMarch * 0.5f, (float)yInMarch * 0.5f, (float)zInMarch * 0.5f);

                                if (x + xInMarch == 0 && y + yInMarch == 0 && z + zInMarch == 0) {
                                    code <<= 1;
                                    code |= 1;
                                    i++;
                                    continue;
                                }

                                int j = 0;
                                if (marching.ContainsKey(newkey)) {
                                    j = marching[newkey] ? 1 : 0;
                                }

                                if (newkey.y < 0) {
                                    j = 1;
                                }

                                code <<= 1;
                                code |= j;
                                i++;
                            }
                        }
                    }


                    if (i == 8) {
                        if (!marchingInfo.ContainsKey(key)) {
                            marchingInfo.Add(key, code);
                        } else {
                            marchingInfo[key] = code;
                        }
                    }

                    code = 0;
                    i = 0;
                }
            }
        }
        */
    }

    void SetMarchingCubes(Mesh mesh) {
        Vector3 startMarchingInfoKey = GetWorldPosition();

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        bool b = false;

        for (int y = 0; y < worldObj.maxHeight; y++) {
            for (int z = 0; z < worldObj.maxChunkSize; z++) {
                for (int x = 0; x < worldObj.maxChunkSize; x++) {
                    Vector3 marchingCubeBegin = startMarchingInfoKey + new Vector3(x, y, z);

                    //
                    //TODO

                    Vector3[] activeMarchingInfo = new Vector3[]{
                        marchingCubeBegin + new Vector3(0.5f, 0, 0),
                        marchingCubeBegin + new Vector3(0, 0, 0.5f),
                        marchingCubeBegin + new Vector3(1f, 0, 0.5f),
                        marchingCubeBegin + new Vector3(0.5f, 0, 1f),

                        marchingCubeBegin + new Vector3(0, 0.5f, 0),
                        marchingCubeBegin + new Vector3(1f, 0.5f, 0),
                        marchingCubeBegin + new Vector3(0, 0.5f, 1f),
                        marchingCubeBegin + new Vector3(1f, 0.5f, 1f),

                        marchingCubeBegin + new Vector3(0.5f, 1f, 0),
                        marchingCubeBegin + new Vector3(0, 1f, 0.5f),
                        marchingCubeBegin + new Vector3(1f, 1f, 0.5f),
                        marchingCubeBegin + new Vector3(0.5f, 1f, 1f)
                    };

                    //if (!MarchInfoContainsWholeCube(marchingCubeBegin))
                    //    continue;

                    int code = 0;
                    if (marchingInfo.ContainsKey(marchingCubeBegin)) {
                        code = marchingInfo[marchingCubeBegin];
                    }

                    if(!worldObj.gotit) {
                        //Debug.Log(Convert.ToString(code, 2));
                    }

                    /*
                    for (int yInMarch = 0; yInMarch < 2; yInMarch++) {
                        for (int zInMarch = 0; zInMarch < 2; zInMarch++) {
                            for (int xInMarch = 0; xInMarch < 2; xInMarch++) {
                                Vector3 newkey = marchingCubeBegin + new Vector3((float)xInMarch * 0.5f, (float)yInMarch * 0.5f, (float)zInMarch * 0.5f);
                                if (marchingInfo.ContainsKey(newkey)) {
                                    activeMarchingInfo[(yInMarch * 4) + (zInMarch * 2) + xInMarch] = newkey;

                                    code <<= 1;
                                    code |= marchingInfo[newkey]. ? 1 : 0;
                                } else {
                                    continue;
                                }
                            }
                        }
                    }*/

                    if (code == 255 || code == 0) {
                        continue;
                    }

                    /*
                    if (!worldObj.shown) { 
                        //foreach (KeyValuePair<Vector3, int> info in marchingInfo) {
                            //if (!info.Value) {

                            //int index = 0;
                            char[] s = Convert.ToString(code, 2).ToCharArray();
                            for (int i = 0; i < 8; i++) {
                                int k = 8 - s.Length;

                                Vector3 newPos = marchingCubeBegin + new Vector3(0.05f, 0.05f, 0.05f) + new Vector3(Mathf.FloorToInt(i % 2f) * 0.4f, Mathf.FloorToInt(i / 4) * 0.4f, Mathf.FloorToInt((i % 4) / 2) * 0.4f);

                                int currentBit = 0;

                                if (i >= k) {
                                    currentBit = int.Parse(s[i - k].ToString());
                                }

                                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                go.transform.SetParent(chunkObject.transform);
                                go.transform.position = newPos;
                                go.transform.localScale = Vector3.one * 0.05f;
                                go.GetComponent<MeshRenderer>().material.color = currentBit == 1 ? Color.red : Color.yellow;
                            }

                            //GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            //go.transform.SetParent(chunkObject.transform);
                            //go.transform.position = info.Key;
                            //go.transform.localScale = Vector3.one * 0.1f;
                            //go.GetComponent<MeshRenderer>().material.color = info.Value ? Color.red : Color.yellow;
                            //}
                        }
                    //*/

                    /*
                    if (!worldObj.shown && !b) {
                        foreach (KeyValuePair<Vector3, int> info in marchingInfo) {
                            //if (!info.Value) {
                                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                go.transform.SetParent(chunkObject.transform);
                                go.transform.position = info.Key;
                                go.transform.localScale = Vector3.one * 0.05f;
                                go.GetComponent<MeshRenderer>().material.color = Color.yellow;
                            //}
                        }
                        b = true;
                    }
                    //*/

                    //Debug.Log(code);

                    int currentTriCount = tris.Count;

                    //System.Func needed to keep all local variables and don't make it messy
                    Func<int[], bool> AddFaces = delegate (int[] vert) {

                        for (int i = 0; i < vert.Length; i++) {
                            verts.Add(activeMarchingInfo[vert[i]] - GetWorldPosition());
                            //
                            //TODO
                            tris.Add(currentTriCount + (Mathf.FloorToInt(i / 3) * 3) + (2 - (i % 3)));
                            uvs.Add(new Vector2(0, 0));
                        }
                        return true;
                    };

                    switch (code) {

                        case 15: AddFaces(new int[] { 0, 1, 4 }); break;
                        case 16: AddFaces(new int[] { 2, 7, 3 }); break;
                        case 48: AddFaces(new int[] { 1, 2, 7, 1, 7, 6}); break;
                        case 80: AddFaces(new int[] { 0, 5, 3, 3, 5, 7}); break;
                        //case 192: AddFaces(new int[] { }); break;
                        case 112: AddFaces(new int[] { 0, 5, 1, 1, 5, 6, 5, 7, 6 }); break;
                        //case 113: AddFaces(new int[] { 1, 7, 2 }); break;
                        case 128: AddFaces(new int[] { 0, 1, 4 }); break;
                        //case 176: AddFaces(new int[] { 0, 3, 2 }); break;
                        //case 178: AddFaces(new int[] { 0, 3, 6 }); break;
                        case 192: AddFaces(new int[] { 4, 5, 2, 4, 2, 1 }); break;
                        //case 208: AddFaces(new int[] { 0, 1, 3 }); break;
                        //case 212: AddFaces(new int[] { 0, 5, 3 }); break;
                        case 224: AddFaces(new int[] { 4, 5, 6, 6, 5, 2, 6, 2, 3 }); break;
                        //case 232: AddFaces(new int[] { 1, 2, 4 }); break;
                        case 240: AddFaces(new int[] { 4, 5, 7, 4, 7, 6 }); break;
                        case 241: AddFaces(new int[] { 4, 5, 6, 6, 5, 10, 6, 10, 11 }); break;
                        //case 242: AddFaces(new int[] { 0, 1, 3, 0, 3, 6 }); break;
                        case 243: AddFaces(new int[] { 4, 5, 10, 4, 10, 9 }); break;
                        //case 244: AddFaces(new int[] { 4, 8, 6, 8, 10, 6, 10, 7, 6 }); break;
                        case 245: AddFaces(new int[] { 4, 8, 6, 6, 8, 11}); break;
                        case 247: AddFaces(new int[] { 4, 8, 9 }); break;
                        case 248: AddFaces(new int[] { 9, 8, 5, 9, 5, 6, 6, 5, 7 }); break;
                        case 250: AddFaces(new int[] { 6, 4, 8, 6, 8, 11 }); break;
                        case 251: AddFaces(new int[] { 5, 10, 8 }); break;
                        case 252: AddFaces(new int[] { 7, 6, 9, 7, 9, 10 }); break;
                        case 253: AddFaces(new int[] { 6, 3, 11 }); break;
                        case 254: AddFaces(new int[] { 7, 11, 10 }); break;
                    }
                }
            }
        }

        mesh.vertices = verts.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
    }

    bool MarchInfoContainsWholeCube(Vector3 begin) {
        for (int yInMarch = 0; yInMarch < 2; yInMarch++) {
            for (int zInMarch = 0; zInMarch < 2; zInMarch++) {
                for (int xInMarch = 0; xInMarch < 2; xInMarch++) {
                    if (!marchingInfo.ContainsKey(begin + new Vector3((float)xInMarch * 0.5f, (float)yInMarch * 0.5f, (float)zInMarch * 0.5f))) {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
