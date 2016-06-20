using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OutputMeshDetails : MonoBehaviour
{

	// Use this for initialization
	void Start ()
	{
	
		string name = this.gameObject.name;

		Mesh mesh = transform.GetComponent<MeshFilter>().mesh;

		List<string> vs_string = new List<string> ();
		foreach (var v in mesh.vertices) {
			vs_string.Add (v.ToString ("F4"));
		}
		//System.IO.File.WriteAllText (string.Format("verts_{0}.txt",name),
		//                             string.Join (",", vs_string.ToArray ()));

		print (mesh.vertices.Length);

		List<string> tris_string = new List<string> ();
		foreach (var t in mesh.triangles) {
			tris_string.Add (t.ToString ());
		}
		//System.IO.File.WriteAllText (string.Format("tris_{0}.txt",name),
		//                             string.Join (",", tris_string.ToArray ()));

	}
}
