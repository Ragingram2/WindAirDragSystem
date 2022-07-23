using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathExtensions;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TriangleManager))]
public class DragComponent : MonoBehaviour
{
    [HideInInspector] public float pillowEffectFactor = .42f;
    [HideInInspector] public float airDensity = .1f;

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
        rb.isKinematic = triManager.IsPaused();
        rb.useGravity = !triManager.IsPaused();
        UpdateDragForces();
    }

    private void UpdateDragForces()
    {
        if (triManager.IsPaused())
            return;
        bool logInfo = false;

        airNormal = -rb.velocity.normalized;
        velocity = rb.velocity;
        //angularVelocity = rb.angularVelocity;

        if (Input.GetKeyUp(KeyCode.Q))
            logInfo = true;

        Debug.DrawRay(transform.position, -airNormal);
        for (int i = 0; i < triManager.Triangles.Count; i++)
        {
            if (rb.velocity.magnitude < 0.1f)
                return;

            var tri = triManager.Triangles[i];
            var angleDeg = Vector3.Angle(airNormal, tri.normal);
            var theta = Mathf.Deg2Rad * angleDeg;
            var area = tri.airFlowArea(airNormal);
            if (angleDeg < 90.0f)
                continue;

            var force = calculateForce(tri, area, theta);
            Debug.DrawRay(transform.position + tri.center, force * 10f, Color.red);
            Debug.DrawRay(transform.position + tri.center, tri.normal, Color.blue);
            var airProjected = velocity - tri.normal * (velocity.Dot(tri.normal));
            Debug.DrawRay(transform.position + tri.center, airProjected, Color.yellow);

            var leftPoint = tri.closestVertexToPoint(airProjected.normalized * 5f);
            Debug.DrawRay(transform.position + leftPoint, Vector3.up);
            //var eccentricity = Vector3.one;
            //var lAvg = pillowEffectFactor * .75f;
            //var hAvg = Vector3.Cross(airProjected, tri.normal).Dot(eccentricity);
            applyDragForce(transform.position + tri.center + leftPoint, airProjected, force);

            //if (rb.angularVelocity.magnitude < 0.1f)
            //    return;

            //Vector3 integrationAxis = tri.normal.Cross(angularVelocity);
            //var tangentialVel = rb.GetPointVelocity(transform.position + tri.center);
            //theta = Vector3.Angle(integrationAxis, angularVelocity);
            //var torque = tri.airFlowArea(-tangentialVel) * calculateTorque(tri, integrationAxis, theta);
            //applyAngularDragForce(tri, integrationAxis, (tangentialVel.normalized) * torque.magnitude);

            if (logInfo)
            {
                Debug.Log("");
                Debug.Log($"Area:{area}");
                Debug.Log($"Angle:{angleDeg}");
                Debug.Log($"Theta:{theta}");
                Debug.Log($"Cos(Theta):{Mathf.Cos(theta)}");
                Debug.Log($"Force:{force}");
                Debug.Log($"Projected Air:{airProjected}");
            }
        }
    }

    private Vector3 calculateForce(triangle tri, float area, float theta)
    {
        var force = airDensity * area * (velocity.Pow(2f)) * Mathf.Cos(theta) * (1 + (Mathf.Cos(theta) / 2f));
        return force.Mult(tri.normal);
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
    private void applyDragForce(Vector3 leftPoint, Vector3 projAir, Vector3 force)
    {
        force *= fallOffFactor(pillowEffectFactor, .5f, 1f);
        Debug.DrawRay(leftPoint + (projAir * .5f), Vector3.up * .5f);
        rb.AddForceAtPosition(force, leftPoint + (projAir * .5f));
    }

    private void applyAngularDragForce(triangle tri, Vector3 integrationAxis, Vector3 tangentialVel)
    {
    }
}