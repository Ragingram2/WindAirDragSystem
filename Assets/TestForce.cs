using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestForce : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            transform.parent.GetComponent<Rigidbody>().AddForceAtPosition(-Vector3.right * 3000f, transform.position);
        }
    }
}
