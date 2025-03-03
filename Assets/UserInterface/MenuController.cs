using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using System.Linq;
using TMPro;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button closeButton;

    // ---------------------------------------------------------
    // Scrollable Area
    // ---------------------------------------------------------
    [Header("Scrollable Area")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform scrollContent;

    // ---------------------------------------------------------
    // Project Setup Fields
    // ---------------------------------------------------------
    [Header("Project Setup")]
    [SerializeField] private InputField projectNameField;
    [SerializeField] private InputField versionField;
    [SerializeField] private InputField repositoryURLField;

    // ---------------------------------------------------------
    // Camera Controls
    // ---------------------------------------------------------
    [Header("Camera Management")]
    [SerializeField] private Button addCameraButton;
    [SerializeField] private Button removeCameraButton;

    private int cameraCount = 0;

    [Header("Camera Parameter Groups")]
    [SerializeField] private GameObject cameraGroup;
    [SerializeField] private RectTransform cameraContainer;
    [SerializeField] private GameObject cameraGroupPrefab;
    [SerializeField] private RectTransform intrinsicsGroup;

    [SerializeField] private Transform intrinsicsNoiseGroup;
    [SerializeField] private Transform extrinsicsGroup;
    [SerializeField] private Transform extrinsicsNoiseGroup;

    private Dictionary<int, Dictionary<string, Dictionary<string, float>>> cameraGroupValues =
        new Dictionary<int, Dictionary<string, Dictionary<string, float>>>();

    private class InputFieldRefs
    {
        public TMP_InputField fx, fy, cx, cy, width, height;
        public TMP_InputField x, y, z, rx, ry, rz;
    }

    private Dictionary<int, Dictionary<string, InputFieldRefs>> cameraInputFields =
        new Dictionary<int, Dictionary<string, InputFieldRefs>>();

    [Header("Save Configuration")]
    [SerializeField] private Button saveButton;
    [SerializeField] private InputField outputPathField;
    [SerializeField] private InputField outputFolderField;
    [SerializeField] private InputField numSamplesField;

    // ---------------------------------------------------------
    // Multi-Camera Support
    // ---------------------------------------------------------
    [Header("Multi-Camera Settings")]
    [SerializeField] public Toggle multiCameraToggle;
    [SerializeField] private InputField intrinsicsPathField;
    [SerializeField] private InputField extrinsicsPathField;

    // ---------------------------------------------------------
    // Parameter Distribution
    // ---------------------------------------------------------
    [Header("Parameter Distribution")]
    [SerializeField] private Toggle randomizeEyePoseToggle;
    [SerializeField] private InputField distributionTypeField;
    [SerializeField] private InputField distributionParamsField;

    // ---------------------------------------------------------
    // Face & Eye Appearance
    // ---------------------------------------------------------
    [Header("Face & Eye Appearance")]
    [SerializeField] private Toggle enableMorphTargetsToggle;
    [SerializeField] private InputField facialBlendshapesField;
    [SerializeField] private InputField textureVariationsField;

    // ---------------------------------------------------------
    // Environment & Lighting
    // ---------------------------------------------------------
    [Header("Environment & Lighting")]
    [SerializeField] private Toggle randomLightingToggle;
    [SerializeField] private Dropdown lightingModeDropdown;
    [SerializeField] private Slider lightIntensitySlider;

    // ---------------------------------------------------------
    // Dataset Generation
    // ---------------------------------------------------------
    [Header("Dataset Generation")]
    [SerializeField] private InputField sampleCountField;
    [SerializeField] private Toggle headlessModeToggle;
    [SerializeField] private Button generateDatasetButton;

    // ---------------------------------------------------------
    // Ground Truth & Output
    // ---------------------------------------------------------
    [Header("Ground Truth & Output")]
    [SerializeField] private Toggle saveMetadataToggle;
    [SerializeField] private Dropdown annotationFormatDropdown;

    // ---------------------------------------------------------
    // GUI Enhancements
    // ---------------------------------------------------------
    [Header("GUI Enhancements")]
    [SerializeField] private Toggle showPreviewToggle;
    [SerializeField] private Toggle showProgressBarToggle;

    // We track whether the menu is open or not
    private bool isMenuOpen = false;

    private List<GameObject> addedCameras = new List<GameObject>();

    private void Start()
    {
        settingsMenu = GameObject.Find("SettingsMenu");
        cameraGroup = GameObject.Find("CameraGroup");

        cameraContainer = GameObject.Find("CameraContainer").GetComponent<RectTransform>();

        cameraGroupPrefab = Resources.Load<GameObject>("Prefabs/CameraGroup");

        menuButton = GameObject.Find("MenuButton").GetComponent<Button>();
        closeButton = GameObject.Find("ExitButton").GetComponent<Button>();
        addCameraButton = GameObject.Find("AddCamera").GetComponent<Button>();
        removeCameraButton = GameObject.Find("RemoveCamera").GetComponent<Button>();
        saveButton = GameObject.Find("SaveButton").GetComponent<Button>();

        // Initialize the UI
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false);
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(ToggleMenu);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseMenu);
        }

        if (addCameraButton != null)
        {
            Debug.Log("Adding listener to Add Camera button");
            addCameraButton.onClick.AddListener(AddCamera);
        }

        if (removeCameraButton != null)
        {
            Debug.Log("Adding listener to Remove Camera button");
            removeCameraButton.onClick.AddListener(RemoveCamera);
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveConfiguration);
        }

        // Add listeners for all UI elements
        SetupUIListeners();

        // Add the first camera
        if (addedCameras.Count == 0)
        {
            AddCamera();
        }
    }

    private void SetupUIListeners()
    {
        // Project setup fields
        if (projectNameField != null)
            projectNameField.onEndEdit.AddListener(OnProjectNameChanged);
        if (versionField != null)
            versionField.onEndEdit.AddListener(OnVersionChanged);
        if (repositoryURLField != null)
            repositoryURLField.onEndEdit.AddListener(OnRepositoryURLChanged);

        // Multi-camera settings
        if (multiCameraToggle != null)
            multiCameraToggle.onValueChanged.AddListener(OnToggleMultiCamera);
        if (intrinsicsPathField != null)
            intrinsicsPathField.onEndEdit.AddListener(OnIntrinsicsPathChanged);
        if (extrinsicsPathField != null)
            extrinsicsPathField.onEndEdit.AddListener(OnExtrinsicsPathChanged);

        // Parameter distribution
        if (randomizeEyePoseToggle != null)
            randomizeEyePoseToggle.onValueChanged.AddListener(OnToggleEyePoseRandomization);
        if (distributionTypeField != null)
            distributionTypeField.onEndEdit.AddListener(OnDistributionTypeChanged);
        if (distributionParamsField != null)
            distributionParamsField.onEndEdit.AddListener(OnDistributionParamsChanged);

        // Face & Eye Appearance
        if (enableMorphTargetsToggle != null)
            enableMorphTargetsToggle.onValueChanged.AddListener(OnToggleMorphTargets);
        if (facialBlendshapesField != null)
            facialBlendshapesField.onEndEdit.AddListener(OnFacialBlendshapesChanged);
        if (textureVariationsField != null)
            textureVariationsField.onEndEdit.AddListener(OnTextureVariationsChanged);

        // Environment & Lighting
        if (randomLightingToggle != null)
            randomLightingToggle.onValueChanged.AddListener(OnToggleRandomLighting);
        if (lightingModeDropdown != null)
            lightingModeDropdown.onValueChanged.AddListener(OnLightingModeChanged);
        if (lightIntensitySlider != null)
            lightIntensitySlider.onValueChanged.AddListener(OnLightIntensityChanged);

        // Dataset Generation
        if (sampleCountField != null)
            sampleCountField.onEndEdit.AddListener(OnSampleCountChanged);
        if (headlessModeToggle != null)
            headlessModeToggle.onValueChanged.AddListener(OnToggleHeadlessMode);
        if (generateDatasetButton != null)
            generateDatasetButton.onClick.AddListener(OnGenerateDatasetClicked);

        // Ground Truth & Output
        if (outputPathField != null)
            outputPathField.onEndEdit.AddListener(OnOutputPathChanged);
        if (saveMetadataToggle != null)
            saveMetadataToggle.onValueChanged.AddListener(OnToggleSaveMetadata);
        if (annotationFormatDropdown != null)
            annotationFormatDropdown.onValueChanged.AddListener(OnAnnotationFormatChanged);

        // GUI Enhancements
        if (showPreviewToggle != null)
            showPreviewToggle.onValueChanged.AddListener(OnTogglePreview);
        if (showProgressBarToggle != null)
            showProgressBarToggle.onValueChanged.AddListener(OnToggleProgressBar);
    }

    // Event handler implementations
    private void OnProjectNameChanged(string value) { }
    private void OnVersionChanged(string value) { }
    private void OnRepositoryURLChanged(string value) { }
    private void OnToggleMultiCamera(bool value) { }
    private void OnIntrinsicsPathChanged(string value) { }
    private void OnExtrinsicsPathChanged(string value) { }
    private void OnToggleEyePoseRandomization(bool value) { }
    private void OnDistributionTypeChanged(string value) { }
    private void OnDistributionParamsChanged(string value) { }
    private void OnToggleMorphTargets(bool value) { }
    private void OnFacialBlendshapesChanged(string value) { }
    private void OnTextureVariationsChanged(string value) { }
    private void OnToggleRandomLighting(bool value) { }
    private void OnLightingModeChanged(int value) { }
    private void OnLightIntensityChanged(float value) { }
    private void OnSampleCountChanged(string value) { }
    private void OnToggleHeadlessMode(bool value) { }
    private void OnGenerateDatasetClicked() { }
    private void OnOutputPathChanged(string value) { }
    private void OnToggleSaveMetadata(bool value) { }
    private void OnAnnotationFormatChanged(int value) { }
    private void OnTogglePreview(bool value) { }
    private void OnToggleProgressBar(bool value) { }

    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        settingsMenu.SetActive(isMenuOpen);
        if (isMenuOpen)
        {
            // Restore saved values when opening menu
            RestoreInputValues();
        }
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        settingsMenu.SetActive(false);
    }

    public void AddCamera()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(cameraContainer);

        if (cameraGroupPrefab == null)
        {
            Debug.LogError("CameraGroup prefab is null! Please assign it in the Inspector or make sure it exists in Resources/Prefabs.");
            return;
        }

        cameraCount++;

        InitializeCameraValues(cameraCount);
        GameObject newCameraGroup = Instantiate(cameraGroupPrefab, cameraContainer);
        newCameraGroup.name = $"CameraGroup_{cameraCount}";

        InitializeCameraInputFields(newCameraGroup, cameraCount);

        addedCameras.Add(newCameraGroup);

        UpdateCameraLabels();

        UpdateButtonStates();

        Debug.Log($"Added camera group: {newCameraGroup.name}");
    }

    private void InitializeCameraValues(int cameraId)
    {

        cameraGroupValues[cameraId] = new Dictionary<string, Dictionary<string, float>>();
        cameraInputFields[cameraId] = new Dictionary<string, InputFieldRefs>();

        cameraGroupValues[cameraId]["Intrinsics"] = new Dictionary<string, float>();
        cameraGroupValues[cameraId]["IntrinsicsNoise"] = new Dictionary<string, float>();
        cameraGroupValues[cameraId]["Extrinsics"] = new Dictionary<string, float>();
        cameraGroupValues[cameraId]["ExtrinsicsNoise"] = new Dictionary<string, float>();

        // Initialize Intrinsics and IntrinsicsNoise values
        foreach (string group in new[] { "Intrinsics", "IntrinsicsNoise" })
        {
            cameraGroupValues[cameraId][group]["fx"] = 0f;
            cameraGroupValues[cameraId][group]["fy"] = 0f;
            cameraGroupValues[cameraId][group]["cx"] = 0f;
            cameraGroupValues[cameraId][group]["cy"] = 0f;
            cameraGroupValues[cameraId][group]["width"] = 0f;
            cameraGroupValues[cameraId][group]["height"] = 0f;
        }

        // Initialize Extrinsics and ExtrinsicsNoise values
        foreach (string group in new[] { "Extrinsics", "ExtrinsicsNoise" })
        {
            cameraGroupValues[cameraId][group]["x"] = 0f;
            cameraGroupValues[cameraId][group]["y"] = 0f;
            cameraGroupValues[cameraId][group]["z"] = 0f;
            cameraGroupValues[cameraId][group]["rx"] = 0f;
            cameraGroupValues[cameraId][group]["ry"] = 0f;
            cameraGroupValues[cameraId][group]["rz"] = 0f;
        }
    }

    private void InitializeCameraInputFields(GameObject cameraGroup, int cameraId)
    {
        cameraInputFields[cameraId]["Intrinsics"] = new InputFieldRefs();
        cameraInputFields[cameraId]["IntrinsicsNoise"] = new InputFieldRefs();
        cameraInputFields[cameraId]["Extrinsics"] = new InputFieldRefs();
        cameraInputFields[cameraId]["ExtrinsicsNoise"] = new InputFieldRefs();

        TMP_InputField[] inputFields = cameraGroup.GetComponentsInChildren<TMP_InputField>(true);

        foreach (TMP_InputField inputField in inputFields)
        {
            string fieldName = inputField.name.Replace("Input", "").ToLower();
            string groupName = DetermineGroupName(inputField.transform);

            if (!string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(fieldName))
            {
                StoreInputFieldReference(cameraId, groupName, fieldName, inputField);

                SetupInputFieldListener(inputField, cameraId, groupName, fieldName);

                if (cameraGroupValues.ContainsKey(cameraId) &&
                    cameraGroupValues[cameraId].ContainsKey(groupName) &&
                    cameraGroupValues[cameraId][groupName].ContainsKey(fieldName))
                {
                    inputField.text = cameraGroupValues[cameraId][groupName][fieldName].ToString();
                }
            }
        }
    }

    private void StoreInputFieldReference(int cameraId, string groupName, string fieldName, TMP_InputField inputField)
    {
        if (!cameraInputFields.ContainsKey(cameraId) || !cameraInputFields[cameraId].ContainsKey(groupName))
            return;

        var refs = cameraInputFields[cameraId][groupName];

        switch (fieldName)
        {
            case "fx": refs.fx = inputField; break;
            case "fy": refs.fy = inputField; break;
            case "cx": refs.cx = inputField; break;
            case "cy": refs.cy = inputField; break;
            case "width": refs.width = inputField; break;
            case "height": refs.height = inputField; break;
            case "x": refs.x = inputField; break;
            case "y": refs.y = inputField; break;
            case "z": refs.z = inputField; break;
            case "rx": refs.rx = inputField; break;
            case "ry": refs.ry = inputField; break;
            case "rz": refs.rz = inputField; break;
        }
    }

    private string DetermineGroupName(Transform inputFieldTransform)
    {
        Transform parent = inputFieldTransform.parent;

        while (parent != null)
        {
            if (parent.name.Contains("Intrinsics") && !parent.name.Contains("Noise"))
                return "Intrinsics";
            if (parent.name.Contains("IntrinsicsNoise"))
                return "IntrinsicsNoise";
            if (parent.name.Contains("Extrinsics") && !parent.name.Contains("Noise"))
                return "Extrinsics";
            if (parent.name.Contains("ExtrinsicsNoise"))
                return "ExtrinsicsNoise";

            parent = parent.parent;
        }

        return "";
    }

    private void SetupInputFieldListener(TMP_InputField inputField, int cameraId, string groupName, string fieldName)
    {
        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();

        inputField.onValueChanged.AddListener((value) => {
            OnValueChanged(cameraId, groupName, fieldName, value);
        });

        inputField.onEndEdit.AddListener((value) => {
            OnValueChanged(cameraId, groupName, fieldName, value);
        });
    }

    private void OnValueChanged(int cameraId, string groupName, string fieldName, string value)
    {
        if (!cameraGroupValues.ContainsKey(cameraId))
        {
            Debug.LogError($"Camera ID {cameraId} not found, initializing it");
            InitializeCameraValues(cameraId);
        }

        if (!cameraGroupValues[cameraId].ContainsKey(groupName))
        {
            Debug.LogError($"Group '{groupName}' not found for Camera {cameraId}");
            cameraGroupValues[cameraId][groupName] = new Dictionary<string, float>();
        }

        if (string.IsNullOrEmpty(value) || !float.TryParse(value, out float parsedValue))
        {
            Debug.LogWarning($"Could not parse '{value}' as float for Camera {cameraId}.{groupName}.{fieldName}, using 0");
            parsedValue = 0f;
        }

        cameraGroupValues[cameraId][groupName][fieldName] = parsedValue;

        Debug.Log($"Updated Camera {cameraId}.{groupName}.{fieldName} to {parsedValue}");
    }

    public void RemoveCamera()
    {
        if (addedCameras.Count <= 1)
        {
            Debug.Log("Cannot remove the last camera group");
            return;
        }

        GameObject lastCamera = addedCameras[addedCameras.Count - 1];
        addedCameras.RemoveAt(addedCameras.Count - 1);

        if (cameraGroupValues.ContainsKey(cameraCount))
        {
            cameraGroupValues.Remove(cameraCount);
            cameraInputFields.Remove(cameraCount);
        }

        Destroy(lastCamera);
        cameraCount--;

        UpdateCameraLabels();

        UpdateButtonStates();

        Debug.Log($"Removed camera group. Remaining: {addedCameras.Count}");
    }

    private void UpdateCameraLabels()
    {
        for (int i = 0; i < addedCameras.Count; i++)
        {
            Text cameraLabel = addedCameras[i].GetComponentInChildren<Text>();
            if (cameraLabel != null)
            {
                cameraLabel.text = $"Camera {i}";
            }
        }
    }

    private void UpdateButtonStates()
    {
        if (removeCameraButton != null)
        {
            removeCameraButton.interactable = (addedCameras.Count > 1);
        }
    }

    private void RestoreInputValues()
    {
        // Restore values for each camera
        foreach (var cameraEntry in cameraGroupValues)
        {
            int cameraId = cameraEntry.Key;

            // Skip if we don't have input field references for this camera
            if (!cameraInputFields.ContainsKey(cameraId))
                continue;

            foreach (var groupEntry in cameraEntry.Value)
            {
                string groupName = groupEntry.Key;

                // Skip if we don't have input field references for this group
                if (!cameraInputFields[cameraId].ContainsKey(groupName))
                    continue;

                var inputRefs = cameraInputFields[cameraId][groupName];
                var values = groupEntry.Value;

                // Restore intrinsics values
                if (groupName.Contains("Intrinsics"))
                {
                    if (inputRefs.fx != null && values.ContainsKey("fx"))
                        inputRefs.fx.text = values["fx"].ToString();

                    if (inputRefs.fy != null && values.ContainsKey("fy"))
                        inputRefs.fy.text = values["fy"].ToString();

                    if (inputRefs.cx != null && values.ContainsKey("cx"))
                        inputRefs.cx.text = values["cx"].ToString();

                    if (inputRefs.cy != null && values.ContainsKey("cy"))
                        inputRefs.cy.text = values["cy"].ToString();

                    if (inputRefs.width != null && values.ContainsKey("width"))
                        inputRefs.width.text = values["width"].ToString();

                    if (inputRefs.height != null && values.ContainsKey("height"))
                        inputRefs.height.text = values["height"].ToString();
                }
                // Restore extrinsics values
                else if (groupName.Contains("Extrinsics"))
                {
                    if (inputRefs.x != null && values.ContainsKey("x"))
                        inputRefs.x.text = values["x"].ToString();

                    if (inputRefs.y != null && values.ContainsKey("y"))
                        inputRefs.y.text = values["y"].ToString();

                    if (inputRefs.z != null && values.ContainsKey("z"))
                        inputRefs.z.text = values["z"].ToString();

                    if (inputRefs.rx != null && values.ContainsKey("rx"))
                        inputRefs.rx.text = values["rx"].ToString();

                    if (inputRefs.ry != null && values.ContainsKey("ry"))
                        inputRefs.ry.text = values["ry"].ToString();

                    if (inputRefs.rz != null && values.ContainsKey("rz"))
                        inputRefs.rz.text = values["rz"].ToString();
                }
            }
        }
    }

    private void SaveConfiguration()
    {
        JSONNode rootNode = new JSONClass();

        rootNode.Add("outputPath", new JSONData(outputPathField?.text ?? "~/data/"));
        rootNode.Add("outputFolder", new JSONData(outputFolderField?.text ?? "EER_eye_data"));
        rootNode.Add("num_samples", new JSONData(int.Parse(numSamplesField?.text ?? "10000")));

        // Create cameras array
        JSONArray camerasArray = new JSONArray();
        rootNode.Add("cameras", camerasArray);

        // Add configuration for each camera
        for (int i = 1; i <= cameraCount; i++)
        {
            if (!cameraGroupValues.ContainsKey(i))
                continue;

            JSONNode cameraNode = new JSONClass();
            cameraNode.Add("name", new JSONData($"cam{i - 1}"));
            cameraNode.Add("noise_distribution", new JSONData("uniform"));

            var cameraValues = cameraGroupValues[i];

            // Add intrinsics
            JSONNode intrinsicsNode = new JSONClass();
            if (cameraValues.ContainsKey("Intrinsics"))
            {
                var intrinsics = cameraValues["Intrinsics"];
                intrinsicsNode.Add("fx", new JSONData(intrinsics.ContainsKey("fx") ? intrinsics["fx"] : 0f));
                intrinsicsNode.Add("fy", new JSONData(intrinsics.ContainsKey("fy") ? intrinsics["fy"] : 0f));
                intrinsicsNode.Add("cx", new JSONData(intrinsics.ContainsKey("cx") ? intrinsics["cx"] : 0f));
                intrinsicsNode.Add("cy", new JSONData(intrinsics.ContainsKey("cy") ? intrinsics["cy"] : 0f));
                intrinsicsNode.Add("w", new JSONData(intrinsics.ContainsKey("width") ? intrinsics["width"] : 0f));
                intrinsicsNode.Add("h", new JSONData(intrinsics.ContainsKey("height") ? intrinsics["height"] : 0f));
            }
            cameraNode.Add("intrinsics", intrinsicsNode);

            // Add intrinsics noise
            JSONNode intrinsicsNoiseNode = new JSONClass();
            if (cameraValues.ContainsKey("IntrinsicsNoise"))
            {
                var intrinsicsNoise = cameraValues["IntrinsicsNoise"];
                intrinsicsNoiseNode.Add("fx", new JSONData(intrinsicsNoise.ContainsKey("fx") ? intrinsicsNoise["fx"] : 0f));
                intrinsicsNoiseNode.Add("fy", new JSONData(intrinsicsNoise.ContainsKey("fy") ? intrinsicsNoise["fy"] : 0f));
                intrinsicsNoiseNode.Add("cx", new JSONData(intrinsicsNoise.ContainsKey("cx") ? intrinsicsNoise["cx"] : 0f));
                intrinsicsNoiseNode.Add("cy", new JSONData(intrinsicsNoise.ContainsKey("cy") ? intrinsicsNoise["cy"] : 0f));
                intrinsicsNoiseNode.Add("w", new JSONData(intrinsicsNoise.ContainsKey("width") ? intrinsicsNoise["width"] : 0f));
                intrinsicsNoiseNode.Add("h", new JSONData(intrinsicsNoise.ContainsKey("height") ? intrinsicsNoise["height"] : 0f));
            }
            cameraNode.Add("intrinsics_noise", intrinsicsNoiseNode);

            // Add extrinsics
            JSONNode extrinsicsNode = new JSONClass();
            if (cameraValues.ContainsKey("Extrinsics"))
            {
                var extrinsics = cameraValues["Extrinsics"];
                extrinsicsNode.Add("x", new JSONData(extrinsics.ContainsKey("x") ? extrinsics["x"] : 0f));
                extrinsicsNode.Add("y", new JSONData(extrinsics.ContainsKey("y") ? extrinsics["y"] : 0f));
                extrinsicsNode.Add("z", new JSONData(extrinsics.ContainsKey("z") ? extrinsics["z"] : 0f));
                extrinsicsNode.Add("rx", new JSONData(extrinsics.ContainsKey("rx") ? extrinsics["rx"] : 0f));
                extrinsicsNode.Add("ry", new JSONData(extrinsics.ContainsKey("ry") ? extrinsics["ry"] : 0f));
                extrinsicsNode.Add("rz", new JSONData(extrinsics.ContainsKey("rz") ? extrinsics["rz"] : 0f));
            }
            cameraNode.Add("extrinsics", extrinsicsNode);

            // Add extrinsics noise
            JSONNode extrinsicsNoiseNode = new JSONClass();
            if (cameraValues.ContainsKey("ExtrinsicsNoise"))
            {
                var extrinsicsNoise = cameraValues["ExtrinsicsNoise"];
                extrinsicsNoiseNode.Add("x", new JSONData(extrinsicsNoise.ContainsKey("x") ? extrinsicsNoise["x"] : 0f));
                extrinsicsNoiseNode.Add("y", new JSONData(extrinsicsNoise.ContainsKey("y") ? extrinsicsNoise["y"] : 0f));
                extrinsicsNoiseNode.Add("z", new JSONData(extrinsicsNoise.ContainsKey("z") ? extrinsicsNoise["z"] : 0f));
                extrinsicsNoiseNode.Add("rx", new JSONData(extrinsicsNoise.ContainsKey("rx") ? extrinsicsNoise["rx"] : 0f));
                extrinsicsNoiseNode.Add("ry", new JSONData(extrinsicsNoise.ContainsKey("ry") ? extrinsicsNoise["ry"] : 0f));
                extrinsicsNoiseNode.Add("rz", new JSONData(extrinsicsNoise.ContainsKey("rz") ? extrinsicsNoise["rz"] : 0f));
            }
            cameraNode.Add("extrinsics_noise", extrinsicsNoiseNode);

            camerasArray.Add(cameraNode);
        }

        // Save to file
        try
        {
            string path = Path.Combine(Application.dataPath, "..", "this_is_my_config.json");
            File.WriteAllText(path, rootNode.ToJSON(4)); // Using indent of 4 for pretty printing
            Debug.Log($"Configuration saved successfully to: {path}");
            Debug.Log($"JSON Content:\n{rootNode.ToJSON(4)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving configuration: {e.Message}");
        }
    }

    private Transform FindChildByName(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform childTransform = parent.GetChild(i);

            if (childTransform.name == childName)
                return childTransform;

            Transform result = FindChildByName(childTransform, childName);
            if (result != null)
                return result;
        }

        return null;
    }
}
