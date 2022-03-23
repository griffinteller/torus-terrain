using System;
using UnityEngine;

public class TorusTerrain : MonoBehaviour
{
    public TorusTerrainData data;
    
    
    [Header("Generation Data")]
    [SerializeField] private float majorRadius;
    [SerializeField] private float minorRadius;
    [SerializeField] private Vector2Int quadResolution;
    [SerializeField] private int verticalQuadVerts;
    [SerializeField] private Material material;

    [Header("Heightmap Data")]
    [SerializeField] private Texture2D heightmap;
    [SerializeField] private float scale;
    [SerializeField] private float offset;

    public TorusTerrainQuad[,] Quads;

    public float MajorRadius => majorRadius;
    public float MinorRadius => minorRadius;
    public Vector2Int QuadResolution => quadResolution;
    public int VerticalQuadVerts => verticalQuadVerts;
    public Material Material => material;
    public Texture2D Heightmap => heightmap;
    public float Scale => scale;
    public float Offset => offset;
    public float Parameter => majorRadius / minorRadius;
}