using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(TorusTerrain))]
    public class TorusTerrainEditor : UnityEditor.Editor
    {
        private TorusTerrain _terrain;

        private const string SavePath = "Assets/TorusTerrainData.asset";

        public void OnEnable()
        {
            _terrain = (TorusTerrain) target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Generate"))
                Generate();
            
            if (GUILayout.Button("Apply Heightmap"))
                ApplyHeightmap();
        }

        private void Generate()
        {
            while (_terrain.transform.childCount > 0)
            {
                DestroyImmediate(_terrain.transform.GetChild(0).gameObject);
            }
            
            _terrain.Quads = new TorusTerrainQuad[_terrain.QuadResolution.x, _terrain.QuadResolution.y];
            
            TorusTerrainData data = CreateInstance<TorusTerrainData>();
            _terrain.data = data;
            data.Initialize(_terrain.QuadResolution.x, _terrain.QuadResolution.y);
            AssetDatabase.CreateAsset(data, SavePath);

            for (int i = 0; i < _terrain.QuadResolution.x; i++)
            for (int j = 0; j < _terrain.QuadResolution.y; j++)
            {
                Mesh mesh = GenerateQuadMesh(i, j);
                AssetDatabase.AddObjectToAsset(mesh, SavePath);
                
                data.SetMesh(i, j, mesh);

                GameObject obj = new ($"Mesh {i} {j}");
                obj.transform.parent = _terrain.transform;
                obj.transform.localPosition = Vector3.zero;

                TorusTerrainQuad quad = obj.AddComponent<TorusTerrainQuad>();
                _terrain.Quads[i, j] = quad;
                
                obj.GetComponent<MeshFilter>().mesh = mesh;
                obj.GetComponent<MeshRenderer>().material = _terrain.Material;
            }
            
            AssetDatabase.ImportAsset(SavePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private Mesh GenerateQuadMesh(int i, int j)
        {
            int[] cols = TorusUtility.GenerateQuadVertexDimensions(
                _terrain.QuadResolution, new Vector2Int(i, j), 
                _terrain.Parameter, _terrain.VerticalQuadVerts);

            Mesh mesh = new Mesh();
            mesh.vertices = GenerateMeshVerts(i, j, cols);
            mesh.triangles = TorusUtility.GenerateMeshTriangles(cols);
            mesh.uv = TorusUtility.GenerateQuadUvs(_terrain.QuadResolution, new Vector2Int(i, j), cols);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();

            return mesh;
        }

        private Vector3[] GenerateMeshVerts(int quadI, int quadJ, int[] cols)
        {
            List<Vector3> verts = new ();
            
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

        private void ApplyHeightmap()
        {
            for (int i = 0; i < _terrain.QuadResolution.x; i++)
            for (int j = 0; j < _terrain.QuadResolution.y; j++)
            {
                
                Mesh mesh = _terrain.data.GetMesh(i, j);
                Vector3[] verts = mesh.vertices;
                Vector2[] uvs = mesh.uv;

                for (int vert = 0; vert < verts.Length; vert++)
                {
                    (float u, float v) = (uvs[vert].x, uvs[vert].y);

                    float theta = u * Mathf.PI * 2; // horizontal angle
                    float phi = v * Mathf.PI * 2;
                    Vector3 normal = (Quaternion.Euler(0, -theta * Mathf.Rad2Deg, 0)
                                     * new Vector3(Mathf.Cos(phi), Mathf.Sin(phi), 0)).normalized;

                    float normHeight = _terrain.Heightmap.GetPixelBilinear(u, v).r;
                    float radius = _terrain.MinorRadius + normHeight * _terrain.Scale + _terrain.Offset;

                    Vector3 ringPosition = _terrain.MajorRadius
                                           * new Vector3(Mathf.Cos(theta), 0, Mathf.Sin(theta));

                    Vector3 vertPosition = ringPosition + radius * normal;
                    verts[vert] = vertPosition;
                }

                mesh.vertices = verts;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}