using UnityEngine;
using System.Collections;

public class EyeRegionTopology : MonoBehaviour {

	public static int[] interior_margin_idxs =            {12, 13, 14, 15, 16, 17,  18, 132, 131, 130, 6, 2, 1, 7,  8,  0 };
    public static int[] interior_margin_outer_loop_idxs = {27, 24, 23, 22, 21, 20,  19,  93, 112, 113, 5, 4, 3, 9, 10, 11 };
    public static int[] caruncle_idxs = {28, 27, 25, 32, 31, 30, 33};

	public static int[,] loops = {
		{ 33,  32,  25,  13,  14, 15, 16, 17, 18, 132, 131, 130,  6,  2,  1,   7,  8,  0, 28,  30}, // interior margin + caruncle
		{ 35,  36,  26,  24,  23, 22, 21, 20, 19,  93, 112, 113,  5,  4,  3,   9, 10, 11, 29,  34}, // anterior margin
		{ 83,  82,  77,  62,  61, 60, 37, 38, 39,  92, 139, 136, 40, 41, 42,  43, 44, 45, 46,  84}, // eyelid "peak"
		{ 86,  85,  78,  72,  71, 70, 47, 48, 49, 135, 141, 140, 53, 54, 55,  56, 57, 58, 59,  87},
		{ 80,  79,  76,  75,  74, 73, 50, 51, 52, 137, 138, 142, 63, 64, 65,  66, 67, 68, 69,  81}, // crease
		{101, 102, 103, 104, 105, 88, 89, 90, 91, 146, 145, 143, 94, 95, 96, 217, 97, 98, 99, 100}
	};

	public static int[] iris_idxs = {3, 7, 11, 14, 18, 21, 25, 29, 33, 37, 41, 45, 49,
		52, 56, 60, 64, 68, 72, 76, 80, 84, 88, 92, 96, 100, 104, 108, 112, 116, 120, 124};

	public static float middleness(int idx){
	
		if (idx <= 5) {
			return 1.0f - (float)(5 - idx) / 5.0f;
		} else if (idx <= 10) {
			return (float) (10 - idx) / 4.0f;
		} else if (idx <= 14) {
			return 1.0f - (float) (14 - idx) / 4.0f;
		} else {
			return (float) (20 - idx) / 5.0f;
		}

	}

    public static int[,] eyebrow_idxs = {
        {160, 161, 109, 166, 167, 168},
		{157, 156, 108, 144, 147, 153}
	};

    public static float[] eyebrow_weights = { 0.5f, 0.5f, 0.5f, 0.5f, 0.75f, 1f };

    public static Vector3[] getEyebrowLdmks(Vector3[] vs) {

        Vector3[] ldmks = new Vector3[eyebrow_weights.Length];
        for (int i = 0; i < eyebrow_weights.Length; i++) {
            ldmks[i] = Vector3.Lerp(vs[eyebrow_idxs[0, i]], vs[eyebrow_idxs[1, i]], eyebrow_weights[i]);
        }

        return ldmks;
    }

}
