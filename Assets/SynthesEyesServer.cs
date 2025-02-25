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
            LoadCamerasFromConfig(jsonConfigPath);
        }
        else if (File.Exists(xmlCameraFilePath))
        {
            Debug.LogWarning("Using legacy XML config");
            LoadCameraFromFile(xmlCameraFilePath);
        }
    }

    // Camera loading implementation
    private void LoadCamerasFromConfig(string configPath)
    {
        cameraList.Clear();

        string jsonData = File.ReadAllText(configPath);
        CameraConfiguration config = JsonUtility.FromJson<CameraConfiguration>(jsonData);

        foreach (CameraConfig camConfig in config.cameras)
        {
            GameObject camObj = new GameObject(camConfig.name);
            Camera newCam = camObj.AddComponent<Camera>();

            camObj.transform.position = camConfig.position;
            camObj.transform.eulerAngles = camConfig.rotation;

            ConfigureCameraFromIntrinsics(newCam, camConfig);

            newCam.tag = cameraList.Count == 0 ? "MainCamera" : "Untagged";
            newCam.enabled = (cameraList.Count == 0);

            cameraList.Add(newCam);
        }
    }

    // Camera configuration logic
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
                config.intrinsics.width / config.intrinsics.fx * 36f,
                config.intrinsics.height / config.intrinsics.fy * 24f
            );
        }
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


        // Sample offsets using NextGaussianDouble(), then clamp to ± random*Range
        float offsetX = (float)SyntheseyesUtils.NextGaussianDouble() * (randomXRange / 2f);
        offsetX = Mathf.Clamp(offsetX, -randomXRange, randomXRange);

        float offsetY = (float)SyntheseyesUtils.NextGaussianDouble() * (randomYRange / 2f);
        offsetY = Mathf.Clamp(offsetY, -randomYRange, randomYRange);

        float offsetZ = (float)SyntheseyesUtils.NextGaussianDouble() * (randomZRange / 2f);
        offsetZ = Mathf.Clamp(offsetZ, -randomZRange, randomZRange);

        float offsetPitch = (float)SyntheseyesUtils.NextGaussianDouble() * (randomPitchRange / 2f);
        offsetPitch = Mathf.Clamp(offsetPitch, -randomPitchRange, randomPitchRange);

        float offsetYaw = (float)SyntheseyesUtils.NextGaussianDouble() * (randomYawRange / 2f);
        offsetYaw = Mathf.Clamp(offsetYaw, -randomYawRange, randomYawRange);

        float offsetRoll = (float)SyntheseyesUtils.NextGaussianDouble() * (randomRollRange / 2f);
        offsetRoll = Mathf.Clamp(offsetRoll, -randomRollRange, randomRollRange);

        // Get current active camera
        Camera currentCam = cameraList[currentCameraIndex];

        // Apply offsets to active camera
        currentCam.transform.position = xmlBasePosition + new Vector3(offsetX, offsetY, offsetZ);
        currentCam.transform.eulerAngles = xmlBaseEulerAngles + new Vector3(offsetPitch, offsetYaw, offsetRoll);
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

    private IEnumerator saveFrame()
    {
        framesSaved++;
        // Wait until the end of frame so that the screen buffer is ready
        yield return new WaitForEndOfFrame();

        int width = Screen.width;
        int height = Screen.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] imgBytes = tex.EncodeToJPG();
        File.WriteAllBytes(string.Format("imgs/{0}.jpg", framesSaved), imgBytes);

        saveDetails(framesSaved);
        Object.Destroy(tex);
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

        // Log: Start loading camera settings
        Debug.Log("Loading camera settings from XML file: " + file);

        // Apply resolution settings
        if (xmlCam.Resolution.x > Screen.width || xmlCam.Resolution.y > Screen.height)
        {
            // Optionally, you could set up a RenderTexture here.
            Debug.Log($"Resolution exceeds screen dimensions. Width: {xmlCam.Resolution.x}, Height: {xmlCam.Resolution.y}");
        }
        else
        {
            Screen.SetResolution((int)xmlCam.Resolution.x, (int)xmlCam.Resolution.y, FullScreenMode.FullScreenWindow);
            Debug.Log($"Resolution set to Width: {xmlCam.Resolution.x}, Height: {xmlCam.Resolution.y}");
        }

        // Set intrinsic camera parameters
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

        // Apply extrinsic parameters
        Camera.main.transform.position = xmlCam.Position;
        Camera.main.transform.rotation = Quaternion.Euler(xmlCam.Pitch, xmlCam.Yaw, xmlCam.Roll);

        // Save the base transform for later randomization
        xmlBasePosition = xmlCam.Position;
        xmlBaseEulerAngles = new Vector3(xmlCam.Pitch, xmlCam.Yaw, xmlCam.Roll);

        Debug.Log($"Position: X={xmlCam.Position.x}, Y={xmlCam.Position.y}, Z={xmlCam.Position.z}");
        Debug.Log($"Rotation (Pitch, Yaw, Roll): Pitch={xmlCam.Pitch}, Yaw={xmlCam.Yaw}, Roll={xmlCam.Roll}");

        // Log: Finished loading camera settings
        Debug.Log("Finished applying camera settings from XML.");
    }

}
