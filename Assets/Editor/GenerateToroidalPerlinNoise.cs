using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

namespace Editor
{
    public class GenerateToroidalPerlinNoise : ScriptableWizard
    {
        [Tooltip("Major radius : Minor radius")]
        [Min(1)]
        public float parameter = 2;

        public Vector2Int textureSize = new(480, 480);
        public int startLevel = 2;
        public int endLevel = 6;
        public string path = "Assets/tex.asset";
        
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
            RenderTexture renderTex = new(
                textureSize.x, textureSize.y,
                GraphicsFormat.R32_SFloat, GraphicsFormat.None);
            renderTex.enableRandomWrite = true;
            renderTex.Create();

            int maxRows = Mathf.RoundToInt(Mathf.Pow(2, endLevel));
            ComputeBuffer gradientStructureBuffer = new(maxRows, sizeof(int) * 2);

            int level = startLevel;
            bool clear = true;
            while (level <= endLevel)
            {
                int noiseRows = Mathf.RoundToInt(Mathf.Pow(2, level));
                ApplyLayer(noiseRows, renderTex, gradientStructureBuffer, clear);
                clear = false;
                level++;
            }

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
        }

        private void ApplyLayer(
            int noiseRows,
            RenderTexture renderTex,
            ComputeBuffer gradientStructureBuffer,
            bool clear)
        {
            int kernel = shader.FindKernel(KernelName);
            
            shader.SetTexture(kernel, "noiseTexture", renderTex);

            Profiler.BeginSample("Gradient Structure");
            
            int[] cols =
                TorusUtility.GenerateQuadVertexDimensions(
                    Vector2Int.one, Vector2Int.zero, 
                    parameter, noiseRows);
            int[] gradientStructure = GenerateGradientStructure(cols);
            gradientStructureBuffer.SetData(gradientStructure);
            shader.SetBuffer(kernel, "gradientStructure", gradientStructureBuffer);
            
            Profiler.EndSample();

            float uvRowSeparation = 1f / (cols.Length - 1);
            shader.SetFloat("uvRowSeparation", uvRowSeparation);

            float rowSepRad = Mathf.PI * 2 / (noiseRows - 1);
            float sphereRadius = Mathf.Sin(rowSepRad) / Mathf.Sin((Mathf.PI - rowSepRad) / 2);
            shader.SetFloat("sphereRadius", sphereRadius);
            
            shader.SetFloat("torusParameter", parameter);
            shader.SetInt("gradRows", noiseRows);
            shader.SetInts("texDims", textureSize.x, textureSize.y);
            shader.SetBool("clearTexture", clear);
            
            Profiler.BeginSample("Shader Execution");
            
            shader.Dispatch(
                kernel, 
                textureSize.x / ThreadGroupSize.Item1, 
                textureSize.y / ThreadGroupSize.Item2,
                1);
            
            Profiler.EndSample();
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
    }
}