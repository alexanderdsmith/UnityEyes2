using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using SimpleJSON;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

[System.Serializable]
public class CameraIntrinsics{
    public float fx;
    public float fy;
    public float cx;
    public float cy;
    public int width;
    public int height;
}

[System.Serializable]
public class CameraConfig{
    public string name;
    public Vector3 position;
    public Vector3 rotation;
    public bool is_orthographic;
    public CameraIntrinsics intrinsics;
}

[System.Serializable]
public class CameraConfiguration{
    public List<CameraConfig> cameras;
}

[System.Serializable]
public class EyeParameters
{
    public Vector2 pupilSizeRange;
    public Vector2 irisSizeRange;
    public float defaultYaw;
    public float defaultPitch;
    public float yawNoise;
    public float pitchNoise;
}


public class SynthesEyesServer : MonoBehaviour{
    public GameObject lightDirectionalObj;
    public GameObject eyeballObj;
    public GameObject eyeRegionObj;
    public GameObject eyeRegionSubdivObj;
    public GameObject eyeWetnessObj;
    public GameObject eyeWetnessSubdivObj;
    public GameObject eyeLashesObj;

    // Component references
    private EyeballController eyeball;
    private EyeRegionController eyeRegion;
    private SubdivMesh eyeRegionSubdiv;
    private EyeWetnessController eyeWetness;
    private SubdivMesh eyeWetnessSubdiv;
    private DeformEyeLashes[] eyeLashes;

    private LightingController lightingController;

    // Render settings for randomization
    public float defaultCameraPitch = 0;
    public float defaultCameraYaw = 0;
    public float cameraPitchNoise = Mathf.Deg2Rad * 20;
    public float cameraYawNoise = Mathf.Deg2Rad * 40;
    public float defaultEyePitch = 0;
    public float defaultEyeYaw = 0;
    public float eyePitchNoise = 30;
    public float eyeYawNoise = 30;

    private float randomizeSceneStartTime = 0f;
    private int randomizeSceneCallCount = 0;

    // Camera management fields
    private List<Camera> cameraList = new List<Camera>();
    private int currentCameraIndex = 0;
    public string jsonConfigPath = "camera_config.json";

    private int maxSamplesToSave = 10000;

    // Motion Center Feature
    private bool useMotionCenter = true;
    private Vector3 cameraArrayCenter = Vector3.zero;
    private Vector3 cameraArrayRotation = Vector3.zero;
    private Vector3 cameraArrayPositionNoise = Vector3.zero;
    private Vector3 cameraArrayRotationNoise = Vector3.zero;
    private GameObject cameraParent;
    private List<Vector3> cameraOriginalPositions = new List<Vector3>();
    private List<Vector3> cameraOriginalRotations = new List<Vector3>();

    // Motion Center -> False (Individual camera extrinsics noise)
    private List<Vector3> cameraExtrinsicsPositionNoise = new List<Vector3>();
    private List<Vector3> cameraExtrinsicsRotationNoise = new List<Vector3>();

    private List<CameraIntrinsics> cameraOriginalIntrinsics = new List<CameraIntrinsics>();

    // Point light tracking
    private List<Light> pointLightList = new List<Light>();
    private List<Vector3> pointLightOriginalPositions = new List<Vector3>();
    private List<Vector3> pointLightOriginalRotations = new List<Vector3>();
    private List<bool> pointLightArrayMounted = new List<bool>();

    private EyeParameters eyeParameters;

    public bool isSavingData = false;

	private Mesh eyemesh;

    int framesSaved = 0;

    private Vector3 xmlBasePosition;
    private Vector3 xmlBaseEulerAngles;

    private EyeVisualizationManager visualizationManager;

    void Start()
    {
        EnsureDirectoryExists("imgs");
        eyeRegion = eyeRegionObj.GetComponent<EyeRegionController>();
        eyeball = eyeballObj.GetComponent<EyeballController>();
        eyeRegionSubdiv = eyeRegionSubdivObj.GetComponent<SubdivMesh>();
        eyeWetness = eyeWetnessObj.GetComponent<EyeWetnessController>();
        eyeWetnessSubdiv = eyeWetnessSubdivObj.GetComponent<SubdivMesh>();
        eyeLashes = eyeLashesObj.GetComponentsInChildren<DeformEyeLashes>(true);

        lightingController = GameObject.Find("lighting_controller").GetComponent<LightingController>();

        visualizationManager = FindFirstObjectByType<EyeVisualizationManager>();
        if (visualizationManager == null)
        {
            GameObject visualMgrObj = new GameObject("VisualizationManager");
            visualizationManager = visualMgrObj.AddComponent<EyeVisualizationManager>();
            visualizationManager.synthesEyesServer = this;
        }

        // Load cameras from JSON
        if (File.Exists(jsonConfigPath))
        {
            LoadCamerasFromConfig(jsonConfigPath);

            if (eyeParameters != null)
            {
                eyeball.SetPupilSizeRange(eyeParameters.pupilSizeRange);
                eyeball.SetIrisSizeRange(eyeParameters.irisSizeRange);
            }
        }
    }

    private void LoadCamerasFromConfig(string configPath)
    {
        cameraList.Clear();
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0.5f, 0.5f, 0.5f);

        try
        {
            string jsonData = File.ReadAllText(configPath);
            Debug.Log($"Reading config from: {configPath}");

            JSONNode rootNode = JSON.Parse(jsonData);
            if (rootNode == null)
            {
                Debug.LogError("Failed to parse JSON config file");
                return;
            }

            if (rootNode["num_samples"] != null)
            {
                maxSamplesToSave = rootNode["num_samples"].AsInt;
                Debug.Log($"Will save a maximum of {maxSamplesToSave} images");
            }

            if (rootNode["motion_center"] != null)
            {
                useMotionCenter = rootNode["motion_center"].AsInt == 1;
                Debug.Log($"Motion center mode: {useMotionCenter}");
            }

            if (rootNode["camera_array_center"] != null)
            {
                JSONNode centerNode = rootNode["camera_array_center"];

                Vector3 arrayPosition = new Vector3(
                    centerNode["x"] != null ? centerNode["x"].AsFloat : 0f,
                    centerNode["y"] != null ? centerNode["y"].AsFloat : 0f,
                    centerNode["z"] != null ? centerNode["z"].AsFloat : 0f
                );

                Vector3 arrayRotation = new Vector3(
                    centerNode["rx"] != null ? centerNode["rx"].AsFloat : 0f,
                    centerNode["ry"] != null ? centerNode["ry"].AsFloat : 0f,
                    centerNode["rz"] != null ? centerNode["rz"].AsFloat : 0f
                );

                // Convert from right-handed meters to left-handed centimeters to accommodate Unity's coordinate convention
                cameraArrayCenter = new Vector3(
                    arrayPosition.x * 100f,           // Scale to cm
                    -arrayPosition.y * 100f,          // Invert Y and scale to cm
                    arrayPosition.z * 100f            // Scale to cm
                );

                cameraArrayRotation = new Vector3(
                    -arrayRotation.x,                 // Invert X rotation
                    arrayRotation.y,                  // Keep Y rotation
                    -arrayRotation.z                  // Invert Z rotation
                );
                Debug.Log("Camera array center: " + cameraArrayCenter);
                Debug.Log("Camera array rotation: " + cameraArrayRotation);
            }

            if (rootNode["camera_array_center_noise"] != null)
            {
                JSONNode noiseNode = rootNode["camera_array_center_noise"];

                // Parse position noise values from JSON (in right-handed meters)
                Vector3 positionNoise = new Vector3(
                    noiseNode["x"] != null ? noiseNode["x"].AsFloat : 0f,
                    noiseNode["y"] != null ? noiseNode["y"].AsFloat : 0f,
                    noiseNode["z"] != null ? noiseNode["z"].AsFloat : 0f
                );

                Vector3 rotationNoise = new Vector3(
                    noiseNode["rx"] != null ? noiseNode["rx"].AsFloat : 0f,
                    noiseNode["ry"] != null ? noiseNode["ry"].AsFloat : 0f,
                    noiseNode["rz"] != null ? noiseNode["rz"].AsFloat : 0f
                );

                // Convert noise values from right-handed meters to left-handed centimeters to accommodate Unity's coordinate convention
                cameraArrayPositionNoise = new Vector3(
                    positionNoise.x * 100f,           // Scale to cm
                    -positionNoise.y * 100f,          // Invert Y and scale to cm
                    positionNoise.z * 100f            // Scale to cm
                );

                cameraArrayRotationNoise = new Vector3(
                    -rotationNoise.x,                 // Invert X rotation
                    rotationNoise.y,                  // Keep Y rotation
                    -rotationNoise.z                  // Invert Z rotation
                );
                Debug.Log("Camera array center noise: " + cameraArrayPositionNoise);
                Debug.Log("Camera array rotation noise: " + cameraArrayRotationNoise);
            }

            if (rootNode["eye_parameters"] != null)
            {
                JSONNode eyeParamsNode = rootNode["eye_parameters"];
                eyeParameters = new EyeParameters();

                if (eyeParamsNode["pupil_size_range"] != null)
                {
                    eyeParameters.pupilSizeRange = new Vector2(
                        eyeParamsNode["pupil_size_range"]["min"].AsFloat,
                        eyeParamsNode["pupil_size_range"]["max"].AsFloat
                    );
                }

                if (eyeParamsNode["iris_size_range"] != null)
                {
                    eyeParameters.irisSizeRange = new Vector2(
                        eyeParamsNode["iris_size_range"]["min"].AsFloat,
                        eyeParamsNode["iris_size_range"]["max"].AsFloat
                    );
                }

                eyeParameters.defaultYaw = eyeParamsNode["default_yaw"] != null ? eyeParamsNode["default_yaw"].AsFloat : 0f;
                eyeParameters.defaultPitch = eyeParamsNode["default_pitch"] != null ? eyeParamsNode["default_pitch"].AsFloat : 0f;
                eyeParameters.yawNoise = eyeParamsNode["yaw_noise"] != null ? eyeParamsNode["yaw_noise"].AsFloat : 30f;
                eyeParameters.pitchNoise = eyeParamsNode["pitch_noise"] != null ? eyeParamsNode["pitch_noise"].AsFloat : 30f;

            }

            JSONArray camerasArray = rootNode["cameras"].AsArray;
            if (camerasArray == null)
            {
                Debug.LogError("No cameras array found in config");
                return;
            }

            Debug.Log($"Found {camerasArray.Count} cameras");

            for (int i = 0; i < camerasArray.Count; i++)
            {
                JSONNode camNode = camerasArray[i];

                string cameraName = camNode["name"] != null ? camNode["name"].Value : $"Camera_{i}";


                GameObject camObj = new GameObject(cameraName);
                Camera newCam = camObj.AddComponent<Camera>();

                JSONNode extrinsics = camNode["extrinsics"];
                Vector3 position = Vector3.zero;
                Vector3 rotation = Vector3.zero;

                if (extrinsics != null)
                {
                    Vector3 extrinsicPosition = new Vector3(
                        extrinsics["x"] != null ? extrinsics["x"].AsFloat : 0f,
                        extrinsics["y"] != null ? extrinsics["y"].AsFloat : 0f,
                        extrinsics["z"] != null ? extrinsics["z"].AsFloat : 0f
                    );

                    Vector3 extrinsicRotation = new Vector3(
                        extrinsics["rx"] != null ? extrinsics["rx"].AsFloat : 0f,
                        extrinsics["ry"] != null ? extrinsics["ry"].AsFloat : 0f,
                        extrinsics["rz"] != null ? extrinsics["rz"].AsFloat : 0f
                    );
                    // Convert from right-handed meters to left-handed centimeters to accommodate Unity's coordinate convention
                    position = new Vector3(
                        extrinsicPosition.x * 100f,          // Scale to cm
                        -extrinsicPosition.y * 100f,         // Invert Y and scale to cm
                        extrinsicPosition.z * 100f           // Scale to cm
                    );
                    rotation = new Vector3(
                        -extrinsicRotation.x,                // Invert X rotation
                        extrinsicRotation.y,                 // Keep Y rotation
                        -extrinsicRotation.z                 // Invert Z rotation
                    );
                }

                camObj.transform.position = position;
                camObj.transform.eulerAngles = rotation;

                JSONNode extrinsicsNoise = camNode["extrinsics_noise"];
                Vector3 extrinsicsPositionNoise = Vector3.zero;
                Vector3 extrinsicsRotationNoise = Vector3.zero;

                if (extrinsicsNoise != null)
                {
                    extrinsicsPositionNoise = new Vector3(
                        extrinsicsNoise["x"] != null ? extrinsicsNoise["x"].AsFloat * 100f : 0f,
                        extrinsicsNoise["y"] != null ? -extrinsicsNoise["y"].AsFloat * 100f : 0f,
                        extrinsicsNoise["z"] != null ? extrinsicsNoise["z"].AsFloat * 100f : 0f
                    );

                    extrinsicsRotationNoise = new Vector3(
                        extrinsicsNoise["rx"] != null ? -extrinsicsNoise["rx"].AsFloat : 0f,
                        extrinsicsNoise["ry"] != null ? extrinsicsNoise["ry"].AsFloat : 0f,
                        extrinsicsNoise["rz"] != null ? -extrinsicsNoise["rz"].AsFloat : 0f
                    );
                }

                cameraExtrinsicsPositionNoise.Add(extrinsicsPositionNoise);
                cameraExtrinsicsRotationNoise.Add(extrinsicsRotationNoise);

                JSONNode intrinsics = camNode["intrinsics"];
                bool isOrthographic = camNode["is_orthographic"] != null && camNode["is_orthographic"].AsBool;

                // Set default values
                CameraIntrinsics camIntrinsics = new CameraIntrinsics
                {
                    fx = 500f,
                    fy = 500f,
                    cx = 320f,
                    cy = 240f,
                    width = 640,
                    height = 480
                };

                if (intrinsics != null)
                {
                    if (intrinsics["fx"] != null) camIntrinsics.fx = intrinsics["fx"].AsFloat;
                    if (intrinsics["fy"] != null) camIntrinsics.fy = intrinsics["fy"].AsFloat;
                    if (intrinsics["cx"] != null) camIntrinsics.cx = intrinsics["cx"].AsFloat;
                    if (intrinsics["cy"] != null) camIntrinsics.cy = intrinsics["cy"].AsFloat;
                    if (intrinsics["w"] != null) camIntrinsics.width = intrinsics["w"].AsInt;
                    if (intrinsics["h"] != null) camIntrinsics.height = intrinsics["h"].AsInt;
                }

                CameraConfig config = new CameraConfig
                {
                    name = cameraName,
                    position = position,
                    rotation = rotation,
                    is_orthographic = isOrthographic,
                    intrinsics = camIntrinsics
                };

                cameraOriginalIntrinsics.Add(new CameraIntrinsics
                {
                    fx = camIntrinsics.fx,
                    fy = camIntrinsics.fy,
                    cx = camIntrinsics.cx,
                    cy = camIntrinsics.cy,
                    width = camIntrinsics.width,
                    height = camIntrinsics.height,
                });

                ConfigureCameraFromIntrinsics(newCam, config);

                newCam.tag = cameraList.Count == 0 ? "MainCamera" : "Untagged";
                newCam.enabled = (cameraList.Count == 0);

                cameraList.Add(newCam);
                cameraOriginalPositions.Add(position);
                cameraOriginalRotations.Add(rotation);
            }

            LoadPointLightsFromConfig(rootNode);

            if (useMotionCenter)
            {
                cameraParent = new GameObject("CameraArrayParent");

                cameraParent.transform.position = cameraArrayCenter;
                cameraParent.transform.eulerAngles = cameraArrayRotation;


                foreach (Camera cam in cameraList)
                {
                    Vector3 currentLocalPosition = cam.transform.localPosition;
                    Quaternion currentLocalRotation = cam.transform.localRotation;

                    Vector3 desiredWorldPosition = cameraParent.transform.TransformPoint(currentLocalPosition);
                    Quaternion desiredWorldRotation = cameraParent.transform.rotation * currentLocalRotation;

                    cam.transform.position = desiredWorldPosition;
                    cam.transform.rotation = desiredWorldRotation;

                    cam.transform.parent = cameraParent.transform;
                }

                for (int i = 0; i < pointLightList.Count; i++)
                {
                    Light light = pointLightList[i];
                    bool isArrayMounted = pointLightArrayMounted[i];

                    if (isArrayMounted)
                    {
                        Vector3 currentLocalPosition = light.transform.localPosition;
                        Quaternion currentLocalRotation = light.transform.localRotation;

                        Vector3 desiredWorldPosition = cameraParent.transform.TransformPoint(currentLocalPosition);
                        Quaternion desiredWorldRotation = cameraParent.transform.rotation * currentLocalRotation;

                        light.transform.position = desiredWorldPosition;
                        light.transform.rotation = desiredWorldRotation;

                        light.transform.parent = cameraParent.transform;
                    }
                }

                if (cameraList.Count > 0)
                {
                    xmlBasePosition = cameraList[0].transform.position;
                    xmlBaseEulerAngles = cameraList[0].transform.eulerAngles;
                }

            }

            if (!useMotionCenter)
            {
                foreach (Camera cam in cameraList)
                {
                    Vector3 currentRotation = cam.transform.eulerAngles;
                    cam.transform.eulerAngles = new Vector3(currentRotation.x, currentRotation.y + 180f, currentRotation.z);
                }

                for (int i = 0; i < cameraOriginalRotations.Count; i++)
                {
                    Vector3 origRot = cameraOriginalRotations[i];
                    cameraOriginalRotations[i] = new Vector3(origRot.x, origRot.y + 180f, origRot.z);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading camera config: {e.Message}\n{e.StackTrace}");
        }
    }

    private void LoadPointLightsFromConfig(JSONNode rootNode)
    {
        pointLightList.Clear();
        pointLightOriginalPositions.Clear();
        pointLightOriginalRotations.Clear();
        pointLightArrayMounted.Clear();

        if (rootNode["lights"] == null)
        {
            Debug.Log("No lights array found in config");
            return;
        }

        JSONArray lightsArray = rootNode["lights"].AsArray;
        if (lightsArray == null)
        {
            Debug.Log("Invalid lights array in config");
            return;
        }

        Debug.Log($"Found {lightsArray.Count} point lights");

        for (int i = 0; i < lightsArray.Count; i++)
        {
            JSONNode lightNode = lightsArray[i];
            string lightName = lightNode["name"] != null ? lightNode["name"].Value : $"PointLight_{i}";
            bool isArrayMounted = lightNode["array_mounted"] != null ? lightNode["array_mounted"].AsInt == 1 : false;

            GameObject lightObj = new GameObject(lightName);
            Light pointLight = lightObj.AddComponent<Light>();

            pointLight.type = LightType.Point;

            JSONNode extrinsics = lightNode["position"];
            Vector3 position = Vector3.zero;
            Vector3 rotation = Vector3.zero;

            if (extrinsics != null)
            {
                Vector3 extrinsicPosition = new Vector3(
                    extrinsics["x"] != null ? extrinsics["x"].AsFloat : 0f,
                    extrinsics["y"] != null ? extrinsics["y"].AsFloat : 0f,
                    extrinsics["z"] != null ? extrinsics["z"].AsFloat : 0f
                );

                Vector3 extrinsicRotation = new Vector3(
                    extrinsics["rx"] != null ? extrinsics["rx"].AsFloat : 0f,
                    extrinsics["ry"] != null ? extrinsics["ry"].AsFloat : 0f,
                    extrinsics["rz"] != null ? extrinsics["rz"].AsFloat : 0f
                );

                // Convert from right-handed meters to left-handed centimeters to accommodate Unity's coordinate convention
                position = new Vector3(
                    extrinsicPosition.x * 100f,          // Scale to cm
                    -extrinsicPosition.y * 100f,         // Invert Y and scale to cm
                    extrinsicPosition.z * 100f           // Scale to cm
                );

                rotation = new Vector3(
                    -extrinsicRotation.x,                // Invert X rotation
                    extrinsicRotation.y,                 // Keep Y rotation
                    -extrinsicRotation.z                 // Invert Z rotation
                );
            }

            lightObj.transform.position = position;
            lightObj.transform.eulerAngles = rotation;

            JSONNode properties = lightNode["properties"];
            if (properties != null)
            {
                if (properties["range"] != null)
                    pointLight.range = properties["range"].AsFloat;
                else
                    pointLight.range = 100f;

                if (properties["intensity"] != null)
                    pointLight.intensity = properties["intensity"].AsFloat;
                else
                    pointLight.intensity = 0.3f;

                JSONNode colorNode = properties["color"];
                if (colorNode != null)
                {
                    float r = colorNode["r"] != null ? colorNode["r"].AsFloat : 1.0f;
                    float g = colorNode["g"] != null ? colorNode["g"].AsFloat : 1.0f;
                    float b = colorNode["b"] != null ? colorNode["b"].AsFloat : 1.0f;
                    pointLight.color = new Color(r, g, b);
                }
                else
                {
                    pointLight.color = Color.white;
                }

                if (properties["shadows"] != null)
                {
                    string shadowsValue = properties["shadows"].Value.ToLower();
                    if (shadowsValue == "hard") pointLight.shadows = LightShadows.Hard;
                    else if (shadowsValue == "soft") pointLight.shadows = LightShadows.Soft;
                    else pointLight.shadows = LightShadows.None;
                }
                else
                {
                    pointLight.shadows = LightShadows.Soft;
                }

                if (properties["shadow_bias"] != null)
                    pointLight.shadowBias = properties["shadow_bias"].AsFloat;
                else
                    pointLight.shadowBias = 0.05f;
            }
            else
            {
                // Set defaults if no properties are defined
                pointLight.range = 100f;
                pointLight.intensity = 0.3f;
                pointLight.color = Color.white;
                pointLight.shadows = LightShadows.Soft;
                pointLight.shadowBias = 0.05f;
            }

            pointLightList.Add(pointLight);
            pointLightOriginalPositions.Add(position);
            pointLightOriginalRotations.Add(rotation);
            pointLightArrayMounted.Add(isArrayMounted);
        }
    }

    private void ConfigureCameraFromIntrinsics(Camera cam, CameraConfig config)
    {
        cam.orthographic = config.is_orthographic;
        cam.nearClipPlane = 0.3f; 
        cam.farClipPlane = 1000f;

        if (config.is_orthographic)
        {
            cam.orthographicSize = config.intrinsics.height / (2 * config.intrinsics.fy);
        }
        else
        {
            float fov = 2 * Mathf.Atan(config.intrinsics.height / (2 * config.intrinsics.fy)) * Mathf.Rad2Deg;
            Debug.Log($"Setting FOV to {fov}");
            cam.fieldOfView = fov;
        }

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.5f, 0.5f, 0.5f);

    }

    private float GetRandomOffset(float range)
    {
        if (range == 0) return 0;

        range = Mathf.Abs(range);

        float offset = (float)SyntheseyesUtils.NextGaussianDouble() * (range / 2f);
        return Mathf.Clamp(offset, -range, range);
    }

    void RandomizeScene()
    {
        if (randomizeSceneCallCount == 0)
        {
            randomizeSceneStartTime = Time.time;
        }
        randomizeSceneCallCount++;

        float randomYaw = Random.Range(-eyeYawNoise, eyeYawNoise) + defaultEyeYaw;
        float randomPitch = Random.Range(-eyePitchNoise, eyePitchNoise) + defaultEyePitch;

        eyeball.SetEyeRotation(randomYaw, randomPitch);

        if (useMotionCenter)
        {
            float offsetX = GetRandomOffset(cameraArrayPositionNoise.x);
            float offsetY = GetRandomOffset(cameraArrayPositionNoise.y);
            float offsetZ = GetRandomOffset(cameraArrayPositionNoise.z);
            float offsetPitch = GetRandomOffset(cameraArrayRotationNoise.x);
            float offsetYaw = GetRandomOffset(cameraArrayRotationNoise.y);
            float offsetRoll = GetRandomOffset(cameraArrayRotationNoise.z);


            cameraParent.transform.position = cameraArrayCenter + new Vector3(offsetX, offsetY, offsetZ);
            cameraParent.transform.eulerAngles = cameraArrayRotation + new Vector3(offsetPitch, offsetYaw, offsetRoll);
        }
        else
        {
            Camera currentCam = cameraList[currentCameraIndex];

            Vector3 originalPosition = cameraOriginalPositions[currentCameraIndex];
            Vector3 originalRotation = cameraOriginalRotations[currentCameraIndex];

            Vector3 positionNoise = cameraExtrinsicsPositionNoise[currentCameraIndex];
            Vector3 rotationNoise = cameraExtrinsicsRotationNoise[currentCameraIndex];

            float offsetX = GetRandomOffset(positionNoise.x);
            float offsetY = GetRandomOffset(positionNoise.y);
            float offsetZ = GetRandomOffset(positionNoise.z);
            float offsetPitch = GetRandomOffset(rotationNoise.x);
            float offsetYaw = GetRandomOffset(rotationNoise.y);
            float offsetRoll = GetRandomOffset(rotationNoise.z);

            currentCam.transform.position = originalPosition + new Vector3(offsetX, offsetY, offsetZ);
            currentCam.transform.eulerAngles = originalRotation + new Vector3(offsetPitch, offsetYaw, offsetRoll);

            for (int i = 0; i < pointLightList.Count; i++)
            {
                if (pointLightArrayMounted[i]) continue;

                Light light = pointLightList[i];
                Vector3 lightOriginalPosition = pointLightOriginalPositions[i];
                Vector3 lightOriginalRotation = pointLightOriginalRotations[i];

                JSONNode lightsArray = JSON.Parse(File.ReadAllText(jsonConfigPath))["lights"].AsArray;
                if (i >= lightsArray.Count) continue;

                JSONNode lightNode = lightsArray[i];
                JSONNode lightExtrinsicsNoise = lightNode["extrinsics_noise"];

                if (lightExtrinsicsNoise != null)
                {
                    Vector3 lightPositionNoise = new Vector3(
                        lightExtrinsicsNoise["x"] != null ? lightExtrinsicsNoise["x"].AsFloat : 0f,
                        lightExtrinsicsNoise["y"] != null ? lightExtrinsicsNoise["y"].AsFloat : 0f,
                        lightExtrinsicsNoise["z"] != null ? lightExtrinsicsNoise["z"].AsFloat : 0f
                    );

                    Vector3 lightRotationNoise = new Vector3(
                        lightExtrinsicsNoise["rx"] != null ? lightExtrinsicsNoise["rx"].AsFloat : 0f,
                        lightExtrinsicsNoise["ry"] != null ? lightExtrinsicsNoise["ry"].AsFloat : 0f,
                        lightExtrinsicsNoise["rz"] != null ? lightExtrinsicsNoise["rz"].AsFloat : 0f
                    );

                    float lightOffsetX = GetRandomOffset(lightPositionNoise.x * 100f); 
                    float lightOffsetY = GetRandomOffset(-lightPositionNoise.y * 100f); 
                    float lightOffsetZ = GetRandomOffset(lightPositionNoise.z * 100f);
                    float lightOffsetPitch = GetRandomOffset(-lightRotationNoise.x);
                    float lightOffsetYaw = GetRandomOffset(lightRotationNoise.y); 
                    float lightOffsetRoll = GetRandomOffset(-lightRotationNoise.z); 

                    light.transform.position = lightOriginalPosition + new Vector3(lightOffsetX, lightOffsetY, lightOffsetZ);
                    light.transform.eulerAngles = lightOriginalRotation + new Vector3(lightOffsetPitch, lightOffsetYaw, lightOffsetRoll);
                }
            }
        }
    }

    public void UpdateOutputPath(string newPath)
    {
        if (!string.IsNullOrEmpty(newPath))
        {
            Debug.Log($"Setting output path to: {newPath}");
            // Create the full output path
            string outputFolder = Path.Combine(newPath, "imgs");
            EnsureDirectoryExists(outputFolder);
            Debug.Log($"Created output directory at: {outputFolder}");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SwitchCamera(+1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SwitchCamera(-1);
        }

        if (isSavingData || Input.GetKey("c"))
        {
            RandomizeScene();
        }

        if (Input.GetKeyDown("p"))
        {
            ToggleOutputPreview();
        }

        if (isSavingData || Input.GetKey("r"))
        {
            eyeRegion.RandomizeAppearance();
            eyeball.RandomizeEyeball();
        }

        if (isSavingData || Input.GetKey("l"))
        {
            lightingController.RandomizeLighting();
        }

        eyeRegion.UpdateEyeRegion();
        eyeRegionSubdiv.Subdivide();
        eyeWetness.UpdateEyeWetness();
        eyeWetnessSubdiv.Subdivide();
        foreach (DeformEyeLashes eyeLash in eyeLashes)
            eyeLash.UpdateLashes();

        if (isSavingData || Input.GetKey("s"))
        {
            StartCoroutine(saveFrame());
        }

        if (Input.GetKeyUp("h"))
            GameObject.Find("GUI Canvas").GetComponent<Canvas>().enabled = !GameObject.Find("GUI Canvas").GetComponent<Canvas>().enabled;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Escape key pressed - quitting application");
            Application.Quit();
        }
    }

    private void SwitchCamera(int direction) {
        cameraList[currentCameraIndex].enabled = false;
        
        currentCameraIndex = (currentCameraIndex + direction) % cameraList.Count;
        if (currentCameraIndex < 0) currentCameraIndex = cameraList.Count - 1;
        
        cameraList[currentCameraIndex].enabled = true;
        cameraList[currentCameraIndex].tag = "MainCamera";
    }

    public void ToggleOutputPreview() {
        visualizationManager.ToggleVisualization();
    }


    private Color parseColor(JSONNode jN)
    {
        return new Color(jN[0].AsFloat, jN[1].AsFloat, jN[2].AsFloat, 1.0f);
    }

    private Vector3 parseVec(JSONNode jN)
    {
        return new Vector3(jN[0].AsFloat, jN[1].AsFloat, jN[2].AsFloat);
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"Created directory: {path}");
        }
    }

    private IEnumerator saveFrame()
    {
        if (framesSaved >= maxSamplesToSave)
        {
            Debug.Log($"Maximum number of samples ({maxSamplesToSave}) reached. Stopping data collection.");
            isSavingData = false;
            GameObject.Find("GUI Canvas").GetComponent<Canvas>().enabled = true;
            yield break;
        }

        framesSaved++;

        //if (!headlessMode)
        //{
        //    yield return new WaitForEndOfFrame();
        //}

        int originalCameraIndex = currentCameraIndex;

        string outputPath = "imgs";
        if (File.Exists(jsonConfigPath))
        {
            string jsonData = File.ReadAllText(jsonConfigPath);
            JSONNode rootNode = JSON.Parse(jsonData);
            if (rootNode["outputPath"] != null)
            {
                string configOutputPath = rootNode["outputPath"];
                string configOutputFolder = rootNode["outputFolder"] != null ? rootNode["outputFolder"] : "EER_eye_data";
                outputPath = Path.Combine(configOutputPath, configOutputFolder);
                EnsureDirectoryExists(outputPath);
            }
        }

        for (int i = 0; i < cameraList.Count; i++)
        {
            Camera cam = cameraList[i];
            string cameraName = cam.gameObject.name;

            Debug.Log(cameraName + " frame: " + framesSaved);

            //if (!headlessMode)
            //{
            //    SwitchCamera(i - currentCameraIndex);
            //    yield return new WaitForEndOfFrame();
            //}

            SwitchCamera(i - currentCameraIndex);

            RenderTexture renderTexture = RenderTexture.GetTemporary(
                cam.pixelWidth,
                cam.pixelHeight,
                24,
                RenderTextureFormat.ARGB32);

            RenderTexture originalRenderTexture = cam.targetTexture;
            cam.targetTexture = renderTexture;
            cam.Render();

            Texture2D tex = new Texture2D(cam.pixelWidth, cam.pixelHeight, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, cam.pixelWidth, cam.pixelHeight), 0, 0);
            tex.Apply();

            cam.targetTexture = originalRenderTexture;
            RenderTexture.active = null;

            byte[] imgBytes = tex.EncodeToJPG();
            string fileName = Path.Combine(outputPath, string.Format("{0}_{1}.jpg", framesSaved, cameraName));
            File.WriteAllBytes(fileName, imgBytes);

            RenderTexture.ReleaseTemporary(renderTexture);
            Object.Destroy(tex);
        }

        saveAllCamerasDetails(framesSaved, outputPath);

        //if (!headlessMode)
        //{
        //    SwitchCamera(originalCameraIndex - currentCameraIndex);
        //}
        SwitchCamera(originalCameraIndex - currentCameraIndex);
    }

    public void ResetFrameCounter()
    {
        framesSaved = 0;
        Debug.Log("Frame counter reset. Ready to collect new samples.");
    }

    public void ReloadConfiguration(string configPath = null)
    {
        string path = configPath != null ? configPath : jsonConfigPath;
        Debug.Log($"Reloading configuration from: {path}");

        CleanupCurrentScene();

        ResetFrameCounter();

        if (File.Exists(path))
        {
            LoadCamerasFromConfig(path);

            if (eyeParameters != null)
            {
                eyeball.SetPupilSizeRange(eyeParameters.pupilSizeRange);
                eyeball.SetIrisSizeRange(eyeParameters.irisSizeRange);
            }

            RandomizeScene();

            ToggleOutputPreview();
            ToggleOutputPreview();

            Debug.Log("Configuration reloaded successfully");
        }
        else
        {
            Debug.LogError($"Config file not found at: {path}");
        }
    }

    private void CleanupCurrentScene()
    {
        foreach (Camera cam in cameraList)
        {
            if (cam != null)
            {
                Destroy(cam.gameObject);
            }
        }
        cameraList.Clear();
        cameraOriginalPositions.Clear();
        cameraOriginalRotations.Clear();
        cameraExtrinsicsPositionNoise.Clear();
        cameraExtrinsicsRotationNoise.Clear();
        cameraOriginalIntrinsics.Clear();

        foreach (Light light in pointLightList)
        {
            if (light != null)
            {
                Destroy(light.gameObject);
            }
        }
        pointLightList.Clear();
        pointLightOriginalPositions.Clear();
        pointLightOriginalRotations.Clear();
        pointLightArrayMounted.Clear();

        if (cameraParent != null)
        {
            Destroy(cameraParent);
            cameraParent = null;
        }

        currentCameraIndex = 0;
    }


    private void saveAllCamerasDetails(int frame, string outputPath = "imgs")
    {
        JSONNode rootNode = new JSONClass();

        rootNode.Add("eye_details", eyeball.GetEyeballDetails());
        rootNode.Add("lighting_details", lightingController.GetLightingDetails());
        rootNode.Add("eye_region_details", eyeRegion.GetEyeRegionDetails());

        JSONNode camerasNode = new JSONClass();
        rootNode.Add("cameras", camerasNode);

        Mesh meshEyeRegion = eyeRegion.transform.GetComponent<MeshFilter>().mesh;
        Mesh meshEyeBall = eyeball.transform.GetComponent<MeshFilter>().mesh;

        for (int i = 0; i < cameraList.Count; i++)
        {
            Camera cam = cameraList[i];
            string cameraName = cam.gameObject.name;
            JSONNode cameraNode = new JSONClass();
            camerasNode.Add(cameraName, cameraNode);

            cameraNode.Add("head_pose", cam.transform.rotation.eulerAngles.ToString("F4"));
            cameraNode.Add("camera_pose", eyeball.GetCameratoEyeCenterPose());

            JSONArray listInteriorMargin2D = new JSONArray();
            cameraNode.Add("interior_margin_2d", listInteriorMargin2D);
            foreach (var idx in EyeRegionTopology.interior_margin_idxs)
            {
                Vector3 v_3d = eyeRegion.transform.localToWorldMatrix * meshEyeRegion.vertices[idx];
                listInteriorMargin2D.Add(new JSONData(cam.WorldToScreenPoint(v_3d).ToString("F4")));
            }

            JSONArray listCaruncle2D = new JSONArray();
            cameraNode.Add("caruncle_2d", listCaruncle2D);
            foreach (var idx in EyeRegionTopology.caruncle_idxs)
            {
                Vector3 v_3d = eyeRegion.transform.localToWorldMatrix * meshEyeRegion.vertices[idx];
                listCaruncle2D.Add(new JSONData(cam.WorldToScreenPoint(v_3d).ToString("F4")));
            }

            JSONArray listIris2D = new JSONArray();
            cameraNode.Add("iris_2d", listIris2D);
            foreach (var idx in EyeRegionTopology.iris_idxs)
            {
                Vector3 v_3d = eyeball.transform.localToWorldMatrix * meshEyeBall.vertices[idx];
                listIris2D.Add(new JSONData(cam.WorldToScreenPoint(v_3d).ToString("F4")));
            }

            cameraNode.Add("ground_truth", eyeball.GetGazeVector(cam));
        }

        string filePath = Path.Combine(outputPath, string.Format("{0}.json", frame));
        File.WriteAllText(filePath, rootNode.ToJSON(0));
    }
}
