using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathExtensions;
using System;
using UnityEditor;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TriangleManager))]
public class DragComponent : MonoBehaviour
{

    [HideInInspector] public float pillowEffectFactor = .42f;
    [HideInInspector] public float airDensity = .1f;
    [HideInInspector] public float forceMod = 10f;
    [HideInInspector] public Vector3 airVelocity;
    public bool interactable = false;
    public bool debugDrag = false;
    public bool debugProjectedAir = false;
    public bool debugNormals = false;

    private Rigidbody rb;
    private TriangleManager triManager;
    private Vector3 startPos;
    private Vector3 velocity;
    private Vector3 angularVelocity;
    private Vector3 airNormal;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
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

        if (triManager.IsPaused())
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        rb.isKinematic = interactable ? triManager.IsPaused() : rb.isKinematic;
        rb.useGravity = interactable ? !triManager.IsPaused() : rb.useGravity;
        UpdateDragForces();
    }

    private void OnDrawGizmos()
    {
        if (triManager == null)
            return;

        foreach (var tri in triManager.Triangles)
        {
            var r = tri.center - transform.position;
            velocity = airVelocity + rb.velocity + rb.angularVelocity.Cross(r);
            airNormal = -velocity.normalized;
            var airProjected = Vector3.ProjectOnPlane(velocity, tri.normal);
            var leftVertex = tri.closestVertexToPoint(-airProjected.normalized * 2f);
            var angleDeg = Vector3.Angle(airNormal, tri.normal);
            if (angleDeg > 90.0f)
            {
                if (debugNormals)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(tri.center, tri.normal / 2f);
                }
                if (debugProjectedAir)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(leftVertex.point, -airProjected.normalized * 2f);
                }
                if (debugDrag)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(tri.center, tri.drag);
                }
            }

            float length = 0f;
            switch (leftVertex.idx)
            {
                case 0:
                    length = tri.airFlowArea(airNormal) / tri.side2.magnitude;
                    break;
                case 1:
                    length = tri.airFlowArea(airNormal) / tri.side3.magnitude;
                    break;
                case 2:
                    length = tri.airFlowArea(airNormal) / tri.side1.magnitude;
                    break;
            }
            var integrationAxis = rb.angularVelocity.Cross(tri.normal);

            //var v = rb.angularVelocity.Cross(r);
            //angleDeg = Vector3.Angle(v, tri.normal);
            //if (angleDeg > 90f)
            //    continue;

            //Gizmos.color = Color.white;
            //Handles.Label(tri.center, $"Degrees: {angleDeg},AxisLength: {length}");
            //Gizmos.color = Color.blue;
            //Gizmos.DrawRay(tri.center, tri.normal);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(tri.center, velocity);
            Handles.Label(tri.center, $"Velocity: {velocity}");

            //if (90f - angleDeg > .1f)
            //{
            //    for (int i = -5; i < 5; i++)
            //    {
            //        var offset = integrationAxis.normalized * length / 2f * i / 5f;
            //        Gizmos.color = Color.red;
            //        Gizmos.DrawRay(tri.center + offset, rb.angularVelocity.Cross(r + offset));
            //        Gizmos.color = Color.green;
            //        Gizmos.DrawRay(tri.center + offset, integrationAxis.Cross(tri.normal));
            //    }
            //}
        }
    }

    private void UpdateDragForces()
    {
        if (triManager.IsPaused())
            return;
        bool logInfo = false;


        if (Input.GetKeyUp(KeyCode.Q))
            logInfo = true;

        velocity = airVelocity + rb.velocity;
        angularVelocity = rb.angularVelocity;
        airNormal = -velocity.normalized;
        for (int i = 0; i < triManager.Triangles.Count; i++)
        {
            var tri = triManager.Triangles[i];
            var angleDeg = Vector3.Angle(airNormal, tri.normal);
            var theta = Mathf.Deg2Rad * angleDeg;
            var area = tri.airFlowArea(airNormal);
            if (velocity.magnitude > 0.1f)
            {
                if (angleDeg < 90.0f)
                    continue;

                var force = calculateForce(tri, area, theta);

                var airProjected = Vector3.ProjectOnPlane(velocity, tri.normal) + tri.center;
                var leftVertex = tri.closestVertexToPoint(-airProjected.normalized * 2f);
                var eccentricity = tri.center - tri.inCenter();

                var h = Vector3.Cross(airProjected.normalized, tri.normal);
                var hAvg = h.Dot(eccentricity);

                float length = 0f;
                Vector3 p2 = Vector3.zero;
                switch (leftVertex.idx)
                {
                    case 0:
                        length = tri.area / tri.side2.magnitude;
                        p2 = Vector3.Distance(tri.v1.point, tri.v2.point) < Vector3.Distance(tri.v1.point, tri.v3.point) ? tri.v2.point : tri.v3.point;
                        break;
                    case 1:
                        length = tri.area / tri.side3.magnitude;
                        p2 = Vector3.Distance(tri.v2.point, tri.v1.point) < Vector3.Distance(tri.v2.point, tri.v3.point) ? tri.v1.point : tri.v3.point;
                        break;
                    case 2:
                        length = tri.area / tri.side1.magnitude;
                        p2 = Vector3.Distance(tri.v3.point, tri.v1.point) < Vector3.Distance(tri.v3.point, tri.v2.point) ? tri.v1.point : tri.v2.point;
                        break;
                }

                var b = p2.Dot(airProjected.normalized);

                var lAvg = (topLeft(b, length) + topRight(b, length)) / (bottomLeft(b, length) + bottomRight(b, length));
                tri.drag = applyDragForce(leftVertex.point + (lAvg * airProjected.normalized) + (hAvg * h), force);
                if (logInfo)
                {
                    Debug.Log("");
                    Debug.Log($"Area:{area}");
                    Debug.Log($"Angle:{angleDeg}");
                    Debug.Log($"Theta:{theta}");
                    Debug.Log($"Cos(Theta):{Mathf.Cos(theta)}");
                    Debug.Log($"Force:{force}");
                    Debug.Log($"Projected Air:{airProjected}");
                    Debug.Log($"height:{h}");
                    Debug.Log($"heightAvg:{hAvg}");
                }
            }

            if (angularVelocity.magnitude > .1f)
            {
                var torque = calculateTorque(tri);
                Debug.Log(torque);
                rb.AddTorque(torque);
            }
        }
    }



    private Vector3 calculateForce(triangle tri, float area, float theta)
    {
        var force = airDensity * area * (velocity.Pow(2f)) * Mathf.Cos(theta) * (1 + (Mathf.Cos(theta) / 2f));
        return force.Mult(tri.normal);
    }
    private float fallOffFactor(float distance, float length) => (pillowEffectFactor * Mathf.Sqrt(1 - Mathf.Pow(distance / length, 2))) + (1 - pillowEffectFactor);

    private Vector3 applyDragForce(Vector3 forcePoint, Vector3 force)
    {
        force *= fallOffFactor(.5f, 1f);
        rb.AddForceAtPosition(force * forceMod, forcePoint);
        return force * forceMod;
    }

    private Vector3 calculateTorque(triangle tri)
    {
        Vector3 integrationAxis = angularVelocity.Cross(tri.normal);
        Vector3 heightAxis = integrationAxis.Cross(tri.normal);
        float angle = Vector3.Angle(tri.normal, angularVelocity.normalized);
        float a = -1, b = -.5f, c = .5f, d = 1;
        float maxHeight = 2f;
        float delta = .1f;
        float f1 = 0f, f2 = 0f, f3 = 0f;
        float range = (b - a) / delta;

        for (float x = a; x < b; x += delta)
        {
            f1 += dF1(x, () => h1(x, a, b, maxHeight), () => h1m(x, a, b, c, maxHeight), maxHeight, angle) * x;
        }
        f1 /= range;

        range = (0 - b) / delta;
        for (float x = b; x < 0; x += delta)
        {
            f2 += dF1(x, () => h2(x, b, c, maxHeight), () => h2m(x, b, c, d, maxHeight), maxHeight, angle) * x;
        }
        f2 /= range;

        range = (c - 0) / delta;
        for (float x = 0; x < c; x += delta)
        {
            f3 += dF1(x, () => h2(x, b, c, maxHeight), () => h2m(x, b, c, d, maxHeight), maxHeight, angle) * x;
        }
        f3 /= range;
        return heightAxis * (f1 + f2 + f3);
    }
    private float dF1(float x, Func<float> h, Func<float> hm, float maxHeight, float angle)
    {
        var hOut = h.Invoke();
        var hmOut = hm.Invoke();
        var df1out = (1f - (hm.Invoke() / h.Invoke())) * forceDiff(x, h, maxHeight, angle);
        return df1out;
    }

    private float forceDiff(float x, Func<float> h, float maxHeight, float angle)
    {
        var df = airDensity * h.Invoke() * x.Pow(2) * angularVelocity.magnitude.Pow(2) * Mathf.Cos(angle) * (1f + (Mathf.Cos(angle) / 2f)) * fallOffFactor(x, maxHeight);
        return df;
    }

    private float h1(float x, float a, float b, float maxHeight)
    {
        var h1out = maxHeight * (x - a) / (b - a);
        return h1out;
    }

    private float h2(float x, float b, float c, float maxHeight)
    {
        var h2out = maxHeight * (1f - ((x - b) / (c - b)));
        return h2out;
    }

    private float h1m(float x, float a, float b, float c, float maxHeight)
    {
        var h1mout = (x - a) / (b - a) * (c - (maxHeight / 2f));
        return h1mout;
    }

    private float h2m(float x, float b, float c, float d, float maxHeight)
    {
        var h2mout = d + (x - b) / (c - b) * (d - c + (maxHeight / 2f));
        return h2mout;
    }

    private float topLeft(float b, float L)
    {
        var a = pillowEffectFactor;
        return ((-a * b.Pow(8)) +
                    (a.Pow(2) * b.Pow(8) * L.Pow(3)) -
                    (3 * a * b.Pow(6f) * L.Pow(3)) +
                    (3 * b.Pow(6) * L.Pow(3))) /
                    (12 * L.Pow(3));
    }
    private float topRight(float b, float L)
    {
        var a = pillowEffectFactor;
        return ((-a.Pow(2) * b.Pow(7)) +
                    (2 * a.Pow(2) * b.Pow(6)) +
                    (a.Pow(2) * b.Pow(3) * L.Pow(4)) -
                    (3 * a * b.Pow(5) * L.Pow(3)) +
                    (6 * a * b.Pow(4) * L.Pow(4)) +
                    (3 * b.Pow(5) * L.Pow(3)) -
                    (6 * b.Pow(4) * L.Pow(4)) +
                    (2 * a.Pow(2) * L.Pow(7)) -
                    (4 * a * L.Pow(8)) -
                    (3 * L.Pow(8))) /
                    ((12 * b * L.Pow(3)) - (12 * L.Pow(4)));
    }
    private float bottomLeft(float b, float L)
    {
        var a = pillowEffectFactor;
        return ((-2 * a.Pow(2) * b * L.Pow(5)) +
                    (a * b.Pow(3) * L.Pow(3)) -
                    (3 * a * b.Pow(4) * L.Pow(3)) +
                    (3 * b.Pow(4) * L.Pow(3))) /
                    (6 * L.Pow(3));
    }
    private float bottomRight(float b, float L)
    {
        var a = pillowEffectFactor;
        return ((-a.Pow(2) * b.Pow(5)) +
                    (2 * a.Pow(2) * b.Pow(4) * L) +
                    (a.Pow(2) * b.Pow(2) * L.Pow(3)) -
                    (2 * a.Pow(2) * b * L.Pow(4)) -
                    (3 * a * b.Pow(3) * L.Pow(3)) +
                    (6 * a * b.Pow(2) * L.Pow(4)) +
                    (3 * b.Pow(3) * L.Pow(3)) -
                    (6 * b.Pow(2) * L.Pow(4)) +
                    (2 * a.Pow(2) * L.Pow(5)) +
                    (3 * a * L.Pow(6)) -
                    (3 * L.Pow(6))) /
                    ((6 * b * L.Pow(3)) - (3 * L.Pow(6)));
    }
}