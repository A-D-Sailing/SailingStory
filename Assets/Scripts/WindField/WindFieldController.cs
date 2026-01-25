using System;
using System.Collections.Generic;
using KToolkit;
using UnityEngine;

public class WindFieldController : MonoBehaviour
{
    [SerializeField] 
    [Tooltip("Wind Intensity")]
    private float forceIntensity;
    
    [SerializeField] 
    [Tooltip("Wind Direction")]
    private Vector3 forceDirection;

    private Transform windRoot;
    
    List<GameObject> effectObjects = new List<GameObject>();
    
    void Start()
    {
        windRoot = transform.Find("wind_root");
    }

    // Update is called once per frame
    void Update()
    {
        windRoot.rotation = Quaternion.LookRotation(forceDirection);
    }

    private void FixedUpdate()
    {
        foreach (var item in effectObjects)
        {
            if (item)
            {
                // 仅对被推动物体的forward方向生效
                Vector3 boatDirection = item.transform.forward;
                float actualForce = forceIntensity *
                                    Mathf.Cos(Mathf.Deg2Rad * Vector3.Angle(boatDirection, forceDirection.normalized));
                item.GetComponent<Rigidbody>().AddForce(actualForce * boatDirection.normalized);
                KDebugLogger.Cortex_DebugLog(Vector3.Angle(boatDirection, forceDirection.normalized),
                    Mathf.Cos(Vector3.Angle(boatDirection, forceDirection.normalized)));
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        effectObjects.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        effectObjects.Remove(other.gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + forceDirection.normalized * 5f);
        foreach (var item in effectObjects)
        {
            if (item)
            {
                Gizmos.DrawLine(item.transform.position, item.transform.position + item.transform.forward * 5f);
            }
        }
    }
}