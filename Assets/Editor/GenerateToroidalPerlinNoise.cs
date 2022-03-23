using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.Mathematics;

namespace Editor
{
    public class GenerateToroidalPerlinNoise : ScriptableWizard
    {
        [Tooltip("Major radius : Minor radius")]
        [Min(1)]
        public float parameter = 2;

        public Vector2Int textureSize = new Vector2Int(500, 500);
        public int noiseRows = 8;
        public string path = "Assets/tex.asset";
        public bool seamless = true;
        
        [MenuItem("Assets/Generate Toroidal Perlin Noise")]
        public static void CreateWizard()
        {
            DisplayWizard<GenerateToroidalPerlinNoise>("Toroidal Perlin Noise", "Create");
        }

        public void OnWizardCreate()
        {
            Random.InitState(0);
            int[] cols = TorusUtility.GenerateQuadVertexDimensions(
                Vector2Int.one, Vector2Int.zero, parameter, noiseRows);
            Vector2[] uvs = TorusUtility.GenerateQuadUvs(
                Vector2Int.one, Vector2Int.zero, cols);
            Vector3[] grads = GenerateGradients(cols);

            float rowSepRad = Mathf.PI * 2 / (noiseRows - 1);
            float rowSepLin = Mathf.Sin(rowSepRad) / Mathf.Sin((Mathf.PI - rowSepRad) / 2);

            Texture2D tex = GenerateTexture(uvs, grads, rowSepLin);
            
            AssetDatabase.CreateAsset(tex, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private Vector3[] GenerateGradients(int[] cols)
        {
            List<Vector3> grads = new();

            for (int i = 0; i < cols.Length; i++)
            for (int j = 0; j < cols[i]; j++)
            {
                float theta = (float)j / (cols[i] - 1) * (Mathf.PI * 2);
                float phi = (float)i / (cols.Length - 1) * (Mathf.PI * 2);
                Vector3 normal = new(
                    Mathf.Cos(phi) * Mathf.Cos(theta), 
                    Mathf.Sin(phi), 
                    Mathf.Cos(phi) * Mathf.Sin(theta));
                Vector3 tangent = new(-Mathf.Sin(theta), 0, Mathf.Cos(theta));
                Vector3 gradient = Quaternion.AngleAxis(Random.value * 360, normal) * tangent;
                grads.Add(gradient);
            }

            if (seamless)
            {
                int total = 0;
                int j = 0;
                for (; j < cols.Length - 1; j++)
                {
                    grads[total + cols[j] - 1] = grads[total];
                    total += cols[j];
                }

                for (int i = 0; i < cols[0]; i++)
                {
                    grads[total + i] = grads[i];
                }
            }

            return grads.ToArray();
        }
        private Texture2D GenerateTexture(Vector2[] uvs, Vector3[] grads, float sphereRadius)
        {
            // triangles are numbered clockwise

            Texture2D tex = new(textureSize.x, textureSize.y, TextureFormat.RGBAFloat, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            
            for (int i = 0; i < textureSize.x; i++)
            for (int j = 0; j < textureSize.y; j++)
            {
                tex.SetPixel(i, j, new Color(0, 0, 0, 0));
            }
            
            float verticalRadiusAngle = Mathf.Acos(1 - sphereRadius * sphereRadius / 2);
            float radiusV = verticalRadiusAngle / (Mathf.PI * 2);

            for (int vert = 0; vert < uvs.Length; vert++)
            {
                Vector3 vertPos = TorusUtility.UVToCoord(uvs[vert], parameter, parameter);
                
                float bottomV = uvs[vert].y - radiusV;
                float topV = uvs[vert].y + radiusV;
            
                int bottomPix = Mathf.Max(0, Mathf.FloorToInt(bottomV * textureSize.y));
                int topPix = Mathf.Min(Mathf.CeilToInt(topV * textureSize.y), textureSize.y - 1);

                int middlePix = Mathf.RoundToInt(Mathf.Clamp(uvs[vert].x, 0, 1) * textureSize.x);
                
                for (int j = bottomPix; j <= topPix; j++)
                {
                    int i = middlePix - 1;
                    
                    // go to the left until we are out of range
                    while (i >= 0)
                    {
                        Vector3 pixelPos = TorusUtility.UVToCoord(
                            new Vector2(i, j) / textureSize,
                            parameter, parameter);

                        float distance = Vector3.Distance(vertPos, pixelPos);
                        if (distance >= sphereRadius)
                            break;

                        float dot = Vector3.Dot(grads[vert], pixelPos - vertPos);
                        float heightToAdd = Mathf.SmoothStep(dot, 0, distance / sphereRadius);

                        float currentHeight = tex.GetPixel(i, j).r;
                        tex.SetPixel(i, j, new Color(currentHeight + heightToAdd, 0, 0, 0));

                        i--;
                    }

                    i = middlePix;
                    
                    // go to the right until we are out of range
                    while (i < textureSize.x)
                    {
                        Vector3 pixelPos = TorusUtility.UVToCoord(
                            new Vector2(i, j) / textureSize,
                            parameter, parameter);

                        float distance = Vector3.Distance(vertPos, pixelPos);
                        if (distance >= sphereRadius)
                            break;

                        float dot = Vector3.Dot(grads[vert], pixelPos - vertPos);
                        float heightToAdd = Mathf.SmoothStep(dot, 0, distance / sphereRadius);
                        
                        float currentHeight = tex.GetPixel(i, j).r;
                        tex.SetPixel(i, j, new Color(currentHeight + heightToAdd, 0, 0, 0));

                        i++;
                    }
                }
            }

            return tex;
        }
    }
}