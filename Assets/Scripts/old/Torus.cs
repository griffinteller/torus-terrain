using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Torus : MonoBehaviour
{
    public float majorRadius;
    public float minorRadius;
    public int vertexRows;
}