using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class InputPcaCoeffScale : MonoBehaviour {

    EyeRegionPCA pca;

    void Start()
    {
        pca = GameObject.Find("eye_region").GetComponent<EyeRegionPCA>();

        InputField input = this.GetComponent<InputField>() as InputField;
        input.onEndEdit.AddListener(SubmitCameraParams);
    }

    private void SubmitCameraParams(string arg0)
    {
        pca.coeff_scale = float.Parse(arg0);
    }
}
