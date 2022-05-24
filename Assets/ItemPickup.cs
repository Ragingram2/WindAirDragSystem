using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    GameObject pickedUp;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(transform.position, transform.forward, out hit))
            {
                if (hit.transform.tag.Equals("Pickup"))
                {
                    pickedUp = hit.collider.gameObject;
                    pickedUp.transform.parent = transform;
                    pickedUp.GetComponent<Rigidbody>().isKinematic = true;
                    pickedUp.GetComponent<Rigidbody>().useGravity = false;
                    pickedUp.GetComponent<Rigidbody>().velocity = Vector3.zero;
                    pickedUp.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                }
            }
        }
        if (Input.GetMouseButtonUp(0) && pickedUp != null)
        {
            pickedUp.transform.parent = null;
            pickedUp.GetComponent<Rigidbody>().isKinematic = false;
            pickedUp.GetComponent<Rigidbody>().useGravity = true;
            pickedUp.GetComponent<Rigidbody>().velocity = Vector3.zero;
            pickedUp.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            pickedUp.GetComponent<Rigidbody>().AddForce(transform.forward * 9000.0f);
            pickedUp = null;
        }
    }
}
