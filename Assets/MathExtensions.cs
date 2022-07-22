using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MathExtensions
{
    public static class VectorExtensions
    {
        public static Vector3 Mult(this Vector3 v, Vector3 scale)
        {
            return new Vector3(v.x * scale.x, v.y * scale.y, v.z * scale.z);
        }

        public static float Dot(this Vector3 a, Vector3 b)
        {
            return Vector3.Dot(a, b);
        }

        public static Vector3 Cross(this Vector3 a, Vector3 b)
        {
            return Vector3.Cross(a, b);
        }

        public static Vector3 Pow(this Vector3 a, float exp)
        {
            return new Vector3(Mathf.Pow(a.x, exp), Mathf.Pow(a.y, exp), Mathf.Pow(a.z, exp));
        }
    }
}
