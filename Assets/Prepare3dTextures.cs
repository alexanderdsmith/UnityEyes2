using UnityEngine;
//using UnityEditor;
using System.Collections;

public class Prepare3dTextures : MonoBehaviour {

	static int texSize = 2048;
	static int stride = 4;
	static int newSize = texSize / stride;
	string[] meshNames = {"f01", "f02", "f03", "f04", "m01", "m02", "m04", "m05",};

	string[] suffixes = {"disp"};

	// Use this for initialization
	void Start () {

		foreach (string suffix in suffixes) {

			// prepare 3d texture
			int depth = meshNames.Length;
			Texture3D tex3d = new Texture3D (newSize, newSize, depth, TextureFormat.ARGB32, false);

			var cs = new Color[newSize * newSize * depth];
			int idx = 0;
			for (int z = 0; z < meshNames.Length; z++) {
				
				string filename = string.Format ("SkinTextures/{0}_{1}", meshNames [z], suffix);
				Texture2D tex2d = Resources.Load (filename) as Texture2D;
				Color[] cs_tex2dColor = tex2d.GetPixels ();
				
				for (int y = 0; y < texSize; y+=stride) {
					for (int x = 0; x < texSize; x+=stride, idx++) {
						cs [idx] = cs_tex2dColor [y * texSize + x];
					}
				}
			}

			tex3d.SetPixels (cs);
			tex3d.Apply ();
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
