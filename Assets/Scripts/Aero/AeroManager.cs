using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AeroManager : MonoBehaviour
{
    [SerializeField] private Vector3 airVelocity;
    [SerializeField] private float airDensity = .1f;
    [SerializeField] private float pillowEffectFactor = .43f;
    [SerializeField] private float forceMod = 10f;
    private void OnValidate()
    {
        var aeroFoils = FindObjectsOfType<DragComponent>();
        foreach (var dragComp in aeroFoils)
        {
            dragComp.airDensity = airDensity;
            dragComp.pillowEffectFactor = pillowEffectFactor;
            dragComp.forceMod = forceMod;
            dragComp.airVelocity = airVelocity;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(Vector3.zero + new Vector3(0, 5, 0), airVelocity);
        Gizmos.DrawSphere(Vector3.zero + new Vector3(0, 5, 0), .3f);
    }
}
