using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(QuadTest))]
    public class QuadTestEditor : UnityEditor.Editor 
    {
        private QuadTest _target;
        private const string SavePath = "Assets/Meshes/QuadTest.asset";

        public void OnEnable()
        {
            _target = (QuadTest) target;
        }

        public override void OnInspectorGUI() 
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Generate"))
                Generate();
        }

        private void Generate() 
        {
            int[] cols = new int[_target.rows];

            for (int i = 0; i < cols.Length; i++)
            {
                cols[i] = (int) Random.Range(_target.minCols, _target.maxCols + 0.9f);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = GenerateVertices(cols);
            mesh.triangles = GenerateTriangles(cols);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
        
            AssetDatabase.CreateAsset(mesh, SavePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _target.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(SavePath);
        }

        // bottom row offset = number of vertices before bottom row
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

        private Vector3[] GenerateVertices(int[] cols)
        {
            float rowSep = _target.height / (_target.rows - 1);

            List<Vector3> verts = new List<Vector3>();
        
            for (int j = 0; j < _target.rows; j++)
            {
                float y = j * rowSep;
                float colSep = _target.width / (cols[j] - 1);
            
                for (int i = 0; i < cols[j]; i++)
                {
                    verts.Add(new Vector3(i * colSep, y, 0));
                }
            }

            return verts.ToArray();
        }
    }
}