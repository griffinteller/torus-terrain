using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TorusGravity : MonoBehaviour
{
    public TorusTerrain torus;
    public float equatorAccel = 9.8f;

    private float _multiplier;
    private Rigidbody _rigidbody;

    private static float AGM(float a, float b)
    {
        while (!Mathf.Approximately(a, b))
        {
            float oldA = a;
            a = (a + b) / 2;
            b = Mathf.Sqrt(oldA * b);
        }

        return a;
    }

    private static (float, float) AGMWithSum(float a, float b)
    {
        float sum = 0;
        int i = 0;
        while (!Mathf.Approximately(a, b))
        {
            sum += Mathf.Pow(2, i - 1) * Mathf.Abs(a * a - b * b);
            float oldA = a;
            a = (a + b) / 2;
            b = Mathf.Sqrt(oldA * b);
            i++;
        }
        sum += Mathf.Pow(2, i) * Mathf.Abs(a * a - b * b);

        return (a, sum);
    }

    private static float EllipticK(float m)
    {
        return Mathf.PI / (2 * AGM(1, Mathf.Sqrt(1 - m)));
    }

    private static float EllipticE(float m)
    {
        if (m < 0)
        {
            return Mathf.Sqrt(-m + 1) * EllipticE(-m / (-m + 1));
        }
        
        (float a, float sum) = AGMWithSum(1, Mathf.Sqrt(1 - m));
        return Mathf.PI / (2 * a) * (1 - sum);
    }

    private static Vector2 InnerGravityIntegral(Vector2 p, float r)
    {
        float x = p.x;
        float y = p.y;
        float y2 = y * y;
        float rpx2 = (r + x) * (r + x);
        float rmx2 = (r - x) * (r - x);
        float a = rpx2 + y2;
        float b = rmx2 + y2;
        float m = -4 * r * x / b;
        float e = EllipticE(m);
        float k = EllipticK(m);

        float v = -4 * y * e / (Mathf.Sqrt(b) * a);

        if (x == 0)
        {
            return new Vector2(0, v);
        }
        
        float u = ((r * r - x * x + y2) * e - a * k) / (x * Mathf.Sqrt(b) * a);
        
        return new Vector2(u, v);
    }

    public void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _multiplier = equatorAccel
                      / (torus.MajorRadius * InnerGravityIntegral(
                             new Vector2(torus.MajorRadius + torus.MinorRadius, 0), torus.MajorRadius
                             ).magnitude);
    }

    public void FixedUpdate()
    {
        Vector3 pos = _rigidbody.position - torus.transform.position;
        float y = pos.y;
        Vector3 proj = Vector3.ProjectOnPlane(pos, Vector3.up);
        float x = proj.magnitude;
        Vector3 innerIntegral = InnerGravityIntegral(new Vector2(x, y), torus.MajorRadius);
        Vector3 accel = Quaternion.Euler(0, Vector3.SignedAngle(Vector3.right, proj, Vector3.up), 0) 
                        * innerIntegral * (torus.MajorRadius * _multiplier);
        _rigidbody.AddForce(accel, ForceMode.Acceleration);
        print(accel);
    }
}