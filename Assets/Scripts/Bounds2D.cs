using UnityEngine;

public struct Bounds2D
{
    public float left;
    public float right;
    public float top;
    public float bottom;

    public Bounds2D(Vector2 center)
    {
        left = center.x;
        right = center.x;
        top = center.y;
        bottom = center.y;
    }

    public void Encapsulate(Vector2 point)
    {
        if (point.x < left)
            left = point.x;
        else if (right < point.x)
            right = point.x;

        if (point.y < bottom)
            bottom = point.y;
        else if (top < point.y)
            top = point.y;
    }
}