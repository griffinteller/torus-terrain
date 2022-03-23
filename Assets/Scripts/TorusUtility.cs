using System.Collections.Generic;
using UnityEngine;

public static class TorusUtility
{
    public static Vector2[] GenerateQuadUvs(Vector2Int quadResolution, Vector2Int quad, int[] cols)
    {
        List<Vector2> uvs = new();
        Vector2 lowerLeft = new(
            (float)quad.x / quadResolution.x,
            (float)quad.y / quadResolution.y);

        float vSep = 1f / (quadResolution.y * (cols.Length - 1));
            
        for (int j = 0; j < cols.Length; j++)
        {
            float v = lowerLeft.y + vSep * j;
            float uSep = 1f / (quadResolution.x * (cols[j] - 1));
            for (int i = 0; i < cols[j]; i++)
            {
                uvs.Add(new Vector2(lowerLeft.x + uSep * i, v));
            }
        }

        return uvs.ToArray();
    }

    public static int[] GenerateQuadVertexDimensions(
        Vector2Int quadResolution, Vector2Int quad, float parameter, int rows)
    {
        int[] cols = new int[rows]; // wrapping means one overlapping layer
            
        float xQuadRadians = Mathf.PI * 2 / quadResolution.x;
        float yQuadRadians = Mathf.PI * 2 / quadResolution.y;

        float rowSepRad = yQuadRadians / (rows - 1);
        float rowSepLin = Mathf.Sin(rowSepRad) / Mathf.Sin((Mathf.PI - rowSepRad) / 2);
            
        for (int row = 0; row < rows; row++)
        {
            float theta = yQuadRadians * quad.y + row * rowSepRad;
            float rowWidth = (parameter + Mathf.Cos(theta)) * xQuadRadians;
            cols[row] = Mathf.Max(2, Mathf.RoundToInt(rowWidth / rowSepLin) + 1); // +1 for overlapping column
        }

        return cols;
    }
    
    public static int[] GenerateMeshTriangles(int[] cols)
    {
        List<int> tris = new ();

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

    public static Vector3 TorusToCoord(Vector2 angles, float parameter, float majorRadius)
    {
        return Quaternion.Euler(0, Mathf.Rad2Deg * -angles.x, 0) 
               * (majorRadius * new Vector3(
                   1 + Mathf.Cos(angles.y) / parameter,
                   1 + Mathf.Sin(angles.y) / parameter,
                   0));
    }

    public static Vector3 UVToCoord(Vector2 uv, float parameter, float majorRadius)
    {
        Vector2 angles = uv * (Mathf.PI * 2);
        return TorusToCoord(angles, parameter, majorRadius);
    }

    public static Vector3 UVToNormal(Vector2 uv)
    {
        float thetaDeg = uv.x * 360;
        float phiRad = uv.y * Mathf.PI * 2;
        return Quaternion.Euler(0, -thetaDeg, 0) * new Vector3(Mathf.Cos(phiRad), Mathf.Sin(phiRad), 0);
    }
}