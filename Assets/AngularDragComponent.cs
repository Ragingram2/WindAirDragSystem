using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathExtensions;

public class AngularDragComponent : MonoBehaviour
{
    [SerializeField] private float pillowEffectFactor = .25f;
    [SerializeField] private float airDensity = .1f;
    [SerializeField] private float integrationSteps = 1f;

    private Rigidbody rb;

    private Vector3 startPos;
    public Vector3 velocity;
    public Vector3 angularVelocity;

    TriangleManager triManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        triManager = GetComponent<TriangleManager>();
        rb.AddTorque(transform.right * 100f);
    }

    // Update is called once per frame
    void Update()
    {

        velocity = rb.velocity;
        angularVelocity = rb.angularVelocity;

        //var airNormal = -Vector3.forward;
        for (int i = 0; i < triManager.Triangles.Count; i++)
        {
            var tri = triManager.Triangles[i];

            Vector3 integrationAxis = tri.normal.Cross(angularVelocity);
            var tangentialVel = rb.GetPointVelocity(transform.position + tri.center);
            var theta = Vector3.Angle(integrationAxis, angularVelocity);
            var torque = calculateTorque(tri, integrationAxis, theta);
            applyAngularDragForce(tri, integrationAxis, tangentialVel/*.normalized) * torque.magnitude*/);

            //var theta = Vector3.Angle(airNormal, tri.normal);

            //if (rb.angularVelocity.magnitude < 0.01f)
            //    return;
            //Vector3 integrationAxis = angularVelocity.normalized.Cross(tri.normal).normalized;
            //var torque = calculateTorque(tri, integrationAxis, theta);
            //applyAngularDragForce(tri, integrationAxis, torque);
        }
    }

    private float calculateArea(triangle tri, Vector3 airNormal)
    {
        return Vector3.Magnitude(Vector3.Cross(tri.side1 - (airNormal * Vector3.Dot(tri.side1, airNormal)), tri.side2 - (airNormal * Vector3.Dot(tri.side2, airNormal)))) * .5f;
    }

    private Vector3 calculateTorque(triangle tri, Vector3 integrationAxis, float theta)
    {
        var temp1 = integrationAxis * Mathf.Pow(angularVelocity.magnitude, 2f);
        var temp2 = Mathf.Cos(1 + (Mathf.Cos(theta) / 2f));
        Vector3 torque = temp1 * airDensity * temp2;
        return torque;
    }

    private List<(Vector3 position, Vector3 force)> applyAngularDragForce(triangle tri, Vector3 integrationAxis, Vector3 torque)
    {
        var dragForces = new List<(Vector3 position, Vector3 force)>();
        for (int x = 0; x < integrationSteps; x++)
        {
            var dist = (x / integrationSteps);
            var forcePos = transform.position + (integrationAxis.normalized * .5f) * (1f - dist);
            var force = torque * fallOffFactor(pillowEffectFactor, dist, 1f);
            //Debug.DrawRay(forcePos, force, Color.yellow);
            Debug.DrawRay(forcePos, torque, Color.red);
            rb.AddForceAtPosition(-force, forcePos);
        }

        return dragForces;
    }

    private float h1m(float a, float b, float c, float H, float x)
    {
        return ((x - a) / (b - a)) * (c - (H / 2f));
    }
    private float h2m(float a, float b, float c, float d, float H, float x)
    {
        return d + ((x - b) / (c - b)) * (d - c + (H / 2f));
    }
    private float integrate_ab(float a, float b, float c, float x, float H)
    {
        float sum = 0;
        for (float i = a; i < b; i++)
        {
            sum += Mathf.Pow(h1m(a, b, c, H, x), 2f) * Mathf.Pow(x, 3f);
        }
        return sum / (b - a);
    }
    private float integrate_b0(float a, float b, float c, float d, float x, float H)
    {
        float sum = 0;
        for (float i = b; i < 0; i++)
        {
            sum += Mathf.Pow(h2m(a, b, c, d, H, x), 2f) * Mathf.Pow(x, 3f);
        }
        return sum / b;
    }
    private float integrate_c0(float a, float b, float c, float d, float x, float H)
    {
        float sum = 0;
        for (float i = 0; i < c; i++)
        {
            sum += Mathf.Pow(h2m(a, b, c, d, H, x), 2f) * Mathf.Pow(x, 3f);
        }
        return sum / b; ;
    }

    private float fallOffFactor(float pillowEffectFactor, float distance, float length)
    {
        return (pillowEffectFactor * Mathf.Sqrt(1 - Mathf.Pow(distance / length, 2))) + (1 - pillowEffectFactor);
    }
}
