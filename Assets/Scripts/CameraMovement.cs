using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private Camera camera;
    public float mainSpeed = 100.0f;
    public float camSens = 0.25f;
    public float fovSpeed = 10;
    public float falloffFactor = 12.0f;

    private bool cameraMovementEnabled = true;
    

    private Vector3 lastMouse = new Vector3(255, 255, 255);

    private void Start()
    {
        camera = GetComponent<Camera>();
    }

    private void Update()
    {
        
        lastMouse = Input.mousePosition - lastMouse;
        lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0);
        var transform1 = transform;
        var eulerAngles = transform1.eulerAngles;
        lastMouse = new Vector3(eulerAngles.x + lastMouse.x, eulerAngles.y + lastMouse.y, 0);
        eulerAngles = lastMouse;
        if(cameraMovementEnabled) transform1.eulerAngles = eulerAngles;
        lastMouse = Input.mousePosition;

        if (Input.GetKey(KeyCode.Q))
        {
            if (camera.fieldOfView < 2 ) fovSpeed += Time.deltaTime;
            camera.fieldOfView += fovSpeed * Time.deltaTime;
        }else if (Input.GetKey(KeyCode.E))
        {
            if (camera.fieldOfView < 2) fovSpeed -= Time.deltaTime;
            camera.fieldOfView -= fovSpeed * Time.deltaTime;
            
        }else if (Input.GetKey(KeyCode.Space))
        {
            cameraMovementEnabled = !cameraMovementEnabled;
        }
        
    }
}
