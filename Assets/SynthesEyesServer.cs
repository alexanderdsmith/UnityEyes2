using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using SimpleJSON;
using System.Collections.Generic;


public class SynthesEyesServer : MonoBehaviour {

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

	private Mesh eyemesh;

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

	void Start () {

		// Initialise SynthesEyes Objects
		eyeRegion = eyeRegionObj.GetComponent<EyeRegionController> ();
		eyeball = eyeballObj.GetComponent<EyeballController> ();
		eyeRegionSubdiv = eyeRegionSubdivObj.GetComponent<SubdivMesh> ();
		eyeWetness = eyeWetnessObj.GetComponent<EyeWetnessController> ();
		eyeWetnessSubdiv = eyeWetnessSubdivObj.GetComponent<SubdivMesh> ();
		eyeLashes = eyeLashesObj.GetComponentsInChildren<DeformEyeLashes> (true);

		lightingController = GameObject.Find ("lighting_controller").GetComponent<LightingController> ();
	}

	void RandomizeScene(){
		
		eyeball.SetEyeRotation(Random.Range(-eyeYawNoise, eyeYawNoise)+defaultEyeYaw,
		                       Random.Range(-eyePitchNoise, eyePitchNoise)+defaultEyePitch);

        Camera.main.transform.position = SyntheseyesUtils.RandomVec(
            defaultCameraPitch - cameraPitchNoise, defaultCameraPitch + cameraPitchNoise,
			defaultCameraYaw - cameraYawNoise, defaultCameraYaw + cameraYawNoise) * 10f;

        Camera.main.transform.LookAt(new Vector3(0f,0f,1f), Quaternion.AngleAxis(-90, Vector3.up) * Vector3.left);

	}

    void Update () {

        if (isSavingData || Input.GetKey("c")) {
            RandomizeScene();
        }

        if (isSavingData || Input.GetKey("r")) {
            eyeRegion.RandomizeAppearance();
            eyeball.RandomizeEyeball();
        }

        if (isSavingData || Input.GetKey("l")) {
            lightingController.RandomizeLighting();
        }

        eyeRegion.UpdateEyeRegion();
		eyeRegionSubdiv.Subdivide();
		eyeWetness.UpdateEyeWetness();
        eyeWetnessSubdiv.Subdivide();
        foreach (DeformEyeLashes eyeLash in eyeLashes) 
			eyeLash.UpdateLashes();

		if (isSavingData || Input.GetKey("s")) {
        	StartCoroutine(saveFrame ());
		}

        if (Input.GetKeyUp("h"))
            GameObject.Find("GUI Canvas").GetComponent<Canvas>().enabled = !GameObject.Find("GUI Canvas").GetComponent<Canvas>().enabled;

    }

	private Color parseColor(JSONNode jN){
		return new Color (jN[0].AsFloat, jN[1].AsFloat, jN[2].AsFloat, 1.0f);
	}
	

	private Vector3 parseVec(JSONNode jN){
		return new Vector3 (jN[0].AsFloat, jN[1].AsFloat, jN[2].AsFloat);
	}

	
	private IEnumerator saveFrame(){

        framesSaved++;

        // We should only read the screen buffer after rendering is complete
        yield return new WaitForEndOfFrame();
		
		// Create a texture the size of the screen, RGB24 format
		int width = Screen.width;
		int height = Screen.height;
		Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
		
		// Read screen contents into the texture
		tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		tex.Apply();

        byte[] imgBytes = tex.EncodeToJPG();
        System.IO.File.WriteAllBytes(string.Format("imgs/{0}.jpg", framesSaved), imgBytes);
        //byte[] imgBytes = tex.EncodeToPNG();
        //System.IO.File.WriteAllBytes(string.Format("imgs/{0}.png", framesSaved), imgBytes);

        saveDetails (framesSaved);

		Object.Destroy(tex);
	}

	private void saveDetails(int frame){

		Mesh meshEyeRegion = eyeRegion.transform.GetComponent<MeshFilter>().mesh;
		Mesh meshEyeBall = eyeball.transform.GetComponent<MeshFilter>().mesh;

		JSONNode rootNode = new JSONClass();

		// JSONArray listInteriorMargin2D = new JSONArray();
		// rootNode.Add ("interior_margin_2d", listInteriorMargin2D);
		// foreach (var idx in EyeRegionTopology.interior_margin_idxs) {
		// 	Vector3 v_3d = eyeRegion.transform.localToWorldMatrix * meshEyeRegion.vertices[idx];
		// 	listInteriorMargin2D.Add(new JSONData(Camera.main.WorldToScreenPoint(v_3d).ToString ("F4")));
		// }

		// JSONArray listCaruncle2D = new JSONArray();
		// rootNode.Add ("caruncle_2d", listCaruncle2D);
		// foreach (var idx in EyeRegionTopology.caruncle_idxs) {
		// 	Vector3 v_3d = eyeRegion.transform.localToWorldMatrix * meshEyeRegion.vertices[idx];
		// 	listCaruncle2D.Add(new JSONData(Camera.main.WorldToScreenPoint(v_3d).ToString ("F4")));
		// }

		// JSONArray listIris2D = new JSONArray();
		// rootNode.Add ("iris_2d", listIris2D);
		// foreach (var idx in EyeRegionTopology.iris_idxs) {
		// 	Vector3 v_3d = eyeball.transform.localToWorldMatrix * meshEyeBall.vertices[idx];
		// 	listIris2D.Add(new JSONData(Camera.main.WorldToScreenPoint(v_3d).ToString ("F4")));
		// }

		// rootNode.Add("eye_details", eyeball.GetEyeballDetails());
        // rootNode.Add("lighting_details", lightingController.GetLightingDetails());
        // rootNode.Add("eye_region_details", eyeRegion.GetEyeRegionDetails());

        // rootNode.Add ("head_pose", (Camera.main.transform.rotation.eulerAngles.ToString ("F4")));

		rootNode.Add ("ground_truth", (eyeball.GetGazeVector()));
		
		System.IO.File.WriteAllText (string.Format("imgs/{0}.json", frame), rootNode.ToJSON(0));

	}
}
