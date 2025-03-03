using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using System;

public class EyeRegionController : MonoBehaviour {

	public GameObject eyeball;

	public float skinThickness = 0.25f;
	public bool doShrinkWrap = true;
	
	private EyeRegionPCA pca;

	public bool randomizeAppearance = true;
	Vector3[] randomMeshFromPca;
	Vector3[] offsets = new Vector3[872];

	Material faceMaterial;      // Skin shader material

	List<Texture2D> colorTexs = new List<Texture2D>();		// Eye region color textures
	List<Texture2D> colorLdTexs = new List<Texture2D>(); 	// Look-down version of eye-region texs
	List<Texture2D> bumpTexs = new List<Texture2D>(); 		// Bumpmap eye-region texs
	
	void Start () {

		// mark current mesh as dynamic for faster updates
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		mesh.MarkDynamic();

		// initialise PCA for mesh randomization
		pca = this.GetComponent<EyeRegionPCA> ();
		randomMeshFromPca = pca.RandomizeMesh ();

		// initialize material for later modifications
		faceMaterial = Resources.Load("Materials/FaceMaterial", typeof(Material)) as Material;

		List<string> texIds = new List<string> ();
		for (int i=1; i<=5; i++) {
			texIds.Add(string.Format("f{0:00}", i));  
		}
		for (int i=1; i<=15; i++) {
			texIds.Add(string.Format("m{0:00}", i));  
		}

		foreach (string texId in texIds) {
			string fn = string.Format ("SkinTextures/{0}_color", texId);
			colorTexs.Add(Resources.Load (fn) as Texture2D);
			colorLdTexs.Add(Resources.Load (fn.Replace("color","color_look_down")) as Texture2D);
			bumpTexs.Add(Resources.Load (fn.Replace("color","disp")) as Texture2D);
		}
	}

	public void RandomizeAppearance(){

		randomMeshFromPca = pca.RandomizeMesh ();
		int randIdx = UnityEngine.Random.Range (0, colorTexs.Count);
		faceMaterial.SetTexture ("_Tex2dColor", colorTexs[randIdx]);
		faceMaterial.SetTexture ("_Tex2dColorLd", colorLdTexs[randIdx]);
		faceMaterial.SetTexture ("_BumpTex", bumpTexs[randIdx]);

	}

	public void UpdateEyeRegion(){

		// also randomize appearance if it hasn't been loaded yet
		if (randomizeAppearance || randomMeshFromPca[0].x == 0) {
			RandomizeAppearance();
		}

		offsets = new Vector3[872];
		
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] vertices = mesh.vertices;
		Vector3[] newVerts = new Vector3[mesh.vertexCount];

		for (int i=0; i<mesh.vertexCount; i++) {
			newVerts [i] = randomMeshFromPca [i];
		}
		
		// EYELID ROTATION
		// --------------------------------
		
		float rot = eyeball.transform.eulerAngles.x;
		rot = (rot>180) ? rot-360 : rot;

        Vector3 startEyelidPos = newVerts[16];
        Vector3 rotatedEyelidPos = newVerts[16] - new Vector3(0f, 0.0062f, 0f); // 0.0052f
        float topEyelidRotMod = Vector3.Angle(startEyelidPos, rotatedEyelidPos) / 20f;

		float t = 1.0f - ((float) rot + 20f) / 40f;
		float downness = Mathf.Lerp(1.0f, 0.4f, t);
		
		Vector3 axis = newVerts[34] - newVerts[139];
		Vector3 pvt = newVerts [34];
        Vector3 newPos;
		
		float[] rot_strengths = {1.0f, 1.0f, 1.0f, 0.8f, 0.4f, 0.2f};
		
		for (int i=0; i<EyeRegionTopology.loops.GetLength(0)-2; i++){
			
			float r_outer_corner = rot * rot_strengths[i] * 0.15f * downness;
			int outer_corner_idx = EyeRegionTopology.loops[i, 10];
			newPos = RotateAroundPivot(
				newVerts[outer_corner_idx],
				Vector3.forward,
				Vector3.zero,
				r_outer_corner);
            offsets[outer_corner_idx] = newPos - newVerts[outer_corner_idx];
			newVerts[outer_corner_idx] = newPos;
			
			axis = newVerts[34]-newVerts[EyeRegionTopology.loops[i, 10]];
			
			for (int j=0; j<EyeRegionTopology.loops.GetLength(1); j++){
				
				int idx1 = EyeRegionTopology.loops[i, j];
				
				t = EyeRegionTopology.middleness(j);
				pvt = Vector3.Lerp(newVerts [34], Vector3.zero, t);
				
				float r = rot * rot_strengths[i];

				newPos = RotateAroundPivot(
					newVerts[idx1],
					axis,
					pvt,
                    (j < 10) ? r * topEyelidRotMod * downness : r * topEyelidRotMod/ 3f * downness);
				offsets[idx1] = newPos-newVerts[idx1];
				newVerts[idx1] = newPos;

			}
		}

		// smooth outer loops for skin stretching
		for (int i=3; i<EyeRegionTopology.loops.GetLength(0)-1; i++){
			for (int j=0; j<EyeRegionTopology.loops.GetLength(1); j++){
				int idx1 = EyeRegionTopology.loops[i, j];
				int idx2 = EyeRegionTopology.loops[i+1, j];
				offsets[idx2] = offsets[idx1] * 0.5f;
				newVerts[idx2] += offsets[idx2];
			}
		}
		
		mesh.vertices = newVerts;
		
		// SHRINKWRAP
		// --------------------------------
		if (!doShrinkWrap) return;

		offsets = new Vector3[872];
		
		// shrink interior margin
		float shrinkwrap_distance = 1f;
		foreach (int idx in EyeRegionTopology.interior_margin_idxs) {
			Vector3 d = -newVerts[idx].normalized;
			RaycastHit hit;
			int layermask = 1 << 8;
			if (Physics.Raycast(transform.TransformPoint(newVerts[idx])-d*shrinkwrap_distance, d, out hit, Mathf.Infinity, layermask)){
				offsets[idx] += ((hit.distance-shrinkwrap_distance) / 100f) * -newVerts[idx].normalized;
				newVerts[idx] += offsets[idx];
			}
		}

        newVerts[12] = CastOntoEyeball((newVerts[0] + newVerts[13]) / 2f, Vector3.forward);

        // handle caruncle
        float offset_decay = 0.9f;
		offsets [27] = offset_decay * (offsets [12]);
		offsets [25] = offset_decay * (offsets [13] + offsets [12])/2f;
		offsets [28] = offset_decay * (offsets [0] + offsets [12])/2f;
		offsets [31] = offsets [32] = offsets [30] =
			offset_decay * (offsets [25] + offsets [27] + offsets [28])/3f;
		offsets [33] = offset_decay * (offsets [32] + offsets [30])/2f;
		
		foreach (int idx in EyeRegionTopology.caruncle_idxs) {	
			newVerts[idx] += offsets[idx];
		}

        // stretch lid skin if too close to eye
        for (int i = 2; i < 5; i++) {
            for (int j = 0; j < EyeRegionTopology.loops.GetLength(1); j++) {
                int idx = EyeRegionTopology.loops[i, j];
                float skinThicknessAtPoint = downness * ((j < 10) ? skinThickness : skinThickness / 2f);
                offsets[idx] = CastOntoEyeball(newVerts[idx], skinThicknessAtPoint) - newVerts[idx];
                newVerts[idx] += offsets[idx];
            }
        }
		
		// smooth eye-region edge loops following shrinkwrap
		for (int i=1; i<EyeRegionTopology.loops.GetLength(0); i++){
			for (int j=0; j<EyeRegionTopology.loops.GetLength(1); j++){
				
				int idx1 = EyeRegionTopology.loops[0, j];
				int idx2 = EyeRegionTopology.loops[i, j];
				
				newVerts[idx2] += offsets[idx1] * rot_strengths[i];
			}
		}

        // blend between look up and look down texture
		float lookDownNess = Mathf.Clamp01(1f-(30f-rot)/30f);
		faceMaterial.SetFloat ("_LookDownNess", lookDownNess * lookDownNess);
		
        // set new mesh positions
		mesh.vertices = newVerts;
		mesh.RecalculateNormals();
		
		// update mesh collider so eyelashes can correctly deform
		// note - this is slow...
		GetComponent<MeshCollider>().sharedMesh = null;
		transform.GetComponent<MeshCollider>().sharedMesh = mesh;
	}


	public Vector3 RotateAroundPivot(Vector3 point, Vector3 axis, Vector3 pivot, float angle) {
		Vector3 v = point - pivot;
		v = Quaternion.AngleAxis(angle, axis) * v;
		return v + pivot;
	}


    Vector3 CastOntoEyeball(Vector3 point) {
        return CastOntoEyeball(point, 0f);
    }

    Vector3 CastOntoEyeball(Vector3 point, Vector3 dir) {
        return CastOntoEyeball(point, dir, 0f);
    }

    Vector3 CastOntoEyeball(Vector3 point, float dist) {
        return CastOntoEyeball(point, point.normalized, dist);
    }

    Vector3 CastOntoEyeball(Vector3 point, Vector3 dir, float dist) {
        float shrinkwrap_distance = 1f;

        RaycastHit hit;
        if (Physics.Raycast(transform.TransformPoint(point) + dir * shrinkwrap_distance, -dir, out hit, Mathf.Infinity, 1 << 8))
        {
            if ((hit.distance - shrinkwrap_distance) < dist)
            {
                return (hit.point + dist * dir) / 100f;
            }
            else
            {
                return point;
            }
        }

        return Vector3.zero;
    }

    public JSONNode GetEyeRegionDetails()
    {
        JSONNode eyeRegionNode = new JSONClass();

        eyeRegionNode.Add("pca_shape_coeffs", pca.GetCoeffs());
        eyeRegionNode.Add("primary_skin_texture", faceMaterial.GetTexture("_Tex2dColor").name);

        return eyeRegionNode;
    }
}




















//using UnityEngine;
//using System.Collections;
//using System.Net;
//using System.Net.Sockets;
//using System.Linq;
//using SimpleJSON;
//using System.Collections.Generic;
//using System.IO;
//using System.Xml.Serialization;

//// Add JSON serializable classes at the top
//[System.Serializable]
//public class CameraIntrinsics
//{
//    public float fx;
//    public float fy;
//    public float cx;
//    public float cy;
//    public int width;
//    public int height;
//}

//[System.Serializable]
//public class CameraConfig
//{
//    public string name;
//    public Vector3 position;
//    public Vector3 rotation;
//    public bool is_orthographic;
//    public CameraIntrinsics intrinsics;
//}

//[System.Serializable]
//public class CameraConfiguration
//{
//    public List<CameraConfig> cameras;
//}



//public class SynthesEyesServer : MonoBehaviour
//{
//    public GameObject lightDirectionalObj;
//    public GameObject eyeballObj;
//    public GameObject eyeRegionObj;
//    public GameObject eyeRegionSubdivObj;
//    public GameObject eyeWetnessObj;
//    public GameObject eyeWetnessSubdivObj;
//    public GameObject eyeLashesObj;

//    private EyeballController eyeball;
//    private EyeRegionController eyeRegion;
//    private SubdivMesh eyeRegionSubdiv;
//    private EyeWetnessController eyeWetness;
//    private SubdivMesh eyeWetnessSubdiv;
//    private DeformEyeLashes[] eyeLashes;

//    private LightingController lightingController;

//    // Render settings for randomization
//    public float defaultCameraPitch = 0;
//    public float defaultCameraYaw = 0;
//    public float cameraPitchNoise = Mathf.Deg2Rad * 20;
//    public float cameraYawNoise = Mathf.Deg2Rad * 40;
//    public float defaultEyePitch = 0;
//    public float defaultEyeYaw = 0;
//    public float eyePitchNoise = 30;
//    public float eyeYawNoise = 30;

//    // Add these public fields somewhere near the top to define your offset ranges:
//    public float randomXRange = 0.5f;
//    public float randomYRange = 0.5f;
//    public float randomZRange = 0.5f;
//    public float randomPitchRange = Mathf.Deg2Rad * 3;
//    public float randomYawRange = Mathf.Deg2Rad * 3;
//    public float randomRollRange = Mathf.Deg2Rad * 3;

//    private float randomizeSceneStartTime = 0f;
//    private int randomizeSceneCallCount = 0;

//    // Camera management fields
//    private List<Camera> cameraList = new List<Camera>();
//    private int currentCameraIndex = 0;
//    //public string jsonConfigPath = "camera_config.json";
//    public string jsonConfigPath = "this_is_my_config.json";

//    // should you save the data or not
//    public bool isSavingData = false;

//    private Mesh eyemesh;

//    // frame index for saving
//    int framesSaved = 0;

//    // Store the camera's original transform from XML
//    private Vector3 xmlBasePosition;
//    private Vector3 xmlBaseEulerAngles;
//    public string xmlCameraFilePath = "camera.xml";

//    void Start()
//    {
//        // Initialise SynthesEyes Objects
//        eyeRegion = eyeRegionObj.GetComponent<EyeRegionController>();
//        eyeball = eyeballObj.GetComponent<EyeballController>();
//        eyeRegionSubdiv = eyeRegionSubdivObj.GetComponent<SubdivMesh>();
//        eyeWetness = eyeWetnessObj.GetComponent<EyeWetnessController>();
//        eyeWetnessSubdiv = eyeWetnessSubdivObj.GetComponent<SubdivMesh>();
//        eyeLashes = eyeLashesObj.GetComponentsInChildren<DeformEyeLashes>(true);

//        lightingController = GameObject.Find("lighting_controller").GetComponent<LightingController>();

//        // Load cameras from JSON oe XML
//        if (File.Exists(jsonConfigPath))
//        {
//            Debug.Log($"-------------------- READ");
//            LoadCamerasFromConfig(jsonConfigPath);
//        }
//        else if (File.Exists(xmlCameraFilePath))
//        {
//            Debug.LogWarning("Using legacy XML config");
//            LoadCameraFromFile(xmlCameraFilePath);
//        }
//    }

//    // Camera loading implementation
//    private void LoadCamerasFromConfig(string configPath)
//    {
//        cameraList.Clear();

//        string jsonData = File.ReadAllText(configPath);
//        CameraConfiguration config = JsonUtility.FromJson<CameraConfiguration>(jsonData);

//        foreach (CameraConfig camConfig in config.cameras)
//        {
//            GameObject camObj = new GameObject(camConfig.name);
//            Camera newCam = camObj.AddComponent<Camera>();

//            camObj.transform.position = camConfig.position;
//            camObj.transform.eulerAngles = camConfig.rotation;

//            ConfigureCameraFromIntrinsics(newCam, camConfig);

//            newCam.tag = cameraList.Count == 0 ? "MainCamera" : "Untagged";
//            newCam.enabled = (cameraList.Count == 0);

//            cameraList.Add(newCam);
//        }
//    }

//    // Camera configuration logic
//    private void ConfigureCameraFromIntrinsics(Camera cam, CameraConfig config)
//    {
//        cam.orthographic = config.is_orthographic;
//        cam.nearClipPlane = 0.3f; // test it out
//        cam.farClipPlane = 1000f;

//        if (config.is_orthographic)
//        {
//            cam.orthographicSize = config.intrinsics.height / (2 * config.intrinsics.fy);
//        }
//        else
//        {
//            float fov = 2 * Mathf.Atan(config.intrinsics.height / (2 * config.intrinsics.fy)) * Mathf.Rad2Deg;
//            cam.fieldOfView = fov;

//            cam.usePhysicalProperties = true;
//            cam.sensorSize = new Vector2(
//                config.intrinsics.width / config.intrinsics.fx * 36f,
//                config.intrinsics.height / config.intrinsics.fy * 24f
//            );
//        }
//    }

//    void RandomizeScene()
//    {
//        // Record the start time on the first call
//        if (randomizeSceneCallCount == 0)
//        {
//            randomizeSceneStartTime = Time.time;
//        }
//        randomizeSceneCallCount++;


//        // After 100 calls, log the elapsed time
//        if (randomizeSceneCallCount == 1000)
//        {
//            float elapsedTime = Time.time - randomizeSceneStartTime;
//            Debug.Log($"RandomizeScene was called 100 times. Elapsed time: {elapsedTime:F2} seconds");
//        }
//        // Randomize eye rotation
//        eyeball.SetEyeRotation(Random.Range(-eyeYawNoise, eyeYawNoise) + defaultEyeYaw,
//                                 Random.Range(-eyePitchNoise, eyePitchNoise) + defaultEyePitch);


//        // Sample offsets using NextGaussianDouble(), then clamp to ± random*Range
//        float offsetX = (float)SyntheseyesUtils.NextGaussianDouble() * (randomXRange / 2f);
//        offsetX = Mathf.Clamp(offsetX, -randomXRange, randomXRange);

//        float offsetY = (float)SyntheseyesUtils.NextGaussianDouble() * (randomYRange / 2f);
//        offsetY = Mathf.Clamp(offsetY, -randomYRange, randomYRange);

//        float offsetZ = (float)SyntheseyesUtils.NextGaussianDouble() * (randomZRange / 2f);
//        offsetZ = Mathf.Clamp(offsetZ, -randomZRange, randomZRange);

//        float offsetPitch = (float)SyntheseyesUtils.NextGaussianDouble() * (randomPitchRange / 2f);
//        offsetPitch = Mathf.Clamp(offsetPitch, -randomPitchRange, randomPitchRange);

//        float offsetYaw = (float)SyntheseyesUtils.NextGaussianDouble() * (randomYawRange / 2f);
//        offsetYaw = Mathf.Clamp(offsetYaw, -randomYawRange, randomYawRange);

//        float offsetRoll = (float)SyntheseyesUtils.NextGaussianDouble() * (randomRollRange / 2f);
//        offsetRoll = Mathf.Clamp(offsetRoll, -randomRollRange, randomRollRange);

//        // Get current active camera
//        Camera currentCam = cameraList[currentCameraIndex];

//        // Apply offsets to active camera
//        currentCam.transform.position = xmlBasePosition + new Vector3(offsetX, offsetY, offsetZ);
//        currentCam.transform.eulerAngles = xmlBaseEulerAngles + new Vector3(offsetPitch, offsetYaw, offsetRoll);
//    }

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.RightArrow))
//        {
//            SwitchCamera(+1);
//        }
//        else if (Input.GetKeyDown(KeyCode.LeftArrow))
//        {
//            SwitchCamera(-1);
//        }


//        if (isSavingData || Input.GetKey("c"))
//        {
//            RandomizeScene();
//        }

//        if (isSavingData || Input.GetKey("r"))
//        {
//            eyeRegion.RandomizeAppearance();
//            eyeball.RandomizeEyeball();
//        }

//        if (isSavingData || Input.GetKey("l"))
//        {
//            lightingController.RandomizeLighting();
//        }

//        eyeRegion.UpdateEyeRegion();
//        eyeRegionSubdiv.Subdivide();
//        eyeWetness.UpdateEyeWetness();
//        eyeWetnessSubdiv.Subdivide();
//        foreach (DeformEyeLashes eyeLash in eyeLashes)
//            eyeLash.UpdateLashes();

//        if (isSavingData || Input.GetKey("s"))
//        {
//            StartCoroutine(saveFrame());
//        }

//        if (Input.GetKeyUp("h"))
//            GameObject.Find("GUI Canvas").GetComponent<Canvas>().enabled = !GameObject.Find("GUI Canvas").GetComponent<Canvas>().enabled;
//    }

//    private void SwitchCamera(int direction)
//    {
//        cameraList[currentCameraIndex].enabled = false;

//        currentCameraIndex = (currentCameraIndex + direction) % cameraList.Count;
//        if (currentCameraIndex < 0) currentCameraIndex = cameraList.Count - 1;

//        cameraList[currentCameraIndex].enabled = true;
//        cameraList[currentCameraIndex].tag = "MainCamera";
//    }


//    private Color parseColor(JSONNode jN)
//    {
//        return new Color(jN[0].AsFloat, jN[1].AsFloat, jN[2].AsFloat, 1.0f);
//    }

//    private Vector3 parseVec(JSONNode jN)
//    {
//        return new Vector3(jN[0].AsFloat, jN[1].AsFloat, jN[2].AsFloat);
//    }

//    private IEnumerator saveFrame()
//    {
//        framesSaved++;
//        // Wait until the end of frame so that the screen buffer is ready
//        yield return new WaitForEndOfFrame();

//        int width = Screen.width;
//        int height = Screen.height;
//        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
//        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//        tex.Apply();

//        byte[] imgBytes = tex.EncodeToJPG();
//        File.WriteAllBytes(string.Format("imgs/{0}.jpg", framesSaved), imgBytes);

//        saveDetails(framesSaved);
//        Object.Destroy(tex);
//    }

//    private void saveDetails(int frame)
//    {
//        Camera activeCam = cameraList[currentCameraIndex];

//        Mesh meshEyeRegion = eyeRegion.transform.GetComponent<MeshFilter>().mesh;
//        Mesh meshEyeBall = eyeball.transform.GetComponent<MeshFilter>().mesh;

//        JSONNode rootNode = new JSONClass();

//        JSONArray listInteriorMargin2D = new JSONArray();
//        rootNode.Add("interior_margin_2d", listInteriorMargin2D);
//        foreach (var idx in EyeRegionTopology.interior_margin_idxs)
//        {
//            Vector3 v_3d = eyeRegion.transform.localToWorldMatrix * meshEyeRegion.vertices[idx];
//            listInteriorMargin2D.Add(new JSONData(Camera.main.WorldToScreenPoint(v_3d).ToString("F4")));
//        }

//        JSONArray listCaruncle2D = new JSONArray();
//        rootNode.Add("caruncle_2d", listCaruncle2D);
//        foreach (var idx in EyeRegionTopology.caruncle_idxs)
//        {
//            Vector3 v_3d = eyeRegion.transform.localToWorldMatrix * meshEyeRegion.vertices[idx];
//            listCaruncle2D.Add(new JSONData(Camera.main.WorldToScreenPoint(v_3d).ToString("F4")));
//        }


//        JSONArray listIris2D = new JSONArray();
//        rootNode.Add("iris_2d", listIris2D);
//        foreach (var idx in EyeRegionTopology.iris_idxs)
//        {
//            Vector3 v_3d = eyeball.transform.localToWorldMatrix * meshEyeBall.vertices[idx];
//            listIris2D.Add(new JSONData(Camera.main.WorldToScreenPoint(v_3d).ToString("F4")));
//        }

//        rootNode.Add("eye_details", eyeball.GetEyeballDetails());
//        rootNode.Add("lighting_details", lightingController.GetLightingDetails());
//        rootNode.Add("eye_region_details", eyeRegion.GetEyeRegionDetails());
//        rootNode.Add("head_pose", (Camera.main.transform.rotation.eulerAngles.ToString("F4")));

//        // New saving method for optical axis and 3D position in space
//        rootNode.Add("ground_truth", (eyeball.GetGazeVector()));
//        rootNode.Add("camera_pose", (eyeball.GetCameratoEyeCenterPose()));

//        File.WriteAllText(string.Format("imgs/{0}.json", frame), rootNode.ToJSON(0));
//    }


//    // Method to load camera settings (both intrinsic and extrinsic) from an XML file.
//    private void LoadCameraFromFile(string file)
//    {
//        XmlSerializer serializer = new XmlSerializer(typeof(XMLCamera));
//        FileStream stream = new FileStream(file, FileMode.Open);
//        XMLCamera xmlCam = serializer.Deserialize(stream) as XMLCamera;
//        stream.Close();

//        // Log: Start loading camera settings
//        Debug.Log("Loading camera settings from XML file: " + file);

//        // Apply resolution settings
//        if (xmlCam.Resolution.x > Screen.width || xmlCam.Resolution.y > Screen.height)
//        {
//            // Optionally, you could set up a RenderTexture here.
//            Debug.Log($"Resolution exceeds screen dimensions. Width: {xmlCam.Resolution.x}, Height: {xmlCam.Resolution.y}");
//        }
//        else
//        {
//            Screen.SetResolution((int)xmlCam.Resolution.x, (int)xmlCam.Resolution.y, FullScreenMode.FullScreenWindow);
//            Debug.Log($"Resolution set to Width: {xmlCam.Resolution.x}, Height: {xmlCam.Resolution.y}");
//        }

//        // Set intrinsic camera parameters
//        Camera.main.nearClipPlane = xmlCam.Near;
//        Camera.main.farClipPlane = xmlCam.Far;
//        Camera.main.orthographicSize = xmlCam.OrthographicSize;
//        Camera.main.orthographic = xmlCam.IsOrthographic;

//        Debug.Log($"Near Clip Plane: {xmlCam.Near}");
//        Debug.Log($"Far Clip Plane: {xmlCam.Far}");
//        Debug.Log($"Orthographic Size: {xmlCam.OrthographicSize}");
//        Debug.Log($"Is Orthographic: {xmlCam.IsOrthographic}");

//        if (!Camera.main.orthographic)
//        {
//            Camera.main.fieldOfView = xmlCam.FieldOfView;
//            Camera.main.usePhysicalProperties = xmlCam.IsPhysicalCamera;

//            Debug.Log($"Field of View: {xmlCam.FieldOfView}");
//            Debug.Log($"Is Physical Camera: {xmlCam.IsPhysicalCamera}");

//            if (Camera.main.usePhysicalProperties)
//            {
//                Camera.main.focalLength = xmlCam.Focal;
//                Camera.main.sensorSize = xmlCam.SensorSize;
//                Camera.main.lensShift = xmlCam.LensShift;
//                Camera.main.gateFit = xmlCam.GateFit;

//                Debug.Log($"Focal Length: {xmlCam.Focal}");
//                Debug.Log($"Sensor Size: X={xmlCam.SensorSize.x}, Y={xmlCam.SensorSize.y}");
//                Debug.Log($"Lens Shift: X={xmlCam.LensShift.x}, Y={xmlCam.LensShift.y}");
//                Debug.Log($"Gate Fit Mode: {xmlCam.GateFit}");
//            }
//        }

//        if (xmlCam.UseProjectionMatrix)
//        {
//            Camera.main.projectionMatrix = xmlCam.ProjectionMatrix;
//            Debug.Log("Custom Projection Matrix Applied");
//            Debug.Log(xmlCam.ProjectionMatrix.ToString());
//        }

//        // Apply extrinsic parameters
//        Camera.main.transform.position = xmlCam.Position;
//        Camera.main.transform.rotation = Quaternion.Euler(xmlCam.Pitch, xmlCam.Yaw, xmlCam.Roll);

//        // Save the base transform for later randomization
//        xmlBasePosition = xmlCam.Position;
//        xmlBaseEulerAngles = new Vector3(xmlCam.Pitch, xmlCam.Yaw, xmlCam.Roll);

//        Debug.Log($"Position: X={xmlCam.Position.x}, Y={xmlCam.Position.y}, Z={xmlCam.Position.z}");
//        Debug.Log($"Rotation (Pitch, Yaw, Roll): Pitch={xmlCam.Pitch}, Yaw={xmlCam.Yaw}, Roll={xmlCam.Roll}");

//        // Log: Finished loading camera settings
//        Debug.Log("Finished applying camera settings from XML.");
//    }

//}
