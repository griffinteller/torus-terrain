using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Editor
{
    [CustomEditor(typeof(Torus))]
    public class TorusEditor : UnityEditor.Editor
    {
        private Torus _target;
        
        private const string SavePath = "Assets/Meshes/QuadTest.asset";

        public void OnEnable()
        {
            _target = (Torus) target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Generate"))
                Generate();
        }

        private void Generate()
        {
            int[] cols = new int[_target.vertexRows + 1]; // wrapping means one overlapping layer
            float rowSepRad = Mathf.PI * 2 / _target.vertexRows;
            float rowSepLin = Mathf.Sin(rowSepRad) / Mathf.Sin((Mathf.PI - rowSepRad) / 2) * _target.minorRadius;
            for (int i = 0; i <= _target.vertexRows; i++)
            {
                float theta = i * rowSepRad;
                float rowCircum = (_target.majorRadius + _target.minorRadius * Mathf.Cos(theta)) * 2 * Mathf.PI;
                cols[i] = Mathf.RoundToInt(rowCircum / rowSepLin) + 1; // +1 for overlapping layer
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = GenerateVertices(cols);
            mesh.triangles = GenerateTriangles(cols);
            mesh.uv = GenerateUvs(cols);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
            
            AssetDatabase.CreateAsset(mesh, SavePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _target.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(SavePath);
        }

        private Vector2[] GenerateUvs(int[] cols)
        {
            List<Vector2> uvs = new List<Vector2>();
            for (int j = 0; j < cols.Length; j++)
            {
                float v = (float) j / cols.Length;
                int rowVerts = cols[j];
                for (int i = 0; i < cols.Length; i++)
                {
                    uvs.Add(new Vector2((float) i / rowVerts, v));
                }
            }

            return uvs.ToArray();
        }

        private Vector3[] GenerateVertices(int[] cols)
        {
            List<Vector3> verts = new List<Vector3>();
            float rowSepRad = Mathf.PI * 2 / _target.vertexRows;
            for (int i = 0; i < cols.Length; i++)
            {
                float rowAngle = i * rowSepRad;
                float colSepRad = Mathf.PI * 2 / (cols[i] - 1);
                for (int j = 0; j < cols[i]; j++)
                {
                    float colAngle = j * colSepRad;
                    Vector3 vertex =
                        Quaternion.Euler(0, -Mathf.Rad2Deg * colAngle, 0)
                        * new Vector3(
                            _target.majorRadius + _target.minorRadius * Mathf.Cos(rowAngle),
                            _target.minorRadius * Mathf.Sin(rowAngle),
                            0);
                    
                    verts.Add(vertex);
                }
            }

            return verts.ToArray();
        }

        private int[] GenerateTriangles(int[] cols)
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
        
        private void TriangulateRows(List<int> tris, int[] cols, int bottomRow, int bottomRowOffset)
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