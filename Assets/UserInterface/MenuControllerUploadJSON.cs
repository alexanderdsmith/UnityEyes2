using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using SimpleJSON;

#if UNITY_STANDALONE || UNITY_EDITOR
using SFB;
#endif

public class MenuControllerUploadJSON : MonoBehaviour
{
    [SerializeField] private MenuController menuController;

    // ---------------------------------------------------------
    // Single JSON Config: "Upload JSON" Button
    // ---------------------------------------------------------
    [Header("Global Config")]
    [SerializeField] private Button uploadJsonButton;    
    [SerializeField] private InputField jsonPathField;  

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


    /**
     * Initializes the JSON upload controller.
     *
     * Connects to the main MenuController instance and sets up the listener for the "Upload JSON" button.
     * Verifies button references and prepares the handler for loading configuration files.
     */
    private void Start()
    {
        Debug.Log("MenuControllerUploadJSON Start() called");

        if (menuController == null)
        {
            menuController = FindFirstObjectByType<MenuController>();
            if (menuController == null)
            {
                Debug.LogError("MenuController not found. Upload JSON functionality won't work.");
            }
        }

        if (uploadJsonButton == null)
        {
            Debug.Log("Looking for Upload JSON button...");
            uploadJsonButton = GameObject.Find("Upload Button")?.GetComponent<Button>();

            if (uploadJsonButton == null)
            {
                Debug.LogError("Upload JSON button reference is still null after attempting to find it.");
            }
            else
            {
                Debug.Log("Upload JSON button found through GameObject.Find()");
            }
        }

        if (uploadJsonButton != null)
        {
            Debug.Log("Upload JSON button found, attaching listener");
            uploadJsonButton.onClick.RemoveAllListeners();
            uploadJsonButton.onClick.AddListener(OnUploadJsonClicked);
        }
        else
        {
            Debug.LogError("Upload JSON button reference is null. Please assign it in the Inspector.");
        }

    }


    /**
     * Handles the "Upload JSON" button click.
     *
     * Opens a file browser to let the user select a JSON configuration file.
     * Parses the file and triggers application of its contents to the UI and system.
     */
    private void OnUploadJsonClicked()
    {
        string selectedPath = "";

        #if UNITY_EDITOR
            selectedPath = UnityEditor.EditorUtility.OpenFilePanel("Select JSON Config", "", "json");
        #elif UNITY_STANDALONE_OSX
            selectedPath = MacNativeFileBrowser.OpenFilePanel("Select JSON Config", "", "json", false);
        #else
            string[] paths = SFB.StandaloneFileBrowser.OpenFilePanel("Select JSON Config", "", "json", false);
            if (paths.Length > 0)
                selectedPath = paths[0];
        #endif

        if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath))
        {
            Debug.Log("Selected JSON path: " + selectedPath);
            string fileContent = File.ReadAllText(selectedPath);
            JSONNode configData = JSON.Parse(fileContent);

            if (configData == null)
            {
                Debug.LogError("Failed to parse JSON file: " + selectedPath);
                return;
            }

            ApplyConfiguration(configData);
        }
        else
        {
            Debug.LogWarning("No file selected or file doesn't exist.");
        }
    }


    /**
     * Applies a full system configuration from a parsed JSON node.
     *
     * @param {JSONNode} configData - Parsed JSON object containing all configuration fields.
     *
     * Delegates setup to helper functions for general, camera, light, and eye parameter settings.
     */
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
        ApplyEyeParameters(configData);

        Debug.Log("Configuration successfully loaded from JSON.");
    }


    /**
     * Applies general project-wide configuration settings.
     *
     * @param {JSONNode} configData - JSON containing fields like outputPath, sample count, and motion center.
     *
     * Updates output path, number of samples, and camera array motion center fields in the UI.
     */
    private void ApplyGeneralSettings(JSONNode configData)
    {
        if (configData["outputPath"] != null && menuController.outputPathTMP != null)
        {
            string outputPath = configData["outputPath"];
            // Expand '~' if needed
            if (outputPath.StartsWith("~"))
            {
                string remainder = outputPath.Substring(1).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                outputPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), remainder);
            }
            menuController.outputPathTMP.text = outputPath;
            menuController.OnOutputPathChanged(outputPath);
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


    /**
     * Applies all camera configuration settings from the loaded JSON.
     *
     * @param {JSONNode} configData - JSON array containing per-camera intrinsics, extrinsics, and noise values.
     *
     * Dynamically configures the UI for each camera and populates the field values accordingly.
     */
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


    /**
     * Configures an individual camera's parameters from JSON data.
     *
     * @param {int} cameraIndex - Zero-based index of the camera in the configuration array.
     * @param {JSONNode} cameraData - JSON data for the specific camera's intrinsics, extrinsics, and noise.
     *
     * Populates corresponding input fields in the UI with parsed configuration values.
     */
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


    /**
     * Applies light group settings from the loaded JSON configuration.
     *
     * @param {JSONNode} configData - JSON array containing light positioning, noise, and color properties.
     *
     * Clears existing lights, recreates each light group, and populates values from the file.
     */
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


    /**
     * Configures an individual light's parameters from JSON data.
     *
     * @param {int} lightId - 1-based ID for the light group in the UI.
     * @param {JSONNode} lightData - JSON object with position, noise, and lighting properties.
     *
     * Sets toggles and fields for light positioning, color, and array-mounted state.
     */
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


    /**
     * Applies eye appearance and gaze-related parameters from JSON configuration.
     *
     * @param {JSONNode} configData - JSON object containing ranges for pupil/iris size and default gaze settings.
     *
     * Updates the relevant UI fields and internal data for eye modeling and animation.
     */
    private void ApplyEyeParameters(JSONNode configData)
    {
        if (configData["eye_parameters"] == null)
        {
            Debug.Log("No eye parameters found in configuration.");
            return;
        }

        JSONNode eyeParams = configData["eye_parameters"];

        if (eyeParams["pupil_size_range"] != null &&
            menuController.eyeSizeInputFields.ContainsKey("PupilSize"))
        {
            JSONNode pupilSizeRange = eyeParams["pupil_size_range"];
            var pupilSizeFields = menuController.eyeSizeInputFields["PupilSize"];

            if (pupilSizeRange["min"] != null && pupilSizeFields.min != null)
            {
                float minValue = pupilSizeRange["min"].AsFloat;
                pupilSizeFields.min.text = minValue.ToString("0.0");
                menuController.eyeParameterValues["PupilSize"]["min"] = minValue;
            }

            if (pupilSizeRange["max"] != null && pupilSizeFields.max != null)
            {
                float maxValue = pupilSizeRange["max"].AsFloat;
                pupilSizeFields.max.text = maxValue.ToString("0.0");
                menuController.eyeParameterValues["PupilSize"]["max"] = maxValue;
            }
        }

        if (eyeParams["iris_size_range"] != null &&
            menuController.eyeSizeInputFields.ContainsKey("IrisSize"))
        {
            JSONNode irisSizeRange = eyeParams["iris_size_range"];
            var irisSizeFields = menuController.eyeSizeInputFields["IrisSize"];

            if (irisSizeRange["min"] != null && irisSizeFields.min != null)
            {
                float minValue = irisSizeRange["min"].AsFloat;
                irisSizeFields.min.text = minValue.ToString("0.0");
                menuController.eyeParameterValues["IrisSize"]["min"] = minValue;
            }

            if (irisSizeRange["max"] != null && irisSizeFields.max != null)
            {
                float maxValue = irisSizeRange["max"].AsFloat;
                irisSizeFields.max.text = maxValue.ToString("0.0");
                menuController.eyeParameterValues["IrisSize"]["max"] = maxValue;
            }
        }

        if (menuController.eyeParamInputFields.ContainsKey("EyeProperties"))
        {
            var eyePropertyFields = menuController.eyeParamInputFields["EyeProperties"];

            if (eyeParams["default_pitch"] != null && eyePropertyFields.rx != null)
            {
                float pitchValue = eyeParams["default_pitch"].AsFloat;
                eyePropertyFields.rx.text = pitchValue.ToString("0.0");
                menuController.eyeParameterValues["EyeProperties"]["pitch"] = pitchValue;
            }

            if (eyeParams["default_yaw"] != null && eyePropertyFields.ry != null)
            {
                float yawValue = eyeParams["default_yaw"].AsFloat;
                eyePropertyFields.ry.text = yawValue.ToString("0.0");
                menuController.eyeParameterValues["EyeProperties"]["yaw"] = yawValue;
            }

            if (eyeParams["pitch_noise"] != null && eyePropertyFields.x != null)
            {
                float pitchNoiseValue = eyeParams["pitch_noise"].AsFloat;
                eyePropertyFields.x.text = pitchNoiseValue.ToString("0.0");
                menuController.eyeParameterValues["EyeProperties"]["pitchnoise"] = pitchNoiseValue;
            }

            if (eyeParams["yaw_noise"] != null && eyePropertyFields.y != null)
            {
                float yawNoiseValue = eyeParams["yaw_noise"].AsFloat;
                eyePropertyFields.y.text = yawNoiseValue.ToString("0.0");
                menuController.eyeParameterValues["EyeProperties"]["yawnoise"] = yawNoiseValue;
            }
        }

        Debug.Log("Eye parameters successfully loaded from JSON.");
    }
}
