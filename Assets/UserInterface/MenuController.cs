using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using System.Linq;


public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenu;  // The "Settings Menu" GameObject in your Canvas
    [SerializeField] private Button menuButton;        // The "Menu Button" that will open/close the dialog
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
    //[Header("Camera Controls")]
    [Header("Camera Management")]
    // [SerializeField] private Button addCameraButton;       // "+" button for adding cameras
    // [SerializeField] private Button removeCameraButton;    // "-" button for removing cameras

    private int cameraCount = 1; // Tracks the number of cameras added (starts from 1 for Camera1)

    [Header("Camera Parameter Groups")]
    [SerializeField] private GameObject cameraGroup;
    [SerializeField] private RectTransform intrinsicsGroup;
    //[SerializeField] private InputField fxnput;

    [SerializeField] private Transform intrinsicsNoiseGroup;
    [SerializeField] private Transform extrinsicsGroup;
    [SerializeField] private Transform extrinsicsNoiseGroup;

    // Dictionary to store values for each group
    private Dictionary<string, Dictionary<string, float>> groupValues = new Dictionary<string, Dictionary<string, float>>();

    // References to input fields for each group
    private class InputFieldRefs
    {
        public InputField fx, fy, cx, cy, width, height;    // For intrinsics groups
        public InputField x, y, z, rx, ry, rz;             // For extrinsics groups
    }

    private Dictionary<string, InputFieldRefs> groupInputs = new Dictionary<string, InputFieldRefs>();

    [Header("Save Configuration")]
    [SerializeField] private Button saveButton;
    [SerializeField] private InputField outputPathField;
    [SerializeField] private InputField outputFolderField;
    [SerializeField] private InputField numSamplesField;
    //[SerializeField] private Toggle headlessModeToggle;

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
    //[SerializeField] private InputField outputPathField;
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
        settingsMenu = GameObject.Find("SettingsMenu"); // Find the GameObject by name
        cameraGroup = GameObject.Find("CameraGroup");
        intrinsicsGroup = transform.Find("IntrinsicsGroup").GetComponent<RectTransform>();

        menuButton = GameObject.Find("MenuButton").GetComponent<Button>(); // Find the Button component
        closeButton = GameObject.Find("ExitButton").GetComponent<Button>();
        // addCameraButton = GameObject.Find("AddCamera").GetComponent<Button>();
        // removeCameraButton = GameObject.Find("RemoveCamera").GetComponent<Button>();
        saveButton = GameObject.Find("SaveButton").GetComponent<Button>();


        InitializeCameraGroups();

        // Ensure the Settings Menu is hidden initially
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false);
        }

        // Hook up the button's onClick event to our ToggleMenu method
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(ToggleMenu);
        }
        // Add close button listener
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseMenu);
        }

        // Initialize camera buttons
        //if (addcamerabutton != null)
        //    addcamerabutton.onclick.addlistener(addcamera);
        //if (removecamerabutton != null)
        //    removeCameraButton.onClick.AddListener(RemoveCamera);

        // Initialize save button
        if (saveButton != null)
        {
            Debug.Log("Pressed this button");
            saveButton.onClick.AddListener(SaveConfiguration);
        }


        // All listeners in the menu
        if (projectNameField != null)
            projectNameField.onEndEdit.AddListener(OnProjectNameChanged);
        if (versionField != null)
            versionField.onEndEdit.AddListener(OnVersionChanged);
        if (repositoryURLField != null)
            repositoryURLField.onEndEdit.AddListener(OnRepositoryURLChanged);

        if (multiCameraToggle != null)
            multiCameraToggle.onValueChanged.AddListener(OnToggleMultiCamera);
        if (intrinsicsPathField != null)
            intrinsicsPathField.onEndEdit.AddListener(OnIntrinsicsPathChanged);
        if (extrinsicsPathField != null)
            extrinsicsPathField.onEndEdit.AddListener(OnExtrinsicsPathChanged);

        if (randomizeEyePoseToggle != null)
            randomizeEyePoseToggle.onValueChanged.AddListener(OnToggleEyePoseRandomization);
        if (distributionTypeField != null)
            distributionTypeField.onEndEdit.AddListener(OnDistributionTypeChanged);
        if (distributionParamsField != null)
            distributionParamsField.onEndEdit.AddListener(OnDistributionParamsChanged);

        if (enableMorphTargetsToggle != null)
            enableMorphTargetsToggle.onValueChanged.AddListener(OnToggleMorphTargets);
        if (facialBlendshapesField != null)
            facialBlendshapesField.onEndEdit.AddListener(OnFacialBlendshapesChanged);
        if (textureVariationsField != null)
            textureVariationsField.onEndEdit.AddListener(OnTextureVariationsChanged);

        if (randomLightingToggle != null)
            randomLightingToggle.onValueChanged.AddListener(OnToggleRandomLighting);
        if (lightingModeDropdown != null)
            lightingModeDropdown.onValueChanged.AddListener(OnLightingModeChanged);
        if (lightIntensitySlider != null)
            lightIntensitySlider.onValueChanged.AddListener(OnLightIntensityChanged);

        if (sampleCountField != null)
            sampleCountField.onEndEdit.AddListener(OnSampleCountChanged);
        if (headlessModeToggle != null)
            headlessModeToggle.onValueChanged.AddListener(OnToggleHeadlessMode);
        if (generateDatasetButton != null)
            generateDatasetButton.onClick.AddListener(OnGenerateDatasetClicked);

        if (outputPathField != null)
            outputPathField.onEndEdit.AddListener(OnOutputPathChanged);
        if (saveMetadataToggle != null)
            saveMetadataToggle.onValueChanged.AddListener(OnToggleSaveMetadata);
        if (annotationFormatDropdown != null)
            annotationFormatDropdown.onValueChanged.AddListener(OnAnnotationFormatChanged);

        if (showPreviewToggle != null)
            showPreviewToggle.onValueChanged.AddListener(OnTogglePreview);
        if (showProgressBarToggle != null)
            showProgressBarToggle.onValueChanged.AddListener(OnToggleProgressBar);
    }



    // This method toggles the menu's active state
    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;                  // flip the boolean
        settingsMenu.SetActive(isMenuOpen);        // show/hide the menu
        if (isMenuOpen)
        {
            // Restore saved values when opening menu
            RestoreInputValues();
        }
    }

    public void CloseMenu()
    {
        Debug.Log("I'm Here");
        isMenuOpen = false;
        settingsMenu.SetActive(false);
    }

    private void InitializeCameraGroups()
    {
        // Initialize dictionaries for each group
        groupValues["Intrinsics"] = new Dictionary<string, float>();
        groupValues["IntrinsicsNoise"] = new Dictionary<string, float>();
        groupValues["Extrinsics"] = new Dictionary<string, float>();
        groupValues["ExtrinsicsNoise"] = new Dictionary<string, float>();

        // Find and initialize Intrinsics group
        if (intrinsicsGroup != null)
        {
            groupInputs["Intrinsics"] = new InputFieldRefs
            {
                fx = intrinsicsGroup.Find("fxInput").GetComponent<InputField>(),
                fy = intrinsicsGroup.Find("fyInput").GetComponent<InputField>(),
                cx = intrinsicsGroup.Find("cxInput").GetComponent<InputField>(),
                cy = intrinsicsGroup.Find("cyInput").GetComponent<InputField>(),
                width = intrinsicsGroup.Find("widthInput").GetComponent<InputField>(),
                height = intrinsicsGroup.Find("heightInput").GetComponent<InputField>()
            };
            InitializeIntrinsicsListeners("Intrinsics");
        }

        // Find and initialize IntrinsicsNoise group
        if (intrinsicsNoiseGroup != null)
        {
            groupInputs["IntrinsicsNoise"] = new InputFieldRefs
            {
                fx = intrinsicsNoiseGroup.Find("fxInput").GetComponent<InputField>(),
                fy = intrinsicsNoiseGroup.Find("fyInput").GetComponent<InputField>(),
                cx = intrinsicsNoiseGroup.Find("cxInput").GetComponent<InputField>(),
                cy = intrinsicsNoiseGroup.Find("cyInput").GetComponent<InputField>(),
                width = intrinsicsNoiseGroup.Find("widthInput").GetComponent<InputField>(),
                height = intrinsicsNoiseGroup.Find("heightInput").GetComponent<InputField>()
            };
            InitializeIntrinsicsListeners("IntrinsicsNoise");
        }

        // Find and initialize Extrinsics group
        if (extrinsicsGroup != null)
        {
            groupInputs["Extrinsics"] = new InputFieldRefs
            {
                x = extrinsicsGroup.Find("xInput").GetComponent<InputField>(),
                y = extrinsicsGroup.Find("yInput").GetComponent<InputField>(),
                z = extrinsicsGroup.Find("zInput").GetComponent<InputField>(),
                rx = extrinsicsGroup.Find("rxInput").GetComponent<InputField>(),
                ry = extrinsicsGroup.Find("ryInput").GetComponent<InputField>(),
                rz = extrinsicsGroup.Find("rzInput").GetComponent<InputField>()
            };
            InitializeExtrinsicsListeners("Extrinsics");
        }

        // Find and initialize ExtrinsicsNoise group
        if (extrinsicsNoiseGroup != null)
        {
            groupInputs["ExtrinsicsNoise"] = new InputFieldRefs
            {
                x = extrinsicsNoiseGroup.Find("xInput").GetComponent<InputField>(),
                y = extrinsicsNoiseGroup.Find("yInput").GetComponent<InputField>(),
                z = extrinsicsNoiseGroup.Find("zInput").GetComponent<InputField>(),
                rx = extrinsicsNoiseGroup.Find("rxInput").GetComponent<InputField>(),
                ry = extrinsicsNoiseGroup.Find("ryInput").GetComponent<InputField>(),
                rz = extrinsicsNoiseGroup.Find("rzInput").GetComponent<InputField>()
            };
            InitializeExtrinsicsListeners("ExtrinsicsNoise");
        }

        // Initialize default values for all groups
        InitializeDefaultValues();
    }

    private void InitializeIntrinsicsListeners(string groupName)
    {
        var inputs = groupInputs[groupName];
        var values = groupValues[groupName];

        inputs.fx?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "fx", value));
        inputs.fy?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "fy", value));
        inputs.cx?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "cx", value));
        inputs.cy?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "cy", value));
        inputs.width?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "width", value));
        inputs.height?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "height", value));
    }

    private void InitializeExtrinsicsListeners(string groupName)
    {
        var inputs = groupInputs[groupName];
        var values = groupValues[groupName];

        inputs.x?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "x", value));
        inputs.y?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "y", value));
        inputs.z?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "z", value));
        inputs.rx?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "rx", value));
        inputs.ry?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "ry", value));
        inputs.rz?.onValueChanged.AddListener((value) => OnValueChanged(groupName, "rz", value));
    }


    private void InitializeDefaultValues()
    {
        // Initialize Intrinsics and IntrinsicsNoise default values
        foreach (string group in new[] { "Intrinsics", "IntrinsicsNoise" })
        {
            groupValues[group]["fx"] = 0f;
            groupValues[group]["fy"] = 0f;
            groupValues[group]["cx"] = 0f;
            groupValues[group]["cy"] = 0f;
            groupValues[group]["width"] = 0f;
            groupValues[group]["height"] = 0f;
        }

        // Initialize Extrinsics and ExtrinsicsNoise default values
        foreach (string group in new[] { "Extrinsics", "ExtrinsicsNoise" })
        {
            groupValues[group]["x"] = 0f;
            groupValues[group]["y"] = 0f;
            groupValues[group]["z"] = 0f;
            groupValues[group]["rx"] = 0f;
            groupValues[group]["ry"] = 0f;
            groupValues[group]["rz"] = 0f;
        }
    }

    private void OnValueChanged(string groupName, string fieldName, string value)
    {
        if (float.TryParse(value, out float result))
        {
            groupValues[groupName][fieldName] = result;

            // Log updated values
            Debug.Log($"=== Value Change Details ===");
            Debug.Log($"Group: {groupName}");
            Debug.Log($"Field: {fieldName}");
            Debug.Log($"New Value: {result}");

            Debug.Log("Current Group Values:");
            foreach (var kvp in groupValues[groupName])
            {
                Debug.Log($"{kvp.Key}: {kvp.Value}");
            }
            Debug.Log("========================");
        }
    }


    private void RestoreInputValues()
    {
        // Restore Intrinsics and IntrinsicsNoise values
        foreach (string group in new[] { "Intrinsics", "IntrinsicsNoise" })
        {
            if (groupInputs.ContainsKey(group))
            {
                var inputs = groupInputs[group];
                var values = groupValues[group];

                if (inputs.fx != null) inputs.fx.text = values["fx"].ToString();
                if (inputs.fy != null) inputs.fy.text = values["fy"].ToString();
                if (inputs.cx != null) inputs.cx.text = values["cx"].ToString();
                if (inputs.cy != null) inputs.cy.text = values["cy"].ToString();
                if (inputs.width != null) inputs.width.text = values["width"].ToString();
                if (inputs.height != null) inputs.height.text = values["height"].ToString();
            }
        }

        // Restore Extrinsics and ExtrinsicsNoise values
        foreach (string group in new[] { "Extrinsics", "ExtrinsicsNoise" })
        {
            if (groupInputs.ContainsKey(group))
            {
                var inputs = groupInputs[group];
                var values = groupValues[group];

                if (inputs.x != null) inputs.x.text = values["x"].ToString();
                if (inputs.y != null) inputs.y.text = values["y"].ToString();
                if (inputs.z != null) inputs.z.text = values["z"].ToString();
                if (inputs.rx != null) inputs.rx.text = values["rx"].ToString();
                if (inputs.ry != null) inputs.ry.text = values["ry"].ToString();
                if (inputs.rz != null) inputs.rz.text = values["rz"].ToString();
            }
        }
    }

    private void SaveConfiguration()
    {
        JSONNode rootNode = new JSONClass();

        // Add basic configuration
        rootNode.Add("outputPath", new JSONData(outputPathField?.text ?? "~/data/"));
        rootNode.Add("outputFolder", new JSONData(outputFolderField?.text ?? "EER_eye_data"));
        rootNode.Add("num_samples", new JSONData(int.Parse(numSamplesField?.text ?? "10000")));
        // rootNode.Add("headless", new JSONData(headlessModeToggle?.isOn ?? false));

        // Create cameras array
        JSONArray camerasArray = new JSONArray();
        rootNode.Add("cameras", camerasArray);

        // Add configuration for each camera
        for (int i = 0; i < cameraCount; i++)
        {
            JSONNode cameraNode = new JSONClass();
            cameraNode.Add("name", new JSONData($"cam{i}"));
            cameraNode.Add("noise_distribution", new JSONData("uniform"));

            // Add intrinsics
            JSONNode intrinsicsNode = new JSONClass();
            intrinsicsNode.Add("fx", new JSONData(groupValues["Intrinsics"]["fx"]));
            intrinsicsNode.Add("fy", new JSONData(groupValues["Intrinsics"]["fy"]));
            intrinsicsNode.Add("cx", new JSONData(groupValues["Intrinsics"]["cx"]));
            intrinsicsNode.Add("cy", new JSONData(groupValues["Intrinsics"]["cy"]));
            intrinsicsNode.Add("w", new JSONData(groupValues["Intrinsics"]["width"]));
            intrinsicsNode.Add("h", new JSONData(groupValues["Intrinsics"]["height"]));
            cameraNode.Add("intrinsics", intrinsicsNode);

            // Add intrinsics noise
            JSONNode intrinsicsNoiseNode = new JSONClass();
            intrinsicsNoiseNode.Add("fx", new JSONData(groupValues["IntrinsicsNoise"]["fx"]));
            intrinsicsNoiseNode.Add("fy", new JSONData(groupValues["IntrinsicsNoise"]["fy"]));
            intrinsicsNoiseNode.Add("cx", new JSONData(groupValues["IntrinsicsNoise"]["cx"]));
            intrinsicsNoiseNode.Add("cy", new JSONData(groupValues["IntrinsicsNoise"]["cy"]));
            intrinsicsNoiseNode.Add("w", new JSONData(groupValues["IntrinsicsNoise"]["width"]));
            intrinsicsNoiseNode.Add("h", new JSONData(groupValues["IntrinsicsNoise"]["height"]));
            cameraNode.Add("intrinsics_noise", intrinsicsNoiseNode);

            // Add extrinsics
            JSONNode extrinsicsNode = new JSONClass();
            extrinsicsNode.Add("x", new JSONData(groupValues["Extrinsics"]["x"]));
            extrinsicsNode.Add("y", new JSONData(groupValues["Extrinsics"]["y"]));
            extrinsicsNode.Add("z", new JSONData(groupValues["Extrinsics"]["z"]));
            extrinsicsNode.Add("rx", new JSONData(groupValues["Extrinsics"]["rx"]));
            extrinsicsNode.Add("ry", new JSONData(groupValues["Extrinsics"]["ry"]));
            extrinsicsNode.Add("rz", new JSONData(groupValues["Extrinsics"]["rz"]));
            cameraNode.Add("extrinsics", extrinsicsNode);

            // Add extrinsics noise
            JSONNode extrinsicsNoiseNode = new JSONClass();
            extrinsicsNoiseNode.Add("x", new JSONData(groupValues["ExtrinsicsNoise"]["x"]));
            extrinsicsNoiseNode.Add("y", new JSONData(groupValues["ExtrinsicsNoise"]["y"]));
            extrinsicsNoiseNode.Add("z", new JSONData(groupValues["ExtrinsicsNoise"]["z"]));
            extrinsicsNoiseNode.Add("rx", new JSONData(groupValues["ExtrinsicsNoise"]["rx"]));
            extrinsicsNoiseNode.Add("ry", new JSONData(groupValues["ExtrinsicsNoise"]["ry"]));
            extrinsicsNoiseNode.Add("rz", new JSONData(groupValues["ExtrinsicsNoise"]["rz"]));
            cameraNode.Add("extrinsics_noise", extrinsicsNoiseNode);

            camerasArray.Add(cameraNode);
        }

        // Save to file
        try
        {
            string path = Path.Combine(Application.dataPath, "this_is_my_config.json");
            File.WriteAllText(path, rootNode.ToJSON(4)); // Using indent of 4 for pretty printing
            Debug.Log($"Configuration saved successfully to: {path}");
            Debug.Log($"JSON Content:\n{rootNode.ToJSON(4)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving configuration: {e.Message}");
        }
    }





    // All methods to handle changes in the UI
    public void OnProjectNameChanged(string newName)
        {
            Debug.Log("[Dummy] Project Name changed to: " + newName);
        }

    public void OnVersionChanged(string newVersion)
    {
        Debug.Log("[Dummy] Version changed to: " + newVersion);
    }

    public void OnRepositoryURLChanged(string url)
    {
        Debug.Log("[Dummy] Repository URL changed to: " + url);
    }

    public void OnToggleMultiCamera(bool isOn)
    {
        Debug.Log("[Dummy] Multi-camera toggled: " + (isOn ? "On" : "Off"));
    }

    public void OnIntrinsicsPathChanged(string path)
    {
        Debug.Log("[Dummy] Intrinsics path changed: " + path);
    }

    public void OnExtrinsicsPathChanged(string path)
    {
        Debug.Log("[Dummy] Extrinsics path changed: " + path);
    }

    public void OnToggleEyePoseRandomization(bool isOn)
    {
        Debug.Log("[Dummy] Eye pose randomization toggled: " + (isOn ? "On" : "Off"));
    }

    public void OnDistributionTypeChanged(string newType)
    {
        Debug.Log("[Dummy] Distribution type changed: " + newType);
    }

    public void OnDistributionParamsChanged(string newParams)
    {
        Debug.Log("[Dummy] Distribution parameters changed: " + newParams);
    }

    public void OnToggleMorphTargets(bool isOn)
    {
        Debug.Log("[Dummy] Morph targets toggled: " + (isOn ? "On" : "Off"));
    }

    public void OnFacialBlendshapesChanged(string blendshapes)
    {
        Debug.Log("[Dummy] Facial blendshapes changed: " + blendshapes);
    }

    public void OnTextureVariationsChanged(string variations)
    {
        Debug.Log("[Dummy] Texture variations changed: " + variations);
    }

    public void OnToggleRandomLighting(bool isOn)
    {
        Debug.Log("[Dummy] Random lighting toggled: " + (isOn ? "On" : "Off"));
    }

    public void OnLightingModeChanged(int index)
    {
        Debug.Log("[Dummy] Lighting mode changed to index: " + index);
        // You could also do: lightingModeDropdown.options[index].text
        // to get the actual string label.
    }

    public void OnLightIntensityChanged(float value)
    {
        Debug.Log("[Dummy] Light intensity changed to: " + value);
    }

    public void OnSampleCountChanged(string count)
    {
        Debug.Log("[Dummy] Sample count changed: " + count);
    }

    public void OnToggleHeadlessMode(bool isOn)
    {
        Debug.Log("[Dummy] Headless mode toggled: " + (isOn ? "On" : "Off"));
    }

    public void OnGenerateDatasetClicked()
    {
        Debug.Log("[Dummy] Generate dataset button clicked.");
    }

    public void OnOutputPathChanged(string path)
    {
        Debug.Log("[Dummy] Output path changed: " + path);
    }

    public void OnToggleSaveMetadata(bool isOn)
    {
        Debug.Log("[Dummy] Save metadata toggled: " + (isOn ? "On" : "Off"));
    }

    public void OnAnnotationFormatChanged(int index)
    {
        Debug.Log("[Dummy] Annotation format changed to index: " + index);
        // e.g. annotationFormatDropdown.options[index].text
    }

    public void OnTogglePreview(bool isOn)
    {
        Debug.Log("[Dummy] Show preview toggled: " + (isOn ? "On" : "Off"));
    }

    public void OnToggleProgressBar(bool isOn)
    {
        Debug.Log("[Dummy] Show progress bar toggled: " + (isOn ? "On" : "Off"));
    }
}