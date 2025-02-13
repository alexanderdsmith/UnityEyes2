using UnityEngine;
using System.Collections;

public class SyntheseyesUtils : MonoBehaviour {

	public static Vector3 RandomVec(float thtMin, float thtMax, float phiMin, float phiMax){

		Vector3 retVec = Vector3.forward;
		retVec = Quaternion.AngleAxis (Random.Range (thtMin, thtMax), Vector3.left) * retVec;
		retVec = Quaternion.AngleAxis (Random.Range (phiMin, phiMax), Vector3.up) * retVec;
		return retVec;
	}

	public static Vector3 RandomVec(float thtMinMax, float phiMinMax){
		return RandomVec (-thtMinMax/2f, thtMinMax/2f, -phiMinMax/2f, phiMinMax/2f);
	}

	public static float NextGaussianDouble() {
		float u, v, S;
		
		do {
			u = 2.0f * Random.value - 1.0f;
			v = 2.0f * Random.value - 1.0f;
			S = u * u + v * v;
		}
		while (S >= 1.0f);
		
		float fac = Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
		return u * fac;
	}

    public static Vector3 CastOntoEyeball(Vector3 point)
    {
        return CastOntoEyeball(point, 0f);
    }

    public static Vector3 CastOntoEyeball(Vector3 point, Vector3 dir)
    {
        return CastOntoEyeball(point, dir, 0f);
    }

    public static Vector3 CastOntoEyeball(Vector3 point, float dist)
    {
        return CastOntoEyeball(point, point.normalized, dist);
    }

    public static Vector3 CastOntoEyeball(Vector3 point, Vector3 dir, float dist)
    {
        float shrinkwrap_distance = 1f;


        RaycastHit hit;
        if (Physics.Raycast(point + dir * shrinkwrap_distance, -dir, out hit, Mathf.Infinity, 1 << 8))
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
