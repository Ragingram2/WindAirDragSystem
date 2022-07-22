using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathExtensions;
using System;

public class DragComponent : MonoBehaviour
{
    [SerializeField] private float pillowEffectFactor = .25f;
    [SerializeField] private float airDensity = .1f;
    [SerializeField] private float integrationSteps = 1f;
    [SerializeField] private float airSpeed;
    [SerializeField] private Vector3 airNormal = Vector3.left;

    //private Rigidbody rb;
    private TriangleManager triManager;
    private Vector3 startPos;
    public Vector3 velocity;
    public Vector3 angularVelocity;

    private void Start()
    {
        //rb = GetComponent<Rigidbody>();
        triManager = GetComponent<TriangleManager>();
        startPos = transform.position;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.R))
        {
            triManager.Pause();
            transform.position = startPos;
        }

        //if (triManager.IsPaused())
        //{
        //    rb.velocity = Vector3.zero;
        //    rb.angularVelocity = Vector3.zero;
        //}
    }

    private void FixedUpdate()
    {
        //rb.isKinematic = triManager.IsPaused();
        //rb.useGravity = !triManager.IsPaused();
        UpdateDragForces();
    }

    private void UpdateDragForces()
    {
        if (triManager.IsPaused())
            return;
        bool logInfo = false;
        velocity = -airNormal*airSpeed;
        //angularVelocity = rb.angularVelocity;
        //var airNormal = -rb.velocity.normalized;

        if (Input.GetKeyUp(KeyCode.Q))
            logInfo = true;

        Debug.DrawRay(transform.position + new Vector3(0, 0, -1.5f), airNormal);
        for (int i = 0; i < triManager.Triangles.Count; i++)
        {
            //if (rb.velocity.magnitude < 0.1f)
            //    return;

            var tri = triManager.Triangles[i];
            var theta = Mathf.Deg2Rad*Vector3.Angle(airNormal, tri.normal);
            var area = tri.airFlowArea(airNormal);
            var force = calculateForce(tri, area, theta);
            Debug.DrawRay(transform.position + tri.center, force *10f, Color.red);
            Debug.DrawRay(transform.position + tri.center, tri.normal, Color.blue);
            if (logInfo)
            {
                Debug.Log("");
                Debug.Log($"Area:{area}");
                Debug.Log($"Theta:{theta}");
                Debug.Log($"Cos(Theta):{Mathf.Cos(theta)}");
                Debug.Log($"Force:{force}");
            }

            //Debug.Log(force);
            //var airProjected = -(velocity - tri.normal * velocity.Dot(tri.normal)).normalized;

            //var points = calculateLeftAndRightPoints(tri, airProjected);
            //tri.dragForces = applyDragForce(points.left, points.right, airProjected, force);

            //if (rb.angularVelocity.magnitude < 0.1f)
            //    return;

            //Vector3 integrationAxis = tri.normal.Cross(angularVelocity);
            //var tangentialVel = rb.GetPointVelocity(transform.position + tri.center);
            //theta = Vector3.Angle(integrationAxis, angularVelocity);
            //var torque = tri.airFlowArea(-tangentialVel) * calculateTorque(tri, integrationAxis, theta);
            //applyAngularDragForce(tri, integrationAxis, (tangentialVel.normalized) * torque.magnitude);
        }
    }

    private void OnDrawGizmos()
    {
        //foreach (triangle tri in triangles)
        //{
        //    Vector3 integrationAxis = angularVelocity.normalized.Cross(tri.normal).normalized;
        //    Vector3 heightAxis = integrationAxis.Cross(tri.normal).normalized;
        //    Vector3 offset = Vector3.ProjectOnPlane(tri.center + new Vector3(.25f, 0f, .25f), tri.normal) + tri.center.Dot(tri.normal) * tri.normal;
        //    //float velMag = offset.magnitude * angularVelocity.magnitude * Mathf.Sin(Vector3.Angle(angularVelocity.normalized, offset));
        //    //Debug.Log(velMag);
        //    Gizmos.color = Color.blue;
        //    Gizmos.DrawLine(transform.position + tri.center, transform.position + offset);
        //    Gizmos.color = Color.green;
        //    Gizmos.DrawRay(transform.position + tri.center, integrationAxis);
        //    Gizmos.color = Color.yellow;
        //    Gizmos.DrawRay(transform.position + tri.center, heightAxis);
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

    private Vector3 calculateForce(triangle tri, float area, float theta)
    {
        var force = airDensity * area * (velocity.Pow(2f)) * Mathf.Cos(theta) * (1 + (Mathf.Cos(theta) / 2f));
        return force.Mult(tri.normal);
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
    private Vector3 calculateTorque(triangle tri, Vector3 integrationAxis, float theta)
    {
        Vector3 torque = integrationAxis.Mult(angularVelocity.Pow(2f)) * airDensity * Mathf.Cos(1 + (Mathf.Cos(theta) / 2f));
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
            //rb.AddForceAtPosition(integratedForce, forcePos);
        }
        return dragForces;
    }

    private List<(Vector3 position, Vector3 force)> applyAngularDragForce(triangle tri, Vector3 integrationAxis, Vector3 tangentialVel)
    {
        var dragForces = new List<(Vector3 position, Vector3 force)>();

        for (int x = 0; x < integrationSteps; x++)
        {
            var dist = (x / integrationSteps);
            var forcePos = transform.position + (integrationAxis.normalized * .5f) * (1f - dist);
            var force = tangentialVel * fallOffFactor(pillowEffectFactor, dist, 1f);
            //Debug.DrawRay(forcePos, force, Color.yellow);
            //rb.AddForceAtPosition(-force, forcePos);
        }
        return dragForces;
    }
}