using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using SimpleJSON;

public class MenuControllerUploadJSON : MonoBehaviour
{
    [SerializeField] private MenuController menuController;

    // ---------------------------------------------------------
    // Single JSON Config: "Upload JSON" Button
    // ---------------------------------------------------------
    [Header("Global Config")]
    [SerializeField] private Button uploadJsonButton;    // Button labeled "Upload JSON"
    [SerializeField] private InputField jsonPathField;   // Optional: field for the user to type the JSON path

    // ---------------------------------------------------------
    // Example UI Fields (Dummy)
    // ---------------------------------------------------------
    [Header("Project Setup")]
    [SerializeField] private InputField projectNameField;
    [SerializeField] private InputField versionField;
    [SerializeField] private InputField repositoryURLField;

    [Header("Multi-Camera Settings")]
    [SerializeField] private Toggle multiCameraToggle;
    [SerializeField] private InputField intrinsicsPathField;
    [SerializeField] private InputField extrinsicsPathField;

    // ... (All your other UI fields, toggles, dropdowns, etc.) ...

    private void Start()
    {
        if (menuController == null)
        {
            menuController = FindObjectOfType<MenuController>();
            if (menuController == null)
            {
                Debug.LogError("MenuController not found. Upload JSON functionality won't work.");
            }
        }

        if (uploadJsonButton != null)
        {
            uploadJsonButton.onClick.AddListener(OnUploadJsonClicked);
        }
    }

    private void OnUploadJsonClicked()
    {
        string selectedPath = (jsonPathField != null && !string.IsNullOrEmpty(jsonPathField.text))
            ? jsonPathField.text
            : Path.Combine(Application.dataPath, "..", "upload_test.json");

        if (!File.Exists(selectedPath))
        {
            Debug.LogWarning("JSON file not found: " + selectedPath);
            return;
        }

        string fileContent = File.ReadAllText(selectedPath);
        JSONNode configData = JSON.Parse(fileContent);

        if (configData == null)
        {
            Debug.LogError("Failed to parse JSON file: " + selectedPath);
            return;
        }

        ApplyConfiguration(configData);
    }

    private void ApplyConfiguration(JSONNode configData)
    {
        if (menuController == null)
        {
            Debug.LogError("MenuController reference is missing. Cannot apply configuration.");
            return;
        }

        ApplyGeneralSettings(configData);
        ApplyCameraSettings(configData);
        ApplyLightSettings(configData);
        // ApplyEyeParameters(configData);

        Debug.Log("Configuration successfully loaded from JSON.");
    }

    private void ApplyGeneralSettings(JSONNode configData)
    {
        if (configData["outputPath"] != null && menuController.outputPathField != null)
        {
            menuController.outputPathField.text = configData["outputPath"];
        }

        if (configData["outputFolder"] != null && menuController.outputFolderField != null)
        {
            menuController.outputFolderField.text = configData["outputFolder"];
        }

        if (configData["num_samples"] != null && menuController.numSamplesField != null)
        {
            menuController.numSamplesField.text = configData["num_samples"].AsInt.ToString();
            menuController.OnSampleCountChanged(configData["num_samples"].AsInt.ToString());
        }

        if (configData["motion_center"] != null && menuController.motionCenterToggle != null)
        {
            menuController.motionCenterToggle.isOn = configData["motion_center"].AsInt > 0;
        }

        if (configData["camera_array_center"] != null && menuController.motionCenterInputFields.ContainsKey("CameraArrayCenter"))
        {
            JSONNode centerNode = configData["camera_array_center"];
            var centerRefs = menuController.motionCenterInputFields["CameraArrayCenter"];

            if (centerNode["x"] != null && centerRefs.x != null)
                centerRefs.x.text = centerNode["x"].AsFloat.ToString();

            if (centerNode["y"] != null && centerRefs.y != null)
                centerRefs.y.text = centerNode["y"].AsFloat.ToString();

            if (centerNode["z"] != null && centerRefs.z != null)
                centerRefs.z.text = centerNode["z"].AsFloat.ToString();

            if (centerNode["rx"] != null && centerRefs.rx != null)
                centerRefs.rx.text = centerNode["rx"].AsFloat.ToString();

            if (centerNode["ry"] != null && centerRefs.ry != null)
                centerRefs.ry.text = centerNode["ry"].AsFloat.ToString();

            if (centerNode["rz"] != null && centerRefs.rz != null)
                centerRefs.rz.text = centerNode["rz"].AsFloat.ToString();
        }

        if (configData["camera_array_center_noise"] != null && menuController.motionCenterInputFields.ContainsKey("CameraArrayCenterNoise"))
        {
            JSONNode noiseNode = configData["camera_array_center_noise"];
            var noiseRefs = menuController.motionCenterInputFields["CameraArrayCenterNoise"];

            if (noiseNode["x"] != null && noiseRefs.x != null)
                noiseRefs.x.text = noiseNode["x"].AsFloat.ToString();

            if (noiseNode["y"] != null && noiseRefs.y != null)
                noiseRefs.y.text = noiseNode["y"].AsFloat.ToString();

            if (noiseNode["z"] != null && noiseRefs.z != null)
                noiseRefs.z.text = noiseNode["z"].AsFloat.ToString();

            if (noiseNode["rx"] != null && noiseRefs.rx != null)
                noiseRefs.rx.text = noiseNode["rx"].AsFloat.ToString();

            if (noiseNode["ry"] != null && noiseRefs.ry != null)
                noiseRefs.ry.text = noiseNode["ry"].AsFloat.ToString();

            if (noiseNode["rz"] != null && noiseRefs.rz != null)
                noiseRefs.rz.text = noiseNode["rz"].AsFloat.ToString();
        }
    }

    private void ApplyCameraSettings(JSONNode configData)
    {
        JSONArray camerasArray = configData["cameras"].AsArray;
        if (camerasArray == null || camerasArray.Count == 0)
        {
            Debug.Log("No cameras found in configuration.");
            return;
        }

        while (menuController.addedCameras.Count > 1)
        {
            menuController.RemoveCamera();
        }

        ConfigureCamera(0, camerasArray[0]);

        for (int i = 1; i < camerasArray.Count; i++)
        {
            menuController.AddCamera();
            ConfigureCamera(i, camerasArray[i]);
        }
    }


    private void ConfigureCamera(int cameraIndex, JSONNode cameraData)
    {
        int cameraId = cameraIndex + 1;

        if (cameraData["intrinsics"] != null && menuController.cameraInputFields.ContainsKey(cameraId) &&
            menuController.cameraInputFields[cameraId].ContainsKey("Intrinsics"))
        {
            JSONNode intrinsics = cameraData["intrinsics"];
            var intrinsicsRefs = menuController.cameraInputFields[cameraId]["Intrinsics"];

            if (intrinsics["fx"] != null && intrinsicsRefs.fx != null)
                intrinsicsRefs.fx.text = intrinsics["fx"].AsFloat.ToString();

            if (intrinsics["fy"] != null && intrinsicsRefs.fy != null)
                intrinsicsRefs.fy.text = intrinsics["fy"].AsFloat.ToString();

            if (intrinsics["cx"] != null && intrinsicsRefs.cx != null)
                intrinsicsRefs.cx.text = intrinsics["cx"].AsFloat.ToString();

            if (intrinsics["cy"] != null && intrinsicsRefs.cy != null)
                intrinsicsRefs.cy.text = intrinsics["cy"].AsFloat.ToString();

            if (intrinsics["w"] != null && intrinsicsRefs.width != null)
                intrinsicsRefs.width.text = intrinsics["w"].AsFloat.ToString();

            if (intrinsics["h"] != null && intrinsicsRefs.height != null)
                intrinsicsRefs.height.text = intrinsics["h"].AsFloat.ToString();
        }

        if (cameraData["intrinsics_noise"] != null && menuController.cameraInputFields.ContainsKey(cameraId) &&
            menuController.cameraInputFields[cameraId].ContainsKey("IntrinsicsNoise"))
        {
            JSONNode intrinsicsNoise = cameraData["intrinsics_noise"];
            var intrinsicsNoiseRefs = menuController.cameraInputFields[cameraId]["IntrinsicsNoise"];

            if (intrinsicsNoise["fx"] != null && intrinsicsNoiseRefs.fx != null)
                intrinsicsNoiseRefs.fx.text = intrinsicsNoise["fx"].AsFloat.ToString();

            if (intrinsicsNoise["fy"] != null && intrinsicsNoiseRefs.fy != null)
                intrinsicsNoiseRefs.fy.text = intrinsicsNoise["fy"].AsFloat.ToString();

            if (intrinsicsNoise["cx"] != null && intrinsicsNoiseRefs.cx != null)
                intrinsicsNoiseRefs.cx.text = intrinsicsNoise["cx"].AsFloat.ToString();

            if (intrinsicsNoise["cy"] != null && intrinsicsNoiseRefs.cy != null)
                intrinsicsNoiseRefs.cy.text = intrinsicsNoise["cy"].AsFloat.ToString();

            if (intrinsicsNoise["w"] != null && intrinsicsNoiseRefs.width != null)
                intrinsicsNoiseRefs.width.text = intrinsicsNoise["w"].AsFloat.ToString();

            if (intrinsicsNoise["h"] != null && intrinsicsNoiseRefs.height != null)
                intrinsicsNoiseRefs.height.text = intrinsicsNoise["h"].AsFloat.ToString();
        }

        if (cameraData["extrinsics"] != null && menuController.cameraInputFields.ContainsKey(cameraId) &&
            menuController.cameraInputFields[cameraId].ContainsKey("Extrinsics"))
        {
            JSONNode extrinsics = cameraData["extrinsics"];
            var extrinsicsRefs = menuController.cameraInputFields[cameraId]["Extrinsics"];

            if (extrinsics["x"] != null && extrinsicsRefs.x != null)
                extrinsicsRefs.x.text = extrinsics["x"].AsFloat.ToString();

            if (extrinsics["y"] != null && extrinsicsRefs.y != null)
                extrinsicsRefs.y.text = extrinsics["y"].AsFloat.ToString();

            if (extrinsics["z"] != null && extrinsicsRefs.z != null)
                extrinsicsRefs.z.text = extrinsics["z"].AsFloat.ToString();

            if (extrinsics["rx"] != null && extrinsicsRefs.rx != null)
                extrinsicsRefs.rx.text = extrinsics["rx"].AsFloat.ToString();

            if (extrinsics["ry"] != null && extrinsicsRefs.ry != null)
                extrinsicsRefs.ry.text = extrinsics["ry"].AsFloat.ToString();

            if (extrinsics["rz"] != null && extrinsicsRefs.rz != null)
                extrinsicsRefs.rz.text = extrinsics["rz"].AsFloat.ToString();
        }

        if (cameraData["extrinsics_noise"] != null && menuController.cameraInputFields.ContainsKey(cameraId) &&
            menuController.cameraInputFields[cameraId].ContainsKey("ExtrinsicsNoise"))
        {
            JSONNode extrinsicsNoise = cameraData["extrinsics_noise"];
            var extrinsicsNoiseRefs = menuController.cameraInputFields[cameraId]["ExtrinsicsNoise"];

            if (extrinsicsNoise["x"] != null && extrinsicsNoiseRefs.x != null)
                extrinsicsNoiseRefs.x.text = extrinsicsNoise["x"].AsFloat.ToString();

            if (extrinsicsNoise["y"] != null && extrinsicsNoiseRefs.y != null)
                extrinsicsNoiseRefs.y.text = extrinsicsNoise["y"].AsFloat.ToString();

            if (extrinsicsNoise["z"] != null && extrinsicsNoiseRefs.z != null)
                extrinsicsNoiseRefs.z.text = extrinsicsNoise["z"].AsFloat.ToString();

            if (extrinsicsNoise["rx"] != null && extrinsicsNoiseRefs.rx != null)
                extrinsicsNoiseRefs.rx.text = extrinsicsNoise["rx"].AsFloat.ToString();

            if (extrinsicsNoise["ry"] != null && extrinsicsNoiseRefs.ry != null)
                extrinsicsNoiseRefs.ry.text = extrinsicsNoise["ry"].AsFloat.ToString();

            if (extrinsicsNoise["rz"] != null && extrinsicsNoiseRefs.rz != null)
                extrinsicsNoiseRefs.rz.text = extrinsicsNoise["rz"].AsFloat.ToString();
        }
    }

    private void ApplyLightSettings(JSONNode configData)
    {
        JSONArray lightsArray = configData["lights"].AsArray;
        if (lightsArray == null || lightsArray.Count == 0)
        {
            Debug.Log("No lights found in configuration.");
            return;
        }

        while (menuController.addedLights.Count > 0)
        {
            menuController.RemoveLight();
        }

        for (int i = 0; i < lightsArray.Count; i++)
        {
            menuController.AddLight();
            ConfigureLight(i + 1, lightsArray[i]); 
        }
    }

    private void ConfigureLight(int lightId, JSONNode lightData)
    {
        if (lightData["array_mounted"] != null && menuController.lightArrayMountedToggles.ContainsKey(lightId))
        {
            bool isArrayMounted = lightData["array_mounted"].AsInt > 0;
            menuController.lightArrayMountedToggles[lightId].isOn = isArrayMounted;
            menuController.lightArrayMountedStates[lightId] = isArrayMounted;
        }

        if (lightData["position"] != null && menuController.lightInputFields.ContainsKey(lightId) &&
            menuController.lightInputFields[lightId].ContainsKey("PositionGroupLight"))
        {
            JSONNode position = lightData["position"];
            var positionRefs = menuController.lightInputFields[lightId]["PositionGroupLight"];

            if (position["x"] != null && positionRefs.x != null)
                positionRefs.x.text = position["x"].AsFloat.ToString();

            if (position["y"] != null && positionRefs.y != null)
                positionRefs.y.text = position["y"].AsFloat.ToString();

            if (position["z"] != null && positionRefs.z != null)
                positionRefs.z.text = position["z"].AsFloat.ToString();

            if (position["rx"] != null && positionRefs.rx != null)
                positionRefs.rx.text = position["rx"].AsFloat.ToString();

            if (position["ry"] != null && positionRefs.ry != null)
                positionRefs.ry.text = position["ry"].AsFloat.ToString();

            if (position["rz"] != null && positionRefs.rz != null)
                positionRefs.rz.text = position["rz"].AsFloat.ToString();
        }

        if (lightData["position_noise"] != null && menuController.lightInputFields.ContainsKey(lightId) &&
            menuController.lightInputFields[lightId].ContainsKey("PositionNoiseGroup"))
        {
            JSONNode positionNoise = lightData["position_noise"];
            var positionNoiseRefs = menuController.lightInputFields[lightId]["PositionNoiseGroup"];

            if (positionNoise["x"] != null && positionNoiseRefs.x != null)
                positionNoiseRefs.x.text = positionNoise["x"].AsFloat.ToString();

            if (positionNoise["y"] != null && positionNoiseRefs.y != null)
                positionNoiseRefs.y.text = positionNoise["y"].AsFloat.ToString();

            if (positionNoise["z"] != null && positionNoiseRefs.z != null)
                positionNoiseRefs.z.text = positionNoise["z"].AsFloat.ToString();

            if (positionNoise["rx"] != null && positionNoiseRefs.rx != null)
                positionNoiseRefs.rx.text = positionNoise["rx"].AsFloat.ToString();

            if (positionNoise["ry"] != null && positionNoiseRefs.ry != null)
                positionNoiseRefs.ry.text = positionNoise["ry"].AsFloat.ToString();

            if (positionNoise["rz"] != null && positionNoiseRefs.rz != null)
                positionNoiseRefs.rz.text = positionNoise["rz"].AsFloat.ToString();
        }

        if (lightData["properties"] != null && menuController.lightPropertyFields.ContainsKey(lightId))
        {
            JSONNode properties = lightData["properties"];
            var propertyRefs = menuController.lightPropertyFields[lightId];

            if (properties["range"] != null && propertyRefs.range != null)
                propertyRefs.range.text = properties["range"].AsFloat.ToString();

            if (properties["intensity"] != null && propertyRefs.intensity != null)
                propertyRefs.intensity.text = properties["intensity"].AsFloat.ToString();

            if (properties["color"] != null)
            {
                JSONNode color = properties["color"];

                if (color["r"] != null && propertyRefs.colorR != null)
                    propertyRefs.colorR.text = color["r"].AsFloat.ToString();

                if (color["g"] != null && propertyRefs.colorG != null)
                    propertyRefs.colorG.text = color["g"].AsFloat.ToString();

                if (color["b"] != null && propertyRefs.colorB != null)
                    propertyRefs.colorB.text = color["b"].AsFloat.ToString();
            }
        }
    }
}






/*
    /// <summary>
    /// Called when the user clicks the "Upload JSON" button at the top of the menu.
    /// In a real implementation, you'd open a file picker or read from 'jsonPathField.text'.
    /// </summary>
    private void OnUploadJsonClicked()
    {
        // 1) Obtain the path to the JSON file
        //    - If you have a file browser, set 'selectedPath' from there,
        //      or simply read it from jsonPathField.text (if you provide that).
        string selectedPath = (jsonPathField != null) ? jsonPathField.text : "DummyPath.json";

        // 2) Check if file exists (dummy check)
        if (!File.Exists(selectedPath))
        {
            Debug.LogWarning("[Dummy] JSON file not found: " + selectedPath);
            return;
        }

        // 3) In real code: read & parse the JSON
        //    string fileContent = File.ReadAllText(selectedPath);
        //    MyConfigData configData = JsonUtility.FromJson<MyConfigData>(fileContent);

        Debug.Log("[Dummy] Upload JSON clicked. Pretending to parse file at: " + selectedPath);

        // 4) Populate UI from the (dummy) configData.
        //    For example, if your configData has 'projectName', 'version', etc.:

        // projectNameField.text = configData.projectName;
        // versionField.text      = configData.version;
        // repositoryURLField.text= configData.repositoryURL;
        // multiCameraToggle.isOn = configData.multiCameraEnabled;
        // intrinsicsPathField.text  = configData.intrinsicsPath;
        // extrinsicsPathField.text  = configData.extrinsicsPath;
        // ... etc.

        // Currently, we'll just do a simple message:
        Debug.Log("[Dummy] Successfully 'loaded' config from JSON (placeholder).");
    }
 */