using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

namespace Editor
{
    public class GenerateToroidalPerlinNoise : ScriptableWizard
    {
        [Tooltip("Major radius : Minor radius")]
        [Min(1)]
        public float parameter = 2;

        public Vector2Int textureSize = new(480, 480);
        public int noiseRows = 5;
        public string path = "Assets/tex.asset";
        public bool seamless = true;

        private ComputeShader shader;
        
        private static readonly (int, int) ThreadGroupSize = (16, 16);
        private const string KernelName = "TorusNoise";
        
        [MenuItem("Assets/Generate Toroidal Perlin Noise")]
        public static void CreateWizard()
        {
            DisplayWizard<GenerateToroidalPerlinNoise>("Toroidal Perlin Noise", "Create");
        }

        public void OnEnable()
        {
            shader = Resources.Load<ComputeShader>("TorusNoise");
        }

        public void OnWizardCreate()
        {
            int kernel = shader.FindKernel(KernelName);
            
            RenderTexture renderTex = new(
                textureSize.x, textureSize.y,
                GraphicsFormat.R32_SFloat, GraphicsFormat.None);
            renderTex.enableRandomWrite = true;
            renderTex.Create();
            shader.SetTexture(kernel, "noiseTexture", renderTex);

            int[] cols =
                TorusUtility.GenerateQuadVertexDimensions(
                    Vector2Int.one, Vector2Int.zero, 
                    parameter, noiseRows);
            int[] gradientStructure = GenerateGradientStructure(cols);
            ComputeBuffer gradientStructureBuffer = new(gradientStructure.Length, sizeof(int) * 2);
            gradientStructureBuffer.SetData(gradientStructure);
            shader.SetBuffer(kernel, "gradientStructure", gradientStructureBuffer);

            Vector3[] gradients = GenerateGradients(cols);
            ComputeBuffer gradientBuffer = new(gradients.Length, sizeof(float) * 3);
            gradientBuffer.SetData(gradients);
            shader.SetBuffer(kernel, "gradients", gradientBuffer);

            float uvRowSeparation = 1f / (cols.Length - 1);
            shader.SetFloat("uvRowSeparation", uvRowSeparation);

            float rowSepRad = Mathf.PI * 2 / (noiseRows - 1);
            float sphereRadius = Mathf.Sin(rowSepRad) / Mathf.Sin((Mathf.PI - rowSepRad) / 2);
            shader.SetFloat("sphereRadius", sphereRadius);
            
            shader.SetFloat("torusParameter", parameter);
            shader.SetInt("gradRows", noiseRows);
            shader.SetInts("texDims", textureSize.x, textureSize.y);
            
            shader.Dispatch(
                kernel, 
                textureSize.x / ThreadGroupSize.Item1, 
                textureSize.y / ThreadGroupSize.Item2,
                1);
            
            Texture2D tex = new(textureSize.x, textureSize.y, TextureFormat.RFloat, false);
            tex.wrapMode = TextureWrapMode.Repeat;

            RenderTexture.active = renderTex;
            tex.ReadPixels(new Rect(0, 0, textureSize.x, textureSize.y), 0, 0);
            RenderTexture.active = null;
                
            AssetDatabase.CreateAsset(tex, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            renderTex.Release();
            gradientStructureBuffer.Dispose();
            gradientBuffer.Dispose();
        }

        private int[] GenerateGradientStructure(int[] cols)
        {
            int[] structure = new int[cols.Length * 2];

            int total = 0;
            for (int i = 0; i < cols.Length; i++)
            {
                structure[i * 2] = cols[i];
                structure[i * 2 + 1] = total;
                total += cols[i];
            }

            return structure;
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
    }
}