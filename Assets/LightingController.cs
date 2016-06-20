using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;

public class LightingController : MonoBehaviour {

    public bool manuallyRandomizeLighting = false;  // Can manually randomize lighting in editor

    List<Cubemap> envTexs = new List<Cubemap>(); 	// Set of HDR environments used for lighting

    private Light directionalLight;					// Directional light for hard shadows

	private ReflectionProbe reflectionProbe;        // Reflection probe for better eye-reflections

    public int envTexSwitchFrequency = 300;			// How often to switch environments
    private int lightingChangeTicks = 0;            // The number of times the lighting has been changed

	void Start () {

		// load all HDR environments
		foreach (Cubemap c in Resources.LoadAll ("Skies", typeof(Cubemap)))
			envTexs.Add(c);

        // initialize game objects
		directionalLight = GameObject.Find ("directional_light").GetComponent<Light> ();
		reflectionProbe = GameObject.Find ("reflection_probe").GetComponent<ReflectionProbe>();

		// initially randomize appearance
		RandomizeLighting ();
		reflectionProbe.RenderProbe ();
	}

    void Update()
    {
        if (manuallyRandomizeLighting) RandomizeLighting();  
    }

	public void RandomizeLighting ()
    {

        lightingChangeTicks++;

        // If enough frames have passed, switch the environment texture
        if (lightingChangeTicks % envTexSwitchFrequency == 0) {
			int randomEnvIdx = Random.Range (0, envTexs.Count);
			RenderSettings.skybox.SetTexture ("_Tex", envTexs [randomEnvIdx]);
			RenderSettings.skybox.SetFloat ("_Exposure", Random.Range(1.0f, 1.2f));
            RenderSettings.skybox.SetFloat("_Rotation", Random.Range(0, 360));
            DynamicGI.UpdateEnvironment();
		}

        // randomize light color
        Color defaultLightColor = new Color(236f / 255f, 248f / 255f, 1f);
        HSBColor lightColor = new HSBColor(defaultLightColor);
        lightColor.h = Random.value;
        directionalLight.color = lightColor.ToColor();

        // randomize light direction and intensity
        Vector3 lightDirection = -SyntheseyesUtils.RandomVec(-10, 90, -90, 90);
        directionalLight.transform.LookAt(directionalLight.transform.position + lightDirection);
		directionalLight.intensity = Random.Range(0.6f, 1.2f);

        // randomly vary environment intensity
		RenderSettings.ambientIntensity = Random.Range (0.8f, 1.2f);

        // re-render reflection probe for correct reflections
		reflectionProbe.RenderProbe ();
	}

    public JSONNode GetLightingDetails()
    {

        JSONNode lightingNode = new JSONClass();

        // first output environmental lighting information
        lightingNode.Add("skybox_texture", RenderSettings.skybox.GetTexture("_Tex").name);
        lightingNode.Add("skybox_exposure", RenderSettings.skybox.GetFloat("_Exposure").ToString());
        lightingNode.Add("skybox_rotation", RenderSettings.skybox.GetFloat("_Rotation").ToString());
        lightingNode.Add("ambient_intensity", RenderSettings.ambientIntensity.ToString());

        // then output directional light details
        lightingNode.Add("light_rotation", directionalLight.transform.rotation.eulerAngles.ToString());
        lightingNode.Add("light_intensity", directionalLight.intensity.ToString());

        return lightingNode;
    }
}
