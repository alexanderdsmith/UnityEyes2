using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using SimpleJSON;
using System.Collections.Generic;
using System.IO;               // Added for file I/O
using System.Xml.Serialization; // Added for XML deserialization

public class SynthesEyesServer : MonoBehaviour
{

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

    // should you save the data or not
    public bool isSavingData = false;

    // frame index for saving
    int framesSaved = 0;

    // NEW: Public field to hold the path to the camera XML configuration file.
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

        // NEW: Load the camera settings from XML if a valid file is specified.
        if (!string.IsNullOrEmpty(xmlCameraFilePath) && File.Exists(xmlCameraFilePath))
        {
            LoadCameraFromFile(xmlCameraFilePath);
        }
    }

    void RandomizeScene()
    {
        // Randomize eye rotation
        eyeball.SetEyeRotation(Random.Range(-eyeYawNoise, eyeYawNoise) + defaultEyeYaw,
                                Random.Range(-eyePitchNoise, eyePitchNoise) + defaultEyePitch);

        // Only randomize camera transform if the XML file is absent.
        if (string.IsNullOrEmpty(xmlCameraFilePath) || !File.Exists(xmlCameraFilePath))
        {
            Camera.main.transform.position = SyntheseyesUtils.RandomVec(
                defaultCameraPitch - cameraPitchNoise, defaultCameraPitch + cameraPitchNoise,
                defaultCameraYaw - cameraYawNoise, defaultCameraYaw + cameraYawNoise) * 10f;
            Camera.main.transform.LookAt(Vector3.zero);
        }
    }

    void Update()
    {
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

        File.WriteAllText(string.Format("imgs/{0}.json", frame), rootNode.ToJSON(0));
    }

    // NEW: Method to load camera settings (both intrinsic and extrinsic) from an XML file.
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

        Debug.Log($"Position: X={xmlCam.Position.x}, Y={xmlCam.Position.y}, Z={xmlCam.Position.z}");
        Debug.Log($"Rotation (Pitch, Yaw, Roll): Pitch={xmlCam.Pitch}, Yaw={xmlCam.Yaw}, Roll={xmlCam.Roll}");

        // Log: Finished loading camera settings
        Debug.Log("Finished applying camera settings from XML.");
    }

}
