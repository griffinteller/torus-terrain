using System;
using UnityEngine;

public class TorusTerrainData : ScriptableObject
{
    [SerializeField]
    private Mesh[] meshes;

    [SerializeField]
    private int rows;
    
    [SerializeField]
    private int cols;

    public void Initialize(int cols, int rows)
    {
        this.cols = cols;
        this.rows = rows;
        meshes = new Mesh[rows * cols];
    }

    public Mesh GetMesh(int i, int j)
    {
        return meshes[j * cols + i];
    }
    
    public void SetMesh(int i, int j, Mesh mesh)
    {
        meshes[j * cols + i] = mesh;
    }
}