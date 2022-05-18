using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathExtensions;

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
    private List<triangle> triangles = new List<triangle>();
    private bool pause = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        UpdateMesh();
        startPos = transform.position;
    }

    private void FixedUpdate()
    {
        transform.Rotate(transform.right, Time.deltaTime * Input.GetAxis("Horizontal") * 100f);

        if (!pause)
        {
            rb.isKinematic = false;
            UpdateMesh();
            UpdateDragForces();
            //UpdateAngularForces();
        }
        else
            rb.isKinematic = true;

        if (Input.GetKey(KeyCode.R))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            transform.position = startPos;
        }

        if (Input.GetKeyUp(KeyCode.T))
            pause = !pause;

        if (Input.GetKey(KeyCode.Space))
        {
            rb.useGravity = true;
        }
    }
    private void UpdateMesh()
    {
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
        if (!rb.useGravity)
            return;
        var airNormal = -rb.velocity.normalized;
        for (int i = 0; i < triangles.Count; i++)
        {
            var tri = triangles[i];
            var theta = Vector3.Angle(airNormal, tri.normal);
            var area = calculateArea(tri, airNormal);
            var force = calculateForce(tri, rb.velocity, area, theta);
            var airProjected = -(rb.velocity - tri.normal * rb.velocity.Dot(tri.normal)).normalized;

            var points = calculateLeftAndRightPoints(tri, airProjected);
            //Debug.DrawRay(transform.position + tri.center + rb.velocity.normalized*2f, -rb.velocity.normalized, Color.yellow);

            tri.dragForces = applyDragForce(points.left, points.right, airProjected, force);

            //Debug.DrawLine(transform.position + tri.v1.point, transform.position + tri.v2.point, Color.red);
            //Debug.DrawLine(transform.position + tri.v2.point, transform.position + tri.v3.point, Color.red);
            //Debug.DrawLine(transform.position + tri.v3.point, transform.position + tri.v1.point, Color.red);

        }
    }

    private void UpdateAngularForces()
    {

    }

    private void OnDrawGizmos()
    {
        //foreach (triangle tri in triangles)
        //{
        //    Gizmos.color = Color.red;
        //    if (tri.dragForces != null)
        //        foreach (var drag in tri.dragForces)
        //        {
        //            Gizmos.DrawRay(drag.position, drag.force);
        //        }
        //}
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
    private Vector3 calculateForce(triangle tri, Vector3 vel, float area, float theta)
    {
        var force = airDensity * area * (new Vector3(vel.x * vel.x, vel.y * vel.y, vel.z * vel.z)) * Mathf.Cos(theta) * (1 + (Mathf.Cos(theta) / 2f));
        return new Vector3(force.x * tri.normal.x, force.y * tri.normal.y, force.z * tri.normal.z);
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
        //Debug.DrawRay(transform.position + tri.center, up, Color.green);
        //Debug.DrawRay(transform.position + tri.center, airProjected, Color.red);
        //Debug.DrawRay(transform.position + tri.center, tri.normal, Color.blue);

        result.right = findRightMostPoint(projectedPoints).original;
        result.left = findLeftMostPoint(projectedPoints).original;
        return result;
    }
    private List<(Vector3 position, Vector3 force)> applyDragForce(Vector3 leftMostPoint, Vector3 rightMostPoint, Vector3 projAir, Vector3 force)
    {
        var length = Vector3.Distance(rightMostPoint, leftMostPoint);
        var dragForces = new List<(Vector3 position, Vector3 force)>();
        for (int j = 0; j < integrationSteps; j++)
        {
            var dist = length * (j / integrationSteps);
            var fallOff = fallOffFactor(pillowEffectFactor, dist, length);
            var integratedForce = force * fallOff;
            var forcePos = (leftMostPoint) + projAir.normalized * dist;
            dragForces.Add((forcePos, integratedForce));
            rb.AddForceAtPosition(integratedForce, forcePos);
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
    }
}
