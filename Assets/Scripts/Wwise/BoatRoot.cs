using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatRoot : MonoBehaviour
{
    public AK.Wwise.Event Boatsailingsound;

    void Update ()
    {
        Debug.Log("123");
        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
            Boatsailingsound.Post(gameObject);

        }
            
    }
}
