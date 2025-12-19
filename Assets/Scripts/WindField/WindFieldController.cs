using System;
using System.Collections.Generic;
using KToolkit;
using UnityEngine;

public class WindFieldController : MonoBehaviour
{
    [SerializeField] 
    [Header("风力大小")]
    private float forceIntensity;
    
    [SerializeField] 
    [Header("风力方向")]
    private Vector3 forceDirection;
    
    List<GameObject> effectObjects = new List<GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        foreach (var item in effectObjects)
        {
            if (item)
            {
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
