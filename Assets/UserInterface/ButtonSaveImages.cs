using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ButtonSaveImages : MonoBehaviour {

    SynthesEyesServer synthesEyesServer;

    // Use this for initialization
    void Start () {

        synthesEyesServer = GameObject.Find("SynthesEyes").GetComponent<SynthesEyesServer>();

        Button buttonInput = this.GetComponent<Button>() as Button;
        buttonInput.onClick.AddListener(StartSavingData);

    }

    void StartSavingData()
    {
        synthesEyesServer.ResetFrameCounter();
        synthesEyesServer.isSavingData = true;

        GameObject.Find("GUI Canvas").GetComponent<Canvas>().enabled = false;
    }
}
