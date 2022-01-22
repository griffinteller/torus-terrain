using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class QuadTest : MonoBehaviour
{
    [Min(0f)]
    public float width = 0.0f;

    [Min(0f)]
    public float height = 0.0f;

    [Min(2)]
    public int rows = 1;

    [Min(2)]
    public int minCols = 1;
    
    [Min(2)]
    public int maxCols = 10;
}
