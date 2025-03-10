using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using SimpleJSON;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

// Add JSON serializable classes at the top
[System.Serializable]
public class CameraIntrinsics{
    public float fx;
    public float fy;
    public float cx;
    public float cy;
    public int width;
    public int height;
    public float sensor_width;
    public float sensor_height;
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



public class SynthesEyesServer : MonoBehaviour{
    public GameObject lightDirectionalObj;
    public GameObject eyeballObj;
    public GameObject eyeRegionObj;
    public GameObject eyeRegionSubdivObj;
    public GameObject eyeWetnessObj;
    public GameObject eyeWetnessSubdivObj;
    public GameObject eyeLashesObj;

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

    // Add these public fields somewhere near the top to define your offset ranges:
    public float randomXRange = 0.5f;
    public float randomYRange = 0.5f;
    public float randomZRange = 0.5f;
    public float randomPitchRange = Mathf.Deg2Rad * 3;
    public float randomYawRange = Mathf.Deg2Rad * 3;
    public float randomRollRange = Mathf.Deg2Rad * 3;

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

    private List<CameraIntrinsics> cameraOriginalIntrinsics = new List<CameraIntrinsics>();

    // Headless Mode
    private bool headlessMode = false;

    // should you save the data or not
    public bool isSavingData = false;

	private Mesh eyemesh;

    // frame index for saving
    int framesSaved = 0;

    // Store the camera's original transform from XML
    private Vector3 xmlBasePosition;
    private Vector3 xmlBaseEulerAngles;
    public string xmlCameraFilePath = "camera.xml";

    void Start()
    {
        // Initialise SynthesEyes Objects
        eyeRegion = eyeRegionObj.GetComponent<EyeRegionController>();
        eyeball = eyeballObj.GetComponent<EyeballController>();
        eyeRegionSubdiv = eyeRegionSubdivObj.GetComponent<SubdivMesh>();
        eyeWetness = eyeWetnessObj.GetComponent<EyeWetnessController>();
        eyeWetnessSubdiv = eyeWetnessSubdivObj.GetComponent<SubdivMesh>();
        eyeLashes = eyeLashesObj.GetComponentsInChildren<DeformEyeLashes>(true);

        lightingController = GameObject.Find("lighting_controller").GetComponent<LightingController>();

        // Load cameras from JSON oe XML
        if (File.Exists(jsonConfigPath))
        {
            Debug.Log($"-------------------- READ");

            LoadCamerasFromConfig(jsonConfigPath);
        }
        else if (File.Exists(xmlCameraFilePath))
        {
            Debug.LogWarning("Using legacy XML config");
            LoadCameraFromFile(xmlCameraFilePath);
        }
    }

    private void LoadCamerasFromConfig(string configPath)
    {
        cameraList.Clear();

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
                useMotionCenter = rootNode["motion_center"].AsBool;
                Debug.Log($"Motion center mode: {useMotionCenter}");
            }

            if (rootNode["camera_array_center"] != null)
            {
                JSONNode centerNode = rootNode["camera_array_center"];
                cameraArrayCenter = new Vector3(
                    centerNode["x"] != null ? centerNode["x"].AsFloat : 0f,
                    centerNode["y"] != null ? centerNode["y"].AsFloat : 0f,
                    centerNode["z"] != null ? centerNode["z"].AsFloat : 0f
                );
                cameraArrayRotation = new Vector3(
                    centerNode["rx"] != null ? centerNode["rx"].AsFloat : 0f,
                    centerNode["ry"] != null ? centerNode["ry"].AsFloat : 0f,
                    centerNode["rz"] != null ? centerNode["rz"].AsFloat : 0f
                );
            }

            if (rootNode["camera_array_noise"] != null)
            {
                JSONNode noiseNode = rootNode["camera_array_noise"];
                cameraArrayPositionNoise = new Vector3(
                    noiseNode["x"] != null ? noiseNode["x"].AsFloat : 0f,
                    noiseNode["y"] != null ? noiseNode["y"].AsFloat : 0f,
                    noiseNode["z"] != null ? noiseNode["z"].AsFloat : 0f
                );
                cameraArrayRotationNoise = new Vector3(
                    noiseNode["rx"] != null ? noiseNode["rx"].AsFloat : 0f,
                    noiseNode["ry"] != null ? noiseNode["ry"].AsFloat : 0f,
                    noiseNode["rz"] != null ? noiseNode["rz"].AsFloat : 0f
                );
            }

            if (rootNode["headless_mode"] != null)
            {
                headlessMode = rootNode["headless_mode"].AsBool;
                Debug.Log($"Headless mode: {headlessMode}");
            }

            JSONArray camerasArray = rootNode["cameras"].AsArray;
            if (camerasArray == null)
            {
                Debug.LogError("No cameras array found in config");
                return;
            }

            Debug.Log($"Found {camerasArray.Count} cameras");


            for (int i =0; i < camerasArray.Count; i++)
            {
                JSONNode camNode = camerasArray[i];

                string cameraName= camNode["name"] != null ? camNode["name"].Value : $"Camera_{i}";
                Debug.Log($"Processing camera: {cameraName}");


                GameObject camObj = new GameObject(cameraName);
                Camera newCam = camObj.AddComponent<Camera>();

                JSONNode extrinsics = camNode["extrinsics"];
                Vector3 position= Vector3.zero;
                Vector3 rotation = Vector3.zero;

                if (extrinsics != null)
                {
                    position = new Vector3(
                        extrinsics["x"] != null? extrinsics["x"].AsFloat : 0f,
                        extrinsics["y"] != null? extrinsics["y"].AsFloat : 0f,
                        extrinsics["z"] != null? extrinsics["z"].AsFloat : 0f
                    );

                    rotation = new Vector3(
                        extrinsics["rx"] != null ? extrinsics["rx"].AsFloat : 0f,
                        extrinsics["ry"] != null ? extrinsics["ry"].AsFloat : 0f,
                        extrinsics["rz"] != null ? extrinsics["rz"].AsFloat : 0f
                    );
                }

                camObj.transform.position = position;
                camObj.transform.eulerAngles = rotation;

                JSONNode intrinsics = camNode["intrinsics"];
                bool isOrthographic = camNode["is_orthographic"] != null && camNode["is_orthographic"].AsBool;

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

                    if (intrinsics["sensor_width"] != null) camIntrinsics.sensor_width = intrinsics["sensor_width"].AsFloat;
                    else camIntrinsics.sensor_width = 36f;

                    if (intrinsics["sensor_height"] != null) camIntrinsics.sensor_height = intrinsics["sensor_height"].AsFloat;
                    else camIntrinsics.sensor_height = 24f;
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
                    sensor_width = camIntrinsics.sensor_width,
                    sensor_height = camIntrinsics.sensor_height
                });

                ConfigureCameraFromIntrinsics(newCam, config);

                newCam.tag = cameraList.Count == 0 ? "MainCamera" : "Untagged";
                newCam.enabled = (cameraList.Count == 0);

                cameraList.Add(newCam);
                cameraOriginalPositions.Add(position);
                cameraOriginalRotations.Add(rotation);

                Debug.Log($"Added camera {cameraName} to list. Position: {position}, Rotation: {rotation}");
            }

            if (useMotionCenter)
            {
                cameraParent = new GameObject("CameraArrayParent");
                cameraParent.transform.position = cameraArrayCenter;
                cameraParent.transform.eulerAngles = cameraArrayRotation;

                foreach (Camera cam in cameraList)
                {
                    Vector3 relativePosition = cam.transform.position - cameraArrayCenter;
                    Vector3 relativeRotation = cam.transform.eulerAngles - cameraArrayRotation;

                    cam.transform.parent = cameraParent.transform;
                    cam.transform.localPosition = relativePosition;
                    cam.transform.localEulerAngles = relativeRotation;
                }
            }


            if (cameraList.Count > 0)
            {
                xmlBasePosition = cameraList[0].transform.position;
                xmlBaseEulerAngles = cameraList[0].transform.eulerAngles;
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading camera config: {e.Message}\n{e.StackTrace}");
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
            cam.fieldOfView = fov;

            cam.usePhysicalProperties = true;
            cam.sensorSize = new Vector2(
                config.intrinsics.width / config.intrinsics.fx * config.intrinsics.sensor_width,
                config.intrinsics.height / config.intrinsics.fy * config.intrinsics.sensor_height
            );
        }
    }

    private float GetRandomOffset(float range)
    {
        if (range <= 0) return 0;

        float offset = (float)SyntheseyesUtils.NextGaussianDouble() * (range / 2f);
        return Mathf.Clamp(offset, -range, range);
    }

    void RandomizeScene()
    {
        // Record the start time on the first call
        if (randomizeSceneCallCount == 0)
        {
            randomizeSceneStartTime = Time.time;
        }
        randomizeSceneCallCount++;


        // After 100 calls, log the elapsed time
        if (randomizeSceneCallCount == 1000)
        {
            float elapsedTime = Time.time - randomizeSceneStartTime;
            Debug.Log($"RandomizeScene was called 100 times. Elapsed time: {elapsedTime:F2} seconds");
        }
        // Randomize eye rotation
        eyeball.SetEyeRotation(Random.Range(-eyeYawNoise, eyeYawNoise) + defaultEyeYaw,
                                 Random.Range(-eyePitchNoise, eyePitchNoise) + defaultEyePitch);

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

            JSONNode camerasArray = JSON.Parse(File.ReadAllText(jsonConfigPath))["cameras"].AsArray;
            JSONNode camNode = camerasArray[currentCameraIndex];
            JSONNode extrinsicsNoise = camNode["extrinsics_noise"];
            JSONNode intrinsicsNoise = camNode["intrinsics_noise"];

            Vector3 positionNoise = new Vector3(
                extrinsicsNoise["x"] != null ? extrinsicsNoise["x"].AsFloat : 0f,
                extrinsicsNoise["y"] != null ? extrinsicsNoise["y"].AsFloat : 0f,
                extrinsicsNoise["z"] != null ? extrinsicsNoise["z"].AsFloat : 0f
            );

            Vector3 rotationNoise = new Vector3(
                extrinsicsNoise["rx"] != null ? extrinsicsNoise["rx"].AsFloat : 0f,
                extrinsicsNoise["ry"] != null ? extrinsicsNoise["ry"].AsFloat : 0f,
                extrinsicsNoise["rz"] != null ? extrinsicsNoise["rz"].AsFloat : 0f
            );

            float offsetX = GetRandomOffset(positionNoise.x);
            float offsetY = GetRandomOffset(positionNoise.y);
            float offsetZ = GetRandomOffset(positionNoise.z);
            float offsetPitch = GetRandomOffset(rotationNoise.x);
            float offsetYaw = GetRandomOffset(rotationNoise.y);
            float offsetRoll = GetRandomOffset(rotationNoise.z);

            currentCam.transform.position = originalPosition + new Vector3(offsetX, offsetY, offsetZ);
            currentCam.transform.eulerAngles = originalRotation + new Vector3(offsetPitch, offsetYaw, offsetRoll);
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
    }

    private void SwitchCamera(int direction) {
        cameraList[currentCameraIndex].enabled = false;
        
        currentCameraIndex = (currentCameraIndex + direction) % cameraList.Count;
        if (currentCameraIndex < 0) currentCameraIndex = cameraList.Count - 1;
        
        cameraList[currentCameraIndex].enabled = true;
        cameraList[currentCameraIndex].tag = "MainCamera";
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
        // Check if we've reached the maximum number of samples
        if (framesSaved >= maxSamplesToSave)
        {
            Debug.Log($"Maximum number of samples ({maxSamplesToSave}) reached. Stopping data collection.");
            isSavingData = false;
            GameObject.Find("GUI Canvas").GetComponent<Canvas>().enabled = true;
            yield break;
        }

        framesSaved++;

        EnsureDirectoryExists("imgs");

        if (!headlessMode)
        {
            yield return new WaitForEndOfFrame();
        }

        int originalCameraIndex = currentCameraIndex;

        for (int i = 0; i < cameraList.Count; i++)
        {
            Camera cam = cameraList[i];
            string cameraName = cam.gameObject.name;

            if (!headlessMode)
            {
                SwitchCamera(i - currentCameraIndex);
                yield return new WaitForEndOfFrame();
            }

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

            // Save the image with the appropriate name
            byte[] imgBytes = tex.EncodeToJPG();
            string fileName = string.Format("imgs/{0}_{1}.jpg", framesSaved, cameraName);
            File.WriteAllBytes(fileName, imgBytes);

            // Save details for this camera
            // saveDetails(framesSaved, i);

            RenderTexture.ReleaseTemporary(renderTexture);
            Object.Destroy(tex);
        }

        // Restore the original camera if not in headless mode
        if (!headlessMode)
        {
            // Restore the original active camera
            SwitchCamera(originalCameraIndex - currentCameraIndex);
        }
    }

    public void ResetFrameCounter()
    {
        framesSaved = 0;
        Debug.Log("Frame counter reset. Ready to collect new samples.");
    }

    private void saveDetails(int frame)
    {
        Camera activeCam = cameraList[currentCameraIndex];

        Mesh meshEyeRegion = eyeRegion.transform.GetComponent<MeshFilter>().mesh;
        Mesh meshEyeBall = eyeball.transform.GetComponent<MeshFilter>().mesh;

        JSONNode rootNode = new JSONClass();

        JSONArray listInteriorMargin2D = new JSONArray();
        rootNode.Add("interior_margin_2d", listInteriorMargin2D);
        foreach (var idx in EyeRegionTopology.interior_margin_idxs)
        {
            Vector3 v_3d = eyeRegion.transform.localToWorldMatrix * meshEyeRegion.vertices[idx];
            listInteriorMargin2D.Add(new JSONData(Camera.main.WorldToScreenPoint(v_3d).ToString("F4")));
        }

        JSONArray listCaruncle2D = new JSONArray();
        rootNode.Add("caruncle_2d", listCaruncle2D);
        foreach (var idx in EyeRegionTopology.caruncle_idxs)
        {
            Vector3 v_3d = eyeRegion.transform.localToWorldMatrix * meshEyeRegion.vertices[idx];
            listCaruncle2D.Add(new JSONData(Camera.main.WorldToScreenPoint(v_3d).ToString("F4")));
        }


        JSONArray listIris2D = new JSONArray();
        rootNode.Add("iris_2d", listIris2D);
        foreach (var idx in EyeRegionTopology.iris_idxs)
        {
            Vector3 v_3d = eyeball.transform.localToWorldMatrix * meshEyeBall.vertices[idx];
            listIris2D.Add(new JSONData(Camera.main.WorldToScreenPoint(v_3d).ToString("F4")));
        }

        rootNode.Add("eye_details", eyeball.GetEyeballDetails());
        rootNode.Add("lighting_details", lightingController.GetLightingDetails());
        rootNode.Add("eye_region_details", eyeRegion.GetEyeRegionDetails());
        rootNode.Add("head_pose", (Camera.main.transform.rotation.eulerAngles.ToString("F4")));
        
        // New saving method for optical axis and 3D position in space
        rootNode.Add("ground_truth", (eyeball.GetGazeVector()));
        rootNode.Add("camera_pose", (eyeball.GetCameratoEyeCenterPose()));

        File.WriteAllText(string.Format("imgs/{0}.json", frame), rootNode.ToJSON(0));
    }


    // Method to load camera settings (both intrinsic and extrinsic) from an XML file.
    private void LoadCameraFromFile(string file)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(XMLCamera));
        FileStream stream = new FileStream(file, FileMode.Open);
        XMLCamera xmlCam = serializer.Deserialize(stream) as XMLCamera;
        stream.Close();

        Debug.Log("Loading camera settings from XML file: " + file);

        if (xmlCam.Resolution.x > Screen.width || xmlCam.Resolution.y > Screen.height)
        {
            Debug.Log($"Resolution exceeds screen dimensions. Width: {xmlCam.Resolution.x}, Height: {xmlCam.Resolution.y}");
        }
        else
        {
            Screen.SetResolution((int)xmlCam.Resolution.x, (int)xmlCam.Resolution.y, FullScreenMode.FullScreenWindow);
            Debug.Log($"Resolution set to Width: {xmlCam.Resolution.x}, Height: {xmlCam.Resolution.y}");
        }

        Camera.main.nearClipPlane = xmlCam.Near;
        Camera.main.farClipPlane = xmlCam.Far;
        Camera.main.orthographicSize = xmlCam.OrthographicSize;
        Camera.main.orthographic = xmlCam.IsOrthographic;

        Debug.Log($"Near Clip Plane: {xmlCam.Near}");
        Debug.Log($"Far Clip Plane: {xmlCam.Far}");
        Debug.Log($"Orthographic Size: {xmlCam.OrthographicSize}");
        Debug.Log($"Is Orthographic: {xmlCam.IsOrthographic}");

        if (!Camera.main.orthographic)
        {
            Camera.main.fieldOfView = xmlCam.FieldOfView;
            Camera.main.usePhysicalProperties = xmlCam.IsPhysicalCamera;

            Debug.Log($"Field of View: {xmlCam.FieldOfView}");
            Debug.Log($"Is Physical Camera: {xmlCam.IsPhysicalCamera}");

            if (Camera.main.usePhysicalProperties)
            {
                Camera.main.focalLength = xmlCam.Focal;
                Camera.main.sensorSize = xmlCam.SensorSize;
                Camera.main.lensShift = xmlCam.LensShift;
                Camera.main.gateFit = xmlCam.GateFit;

                Debug.Log($"Focal Length: {xmlCam.Focal}");
                Debug.Log($"Sensor Size: X={xmlCam.SensorSize.x}, Y={xmlCam.SensorSize.y}");
                Debug.Log($"Lens Shift: X={xmlCam.LensShift.x}, Y={xmlCam.LensShift.y}");
                Debug.Log($"Gate Fit Mode: {xmlCam.GateFit}");
            }
        }

        if (xmlCam.UseProjectionMatrix)
        {
            Camera.main.projectionMatrix = xmlCam.ProjectionMatrix;
            Debug.Log("Custom Projection Matrix Applied");  
            Debug.Log(xmlCam.ProjectionMatrix.ToString());
        }


        Camera.main.transform.position = xmlCam.Position;
        Camera.main.transform.rotation = Quaternion.Euler(xmlCam.Pitch, xmlCam.Yaw, xmlCam.Roll);

        xmlBasePosition = xmlCam.Position;
        xmlBaseEulerAngles = new Vector3(xmlCam.Pitch, xmlCam.Yaw, xmlCam.Roll);

        Debug.Log($"Position: X={xmlCam.Position.x}, Y={xmlCam.Position.y}, Z={xmlCam.Position.z}");
        Debug.Log($"Rotation (Pitch, Yaw, Roll): Pitch={xmlCam.Pitch}, Yaw={xmlCam.Yaw}, Roll={xmlCam.Roll}");

        Debug.Log("Finished applying camera settings from XML.");
    }

}
