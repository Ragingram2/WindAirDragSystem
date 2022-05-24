using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathExtensions;
using System;

class triangle
{
    public (int idx, Vector3 point) v1 { get; set; }

    public (int idx, Vector3 point) v2 { get; set; }

    public (int idx, Vector3 point) v3 { get; set; }
    public Vector3 side1 { get; set; }
    public Vector3 side2 { get; set; }
    public Vector3 side3 { get; set; }
    public Vector3 normal { get; set; }
    public Vector3 center { get; set; }
    public List<(Vector3 position, Vector3 force)> dragForces { get; set; }

    public triangle() { }

    public override string ToString()
    {
        return $"V1: {v1.point}\n" +
                   $"V2: {v2.point}\n" +
                   $"V3: {v3.point}\n" +
                   $"Normal: {normal}";
    }
}

public class DragComponent : MonoBehaviour
{
    [SerializeField] private float pillowEffectFactor = .25f;
    [SerializeField] private float airDensity = .1f;
    [SerializeField] private float integrationSteps = 1f;

    private Rigidbody rb;
    private Mesh mesh;

    private Vector3 startPos;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    private List<triangle> triangles = new List<triangle>();
    public bool pause = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        UpdateMesh();
        startPos = transform.position;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.R))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            pause = true;
            transform.position = startPos;
            Debug.Log("Reset");
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            pause = !pause;
            Debug.Log(pause ? "Paused" : "Un-Paused");
        }
    }

    private void FixedUpdate()
    {
        //transform.Rotate(transform.right, Time.deltaTime * Input.GetAxis("Horizontal") * 100f);

        if (!pause)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            UpdateMesh();
            UpdateDragForces();
            Debug.Log("Updating");
            //UpdateAngularForces();
        }
        else
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
    private void UpdateMesh()
    {
        if (pause)
            return;

        mesh = GetComponent<MeshFilter>().mesh;
        var tris = mesh.GetTriangles(0);
        triangles.Clear();
        for (int i = 0; i < tris.Length;)
        {
            var point1 = transform.localToWorldMatrix * mesh.vertices[tris[i + 0]];
            var point2 = transform.localToWorldMatrix * mesh.vertices[tris[i + 1]];
            var point3 = transform.localToWorldMatrix * mesh.vertices[tris[i + 2]];
            triangles.Add(new triangle
            {
                v1 = (i + 0, point1),
                v2 = (i + 1, point2),
                v3 = (i + 2, point3),
                side1 = point2 - point1,
                side2 = point3 - point2,
                side3 = point1 - point3,
                normal = Vector3.Normalize(Vector3.Cross(point3 - point1, point2 - point1)),
                center = ((point1 + point2 + point3) / 3.0f)
            });
            i += 3;
        }
    }
    private void UpdateDragForces()
    {
        if (pause)
            return;
        velocity = rb.velocity;
        angularVelocity = rb.angularVelocity;
        var airNormal = -rb.velocity.normalized;
        for (int i = 0; i < triangles.Count; i++)
        {
            if (rb.velocity.magnitude < 0.1f)
                return;

            var tri = triangles[i];
            var theta = Vector3.Angle(airNormal, tri.normal);
            var area = calculateArea(tri, airNormal);
            var force = calculateForce(tri, area, theta);
            var airProjected = -(velocity - tri.normal * velocity.Dot(tri.normal)).normalized;

            var points = calculateLeftAndRightPoints(tri, airProjected);
            tri.dragForces = applyDragForce(points.left, points.right, airProjected, force);
            Vector3 integrationAxis = angularVelocity.normalized.Cross(tri.normal).normalized;
            var torque = calculateTorque(tri, integrationAxis, theta);
            applyAngularDragForce(integrationAxis, torque);
            Debug.DrawRay(transform.position + tri.center, torque);
        }
    }

    private void OnDrawGizmos()
    {
        foreach (triangle tri in triangles)
        {
            Vector3 integrationAxis = angularVelocity.normalized.Cross(tri.normal).normalized;
            Vector3 heightAxis = integrationAxis.Cross(tri.normal).normalized;
            Vector3 offset = Vector3.ProjectOnPlane(tri.center + new Vector3(.25f, 0f, .25f), tri.normal) + tri.center.Dot(tri.normal) * tri.normal;
            //float velMag = offset.magnitude * angularVelocity.magnitude * Mathf.Sin(Vector3.Angle(angularVelocity.normalized, offset));
            //Debug.Log(velMag);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position + tri.center, transform.position + offset);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + tri.center, integrationAxis);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + tri.center, heightAxis);
            Gizmos.color = Color.red;
            if (tri.dragForces != null)
                foreach (var drag in tri.dragForces)
                {
                    Gizmos.DrawRay(drag.position, drag.force);
                }
        }
    }

    private (Vector3 original, Vector3 projected) findLeftMostPoint(List<(Vector3 original, Vector3 projected)> points)
    {
        (Vector3 original, Vector3 projected) leftMostPoint = (Vector3.zero, Vector3.zero);
        float maxVal = float.MaxValue;
        foreach (var point in points)
        {
            if (point.projected.x < maxVal)
            {
                maxVal = point.projected.x;
                leftMostPoint = point;
            }
        }
        return leftMostPoint;
    }
    private (Vector3 original, Vector3 projected) findRightMostPoint(List<(Vector3 original, Vector3 projected)> points)
    {
        (Vector3 original, Vector3 projected) rightMostPoint = (Vector3.zero, Vector3.zero);
        float minVal = float.MinValue;
        foreach (var point in points)
        {
            if (point.projected.x > minVal)
            {
                minVal = point.projected.x;
                rightMostPoint = point;
            }
        }
        return rightMostPoint;
    }
    Vector3 transformPoint(Vector3 right, Vector3 up, Vector3 forward, Vector3 point)
    {
        return (right * point.x) + (up * point.y) + (forward * point.z);
    }
    private float calculateArea(triangle tri, Vector3 airNormal)
    {
        return Vector3.Magnitude(Vector3.Cross(tri.side1 - (airNormal * Vector3.Dot(tri.side1, airNormal)), tri.side2 - (airNormal * Vector3.Dot(tri.side2, airNormal)))) * .5f;
    }
    private Vector3 calculateForce(triangle tri, float area, float theta)
    {
        var force = airDensity * area * (velocity.Pow(2f)) * Mathf.Cos(theta) * (1 + (Mathf.Cos(theta) / 2f));
        return new Vector3(force.x * tri.normal.x, force.y * tri.normal.y, force.z * tri.normal.z);
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
    private float integrate_c0(float a, float b, float c, float x)
    {
        return 0f;
    }
    private Vector3 calculateTorque(triangle tri, Vector3 integrationAxis, float theta)
    {
        Vector3 torque = integrationAxis.Mult(angularVelocity.Pow(2f)) * airDensity * Mathf.Cos(1 + (Mathf.Cos(theta) / 2f));
        //var ab = ;
        //var b0;
        //var c0;
        return torque;
    }
    private float fallOffFactor(float pillowEffectFactor, float distance, float length)
    {
        return (pillowEffectFactor * Mathf.Sqrt(1 - Mathf.Pow(distance / length, 2))) + (1 - pillowEffectFactor);
    }
    private (Vector3 left, Vector3 right) calculateLeftAndRightPoints(triangle tri, Vector3 airProjected)
    {
        (Vector3 left, Vector3 right) result = (Vector3.zero, Vector3.zero);

        var up = Vector3.Cross(airProjected, tri.normal).normalized;

        List<(Vector3 original, Vector3 projected)> projectedPoints = new List<(Vector3 original, Vector3 projected)>();
        projectedPoints.Add((transform.position + tri.v1.point, Vector3.Project(transformPoint(airProjected, up, tri.normal, tri.v1.point), transform.position + airProjected)));
        projectedPoints.Add((transform.position + tri.v2.point, Vector3.Project(transformPoint(airProjected, up, tri.normal, tri.v2.point), transform.position + airProjected)));
        projectedPoints.Add((transform.position + tri.v3.point, Vector3.Project(transformPoint(airProjected, up, tri.normal, tri.v3.point), transform.position + airProjected)));

        result.right = findRightMostPoint(projectedPoints).original;
        result.left = findLeftMostPoint(projectedPoints).original;
        return result;
    }
    private List<(Vector3 position, Vector3 force)> applyDragForce(Vector3 leftMostPoint, Vector3 rightMostPoint, Vector3 projAir, Vector3 force)
    {
        var length = Vector3.Distance(rightMostPoint, leftMostPoint);
        var dragForces = new List<(Vector3 position, Vector3 force)>();
        int halfPoint = Mathf.Max((int)(integrationSteps / 2), 1);

        for (int i = 0; i < integrationSteps; i++)
        {
            //Linear Drag
            var dist = length * (i / integrationSteps);//dist
            var triThickness = (length / integrationSteps);
            var fallOff = fallOffFactor(pillowEffectFactor, dist, length);
            var integratedForce = force * fallOff/* * triThickness * (j < halfPoint ? dist / halfPoint : 1 - ((dist - halfPoint) / (length - halfPoint)))*/;
            var forcePos = (leftMostPoint) + projAir.normalized * dist;
            dragForces.Add((forcePos, integratedForce));
            rb.AddForceAtPosition(integratedForce, forcePos);
        }
        return dragForces;
    }

    private List<(Vector3 position, Vector3 force)> applyAngularDragForce(Vector3 integrationAxis, Vector3 torque)
    {
        var dragForces = new List<(Vector3 position, Vector3 force)>();

        for (int i = 0; i < integrationSteps; i++)
        {
            //Angular Drag 
        }
        return dragForces;
    }

}

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
