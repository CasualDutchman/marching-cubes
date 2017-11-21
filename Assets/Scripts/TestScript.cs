using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour {

    public int codex = 0;

    Block[] blockarray = new Block[256];

	void Start () {
        GenerateGrid();

    }

    void GenerateGrid() {
        for (int z = 0; z < 16; z++) {
            for (int x = 0; x < 16; x++) {
                Block block = new Block(0, new Vector3(x * 2.5f, 0, z * 2.5f), null);
                block.code = (z * 16) + x;
                blockarray[(z * 16) + x] = block;

                GameObject go = new GameObject();
                go.name = block.code.ToString();
                go.transform.position = block.positionInChunk;
                go.transform.SetParent(transform);

                MeshFilter meshFilter = go.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

                meshFilter.mesh = SetMesh(block);
                SetMarchingCubes(block, go);

                meshRenderer.material = Resources.Load<Material>("Material/WorldMaterial");
            }
        }
    }

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

    Vector3[] supportedConncetions = new Vector3[]{
        new Vector3(0, 1, 4),
        new Vector3(0, 2, 5),
        new Vector3(1, 3, 6),
        new Vector3(2, 3, 7),
        new Vector3(4, 8, 9),
        new Vector3(5, 8, 10),
        new Vector3(6, 9, 11),
        new Vector3(7, 10, 11)
    };

    void SetMarchingCubes(Block block, GameObject obj) {
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        List<Vector3> verticesConnectionPoints = new List<Vector3>();
        List<int> temp = new List<int>();

        int[] points = new int[8];
        string s = Convert.ToString(block.code, 2);
        string st = "";
        for (int i = 0; i < points.Length; i++) {
            int currentPoint = i >= points.Length - s.Length ? int.Parse(s.ToCharArray()[i - (points.Length - s.Length)].ToString()) : 0;
            points[i] = currentPoint;
            st += points[i];

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(obj.transform);
            go.transform.position = block.positionInChunk + new Vector3(i % 2, i < 4 ? 0 : 1, i % 4 < 2 ? 0 : 1);
            go.transform.localScale = Vector3.one * 0.1f;
            go.GetComponent<MeshRenderer>().material.color = currentPoint == 1 ? Color.red : Color.yellow;

            if (currentPoint == 1) {
                if (temp.Contains((int)supportedConncetions[i].x)) {
                    temp.Remove((int)supportedConncetions[i].x);
                }else {
                    temp.Add((int)supportedConncetions[i].x);
                }

                if (temp.Contains((int)supportedConncetions[i].y)) {
                    temp.Remove((int)supportedConncetions[i].y);
                } else {
                    temp.Add((int)supportedConncetions[i].y);
                }

                if (temp.Contains((int)supportedConncetions[i].z)) {
                    temp.Remove((int)supportedConncetions[i].z);
                } else {
                    temp.Add((int)supportedConncetions[i].z);
                }
            }
        }

        foreach (int v in temp) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = v.ToString();
            go.transform.SetParent(obj.transform);
            go.transform.localPosition = activeMarchingInfo[v];
            go.transform.localScale = Vector3.one * 0.1f;
        }

        //Mesh mesh = new Mesh();
        //mesh.name = st;
        //return mesh;
    }

    Mesh SetMesh(Block block) {

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        //baseline for uv
        Vector2[] uvHelper = new Vector2[] {
            new Vector2(0.05f, 0.05f),
            new Vector2(0.15f, 0.05f),
            new Vector2(0.15f, 0.15f)
        };

        int code = block.code;

        //if fully surrounded or no data, dont bother adding mesh
        if (code == 255 || code == 0) {
            return null;
        }

        int triCount = tris.Count;

        //System.Func needed to keep all local variables and don't make it messy
        Func<int[], bool> AddFaces = delegate (int[] vert) {

            for (int i = 0; i < vert.Length; i++) {
                verts.Add(activeMarchingInfo[vert[i]]);
                tris.Add(triCount + (Mathf.FloorToInt(i / 3) * 3) + (2 - (i % 3)));
                uvs.Add(new Vector2(uvHelper[i % 3].x + (int)(0 % 4) * 0.25f, uvHelper[i % 3].y + (int)(0 / 4) * 0.25f));
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
            case 236: AddFaces(new int[] { 2, 3, 6, 2, 6, 9, 2, 9, 10 }); break;
            case 238: AddFaces(new int[] { 11, 2, 3, 11, 10, 2 }); break;
            case 240: AddFaces(new int[] { 4, 5, 7, 4, 7, 6 }); break;
            case 241: AddFaces(new int[] { 4, 5, 6, 6, 5, 10, 6, 10, 11 }); break;
            case 242: AddFaces(new int[] { 4, 5, 7, 4, 7, 11, 4, 11, 9 }); break;
            case 243: AddFaces(new int[] { 4, 5, 10, 4, 10, 9 }); break;
            case 244: AddFaces(new int[] { 7, 6, 4, 7, 4, 8, 7, 8, 10 }); break;
            case 245: AddFaces(new int[] { 4, 8, 6, 6, 8, 11 }); break;
            case 246: AddFaces(new int[] { 4, 8, 9, 7, 11, 10 }); break;
            case 247: AddFaces(new int[] { 4, 8, 9 }); break;
            case 248: AddFaces(new int[] { 9, 8, 5, 9, 5, 6, 6, 5, 7 }); break;
            case 249: AddFaces(new int[] { 8, 5, 10, 11, 6, 9 }); break;
            case 250: AddFaces(new int[] { 5, 7, 11, 5, 11, 8 }); break;
            case 251: AddFaces(new int[] { 5, 10, 8 }); break;
            case 252: AddFaces(new int[] { 7, 6, 9, 7, 9, 10 }); break;
            case 253: AddFaces(new int[] { 6, 9, 11 }); break;
            case 254: AddFaces(new int[] { 7, 11, 10 }); break;
        }

        Mesh mesh = new Mesh();
        mesh.name = block.code.ToString();

        mesh.vertices = verts.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();

        mesh.RecalculateNormals();

        return mesh;
    }

    void Update () {
		
	}
}
