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

    Dictionary<Vector3, bool> marchingInfo = new Dictionary<Vector3, bool>();

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

        if(!worldObj.shown)
            SetMarchingCubes(mesh);

        /*
        if (!worldObj.shown)
            foreach (KeyValuePair<Vector3, bool> info in marchingInfo) {
                //if (!info.Value) {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.SetParent(chunkObject.transform);
                    go.transform.position = info.Key;
                    go.transform.localScale = Vector3.one * 0.1f;
                    go.GetComponent<MeshRenderer>().material.color = info.Value ? Color.red : Color.yellow;
                //}
            }
            //*/

        worldObj.shown = true;

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = Resources.Load<Material>("Material/WorldMaterial");
    }

    void CreateMesh(Mesh mesh) {
        for (int x = 0; x < worldObj.maxChunkSize; x++) {//check every block in the chunk
            for (int z = 0; z < worldObj.maxChunkSize; z++) {//
                for (int y = 0; y < worldObj.maxHeight; y++) {//
                    Block block = GetBlock(x, y, z);

                    if (block.data == 0) //block has no data? dont bother looking further
                        continue;

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

        for (int y = -1; y <= 2; y++) {
            for (int z = -2; z <= 2; z++) {
                for (int x = -2; x <= 2; x++) {
                    Vector3 key = center + new Vector3((float)x * 0.5f, (float)y * 0.5f, (float)z * 0.5f);

                    Vector3 blockLookup = center + new Vector3(x, y, z);

                    //check if it can 
                    
                    if (x == 0 && y == 0 && z == 0) {
                        SetMarchingInfo(key, true);
                    }

                    if (blockLookup.y < 0) {
                        SetMarchingInfo(key, true);
                    }

                    if (neighbours.ContainsKey(blockLookup)) {
                        SetMarchingInfo(key, neighbours[blockLookup].data == 1);
                    }

                    /*
                    Vector3 lookup = center + new Vector3(x, y, z);

                    if (Mathf.Abs(x) == 1 && y == 0 && Mathf.Abs(z) == 1) {
                        if (neighbours.ContainsKey(center + new Vector3(x, y, y))){
                            if(neighbours[center + new Vector3(x, y, y)].data == 1)
                                SetMarchingInfo(key, true);
                        }

                        if (neighbours.ContainsKey(center + new Vector3(y, y, z))) {
                            if (neighbours[center + new Vector3(y, y, z)].data == 1)
                                SetMarchingInfo(key, true);
                        }
                    }
                    if (Mathf.Abs(x) == 1 && Mathf.Abs(y) == 1 && z == 0) {
                        if (neighbours.ContainsKey(center + new Vector3(x, z, z))) {
                            if (neighbours[center + new Vector3(x, z, z)].data == 1)
                                SetMarchingInfo(key, true);
                        }
                    }
                    if (x == 0 && Mathf.Abs(y) == 1 && Mathf.Abs(z) == 1) {
                        if (neighbours.ContainsKey(center + new Vector3(x, x, z))) {
                            if (neighbours[center + new Vector3(x, x, z)].data == 1)
                                SetMarchingInfo(key, true);
                        }
                    }
                    if (Mathf.Abs(x) == 1 && Mathf.Abs(y) == 1 && Mathf.Abs(z) == 1) {
                        for(int checkY = 0; checkY <= 1; checkY++) {
                            int i = 0;
                            if(y == -1) {
                                i = y + checkY;
                            }
                            else if (y == 1) {
                                i = y - checkY;
                            }

                            if (neighbours.ContainsKey(center + new Vector3(x, i, z))) {
                                if (neighbours[center + new Vector3(x, i, z)].data == 1) {
                                    SetMarchingInfo(key, true);
                                    break;
                                }
                            }
                        }
                    }

                    SetMarchingInfo(key, false);
                    */
                }
            }
        }
    }

    void SetMarchingInfo(Vector3 key, bool value) {
        if (!marchingInfo.ContainsKey(key)) {
            marchingInfo.Add(key, value);
        } else {
            if (!marchingInfo[key])
                marchingInfo[key] = value;
        }
    }

    void SetMarchingCubes(Mesh mesh) {
        Vector3 startMarchingInfoKey = GetWorldPosition() - (Vector3.one * 0.5f);

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int y = 0; y < worldObj.maxHeight * 2 + 1; y++) {
            for (int z = 0; z < worldObj.maxChunkSize * 2; z++) {
                for (int x = 0; x < worldObj.maxChunkSize * 2; x++) {
                    Vector3 marchingCubeBegin = startMarchingInfoKey + new Vector3((float)x * 0.5f, (float)y * 0.5f, (float)z * 0.5f);

                    Vector3[] activeMarchingInfo = new Vector3[8];

                    if (!MarchInfoContainsWholeCube(marchingCubeBegin))
                        continue;

                    int code = 0;

                    for (int yInMarch = 0; yInMarch < 2; yInMarch++) {
                        for (int zInMarch = 0; zInMarch < 2; zInMarch++) {
                            for (int xInMarch = 0; xInMarch < 2; xInMarch++) {
                                Vector3 newkey = marchingCubeBegin + new Vector3((float)xInMarch * 0.5f, (float)yInMarch * 0.5f, (float)zInMarch * 0.5f);
                                if (marchingInfo.ContainsKey(newkey)) {
                                    activeMarchingInfo[(yInMarch * 4) + (zInMarch * 2) + xInMarch] = newkey;

                                    code <<= 1;
                                    code |= marchingInfo[newkey] ? 1 : 0;
                                } else {
                                    continue;
                                }
                            }
                        }
                    }

                    if (code == 255) {
                        continue;
                    }

                    //Debug.Log(code);

                    int currentTriCount = tris.Count;

                    //System.Func needed to keep all local variables and don't make it messy
                    Func<int[], int[], bool> AddTwoFaces = delegate (int[] vert, int[] tri) {
                        verts.Add(activeMarchingInfo[vert[0]] - GetWorldPosition());
                        verts.Add(activeMarchingInfo[vert[1]] - GetWorldPosition());
                        verts.Add(activeMarchingInfo[vert[2]] - GetWorldPosition());
                        verts.Add(activeMarchingInfo[vert[3]] - GetWorldPosition());
                        verts.Add(activeMarchingInfo[vert[4]] - GetWorldPosition());
                        verts.Add(activeMarchingInfo[vert[5]] - GetWorldPosition());

                        tris.Add(currentTriCount + tri[0]);
                        tris.Add(currentTriCount + tri[1]);
                        tris.Add(currentTriCount + tri[2]);
                        tris.Add(currentTriCount + tri[3]);
                        tris.Add(currentTriCount + tri[4]);
                        tris.Add(currentTriCount + tri[5]);

                        uvs.Add(new Vector2(0, 0));
                        uvs.Add(new Vector2(1, 0));
                        uvs.Add(new Vector2(0, 1));
                        uvs.Add(new Vector2(1, 0));
                        uvs.Add(new Vector2(1, 1));
                        uvs.Add(new Vector2(0, 1));
                        return true;
                    };

                    Func<int[], int[], Vector2[], bool> AddFaces = delegate (int[] vert, int[] tri, Vector2[] uv) {
                        for(int i = 0; i < vert.Length; i++) {
                            verts.Add(activeMarchingInfo[vert[i]] - GetWorldPosition());
                            tris.Add(currentTriCount + tri[i]);
                            uvs.Add(uv[i]);
                        }
                        return true;
                    };


                    if (code == 112)
                        AddFaces(new int[] { 1, 3, 2 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 113)
                        AddFaces(new int[] { 1, 7, 2 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 176)
                        AddFaces(new int[] { 0, 3, 2 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 178)
                        AddFaces(new int[] { 0, 3, 6 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 208)
                        AddFaces(new int[] { 0, 1, 3 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 212)
                        AddFaces(new int[] { 0, 5, 3 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 224)
                        AddFaces(new int[] { 0, 1, 2 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 232)
                        AddFaces(new int[] { 1, 2, 4 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 240)
                        AddTwoFaces(new int[] { 0, 1, 2, 1, 3, 2 }, new int[] { 2, 1, 0, 5, 4, 3 });

                    else if (code == 241)
                        AddTwoFaces(new int[] { 0, 1, 2, 1, 7, 2 }, new int[] { 2, 1, 0, 5, 4, 3 });

                    else if (code == 242)
                        AddTwoFaces(new int[] { 0, 1, 3, 0, 3, 6 }, new int[] { 2, 1, 0, 5, 4, 3 });

                    else if (code == 243) 
                        AddTwoFaces(new int[] { 0, 1, 6, 1, 7, 6 }, new int[] { 2, 1, 0, 5, 4, 3 });

                    else if (code == 244)
                        AddTwoFaces(new int[] { 0, 3, 2, 0, 5, 3 }, new int[] { 2, 1, 0, 5, 4, 3 });

                    else if (code == 245)
                        AddTwoFaces(new int[] { 0, 5, 2, 5, 7, 2 }, new int[] { 2, 1, 0, 5, 4, 3 });

                    else if (code == 247)
                        AddFaces(new int[] { 0, 5, 6 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 248) 
                        AddTwoFaces(new int[] { 1, 3, 2, 4, 1, 2 }, new int[] { 2, 1, 0, 5, 4, 3 });

                    else if (code == 250)
                        AddTwoFaces(new int[] { 4, 1, 3, 4, 3, 6 }, new int[] { 2, 1, 0, 5, 4, 3 });

                    else if (code == 251)
                        AddFaces(new int[] { 1, 7, 4 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 252)
                        AddTwoFaces(new int[] { 4, 5, 2, 5, 3, 2 }, new int[] { 2, 1, 0, 5, 4, 3 });

                    else if (code == 253)
                        AddFaces(new int[] { 2, 4, 7 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });

                    else if (code == 254)
                        AddFaces(new int[] { 3, 6, 5 }, new int[] { 2, 1, 0 }, new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) });
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
