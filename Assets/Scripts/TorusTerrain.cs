using System;
using UnityEngine;

public class TorusTerrain : MonoBehaviour
{
    public TorusTerrainData data;
    
    [SerializeField] private float majorRadius;
    [SerializeField] private float minorRadius;
    [SerializeField] private Vector2Int quadResolution;
    [SerializeField] private int verticalQuadVerts;
    [SerializeField] private Material material;

    public TorusTerrainQuad[,] Quads;

    public float MajorRadius => majorRadius;
    public float MinorRadius => minorRadius;
    public Vector2Int QuadResolution => quadResolution;
    public int VerticalQuadVerts => verticalQuadVerts;
    public Material Material => material;
}