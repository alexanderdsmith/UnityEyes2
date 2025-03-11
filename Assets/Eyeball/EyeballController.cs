using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

public class EyeballController : MonoBehaviour {

    List<Texture2D> colorTexs = new List<Texture2D>();		// Eye region color textures
    Dictionary<string, Texture2D> colorTexsDict = new Dictionary<string, Texture2D>();
    Material eyeMaterial;

    public static int[] iris_idxs = { 3, 7, 11, 14, 18, 21, 25, 29, 33, 37, 41, 45, 49, 52, 56, 60, 64, 68, 72, 76, 80, 84, 88, 92, 96, 100, 104, 108, 112, 116, 120, 124 };
    public static Vector3[] iris_start_pos = new Vector3[iris_idxs.Length];
    private float irisSize = 1.0f;

    public bool isInteractive = false;

	// Use this for initialization
	void Start () {

        // initialize collection of color textures
        foreach (Texture2D c in Resources.LoadAll("IrisTextures", typeof(Texture2D))) {
            colorTexs.Add(c);
            colorTexsDict.Add(c.name, c);
        }
            

        // initialize material for later modifications
        eyeMaterial = Resources.Load("Materials/EyeMaterial", typeof(Material)) as Material;

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        for (int i=0; i<iris_idxs.Length; i++)
            iris_start_pos[i] = vertices[iris_idxs[i]];
    }

	// Update is called once per frame
	void Update () {

		if (!isInteractive || !Input.GetMouseButton(2))
			return;

		float p = Mathf.Clamp01(Input.mousePosition.x / (float) Screen.width);
		float q = Mathf.Clamp01(Input.mousePosition.y / (float) Screen.height);

		transform.eulerAngles = new Vector3(
			30 - q * 60,
			30 - p * 60,
			0
		);
    }

    public void RandomizeEyeball() {

        // Slightly decrease iris size on random
        irisSize = Random.Range(0.9f, 1.0f);

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        // calculate position of 3D iris middle
        Vector3 iris_middle = Vector3.zero;
        foreach (int idx in iris_idxs)
            iris_middle += vertices[idx] / (float)iris_idxs.Length;

        // re-position limbus vertices
        for (int i = 0; i < iris_idxs.Length; i++) {
            Vector3 offset = iris_start_pos[i] - iris_middle;
            vertices[iris_idxs[i]] = iris_middle + offset * irisSize;
        }

        mesh.vertices = vertices;

        // TODO: Randomize pupil size based on clamped uniform distribution instead of gaussian noise
        eyeMaterial.SetFloat("_PupilSize", SyntheseyesUtils.NextGaussianDouble()/5.0f);

        if (Random.value > 0.5f) eyeMaterial.SetTexture("_MainTex", colorTexsDict["eyeball_brown"]);
        else eyeMaterial.SetTexture("_MainTex", colorTexs[Random.Range(0, colorTexs.Count)]);
    }

    public Vector3 GetPupilCenter() {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        
        Vector3 iris_middle = Vector3.zero;
        foreach (int idx in iris_idxs) {
            iris_middle += vertices[idx];
        }
        iris_middle /= (float)iris_idxs.Length;
        return transform.TransformPoint(iris_middle);
    }
	
	public void SetEyeRotation(float pitch, float yaw){
		transform.eulerAngles = new Vector3(yaw, pitch, 0);
	}

	public Vector3 GetEyeLookVector(){
		return (transform.localToWorldMatrix * Vector3.forward).normalized;
	}

   public JSONNode GetEyeballDetails() {

        JSONNode eyeballNode = new JSONClass();

        eyeballNode.Add("look_vec", (Camera.main.transform.worldToLocalMatrix * GetEyeLookVector()).ToString("F4"));
        eyeballNode.Add("pupil_size", eyeMaterial.GetFloat("_PupilSize").ToString());
        eyeballNode.Add("iris_size", irisSize.ToString());
        eyeballNode.Add("iris_texture", eyeMaterial.GetTexture("_MainTex").name);

        return eyeballNode;
    }

    public JSONNode GetGazeVector() {
        JSONNode gazeNode = new JSONClass();

        Vector3 irisCameraSpace = Camera.main.transform.InverseTransformPoint(GetPupilCenter());

        Vector3 gazeVector = Camera.main.transform.worldToLocalMatrix * GetEyeLookVector();

        gazeNode.Add("iris_center", (irisCameraSpace).ToString("F4"));
        gazeNode.Add("gaze_vec", (Camera.main.transform.worldToLocalMatrix * GetEyeLookVector()).ToString("F4"));

        return gazeNode;
    }

    public JSONNode GetGazeVector(Camera cam) {
        JSONNode gazeNode = new JSONClass();
        
        // Get the camera transform from the Camera object.
        Transform camTrans = cam.transform;
        
        // 1. Compute the iris center in world space (configuration in centimeters)
        Vector3 irisCenter_cm = GetPupilCenter();
        // Convert iris center to the coordinate system relative to the given camera.
        Vector3 relIrisCenter = camTrans.InverseTransformPoint(irisCenter_cm);
        // Convert to meters.
        relIrisCenter = relIrisCenter * 0.01f;
        // Convert from Unity’s left-handed to right-handed (flip Y axis).
        relIrisCenter.y = -relIrisCenter.y;
        
        // 2. Get the normalized gaze direction vector from the eye (in Unity’s coordinate system).
        Vector3 gazeDir = GetEyeLookVector();
        // Convert the gaze direction into the camera's local space.
        Vector3 relGazeDir = camTrans.InverseTransformDirection(gazeDir);
        
        // 3. Compute a point offset from the iris center by 1 unit along the gaze direction.
        Vector3 gazePoint = relIrisCenter + relGazeDir * 1.0f;
        // (Optional conversion)
        gazePoint = gazePoint * 0.01f;
        gazePoint.y = -gazePoint.y;
        
        // 4. Also compute the world origin (0,0,0) in camera coordinates.
        Vector3 eyeCenter = camTrans.InverseTransformPoint(Vector3.zero);
        eyeCenter = eyeCenter * 0.01f;
        eyeCenter.y = -eyeCenter.y;
        
        // 5. Add these points to the JSON node.
        gazeNode.Add("iris_center", relIrisCenter.ToString("F6"));
        gazeNode.Add("gaze_vector", gazePoint.ToString("F6"));
        gazeNode.Add("eye_center", eyeCenter.ToString("F6"));
        
        return gazeNode;
    }

    public JSONNode GetCameratoEyeCenterPose() {
        JSONNode cameraNode = new JSONClass();

        cameraNode.Add("position", Camera.main.transform.position.ToString("F4"));
        cameraNode.Add("rotation", Camera.main.transform.rotation.eulerAngles.ToString("F4"));

        return cameraNode;
    }
}
