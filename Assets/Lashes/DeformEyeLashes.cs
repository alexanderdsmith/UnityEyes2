using UnityEngine;
using System.Collections;

public class DeformEyeLashes : MonoBehaviour {

	// used for choosing starting index
	public bool isTopLash = true;

	// how close to put hairs to interior margin
	public float margin_offset = 0.5f;

	// control growth of guide hairs
	public float topLashStartAngle = 35f;
	public float topLashDeltaAngle = -20f;
	public float bottomLashStartAngle = 45f;
	public float bottomLashDeltaAngle = -20f;

	public bool debug = true;

	public float hairLengthModifier = 1.0f;

	// index mesh verts by grid position
	private int[,,] grid_idxs = new int[13, 6, 2];

	// the positions of guide hairs are stored here
	private Vector3[,] hairPos = new Vector3[7, 6]; 

	// Use this for initialization
	void Start () {

		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] vs = mesh.vertices;
		Vector3[] ns = mesh.normals;

		// set grid indices for eyelash mesh vertices
		Bounds bounds = GetComponent<Renderer> ().bounds;
		for (int i=0; i<vs.Length; i++) {
			int grid_x = (int) Mathf.Round((vs[i].x-mesh.bounds.min.x) / mesh.bounds.size.x * 12f);
			int up_or_down = ns[i].y > 0 ? 0 : 1;
			int grid_z = (int) Mathf.Round((vs[i].z-mesh.bounds.min.z) / mesh.bounds.size.z * 5f);
			grid_idxs[grid_x, grid_z, up_or_down] = i;
		}

	}

	// Update is called once per frame
	public void UpdateLashes () {

		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] vs = mesh.vertices;
//		for (int i=0; i<12; i++) {
//			Debug.DrawLine(vs[grid_idxs[i,5,0]]*100f,vs[grid_idxs[i+1,5,0]]*100f,Color.blue);
//		}

		GameObject eye_region = GameObject.Find("eye_region");
		Mesh mesh_eye_region = eye_region.GetComponent<MeshFilter> ().mesh;
		Vector3[] er_vs = mesh_eye_region.vertices;
		Vector3[] er_ns = mesh_eye_region.normals;

		int startIdx = isTopLash ? 3 : 11;
		float hairLength = hairLengthModifier * (isTopLash ? 0.8f : 0.5f);
		float startAngle = isTopLash ? topLashStartAngle : bottomLashStartAngle;
		float deltaAngle = isTopLash ? topLashDeltaAngle : bottomLashDeltaAngle;

		// grow out guide hairs
		for (int i=0; i<7; i++) {
		
			int ii = startIdx+i;

			int idx1 = EyeRegionTopology.loops [0, ii];
			int idx2 = EyeRegionTopology.loops [1, ii];
			int idx3 = EyeRegionTopology.loops [2, ii];

			int idx_n1 = EyeRegionTopology.loops [1, ii - 1];
			int idx_n2 = EyeRegionTopology.loops [1, ii + 1];

			Vector3 axis = er_vs [idx_n1] - er_vs [idx_n2];
			Vector3 dir = er_vs [idx2].normalized;
			dir = Quaternion.AngleAxis(startAngle, axis)*dir;

			Vector3 startPos = Vector3.Lerp(er_vs [idx2], er_vs [idx3], margin_offset);
			startPos = eye_region.transform.TransformPoint(startPos);
			startPos -= dir*0.01f;

			hairPos[i, 0] = startPos;
			drawHair (startPos, dir, er_vs [idx_n1] - er_vs [idx_n2], hairLength, deltaAngle, i, 1);
		}

		// set new vertex positions for eyelash mesh
		Vector3[] newVs = new Vector3[156];
		for (int i=0; i<13; i++) {
			for(int j=0; j<6; j++){

				int hairPosX = (int) Mathf.Round(i/13f * 6f);
				int hairPosZ = (int) Mathf.Round(j/5f * 5f);
				Vector3 pos = hairPos[hairPosX,hairPosZ]/100f;

				pos = Vector3.Lerp(hairPos[hairPosX,hairPosZ]/100f,
				                   hairPos[hairPosX+(i%2!=0?1:0),hairPosZ]/100f, 0.5f);

				newVs[grid_idxs[i,j,0]] = pos;
				newVs[grid_idxs[i,j,1]] = pos;
			}
		}

		// smooth the lashes mesh across the x-axis
		for (int i=1; i<12; i++) {
			for(int j=0; j<6; j++){
				for(int k=0; k<2; k++){ 
				newVs[grid_idxs[i,j,k]] =
					newVs[grid_idxs[i,j,k]] * 0.5f +
					newVs[grid_idxs[i-1,j,k]] * 0.25f + 
					newVs[grid_idxs[i+1,j,k]] * 0.25f;
				}
			}
		}

		// set the new vertex positions and recalculate normals
		mesh.vertices = newVs;
		mesh.RecalculateNormals ();

	}

	private float epsilon = 0.0001f;
	private float deltaNormal = 0.02f;
	private int hair_iters_max = 50;

	void drawHair(Vector3 origin, Vector3 dir, Vector3 axis, float totalLength, float deltaAngle, int grid_x, int grid_z){

		bool hairOk = false;
		int iters=0;
		Vector3 endPos = origin;
		float dist = totalLength / (float) 5;

		while(!hairOk && iters<hair_iters_max){
			endPos = origin + dir.normalized*dist;
			iters++;
			
			RaycastHit hit;
			if (Physics.Raycast(origin+dir*epsilon, dir, out hit, dist*2.0f)){
				dir = dir + hit.normal*deltaNormal;
			} else {
				hairOk = true;
				hairPos[grid_x, grid_z] = endPos;
				if (debug) Debug.DrawLine (origin, endPos, Color.Lerp(Color.green, Color.red, grid_z/(float)5));
			}
		}

		if (grid_z < 5) {
			drawHair (endPos, Quaternion.AngleAxis(deltaAngle, axis)*dir, axis, totalLength, deltaAngle, grid_x, grid_z+1);
		}

	}
	
	public Vector3 RotateAroundPivot(Vector3 point, Vector3 axis, Vector3 pivot, float angle) {
		Vector3 v = point - pivot;
		v = Quaternion.AngleAxis(angle, axis) * v;
		return v + pivot;
	}
}
