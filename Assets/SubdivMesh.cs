using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SubdivMesh : MonoBehaviour
{

	public string meshName;
	public bool RightEye = true;

	public GameObject controlMeshObj;

	private int vertCount;      
	private int weightLength;  
	private float[,] weights;   
	private int[,] idxs;        

	void Start()
	{

		controlMeshObj.GetComponent<Renderer>().enabled = false;

		TextAsset t = Resources.Load(string.Format("SubdivData/new_verts_{0}", meshName)) as TextAsset;
		string[] filelines = t.text.Split('\n');
		vertCount = filelines.Length - 1;
		Vector3[] newVerts = new Vector3[vertCount];
		for (int i = 0; i < filelines.Length - 1; i++)
		{
			string[] line = filelines[i].Split(',');
			newVerts[i] = new Vector3(float.Parse(line[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture), float.Parse(line[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture), float.Parse(line[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture));
		}

		t = Resources.Load(string.Format("SubdivData/new_tris_{0}", meshName)) as TextAsset;
		filelines = t.text.Split('\n');
		int[] newTris = new int[filelines.Length - 1];
		for (int i = 0; i < filelines.Length - 1; i++)
		{
			newTris[i] = int.Parse(filelines[i]);
		}

		Mesh newMesh = new Mesh();
		newMesh.MarkDynamic();
		transform.GetComponent<MeshFilter>().mesh = newMesh;
		newMesh.name = "SubdividedMesh";
		newMesh.vertices = newVerts;
		newMesh.triangles = newTris;
		newMesh.normals = new Vector3[vertCount];
		;

		t = Resources.Load(string.Format("SubdivData/new_v_weights_{0}", meshName)) as TextAsset;
		filelines = t.text.Split('\n');
		weightLength = filelines[0].Split(',').Length;
		weights = new float[vertCount, weightLength];
		for (int i = 0; i < filelines.Length - 1; i++)
		{
			string[] line = filelines[i].Split(',');
			for (int j = 0; j < weightLength; j++)
			{
				weights[i, j] = float.Parse(line[j], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		idxs = new int[vertCount, weightLength];
		t = Resources.Load(string.Format("SubdivData/new_v_idxs_{0}", meshName)) as TextAsset;
		filelines = t.text.Split('\n');
		for (int i = 0; i < filelines.Length - 1; i++)
		{
			string[] line = filelines[i].Split(',');
			for (int j = 0; j < weightLength; j++)
			{
				idxs[i, j] = int.Parse(line[j], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
			}
		}
	}

	public void Subdivide()
	{

		Vector3[] new_vs = new Vector3[vertCount];
		Vector3[] new_ns = new Vector3[vertCount];
		Vector2[] new_uvs = new Vector2[vertCount];

		Mesh controlMesh = controlMeshObj.transform.GetComponent<MeshFilter>().mesh;
		Mesh mesh = GetComponent<MeshFilter>().mesh;

		Vector3[] c_vs = controlMesh.vertices;
		Vector3[] c_ns = controlMesh.normals;
		Vector2[] c_uvs = controlMesh.uv;

		for (int i = 0; i < vertCount; i++)
		{
			new_ns[i].Set(0, 0, 0);
			new_vs[i].Set(0, 0, 0);
			new_uvs[i].Set(0, 0);

			for (int j = 0; j < weightLength; j++)
			{
				new_vs[i] += weights[i, j] * c_vs[idxs[i, j]];
				new_ns[i] += weights[i, j] * c_ns[idxs[i, j]];
				new_uvs[i] += weights[i, j] * c_uvs[idxs[i, j]];
			}
		}
	
		mesh.vertices = new_vs;
		mesh.normals = new_ns;
		mesh.uv = new_uvs;
	}
}