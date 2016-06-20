using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class InputCameraParams : MonoBehaviour {

	SynthesEyesServer synthesEyesServer;

	void Start () {

		synthesEyesServer = GameObject.Find ("SynthesEyes").GetComponent<SynthesEyesServer>();

		InputField input = this.GetComponent<InputField>() as InputField;
		input.onEndEdit.AddListener(SubmitCameraParams); 
	}
	
	private void SubmitCameraParams(string arg0)
	{
		string[] args = arg0.Split (',');
		synthesEyesServer.defaultCameraPitch = float.Parse (args[0]);
		synthesEyesServer.defaultCameraYaw =   float.Parse (args[1]);
		synthesEyesServer.cameraPitchNoise =   float.Parse (args[2]);
		synthesEyesServer.cameraYawNoise =     float.Parse (args[3]);
	}
}
