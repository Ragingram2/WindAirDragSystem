using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AeroManager : MonoBehaviour
{
    [SerializeField] private float airDensity =.1f;
    [SerializeField] private float pillowEffectFactor = .43f;
    private void OnValidate()
    {
        var aeroFoils = FindObjectsOfType<DragComponent>();
        foreach(var dragComp in aeroFoils)
        {
            dragComp.airDensity = airDensity;
            dragComp.pillowEffectFactor = pillowEffectFactor;
        }
    }
}
