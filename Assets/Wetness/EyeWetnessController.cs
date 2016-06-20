using UnityEngine;
using System.Collections;

public class EyeWetnessController : MonoBehaviour {

	public GameObject controlMeshObj;

    //public static int[] eyeball_idxs =         {52, 51, 50, 49, 48, 47, 46, 43, 44, 45, 55, 56, 57, 54, 53, 58};
    //public static int[] interior_margin_idxs = {12, 13, 14, 15, 16, 17, 18, 42, 41, 40,  6,  2,  1,  7,  8,  0};
    //public static int[] caruncle_idxs =        {28, 27, 25, 32, 31, 30, 33};
    //public static int[] anterior_margin_idxs = {35, 36, 26, 24, 23, 22, 21, 20, 19, 37, 38, 39, 5, 4, 3, 9, 10, 11, 29, 34};

    public static int[] eyeball_idxs =         {13, 12, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0, 15, 14};
    public static int[] interior_margin_idxs = {46, 44, 42, 40, 38, 34, 35, 47, 45, 43, 41, 39, 36, 37, 32, 33};
    public static int[] anterior_margin_idxs = {29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30};
    public static float[] wetness_ws =         {0.25f, 0.3f, 0.1f, 0.0f, 0.0f, 0.0f, 0.0f, 0.2f, 0.5f, 0.6f, 0.8f, 0.8f, 0.8f, 0.8f, 0.8f, 0.5f };

    // weights for offset ammounts
    //public static float[] interior_ws = {2.0f, 0.5f, 0.25f, 0.25f, 0.25f, 0.25f, 0.25f, 0.5f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f};
    //public static float[] exterior_ws = {0.0f, 0.0f, 0.0f, 0.3f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.8f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.5f, 0.0f, 0.0f, 0.0f};
    //public static float[] caruncle_ws = {2.0f, 2.0f, 2.0f, 1.5f, 1.5f, 1.5f, 1.0f};


    public void UpdateEyeWetness() {

		Mesh mesh_eye_region = controlMeshObj.GetComponent<MeshFilter> ().mesh;
		Vector3[] eye_regions_vs = mesh_eye_region.vertices;
        Vector3[] eye_regions_ns = mesh_eye_region.normals;

        Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] ns = mesh.normals;    

		Vector3[] newVerts = new Vector3[mesh.vertexCount];
		float offset = 0.0005f;

		//for (int i=0; i<interior_margin_idxs.Length; i++) {
		//	int idx1 = interior_margin_idxs[i];
		//	int idx2 = EyeRegionTopology.interior_margin_idxs[i];
		//	newVerts[idx1] = eye_regions_vs[idx2] + ns[idx1]*offset*interior_ws[i];
		//}
		
        // First do eyeball vertices
		for (int i=0; i<eyeball_idxs.Length; i++) {
			int idx1 = eyeball_idxs[i];
            int idx2 = EyeRegionTopology.interior_margin_idxs[i];
            int idx3 = EyeRegionTopology.interior_margin_outer_loop_idxs[i];
            Vector3 v = Vector3.Lerp(-eye_regions_vs[idx2].normalized, Vector3.forward, 0.8f) * 0.0012f;
            
            newVerts[idx1] = CastOntoEyeball(eye_regions_vs[idx2] + v) ;
        }

		//for (int i=0; i<caruncle_idxs.Length; i++) {
		//	int idx1 = caruncle_idxs[i];
		//	int idx2 = EyeRegionTopology.caruncle_idxs[i];
		//	newVerts[idx1] = eye_regions_vs[idx2] + Vector3.forward*offset*caruncle_ws[i]; // + ns[idx1]*offset;
		//}

		for (int i=0; i<anterior_margin_idxs.Length; i++) {
			int idx1 = anterior_margin_idxs[i];
			int idx2 = EyeRegionTopology.interior_margin_idxs[i];
			int idx3 = EyeRegionTopology.interior_margin_outer_loop_idxs[i];
            //Debug.DrawRay(transform.TransformPoint(eye_regions_vs[idx2]), Vector3.Lerp(-eye_regions_vs[idx2].normalized, Vector3.forward, 0.8f));
            newVerts[idx1] = Vector3.Lerp(eye_regions_vs[idx2], eye_regions_vs[idx3], wetness_ws[i]); //
        }

        for (int i = 0; i < interior_margin_idxs.Length; i++)
        {
            int idx1 = interior_margin_idxs[i];
            int idx2 = eyeball_idxs[i];
            int idx3 = anterior_margin_idxs[i];
            Vector3 v = Vector3.Lerp(-eye_regions_vs[idx2].normalized, Vector3.forward, 0.8f) * 0.0005f;
            newVerts[idx1] = Vector3.Lerp(newVerts[idx2], newVerts[idx3], 0.5f) + v;
        }

        // finally set the new vertex positions
        mesh.vertices = newVerts;
		mesh.RecalculateNormals ();
	}

    Vector3 CastOntoEyeball(Vector3 point)
    {
        float shrinkwrap_distance = 1f;
        Vector3 dir = point.normalized;

        RaycastHit hit;
        if (Physics.Raycast(transform.TransformPoint(point) + dir * shrinkwrap_distance, -dir, out hit, Mathf.Infinity, 1 << 8)) {
            return (transform.worldToLocalMatrix * hit.point);
        }

        return Vector3.zero;
    }

    Vector3 CastOntoEyeball(Vector3 point, Vector3 dir)
    {
        return CastOntoEyeball(point, dir, 1000f);
    }

    Vector3 CastOntoEyeball(Vector3 point, float dist)
    {
        return CastOntoEyeball(point, point.normalized, dist);
    }

    Vector3 CastOntoEyeball(Vector3 point, Vector3 dir, float dist)
    {
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
}
