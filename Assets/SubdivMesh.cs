using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SubdivMesh : MonoBehaviour {

	// name used to load susbiv files
	public string meshName;

	// original mesh that this is a subdiv mesh of
	public GameObject controlMeshObj;

	private int vertCount; 		// number of new verticies
	private int weightLength;	// length of each list of weights
	private float[,] weights;	// 2D array of weights for each vertex
	private int[,] idxs;		// 2D array of corresponding original mesh idxs

    // On initializing sub-d surface
	void Start () {

		// Disable rendering original mesh
		controlMeshObj.GetComponent<Renderer>().enabled = false;

        // Read in new vertices
        TextAsset t = Resources.Load(string.Format("SubdivData/new_verts_{0}", meshName)) as TextAsset;
        string[] filelines = t.text.Split('\n');
        vertCount = filelines.Length-1;
		Vector3[] newVerts = new Vector3 [vertCount];
		for (int i=0; i<filelines.Length-1; i++) {
			string[] line = filelines[i].Split(',');
			newVerts[i] = new Vector3(float.Parse(line[0]), float.Parse(line[1]), float.Parse(line[2]));
		}

		// Read in new triangles
        t = Resources.Load(string.Format("SubdivData/new_tris_{0}", meshName)) as TextAsset;
        filelines = t.text.Split('\n');
        int[] newTris = new int [filelines.Length-1];
		for (int i=0; i<filelines.Length-1; i++) {
			newTris[i] = int.Parse(filelines[i]);
		}

        // Create the subdivided mesh with vert and triangle data
		Mesh newMesh = new Mesh ();
		newMesh.MarkDynamic();
		transform.GetComponent<MeshFilter>().mesh = newMesh;
		newMesh.name = "SubdividedMesh";
		newMesh.vertices = newVerts;
		newMesh.triangles = newTris;
		newMesh.normals = new Vector3 [vertCount];
		newMesh.Optimize();

        // Read in weights
        t = Resources.Load(string.Format("SubdivData/new_v_weights_{0}", meshName)) as TextAsset;
        filelines = t.text.Split('\n');
		weightLength = filelines[0].Split(',').Length;
		weights = new float[vertCount, weightLength];
		for (int i=0; i<filelines.Length-1; i++) {
			string[] line = filelines[i].Split(',');
			for(int j=0; j<weightLength; j++){
				weights[i,j] = float.Parse(line[j]);
			}
		}

		// Read in idxs
		idxs = new int[vertCount, weightLength];
        t = Resources.Load(string.Format("SubdivData/new_v_idxs_{0}", meshName)) as TextAsset;
        filelines = t.text.Split('\n');
        for (int i=0; i<filelines.Length-1; i++) {
			string[] line = filelines[i].Split(',');
			for(int j=0; j<weightLength; j++){
				idxs[i,j] = int.Parse(line[j]);
			}
		}
	}

    // Loop subdivision, called each frame
	public void Subdivide(){

		Vector3[] new_vs = new Vector3 [vertCount];
		Vector3[] new_ns = new Vector3 [vertCount];
		Vector2[] new_uvs = new Vector2 [vertCount];

		Mesh controlMesh = controlMeshObj.transform.GetComponent<MeshFilter>().mesh;
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		
		Vector3[] c_vs = controlMesh.vertices;
		Vector3[] c_ns = controlMesh.normals;
		Vector2[] c_uvs = controlMesh.uv;
		
		for (int i=0; i<vertCount; i++) {
			new_ns[i].Set(0,0,0);
			new_vs[i].Set(0,0,0);
			new_uvs[i].Set(0,0);

			for(int j=0; j<weightLength; j++) {
				new_vs[i] += weights[i,j] * c_vs[idxs[i,j]];
				new_ns[i] += weights[i,j] * c_ns[idxs[i,j]];
				new_uvs[i] += weights[i,j] * c_uvs[idxs[i,j]];
			}
		}
		
		mesh.vertices = new_vs;
		mesh.normals = new_ns;
		mesh.uv = new_uvs;
	}
}
