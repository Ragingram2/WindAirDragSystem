using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitialTorque : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Rigidbody>().AddTorque(transform.up * 500f);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
