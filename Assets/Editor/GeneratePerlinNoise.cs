using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class GeneratePerlinNoise : ScriptableWizard
    {
        public int seed = 0;
        public float scale = 1;
        public int width = 128;
        public string path = "Assets/tex.asset";
        
        [MenuItem("Assets/Perlin Noise")]
        public static void CreateWizard()
        {
            DisplayWizard<GeneratePerlinNoise>("Perlin Noise", "CREATE");
        }

        public void OnWizardCreate()
        {
            Texture2D tex = new (width, width, TextureFormat.RFloat, false);
            for (int i = 0; i < width; i++)
            for (int j = 0; j < width; j++)
            {
                float pixelsPerCell = width / scale;
                float val = Mathf.PerlinNoise(
                    (i + width * seed) / pixelsPerCell,
                    (j + width * seed) / pixelsPerCell);
                tex.SetPixel(i, j, new Color(val, 0, 0, 0));
            }
            
            AssetDatabase.CreateAsset(tex, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}