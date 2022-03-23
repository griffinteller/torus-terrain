using System;
using UnityEngine;

public static class MathUtility
{
    public static float SmootherStep(float a, float b, float t)
    {
        return a + (6 * t * t * t * t * t - 15 * t * t * t * t + 10 * t * t * t) * (b - a);
    }
    
    public static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    public static Vector3 Map(Vector3 v, Func<float, float> f)
    {
        return new Vector3(f(v.x), f(v.y), f(v.z));
    }

    public static float BarycentricSmoothStep(Vector3 values, Vector3 coords)
    {
        float xy = Mathf.SmoothStep(values.x, values.y, coords.y / (coords.x + coords.y));
        
        if (float.IsNaN(xy))
            return values.z;

        return Mathf.SmoothStep(xy, values.z, coords.z);
    }

    public static float MinAbs(params float[] values)
    {
        float minAbs = float.MaxValue;
        foreach (float val in values)
        {
            if (Mathf.Abs(val) < Mathf.Abs(minAbs))
            {
                minAbs = val;
            }
        }

        return minAbs;
    }
}