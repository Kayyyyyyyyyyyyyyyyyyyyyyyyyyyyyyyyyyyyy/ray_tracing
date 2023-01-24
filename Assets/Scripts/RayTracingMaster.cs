
using System;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using Random = UnityEngine.Random;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    private RenderTexture _targetRenderTexture;
    public Texture SkyboxTexure;

    [Range(1, 10)]
    public int numberOfReflections;

    [Range(0,1)]
    public float groundSpecular = 0.6f;
    [Range(0,1)]
    public float groundAlbedo = 0.8f;

    public Vector2 sphereRadius = new Vector2(3.0f, 3.0f);
    public uint spheresMax = 100;
    public float spherePlacementRadius = 100.0f;
    private ComputeBuffer _sphereBuffer;

    public Light directionalLight;

    private Camera _camera;
    public bool autoUpdateScene;

    private uint _currentSample = 0;
    private Material _addMaterial;

    private static readonly int Sample = Shader.PropertyToID("_Sample");

    //Called whenever a camera in unity has finished rendering
    private void OnRenderImage(RenderTexture src, RenderTexture destinationRenderTexture)
    {
        SetShaderParameters();
        Render(destinationRenderTexture);
    }

    private void Render(RenderTexture destinationRenderTexture)
    {
        //Make sure we have a current render target
        InitRenderTexture();
        
        
        
        //To render, we create a render target with the right dimensions to tell the compute shader about
        //The zero is the index of the compute shader's kernel function - we only have one
        //Set the target and dispatch the compute shader
        rayTracingShader.SetTexture(0, "Result", _targetRenderTexture);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        //Here we tell the GPU to get busy with a number of thread groups executing our shader code. Each thread group has a number of threads which we set in the shader
        //The size and number of thread groups can be specified up to three dimensions
        //In our case we ant to spawn one thread per pixel of the render target.
        //The default thread group size as defined in the unity compute shader template is [numthreads(8,8,1)]
        //This means we spawn one thread group per 8x8 pixels
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        
        //We write our results to the screen with Graphics.Blit
        Graphics.Blit(_targetRenderTexture, destinationRenderTexture);

        if (_addMaterial == null) _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat(Sample, _currentSample);
        Graphics.Blit(_targetRenderTexture, destinationRenderTexture, _addMaterial);
        _currentSample++;
    }

    private void InitRenderTexture()
    {
        if (_targetRenderTexture == null || _targetRenderTexture.width != Screen.width ||
            _targetRenderTexture.height != Screen.height)
        {
            //Release render texture if we already have one 
            if (_targetRenderTexture != null)  _targetRenderTexture.Release();
            
            //Get a new render target for Ray Tracing
            _targetRenderTexture = new RenderTexture(Screen.width, Screen.height, 0,RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            _targetRenderTexture.enableRandomWrite = true;
            _targetRenderTexture.Create();
        }
    }

    private void OnEnable()
    {
        _currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null)
        {
            _sphereBuffer.Release();
        }
    }

    public void SetUpScene()
    {
        List<Sphere> spheres = new List<Sphere>();

        //Sphere placement
        for (int i = 0; i < spheresMax; i++)
        {
            Sphere sphere = new Sphere();
            sphere.radius = sphereRadius.x + Random.value * (sphereRadius.y - sphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * spherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);
            
            //Make sure spheres dont intersect
            foreach (var other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }
            
            //Albedo and specular color
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
            
            spheres.Add(sphere); 
            SkipSphere:
            continue;
        }

        if (spheres.Count > 0)
        {
            _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
            _sphereBuffer.SetData(spheres);
        }
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
    }

    //Using unity's camera, we can use the calculated matrices to generate some camera rays.
    private void SetShaderParameters()
    {
        rayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        rayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        Vector3 lightVector = directionalLight.transform.forward;
        rayTracingShader.SetVector("_DirectionalLight", new Vector4(lightVector.x, lightVector.y, lightVector.z, directionalLight.intensity));
        rayTracingShader.SetVector("_GroundSpecular", Vector3.one * groundSpecular);
        rayTracingShader.SetVector("_GroundAlbedo", Vector3.one * groundAlbedo);
        rayTracingShader.SetInt("NumberOfReflections", numberOfReflections);
        rayTracingShader.SetTexture(0,"_SkyboxTexture", SkyboxTexure);
        rayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
    }

    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    }
}
