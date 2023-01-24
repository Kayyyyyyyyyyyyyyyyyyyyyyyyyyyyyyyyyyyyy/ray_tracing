using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RayTracingMaster))]
public class RayTraceSceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RayTracingMaster rayMaster = (RayTracingMaster)target;
        if (DrawDefaultInspector())
        {
            if(rayMaster.autoUpdateScene) rayMaster.SetUpScene();
        }
    }

    
}
