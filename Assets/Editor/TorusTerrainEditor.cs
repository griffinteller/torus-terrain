using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(TorusTerrain))]
    public class TorusTerrainEditor : UnityEditor.Editor
    {
        private TorusTerrain _terrain;

        private const string SavePath = "Assets/Meshes/TestQuad.asset";

        public void OnEnable()
        {
            _terrain = (TorusTerrain) target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Generate"))
                GenerateQuad(0, 0);
        }

        private void GenerateQuad(int i, int j)
        {
            int[] cols = new int[_terrain.VerticalQuadVerts]; // wrapping means one overlapping layer
            
            float xQuadRadians = Mathf.PI * 2 / _terrain.QuadResolution.x;
            float yQuadRadians = Mathf.PI * 2 / _terrain.QuadResolution.y;

            float rowSepRad = yQuadRadians / (_terrain.VerticalQuadVerts - 1);
            float rowSepLin = Mathf.Sin(rowSepRad) / Mathf.Sin((Mathf.PI - rowSepRad) / 2) * _terrain.MinorRadius;
            
            for (int row = 0; row < _terrain.VerticalQuadVerts; row++)
            {
                float theta = yQuadRadians * j + row * rowSepRad;
                float rowWidth = (_terrain.MinorRadius + _terrain.MinorRadius * Mathf.Cos(theta)) * xQuadRadians;
                cols[row] = Mathf.Min(2, Mathf.RoundToInt(rowWidth / rowSepLin) + 1); // +1 for overlapping column
            }

            Mesh mesh = new Mesh();
            mesh.vertices = GenerateMeshVerts(i, j, cols);
            mesh.triangles = GenerateMeshTriangles(cols);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();

            _terrain.GetComponent<MeshFilter>().mesh = mesh;
        }

        private Vector3[] GenerateMeshVerts(int quadI, int quadJ, int[] cols)
        {
            List<Vector3> verts = new List<Vector3>();
            
            float xQuadRadians = Mathf.PI * 2 / _terrain.QuadResolution.x;
            float yQuadRadians = Mathf.PI * 2 / _terrain.QuadResolution.y;
            
            float rowSepRad = yQuadRadians / (_terrain.VerticalQuadVerts - 1);
            
            for (int j = 0; j < cols.Length; j++)
            {
                float rowAngle = quadJ * yQuadRadians + j * rowSepRad;
                float colSepRad = xQuadRadians / (cols[j] - 1);
                
                for (int i = 0; i < cols[j]; i++)
                {
                    float colAngle = quadI * xQuadRadians + i * colSepRad;
                    Vector3 vertex =
                        Quaternion.Euler(0, -Mathf.Rad2Deg * colAngle, 0)
                        * new Vector3(
                            _terrain.MajorRadius + _terrain.MinorRadius * Mathf.Cos(rowAngle),
                            _terrain.MinorRadius * Mathf.Sin(rowAngle),
                            0);
                    
                    verts.Add(vertex);
                }
            }

            return verts.ToArray();
        }

        private int[] GenerateMeshTriangles(int[] cols)
        {
            List<int> tris = new List<int>();

            int bottomRowOffset = 0;
            for (int i = 0; i < cols.Length - 1; i++)
            {
                TriangulateRows(tris, cols, i, bottomRowOffset);
                bottomRowOffset += cols[i];
            }

            return tris.ToArray();
        }

        private static void TriangulateRows(List<int> tris, int[] cols, int bottomRow, int bottomRowOffset)
        {
            int topRow = bottomRow + 1;

            int row0, row1, row0Offset, row1Offset;  // 0 has fewer columns
            bool flip = false;
            if (cols[bottomRow] <= cols[topRow])
            {
                row0 = bottomRow;
                row1 = topRow;
                row0Offset = bottomRowOffset;
                row1Offset = bottomRowOffset + cols[bottomRow];
            }
            else
            {
                row0 = topRow;
                row1 = bottomRow;
                row0Offset = bottomRowOffset + cols[bottomRow];
                row1Offset = bottomRowOffset;
                flip = true;
            }

            int lastRow1Index = 0;
            for (int i = 0; i < cols[row0]; i++)
            {
                // add as many triangles as necessary
                int newRow1Index = ((cols[row1]  - 1) * i) / (cols[row0] - 1) + 1;
                if (newRow1Index > cols[row1] - 1)
                    newRow1Index = cols[row1] - 1;
            
                for (int j = lastRow1Index; j < newRow1Index; j++)
                {
                    tris.Add(row0Offset + i);

                    if (!flip)
                    {
                        tris.Add(row1Offset + j);
                        tris.Add(row1Offset + j + 1);
                    }
                    else
                    {
                        tris.Add(row1Offset + j + 1);
                        tris.Add(row1Offset + j);
                    }
                }

                // don't add bottom triangle on last vertex
                if (i < cols[row0] - 1)
                {
                    tris.Add(row0Offset + i);

                    if (!flip)
                    {
                        tris.Add(row1Offset + newRow1Index);
                        tris.Add(row0Offset + i + 1);
                    }
                    else
                    {
                        tris.Add(row0Offset + i + 1);
                        tris.Add(row1Offset + newRow1Index);
                    }
                }

                lastRow1Index = newRow1Index;
            }
        }
    }
}