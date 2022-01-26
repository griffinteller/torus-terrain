using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TorusTerrain : MonoBehaviour
{
    [SerializeField] private float majorRadius;
    [SerializeField] private float minorRadius;
    [SerializeField] private Vector2Int quadResolution;
    [SerializeField] private int verticalQuadVerts;

    public float MajorRadius => majorRadius;
    public float MinorRadius => minorRadius;
    public Vector2Int QuadResolution => quadResolution;
    public int VerticalQuadVerts => verticalQuadVerts;
}