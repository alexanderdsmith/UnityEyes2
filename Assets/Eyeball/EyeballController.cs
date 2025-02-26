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

        // finally update mesh
        mesh.vertices = vertices;

        // also modify pupil size via material
        eyeMaterial.SetFloat("_PupilSize", SyntheseyesUtils.NextGaussianDouble()/5.0f);

        // choose a random iris color
        if (Random.value > 0.5f) eyeMaterial.SetTexture("_MainTex", colorTexsDict["eyeball_brown"]);
        else eyeMaterial.SetTexture("_MainTex", colorTexs[Random.Range(0, colorTexs.Count)]);
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

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        // calculate position of 3D iris middle
        Vector3 iris_middle = Vector3.zero;
        foreach (int idx in iris_idxs)
            iris_middle += vertices[idx] / (float)iris_idxs.Length;

        // Debug: After calculation 
        //Debug.Log($"Calculated iris_middle (local): {iris_middle}");
        //Debug.Log($"World space iris: {transform.TransformPoint(iris_middle)}");

        Vector3 irisCameraSpace = Camera.main.transform.InverseTransformPoint(iris_middle);
        //Debug.Log($"Camera space iris: {irisCameraSpace}");

        Vector3 gazeVector = Camera.main.transform.worldToLocalMatrix * GetEyeLookVector();
        //Debug.Log($"Gaze vector (camera space): {gazeVector}");
        //Debug.DrawRay(iris_middle, gazeVector * 0.5f, Color.blue, 0.1f);

        gazeNode.Add("iris_center", (irisCameraSpace).ToString("F4"));
        gazeNode.Add("gaze_vec", (Camera.main.transform.worldToLocalMatrix * GetEyeLookVector()).ToString("F4"));


        return gazeNode;
    }

    public JSONNode GetCameratoEyeCenterPose() {
        JSONNode cameraNode = new JSONClass();

        cameraNode.Add("position", Camera.main.transform.position.ToString("F4"));
        cameraNode.Add("rotation", Camera.main.transform.rotation.eulerAngles.ToString("F4"));

        return cameraNode;
    }
}



//private void RestoreInputValues()
//{
//    // Restore Intrinsics and IntrinsicsNoise values
//    foreach (string group in new[] { "Intrinsics", "IntrinsicsNoise" })
//    {
//        if (groupInputs.ContainsKey(group) && groupValues.ContainsKey(group))
//        {
//            var inputs = groupInputs[group];
//            var values = groupValues[group];

//            if (inputs.fx != null && values.ContainsKey("fx"))
//                inputs.fx.text = values["fx"].ToString();

//            if (inputs.fy != null && values.ContainsKey("fy"))
//                inputs.fy.text = values["fy"].ToString();

//            if (inputs.cx != null && values.ContainsKey("cx"))
//                inputs.cx.text = values["cx"].ToString();

//            if (inputs.cy != null && values.ContainsKey("cy"))
//                inputs.cy.text = values["cy"].ToString();

//            if (inputs.width != null && values.ContainsKey("width"))
//                inputs.width.text = values["width"].ToString();

//            if (inputs.height != null && values.ContainsKey("height"))
//                inputs.height.text = values["height"].ToString();
//        }
//    }

//    // Restore Extrinsics and ExtrinsicsNoise values
//    foreach (string group in new[] { "Extrinsics", "ExtrinsicsNoise" })
//    {
//        if (groupInputs.ContainsKey(group) && groupValues.ContainsKey(group))
//        {
//            var inputs = groupInputs[group];
//            var values = groupValues[group];

//            if (inputs.x != null && values.ContainsKey("x"))
//                inputs.x.text = values["x"].ToString();

//            if (inputs.y != null && values.ContainsKey("y"))
//                inputs.y.text = values["y"].ToString();

//            if (inputs.z != null && values.ContainsKey("z"))
//                inputs.z.text = values["z"].ToString();

//            if (inputs.rx != null && values.ContainsKey("rx"))
//                inputs.rx.text = values["rx"].ToString();

//            if (inputs.ry != null && values.ContainsKey("ry"))
//                inputs.ry.text = values["ry"].ToString();

//            if (inputs.rz != null && values.ContainsKey("rz"))
//                inputs.rz.text = values["rz"].ToString();
//        }
//    }
//}