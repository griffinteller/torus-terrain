using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
            int[] cols = new int[_target.vertexRows];
            float rowSepRad = Mathf.PI * 2 / _target.vertexRows;
            float rowSepLin = Mathf.Sin(rowSepRad) / Mathf.Sin((Mathf.PI - rowSepRad) / 2) * _target.minorRadius;
            for (int i = 0; i < _target.vertexRows; i++)
            {
                float theta = i * rowSepRad;
                float rowCircum = (_target.majorRadius + _target.minorRadius * Mathf.Cos(theta)) * 2 * Mathf.PI;
                cols[i] = Mathf.RoundToInt(rowCircum / rowSepLin);
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

        private Vector3[] GenerateVertices(int[] cols)
        {
            List<Vector3> verts = new List<Vector3>();
            float rowSepRad = Mathf.PI * 2 / _target.vertexRows;
            for (int i = 0; i < _target.vertexRows; i++)
            {
                float rowAngle = i * rowSepRad;
                float colSepRad = Mathf.PI * 2 / cols[i];
                for (int j = 0; j < cols[i]; j++)
                {
                    float colAngle = j * colSepRad;
                    Vector3 vertex =
                        Quaternion.Euler(0, Mathf.Rad2Deg * colAngle, 0)
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
            for (int i = 0; i < cols.Length; i++)
            {
                TriangulateRows(tris, cols, i, bottomRowOffset);
                bottomRowOffset += cols[i];
            }

            return tris.ToArray();
        }
        
        private void TriangulateRows(List<int> tris, int[] cols, int bottomRow, int bottomRowOffset)
        {
            int topRow = (bottomRow + 1) % cols.Length;

            int row0, row1, row0Offset, row1Offset;  // 0 has fewer columns
            bool flip = false;
            if (cols[bottomRow] <= cols[topRow])
            {
                row0 = bottomRow;
                row1 = topRow;
                row0Offset = bottomRowOffset;
                row1Offset = (bottomRowOffset + cols[bottomRow]);
            }
            else
            {
                row0 = topRow;
                row1 = bottomRow;
                row0Offset = bottomRowOffset + cols[bottomRow];
                row1Offset = bottomRowOffset;
                flip = true;
            }

            if (row1 == 0)
                row1Offset = 0;

            int lastRow1Index = cols[row1] * (cols[row0] - 1) / cols[row0] + 1 - cols[row1];
            for (int i = 0; i < cols[row0]; i++)
            {
                // add as many triangles as necessary
                int newRow1Index = cols[row1] * i / cols[row0] + 1;

                // adding one full row allows for wrapping (hacky, but whatever. whoever decided to make negative
                // modulo not work, it's their fault)
                for (int j = lastRow1Index + cols[row1]; j < newRow1Index + cols[row1]; j++)
                {
                    tris.Add(row0Offset + i);

                    if (flip)
                    {
                        tris.Add(row1Offset + j % cols[row1]);
                        tris.Add(row1Offset + (j + 1) % cols[row1]);
                    }
                    else
                    {
                        tris.Add(row1Offset + (j + 1) % cols[row1]);
                        tris.Add(row1Offset + j % cols[row1]);
                    }
                }
                
                // add bottom triangle
                tris.Add(row0Offset + i);

                if (flip)
                {
                    tris.Add(row1Offset + newRow1Index % cols[row1]);
                    tris.Add(row0Offset + (i + 1) % cols[row0]);
                }
                else
                {
                    tris.Add(row0Offset + (i + 1) % cols[row0]);
                    tris.Add(row1Offset + newRow1Index % cols[row1]);
                }

                lastRow1Index = newRow1Index;
            }
        }
    }
}