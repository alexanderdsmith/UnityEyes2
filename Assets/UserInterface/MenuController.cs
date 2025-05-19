using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using System.Linq;
using TMPro;

#if UNITY_STANDALONE || UNITY_EDITOR
using SFB;
#endif

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button closeButton;

    // ---------------------------------------------------------
    // Server Reference
    // ---------------------------------------------------------
    [Header("Reference to Server")]
    [SerializeField] private SynthesEyesServer synthesEyesServer;

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

    [Header("Output Location")]
    [SerializeField] private Button outputLocationButton;

    // ---------------------------------------------------------
    // Motion Center
    // ---------------------------------------------------------
    [Header("Motion Center")]
    [SerializeField] public Toggle motionCenterToggle;
    [SerializeField] private GameObject motionCenterGroup;
    [SerializeField] private RectTransform cameraArrayCenterGroup;
    [SerializeField] private RectTransform cameraArrayCenterNoiseGroup;

    // Dictionaries to store values and input field references
    public Dictionary<string, Dictionary<string, float>> motionCenterValues =
        new Dictionary<string, Dictionary<string, float>>();
    public Dictionary<string, InputFieldRefs> motionCenterInputFields =
        new Dictionary<string, InputFieldRefs>();

    // ---------------------------------------------------------
    // Camera Controls
    // ---------------------------------------------------------
    [Header("Camera Management")]
    [SerializeField] private Button addCameraButton;
    [SerializeField] private Button removeCameraButton;

    // ---------------------------------------------------------
    // Help Popup
    // ---------------------------------------------------------
    [Header("Help")]
    [SerializeField] private Button helpButton;

    private int cameraCount = 0;

    [Header("Camera Parameter Groups")]
    [SerializeField] private GameObject cameraGroup;
    [SerializeField] private RectTransform cameraContainer;
    [SerializeField] private GameObject cameraGroupPrefab;
    [SerializeField] private RectTransform intrinsicsGroup;

    [SerializeField] private Transform intrinsicsNoiseGroup;
    [SerializeField] private Transform extrinsicsGroup;
    [SerializeField] private Transform extrinsicsNoiseGroup;

    public Dictionary<int, Dictionary<string, Dictionary<string, float>>> cameraGroupValues =
        new Dictionary<int, Dictionary<string, Dictionary<string, float>>>();

    public class InputFieldRefs
    {
        public TMP_InputField fx, fy, cx, cy, width, height;
        public TMP_InputField x, y, z, rx, ry, rz;
    }

    public Dictionary<int, Dictionary<string, InputFieldRefs>> cameraInputFields =
        new Dictionary<int, Dictionary<string, InputFieldRefs>>();

    [Header("Save Configuration")]
    [SerializeField] private Button saveButton;
    [SerializeField] public TMP_Text outputPathTMP;
    [SerializeField] public TMP_InputField outputFolderNameTMP;
    [SerializeField] public TMP_InputField numSamplesField;

    // ---------------------------------------------------------
    // Multi-Camera Support
    // ---------------------------------------------------------
    [Header("Multi-Camera Settings")]
    [SerializeField] public Toggle multiCameraToggle;
    [SerializeField] private InputField intrinsicsPathField;
    [SerializeField] private InputField extrinsicsPathField;

    // ---------------------------------------------------------
    // Light Controls
    // ---------------------------------------------------------
    [Header("Light Management")]
    [SerializeField] private Button addLightButton;
    [SerializeField] private Button removeLightButton;

    private int lightCount = 0;

    [Header("Light Parameter Groups")]
    [SerializeField] private RectTransform lightContainer;
    [SerializeField] private GameObject lightGroupPrefab;

    public List<GameObject> addedLights = new List<GameObject>();

    public Dictionary<int, Dictionary<string, Dictionary<string, float>>> lightGroupValues =
        new Dictionary<int, Dictionary<string, Dictionary<string, float>>>();

    public Dictionary<int, Dictionary<string, InputFieldRefs>> lightInputFields =
        new Dictionary<int, Dictionary<string, InputFieldRefs>>();

    public Dictionary<int, Toggle> lightArrayMountedToggles = new Dictionary<int, Toggle>();

    public class LightPropertyRefs
    {
        public TMP_InputField range, intensity;
        public TMP_InputField colorR, colorG, colorB;
    }

    public Dictionary<int, LightPropertyRefs> lightPropertyFields = new Dictionary<int, LightPropertyRefs>();

    // ---------------------------------------------------------
    // Face & Eye Appearance
    // ---------------------------------------------------------
    [Header("Face & Eye Appearance")]
    [SerializeField] private Toggle enableMorphTargetsToggle;
    [SerializeField] private InputField facialBlendshapesField;
    [SerializeField] private InputField textureVariationsField;

    public Dictionary<string, Dictionary<string, float>> eyeParameterValues =
        new Dictionary<string, Dictionary<string, float>>();

    public class MinMaxInputRefs
    {
        public TMP_InputField min, max;
    }

    public Dictionary<string, MinMaxInputRefs> eyeSizeInputFields =
        new Dictionary<string, MinMaxInputRefs>();
    public Dictionary<string, InputFieldRefs> eyeParamInputFields =
        new Dictionary<string, InputFieldRefs>();

    [SerializeField] private GameObject eyeParamGroup;

    // ---------------------------------------------------------
    // Parameter Distribution
    // ---------------------------------------------------------
    [Header("Parameter Distribution")]
    [SerializeField] private Toggle randomizeEyePoseToggle;
    [SerializeField] private InputField distributionTypeField;
    [SerializeField] private InputField distributionParamsField;

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
    private int sampleCount = 10000;

    public List<GameObject> addedCameras = new List<GameObject>();
    public Dictionary<int, bool> lightArrayMountedStates = new Dictionary<int, bool>();


    /**
     * Initialize the main menu UI and all dynamic UI groups.
     *
     * Loads UI elements, sets default values, adds event listeners, and initializes camera/light/eye parameter containers.
     * Also connects to the SynthesEyesServer and populates default state.
     */
    private void Start()
    {
        settingsMenu = GameObject.Find("SettingsMenu");

        cameraGroup = GameObject.Find("CameraGroup");

        cameraContainer = GameObject.Find("CameraContainer").GetComponent<RectTransform>();
        cameraGroupPrefab = Resources.Load<GameObject>("Prefabs/CameraGroup");

        lightContainer = GameObject.Find("LightContainer").GetComponent<RectTransform>();
        lightGroupPrefab = Resources.Load<GameObject>("Prefabs/LightGroup");

        menuButton = GameObject.Find("MenuButton").GetComponent<Button>();
        closeButton = GameObject.Find("ExitButton").GetComponent<Button>();
        addCameraButton = GameObject.Find("AddCamera").GetComponent<Button>();
        removeCameraButton = GameObject.Find("RemoveCamera").GetComponent<Button>();
        addLightButton = GameObject.Find("AddLight").GetComponent<Button>();
        removeLightButton = GameObject.Find("RemoveLight").GetComponent<Button>();
        saveButton = GameObject.Find("SaveButton").GetComponent<Button>();
        helpButton = GameObject.Find("HelpButton").GetComponent<Button>();

        motionCenterToggle = GameObject.Find("MotionCenterToggle").GetComponent<Toggle>();
        motionCenterGroup = GameObject.Find("MotionCenter");
        cameraArrayCenterGroup = GameObject.Find("CameraArrayCenterGroup").GetComponent<RectTransform>();
        cameraArrayCenterNoiseGroup = GameObject.Find("CameraArrayCenterNoiseGroup").GetComponent<RectTransform>();

        eyeParamGroup = GameObject.Find("EyeParamGroup");

        numSamplesField = GameObject.Find("SampleCount").GetComponent<TMP_InputField>();
        outputPathTMP = GameObject.Find("OutputPathTMP")?.GetComponent<TMP_Text>();
        outputFolderNameTMP = GameObject.Find("OutputFolderName")?.GetComponent<TMP_InputField>();

        if (outputPathTMP != null && !string.IsNullOrEmpty(outputPathTMP.text) && synthesEyesServer != null)
        {
            outputPathTMP.text = "";
            OnOutputPathChanged(outputPathTMP.text);
        }

        if (outputFolderNameTMP != null)
        {
            outputFolderNameTMP.onEndEdit.AddListener(OnOutputFolderNameChanged);
        }

        if (numSamplesField != null)
        {
            numSamplesField.text = "1000";
            numSamplesField.text = sampleCount.ToString();
        }

        if (outputLocationButton == null)
        {
            outputLocationButton = GameObject.Find("Output Location Button")?.GetComponent<Button>();
            if (outputLocationButton != null)
            {
                outputLocationButton.onClick.AddListener(SelectOutputFolder);
            }
        }

        if (motionCenterToggle != null)
        {
            motionCenterToggle.onValueChanged.AddListener(OnToggleMotionCenter);

            motionCenterToggle.isOn = false;
            SetMotionCenterFieldsInteractable(false);
        }

        InitializeMotionCenterValues();
        InitializeMotionCenterInputFields();

        SetMotionCenterFieldsInteractable(motionCenterToggle.isOn);

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

        if (addLightButton != null)
        {
            Debug.Log("Adding listener to Add Light button");
            addLightButton.onClick.AddListener(AddLight);
        }

        if (removeLightButton != null)
        {
            Debug.Log("Adding listener to Remove Light button");
            removeLightButton.onClick.AddListener(RemoveLight);
        }

        if (removeLightButton != null)
        {
            removeLightButton.interactable = false;
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveConfiguration);
        }

        if (helpButton != null)
        {
            helpButton.onClick.AddListener(OpenHelpUrl);
        }

        if (synthesEyesServer == null)
        {
            synthesEyesServer = FindFirstObjectByType<SynthesEyesServer>();
            if (synthesEyesServer == null)
            {
                Debug.LogWarning("SynthesEyesServer not found. Automatic rendering updates won't work.");
            }
        }

        SetupUIListeners();

        if (addedCameras.Count == 0)
        {
            AddCamera();
        }

        InitializeEyeParameterValues();
        InitializeEyeParameterInputFields();
        ApplyEyeParameterValuesToUI();
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
        if (generateDatasetButton != null)
            generateDatasetButton.onClick.AddListener(OnGenerateDatasetClicked);

        // Ground Truth & Output
        if (saveMetadataToggle != null)
            saveMetadataToggle.onValueChanged.AddListener(OnToggleSaveMetadata);
        if (annotationFormatDropdown != null)
            annotationFormatDropdown.onValueChanged.AddListener(OnAnnotationFormatChanged);

        // GUI Enhancements
        if (showPreviewToggle != null)
            showPreviewToggle.onValueChanged.AddListener(OnTogglePreview);
        if (showProgressBarToggle != null)
            showProgressBarToggle.onValueChanged.AddListener(OnToggleProgressBar);

        if (numSamplesField != null)
            numSamplesField.onEndEdit.AddListener(OnSampleCountChanged);
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
    private void OnToggleHeadlessMode(bool value) { }
    private void OnGenerateDatasetClicked() { }
    private void OnToggleSaveMetadata(bool value) { }
    private void OnAnnotationFormatChanged(int value) { }
    private void OnTogglePreview(bool value) { }
    private void OnToggleProgressBar(bool value) { }


    /**
     * Opens a file browser dialog to allow the user to select an output folder.
     *
     * Updates the output path text and notifies the SynthesEyesServer to update its path reference.
     */
    public void SelectOutputFolder()
    {
        string selectedPath = "";
        #if UNITY_EDITOR
            selectedPath = UnityEditor.EditorUtility.OpenFolderPanel("Select Output Folder", "", "");
        #else
            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                selectedPath = MacNativeFileBrowser.OpenFolderPanel("Select Output Folder", "");
            }
            else
            {
                string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Output Folder", "", false);
                if (paths.Length > 0)
                    selectedPath = paths[0];
            }
        #endif

        // If no folder was selected, simply return rather than updating anything.
        if (string.IsNullOrEmpty(selectedPath) || !Directory.Exists(selectedPath))
        {
            Debug.LogWarning("No folder selected or folder doesn't exist. Update canceled.");
            return;
        }

        Debug.Log("Selected output path: " + selectedPath);
        if (outputPathTMP != null)
        {
            outputPathTMP.text = selectedPath;
            OnOutputPathChanged(selectedPath);
        }
        else
        {
            Debug.LogWarning("No valid UI reference found for displaying the output path.");
        }
    }


    /**
     * Updates the output path used by SynthesEyesServer when changed in the UI.
     *
     * @param {string} value - New output directory path entered or selected by the user.
     */
    public void OnOutputPathChanged(string value)
    {
        Debug.Log($"Output path changed to: {value}");
        if (synthesEyesServer != null)
        {
            synthesEyesServer.UpdateOutputPath(value);
        }
    }

    /**
     * Updates the output folder name used by SynthesEyesServer when changed in the UI.
     *
     * @param {string} value - New output folder name entered by the user.
     */
    private void OnOutputFolderNameChanged(string value)
    {
        Debug.Log($"Output folder name changed to: {value}");
        if (synthesEyesServer != null)
        {
            // Update the server's folder name field.
            synthesEyesServer.outputFolderName = string.IsNullOrEmpty(value) ? "imgs" : value;
            // Update output path using the current base folder text.
            if (outputPathTMP != null)
            {
                OnOutputPathChanged(outputPathTMP.text);
            }
        }
    }

    /**
     * Updates the number of samples (images) to generate, based on user input.
     *
     * @param {string} value - Input field value representing the sample count.
     * Validates and clamps to a minimum of 1 if input is invalid or non-positive.
     */
    public void OnSampleCountChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || !int.TryParse(value, out int parsedValue))
        {
            Debug.LogWarning($"Could not parse '{value}' as integer for sample count, using default");

            return;
        }

        if (parsedValue <= 0)
        {
            Debug.LogWarning($"Sample count must be positive, using 1 instead of {parsedValue}");
            parsedValue = 1;
            if (numSamplesField != null)
                numSamplesField.text = "1";
        }

        sampleCount = parsedValue;
        Debug.Log($"Sample count updated to: {sampleCount}");
    }

    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        settingsMenu.SetActive(isMenuOpen);
        if (isMenuOpen)
        {
            RestoreInputValues();
        }
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        settingsMenu.SetActive(false);
    }

    public void OpenHelpUrl()
    {
        Application.OpenURL("https://github.com/alexanderdsmith/UnityEyes2/blob/main/README.md");
        Debug.Log("Opening help URL");
    }

    private void InitializeMotionCenterValues()
    {
        motionCenterValues["CameraArrayCenter"] = new Dictionary<string, float>();
        motionCenterValues["CameraArrayCenterNoise"] = new Dictionary<string, float>();

        motionCenterValues["CameraArrayCenter"]["x"] = 0f;
        motionCenterValues["CameraArrayCenter"]["y"] = 0f;
        motionCenterValues["CameraArrayCenter"]["z"] = 0f;
        motionCenterValues["CameraArrayCenter"]["rx"] = 0f;
        motionCenterValues["CameraArrayCenter"]["ry"] = 0f;
        motionCenterValues["CameraArrayCenter"]["rz"] = 0f;

        motionCenterValues["CameraArrayCenterNoise"]["x"] = 0f;
        motionCenterValues["CameraArrayCenterNoise"]["y"] = 0f;
        motionCenterValues["CameraArrayCenterNoise"]["z"] = 0f;
        motionCenterValues["CameraArrayCenterNoise"]["rx"] = 0f;
        motionCenterValues["CameraArrayCenterNoise"]["ry"] = 0f;
        motionCenterValues["CameraArrayCenterNoise"]["rz"] = 0f;
    }

    private void InitializeMotionCenterInputFields()
    {
        motionCenterInputFields["CameraArrayCenter"] = new InputFieldRefs();
        motionCenterInputFields["CameraArrayCenterNoise"] = new InputFieldRefs();

        if (cameraArrayCenterGroup != null)
        {
            TMP_InputField[] centerInputFields = cameraArrayCenterGroup.GetComponentsInChildren<TMP_InputField>(true);
            foreach (TMP_InputField inputField in centerInputFields)
            {
                string fieldName = inputField.name.Replace("Input", "").ToLower();
                StoreMotionCenterInputFieldReference("CameraArrayCenter", fieldName, inputField);
                SetupMotionCenterInputFieldListener(inputField, "CameraArrayCenter", fieldName);
                inputField.text = "0"; // Initialize to 0
            }
        }

        if (cameraArrayCenterNoiseGroup != null)
        {
            TMP_InputField[] noiseInputFields = cameraArrayCenterNoiseGroup.GetComponentsInChildren<TMP_InputField>(true);
            foreach (TMP_InputField inputField in noiseInputFields)
            {
                string fieldName = inputField.name.Replace("Input", "").ToLower();
                StoreMotionCenterInputFieldReference("CameraArrayCenterNoise", fieldName, inputField);
                SetupMotionCenterInputFieldListener(inputField, "CameraArrayCenterNoise", fieldName);
                inputField.text = "0"; 
            }
        }
    }

    private void StoreMotionCenterInputFieldReference(string groupName, string fieldName, TMP_InputField inputField)
    {
        if (!motionCenterInputFields.ContainsKey(groupName))
            return;

        var refs = motionCenterInputFields[groupName];

        switch (fieldName)
        {
            case "x": refs.x = inputField; break;
            case "y": refs.y = inputField; break;
            case "z": refs.z = inputField; break;
            case "rx": refs.rx = inputField; break;
            case "ry": refs.ry = inputField; break;
            case "rz": refs.rz = inputField; break;
        }
    }

    private void SetupMotionCenterInputFieldListener(TMP_InputField inputField, string groupName, string fieldName)
    {
        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();

        inputField.onValueChanged.AddListener((value) => {
            OnMotionCenterValueChanged(groupName, fieldName, value);
        });

        inputField.onEndEdit.AddListener((value) => {
            OnMotionCenterValueChanged(groupName, fieldName, value);
        });
    }

    private void OnMotionCenterValueChanged(string groupName, string fieldName, string value)
    {
        if (!motionCenterValues.ContainsKey(groupName))
        {
            Debug.LogError($"Group '{groupName}' not found for Motion Center");
            motionCenterValues[groupName] = new Dictionary<string, float>();
        }

        if (string.IsNullOrEmpty(value) || !float.TryParse(value, out float parsedValue))
        {
            Debug.LogWarning($"Could not parse '{value}' as float for Motion Center.{groupName}.{fieldName}, using 0");
            parsedValue = 0f;
        }

        motionCenterValues[groupName][fieldName] = parsedValue;
    }

    private void OnToggleMotionCenter(bool isOn)
    {
        SetMotionCenterFieldsInteractable(isOn);
    }

    private void SetMotionCenterFieldsInteractable(bool interactable)
    {
        if (motionCenterInputFields.ContainsKey("CameraArrayCenter"))
        {
            var centerRefs = motionCenterInputFields["CameraArrayCenter"];
            SetInputFieldInteractable(centerRefs.x, interactable);
            SetInputFieldInteractable(centerRefs.y, interactable);
            SetInputFieldInteractable(centerRefs.z, interactable);
            SetInputFieldInteractable(centerRefs.rx, interactable);
            SetInputFieldInteractable(centerRefs.ry, interactable);
            SetInputFieldInteractable(centerRefs.rz, interactable);
        }

        if (motionCenterInputFields.ContainsKey("CameraArrayCenterNoise"))
        {
            var noiseRefs = motionCenterInputFields["CameraArrayCenterNoise"];
            SetInputFieldInteractable(noiseRefs.x, interactable);
            SetInputFieldInteractable(noiseRefs.y, interactable);
            SetInputFieldInteractable(noiseRefs.z, interactable);
            SetInputFieldInteractable(noiseRefs.rx, interactable);
            SetInputFieldInteractable(noiseRefs.ry, interactable);
            SetInputFieldInteractable(noiseRefs.rz, interactable);
        }
    }

    private void SetInputFieldInteractable(TMP_InputField inputField, bool interactable)
    {
        if (inputField != null)
        {
            inputField.interactable = interactable;
        }
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

        SetDefaultCameraValues(cameraCount);

        InitializeCameraInputFields(newCameraGroup, cameraCount);

        addedCameras.Add(newCameraGroup);

        UpdateCameraLabels();

        UpdateButtonStates();




        Debug.Log($"Added camera group: {newCameraGroup.name}");
    }

    private void SetDefaultCameraValues(int cameraId)
    {
        if (!cameraGroupValues.ContainsKey(cameraId))
            return;

        var intrinsics = cameraGroupValues[cameraId]["Intrinsics"];
        intrinsics["fx"] = 500f;
        intrinsics["fy"] = 500f;
        intrinsics["cx"] = 320f;
        intrinsics["cy"] = 240f;
        intrinsics["width"] = 640f;
        intrinsics["height"] = 480f;

        var extrinsics = cameraGroupValues[cameraId]["Extrinsics"];
        extrinsics["x"] = 0f;
        extrinsics["y"] = 0f;
        extrinsics["z"] = 0.1f; // Distance from eye
        extrinsics["rx"] = 0f;
        extrinsics["ry"] = 0f;
        extrinsics["rz"] = 0f;
    }


    /**
     * Initializes data structures for a new camera by ID.
     *
     * @param {int} cameraId - Unique identifier for the camera group.
     *
     * Allocates dictionaries for intrinsics, extrinsics, and their respective noise values.
     */
    private void InitializeCameraValues(int cameraId)
    {

        cameraGroupValues[cameraId] = new Dictionary<string, Dictionary<string, float>>();
        cameraInputFields[cameraId] = new Dictionary<string, InputFieldRefs>();

        cameraGroupValues[cameraId]["Intrinsics"] = new Dictionary<string, float>();
        cameraGroupValues[cameraId]["IntrinsicsNoise"] = new Dictionary<string, float>();
        cameraGroupValues[cameraId]["Extrinsics"] = new Dictionary<string, float>();
        // cameraGroupValues[cameraId]["ExtrinsicsNoise"] = new Dictionary<string, float>();

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
        foreach (string group in new[] { "Extrinsics" })
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
        // cameraInputFields[cameraId]["ExtrinsicsNoise"] = new InputFieldRefs();

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
            // if (parent.name.Contains("ExtrinsicsNoise"))
            //     return "ExtrinsicsNoise";

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
    }


    /**
     * Removes the most recently added camera group.
     *
     * Ensures at least one camera remains. Cleans up associated UI objects and internal state.
     */
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


    /**
     * Dynamically adds a new light group to the UI.
     *
     * Initializes position, noise, and property parameters, spawns UI panel, and sets default values.
     */
    public void AddLight()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(lightContainer);

        if (lightGroupPrefab == null)
        {
            Debug.LogError("LightGroup prefab is null! Please assign it in the Inspector or make sure it exists in Resources/Prefabs.");
            return;
        }

        lightCount++;

        InitializeLightValues(lightCount);
        GameObject newLightGroup = Instantiate(lightGroupPrefab, lightContainer);
        newLightGroup.name = $"LightGroup_{lightCount}";

        RectTransform rectTransform = newLightGroup.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            Vector2 anchoredPosition = rectTransform.anchoredPosition;

            anchoredPosition.x = 300f;

            rectTransform.anchoredPosition = anchoredPosition;

            Debug.Log($"Set {newLightGroup.name} X position to 300");
        }
        else
        {
            Debug.LogWarning($"Could not find RectTransform on {newLightGroup.name}");
        }

        InitializeLightInputFields(newLightGroup, lightCount);

        addedLights.Add(newLightGroup);

        UpdateLightLabels();
        UpdateLightButtonStates();

        LayoutRebuilder.ForceRebuildLayoutImmediate(lightContainer);

        Debug.Log($"Added light group: {newLightGroup.name}");
    }


    /**
     * Initializes data structures for a new light by ID.
     *
     * @param {int} lightId - Unique identifier for the light group.
     *
     * Sets up dictionaries for position, noise, and light-specific properties like color and intensity.
     */
    private void InitializeLightValues(int lightId)
    {
        lightGroupValues[lightId] = new Dictionary<string, Dictionary<string, float>>();
        lightInputFields[lightId] = new Dictionary<string, InputFieldRefs>();

        lightGroupValues[lightId]["PositionGroupLight"] = new Dictionary<string, float>();
        lightGroupValues[lightId]["PositionNoiseGroup"] = new Dictionary<string, float>();

        // Initialize Extrinsics and ExtrinsicsNoise values
        foreach (string group in new[] { "PositionGroupLight", "PositionNoiseGroup" })
        {
            lightGroupValues[lightId][group]["x"] = 0f;
            lightGroupValues[lightId][group]["y"] = 0f;
            lightGroupValues[lightId][group]["z"] = 0f;
            lightGroupValues[lightId][group]["rx"] = 0f;
            lightGroupValues[lightId][group]["ry"] = 0f;
            lightGroupValues[lightId][group]["rz"] = 0f;
        }

        lightGroupValues[lightId]["Properties"] = new Dictionary<string, float>();
        lightGroupValues[lightId]["Properties"]["range"] = 100.0f;
        lightGroupValues[lightId]["Properties"]["intensity"] = 0.3f;
        lightGroupValues[lightId]["Properties"]["colorR"] = 1.0f;
        lightGroupValues[lightId]["Properties"]["colorG"] = 1.0f;
        lightGroupValues[lightId]["Properties"]["colorB"] = 1.0f;
        lightGroupValues[lightId]["Properties"]["shadow_bias"] = 0.05f;

        lightArrayMountedToggles[lightId] = null;
    }


    /**
     * Removes the most recently added light group.
     *
     * Cleans up the UI and removes associated values from light state dictionaries.
     */
    public void RemoveLight()
    {
        if (addedLights.Count <= 0)
        {
            Debug.Log("No lights to remove");
            return;
        }

        GameObject lastLight = addedLights[addedLights.Count - 1];
        addedLights.RemoveAt(addedLights.Count - 1);

        if (lightGroupValues.ContainsKey(lightCount))
        {
            lightGroupValues.Remove(lightCount);
            lightInputFields.Remove(lightCount);
        }

        Destroy(lastLight);
        lightCount--;

        UpdateLightLabels();
        UpdateLightButtonStates();

        Debug.Log($"Removed light group. Remaining: {addedLights.Count}");
    }

    private void UpdateLightLabels()
    {
        for (int i = 0; i < addedLights.Count; i++)
        {
            Text lightLabel = addedLights[i].GetComponentInChildren<Text>();
            if (lightLabel != null)
            {
                lightLabel.text = $"Light {i + 1}";
            }
        }
    }

    private void UpdateLightButtonStates()
    {
        if (removeLightButton != null)
        {
            removeLightButton.interactable = (addedLights.Count > 0);
        }
    }

    private void InitializeLightInputFields(GameObject lightGroup, int lightId)
    {
        lightInputFields[lightId]["PositionGroupLight"] = new InputFieldRefs();
        lightInputFields[lightId]["PositionNoiseGroup"] = new InputFieldRefs();

        TMP_InputField[] inputFields = lightGroup.GetComponentsInChildren<TMP_InputField>(true);

        foreach (TMP_InputField inputField in inputFields)
        {
            string fieldName = inputField.name.Replace("Input", "").ToLower();
            string groupName = DetermineLightGroupName(inputField.transform);

            if (!string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(fieldName))
            {
                StoreLightInputFieldReference(lightId, groupName, fieldName, inputField);
                SetupLightInputFieldListener(inputField, lightId, groupName, fieldName);

                if (lightGroupValues.ContainsKey(lightId) &&
                    lightGroupValues[lightId].ContainsKey(groupName) &&
                    lightGroupValues[lightId][groupName].ContainsKey(fieldName))
                {
                    inputField.text = lightGroupValues[lightId][groupName][fieldName].ToString();
                }
                else
                {
                    inputField.text = "0";
                }
            }
        }

        Toggle arrayMountedToggle = lightGroup.transform.Find("ArrayMounted").GetComponent<Toggle>();
        if (arrayMountedToggle != null)
        {
            lightArrayMountedToggles[lightId] = arrayMountedToggle;
            arrayMountedToggle.onValueChanged.AddListener((value) => {
                OnArrayMountedChanged(lightId, value);
            });
            arrayMountedToggle.isOn = true;
            lightArrayMountedStates[lightId] = true;
        }

        lightPropertyFields[lightId] = new LightPropertyRefs();

        TMP_InputField rangeInput = lightGroup.transform.Find("PropertiesGroup/RangeInput").GetComponent<TMP_InputField>();
        if (rangeInput != null)
        {
            lightPropertyFields[lightId].range = rangeInput;
            SetupPropertyInputListener(rangeInput, lightId, "range");
            rangeInput.text = "100"; 
        }

        TMP_InputField intensityInput = lightGroup.transform.Find("PropertiesGroup/IntensityInput").GetComponent<TMP_InputField>();
        if (intensityInput != null)
        {
            lightPropertyFields[lightId].intensity = intensityInput;
            SetupPropertyInputListener(intensityInput, lightId, "intensity");
            intensityInput.text = "0.3"; 
        }

        TMP_InputField colorRInput = lightGroup.transform.Find("PropertiesGroup/ColorRInput").GetComponent<TMP_InputField>();
        if (colorRInput != null)
        {
            lightPropertyFields[lightId].colorR = colorRInput;
            SetupPropertyInputListener(colorRInput, lightId, "colorR");
            colorRInput.text = "1";
        }

        TMP_InputField colorGInput = lightGroup.transform.Find("PropertiesGroup/ColorGInput").GetComponent<TMP_InputField>();
        if (colorGInput != null)
        {
            lightPropertyFields[lightId].colorG = colorGInput;
            SetupPropertyInputListener(colorGInput, lightId, "colorG");
            colorGInput.text = "1";
        }

        TMP_InputField colorBInput = lightGroup.transform.Find("PropertiesGroup/ColorBInput").GetComponent<TMP_InputField>();
        if (colorBInput != null)
        {
            lightPropertyFields[lightId].colorB = colorBInput;
            SetupPropertyInputListener(colorBInput, lightId, "colorB");
            colorBInput.text = "1";
        }
    }

    private void SetupPropertyInputListener(TMP_InputField inputField, int lightId, string propertyName)
    {
        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();

        inputField.onEndEdit.AddListener((value) => {
            OnLightPropertyChanged(lightId, propertyName, value);
        });
    }

    private void OnLightPropertyChanged(int lightId, string propertyName, string value)
    {
        if (!lightGroupValues.ContainsKey(lightId) || !lightGroupValues[lightId].ContainsKey("Properties"))
            return;

        if (string.IsNullOrEmpty(value) || !float.TryParse(value, out float parsedValue))
        {
            switch (propertyName)
            {
                case "range": parsedValue = 100.0f; break;
                case "intensity": parsedValue = 0.3f; break;
                case "colorR":
                case "colorG":
                case "colorB": parsedValue = 1.0f; break;
                case "shadow_bias": parsedValue = 0.05f; break;
                default: parsedValue = 0f; break;
            }
        }

        lightGroupValues[lightId]["Properties"][propertyName] = parsedValue;
        Debug.Log($"Updated Light {lightId} property {propertyName} to {parsedValue}");
    }

    private void OnArrayMountedChanged(int lightId, bool isOn)
    {
        lightArrayMountedStates[lightId] = isOn;
        Debug.Log($"Light {lightId} array mounted set to {isOn}");
    }


    private string DetermineLightGroupName(Transform inputFieldTransform)
    {
        Transform parent = inputFieldTransform.parent;

        while (parent != null)
        {
            if (parent.name.Contains("PositionGroupLight"))
                return "PositionGroupLight";
            if (parent.name.Contains("PositionNoiseGroup"))
                return "PositionNoiseGroup";

            parent = parent.parent;
        }

        return "";
    }

    private void StoreLightInputFieldReference(int lightId, string groupName, string fieldName, TMP_InputField inputField)
    {
        if (!lightInputFields.ContainsKey(lightId) || !lightInputFields[lightId].ContainsKey(groupName))
            return;

        var refs = lightInputFields[lightId][groupName];

        switch (fieldName)
        {
            case "x": refs.x = inputField; break;
            case "y": refs.y = inputField; break;
            case "z": refs.z = inputField; break;
            case "rx": refs.rx = inputField; break;
            case "ry": refs.ry = inputField; break;
            case "rz": refs.rz = inputField; break;
        }
    }

    private void SetupLightInputFieldListener(TMP_InputField inputField, int lightId, string groupName, string fieldName)
    {
        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();

        inputField.onValueChanged.AddListener((value) => {
            OnLightValueChanged(lightId, groupName, fieldName, value);
        });

        inputField.onEndEdit.AddListener((value) => {
            OnLightValueChanged(lightId, groupName, fieldName, value);
        });
    }

    private void OnLightValueChanged(int lightId, string groupName, string fieldName, string value)
    {
        if (!lightGroupValues.ContainsKey(lightId))
        {
            Debug.LogError($"Light ID {lightId} not found, initializing it");
            InitializeLightValues(lightId);
        }

        if (!lightGroupValues[lightId].ContainsKey(groupName))
        {
            Debug.LogError($"Group '{groupName}' not found for Light {lightId}");
            lightGroupValues[lightId][groupName] = new Dictionary<string, float>();
        }

        if (string.IsNullOrEmpty(value) || !float.TryParse(value, out float parsedValue))
        {
            Debug.LogWarning($"Could not parse '{value}' as float for Light {lightId}.{groupName}.{fieldName}, using 0");
            parsedValue = 0f;
        }

        lightGroupValues[lightId][groupName][fieldName] = parsedValue;
    }


    /**
     * Sets up default values for all eye-related parameters.
     *
     * Initializes pupil size, iris size, and gaze noise values for use in the UI and configuration export.
     */
    private void InitializeEyeParameterValues()
    {
        eyeParameterValues["PupilSize"] = new Dictionary<string, float>();
        eyeParameterValues["PupilSize"]["min"] = 0.2f;
        eyeParameterValues["PupilSize"]["max"] = 0.8f;

        eyeParameterValues["IrisSize"] = new Dictionary<string, float>();
        eyeParameterValues["IrisSize"]["min"] = 0.9f;
        eyeParameterValues["IrisSize"]["max"] = 1.0f;

        eyeParameterValues["EyeProperties"] = new Dictionary<string, float>();
        eyeParameterValues["EyeProperties"]["pitch"] = 0f;
        eyeParameterValues["EyeProperties"]["yaw"] = 0f;
        eyeParameterValues["EyeProperties"]["pitchnoise"] = 30f;
        eyeParameterValues["EyeProperties"]["yawnoise"] = 30f;
    }

    private void InitializeEyeParameterInputFields()
    {
        eyeSizeInputFields["PupilSize"] = new MinMaxInputRefs();
        eyeSizeInputFields["IrisSize"] = new MinMaxInputRefs();
        eyeParamInputFields["EyeProperties"] = new InputFieldRefs();

        if (eyeParamGroup == null)
        {
            eyeParamGroup = GameObject.Find("EyeParamGroup");
            if (eyeParamGroup == null)
            {
                Debug.LogWarning("EyeParamGroup not found. Eye parameter settings won't work.");
                return;
            }
        }

        Transform pupilSizeGroup = eyeParamGroup.transform.Find("PupilSizeGroup");
        if (pupilSizeGroup != null)
        {
            TMP_InputField minInput = pupilSizeGroup.Find("PupilSizeMinInput")?.GetComponent<TMP_InputField>();
            TMP_InputField maxInput = pupilSizeGroup.Find("PupilSizeMaxInput")?.GetComponent<TMP_InputField>();

            if (minInput != null && maxInput != null)
            {
                eyeSizeInputFields["PupilSize"].min = minInput;
                eyeSizeInputFields["PupilSize"].max = maxInput;

                SetupMinMaxInputListener(minInput, "PupilSize", "min");
                SetupMinMaxInputListener(maxInput, "PupilSize", "max");

                minInput.text = eyeParameterValues["PupilSize"]["min"].ToString("0.0");
                maxInput.text = eyeParameterValues["PupilSize"]["max"].ToString("0.0");
            }
            else
            {
                Debug.LogWarning("Pupil size min/max input fields not found");
            }
        }
        else
        {
            Debug.LogWarning("PupilSizeGroup not found");
        }

        Transform irisSizeGroup = eyeParamGroup.transform.Find("IrisSizeGroup");
        if (irisSizeGroup != null)
        {
            TMP_InputField minInput = irisSizeGroup.Find("IrisSizeMinInput")?.GetComponent<TMP_InputField>();
            TMP_InputField maxInput = irisSizeGroup.Find("IrisSizeMaxInput")?.GetComponent<TMP_InputField>();

            if (minInput != null && maxInput != null)
            {
                eyeSizeInputFields["IrisSize"].min = minInput;
                eyeSizeInputFields["IrisSize"].max = maxInput;

                SetupMinMaxInputListener(minInput, "IrisSize", "min");
                SetupMinMaxInputListener(maxInput, "IrisSize", "max");

                minInput.text = eyeParameterValues["IrisSize"]["min"].ToString("0.0");
                maxInput.text = eyeParameterValues["IrisSize"]["max"].ToString("0.0");
            }
            else
            {
                Debug.LogWarning("Iris size min/max input fields not found");
            }
        }
        else
        {
            Debug.LogWarning("IrisSizeGroup not found");
        }

        Transform eyePropertiesGroup = eyeParamGroup.transform.Find("EyePropertiesGroup");
        if (eyePropertiesGroup != null)
        {
            TMP_InputField pitchField = eyePropertiesGroup.Find("EyePitchInput")?.GetComponent<TMP_InputField>();
            TMP_InputField yawField = eyePropertiesGroup.Find("EyeYawInput")?.GetComponent<TMP_InputField>();
            TMP_InputField pitchNoiseField = eyePropertiesGroup.Find("EyePitchNoiseInput")?.GetComponent<TMP_InputField>();
            TMP_InputField yawNoiseField = eyePropertiesGroup.Find("EyeYawNoiseInput")?.GetComponent<TMP_InputField>();

            if (pitchField != null)
            {
                eyeParamInputFields["EyeProperties"].rx = pitchField;
                SetupEyePropertyListener(pitchField, "pitch");
                pitchField.text = eyeParameterValues["EyeProperties"]["pitch"].ToString("0.0");
            }

            if (yawField != null)
            {
                eyeParamInputFields["EyeProperties"].ry = yawField;
                SetupEyePropertyListener(yawField, "yaw");
                yawField.text = eyeParameterValues["EyeProperties"]["yaw"].ToString("0.0");
            }

            if (pitchNoiseField != null)
            {
                eyeParamInputFields["EyeProperties"].x = pitchNoiseField;
                SetupEyePropertyListener(pitchNoiseField, "pitchnoise");
                pitchNoiseField.text = eyeParameterValues["EyeProperties"]["pitchnoise"].ToString("0.0");
            }

            if (yawNoiseField != null)
            {
                eyeParamInputFields["EyeProperties"].y = yawNoiseField;
                SetupEyePropertyListener(yawNoiseField, "yawnoise");
                yawNoiseField.text = eyeParameterValues["EyeProperties"]["yawnoise"].ToString("0.0");
            }
        }
        else
        {
            Debug.LogWarning("EyePropertiesGroup not found");
        }
    }

    private void ApplyEyeParameterValuesToUI()
    {
        if (eyeSizeInputFields.ContainsKey("PupilSize") && eyeParameterValues.ContainsKey("PupilSize"))
        {
            var pupilSizeFields = eyeSizeInputFields["PupilSize"];
            var pupilSizeValues = eyeParameterValues["PupilSize"];

            if (pupilSizeFields.min != null && pupilSizeValues.ContainsKey("min"))
                pupilSizeFields.min.text = pupilSizeValues["min"].ToString("0.0");

            if (pupilSizeFields.max != null && pupilSizeValues.ContainsKey("max"))
                pupilSizeFields.max.text = pupilSizeValues["max"].ToString("0.0");
        }

        if (eyeSizeInputFields.ContainsKey("IrisSize") && eyeParameterValues.ContainsKey("IrisSize"))
        {
            var irisSizeFields = eyeSizeInputFields["IrisSize"];
            var irisSizeValues = eyeParameterValues["IrisSize"];

            if (irisSizeFields.min != null && irisSizeValues.ContainsKey("min"))
                irisSizeFields.min.text = irisSizeValues["min"].ToString("0.0");

            if (irisSizeFields.max != null && irisSizeValues.ContainsKey("max"))
                irisSizeFields.max.text = irisSizeValues["max"].ToString("0.0");
        }

        if (eyeParamInputFields.ContainsKey("EyeProperties") && eyeParameterValues.ContainsKey("EyeProperties"))
        {
            var eyePropertyFields = eyeParamInputFields["EyeProperties"];
            var eyePropertyValues = eyeParameterValues["EyeProperties"];

            if (eyePropertyFields.rx != null && eyePropertyValues.ContainsKey("pitch"))
                eyePropertyFields.rx.text = eyePropertyValues["pitch"].ToString("0.0");

            if (eyePropertyFields.ry != null && eyePropertyValues.ContainsKey("yaw"))
                eyePropertyFields.ry.text = eyePropertyValues["yaw"].ToString("0.0");

            if (eyePropertyFields.x != null && eyePropertyValues.ContainsKey("pitchnoise"))
                eyePropertyFields.x.text = eyePropertyValues["pitchnoise"].ToString("0.0");

            if (eyePropertyFields.y != null && eyePropertyValues.ContainsKey("yawnoise"))
                eyePropertyFields.y.text = eyePropertyValues["yawnoise"].ToString("0.0");
        }

        Debug.Log("Eye parameter values applied to UI");
    }

    private void SetupMinMaxInputListener(TMP_InputField inputField, string paramName, string minMaxField)
    {
        if (inputField == null)
            return;

        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();

        inputField.onValueChanged.AddListener((value) => {
            OnEyeSizeValueChanged(paramName, minMaxField, value);
        });

        inputField.onEndEdit.AddListener((value) => {
            OnEyeSizeValueChanged(paramName, minMaxField, value);
        });
    }

    private void SetupEyePropertyListener(TMP_InputField inputField, string propertyName)
    {
        if (inputField == null)
            return;

        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();

        inputField.onValueChanged.AddListener((value) => {
            OnEyePropertyValueChanged(propertyName, value);
        });

        inputField.onEndEdit.AddListener((value) => {
            OnEyePropertyValueChanged(propertyName, value);
        });
    }

    private void OnEyeSizeValueChanged(string paramName, string minMaxField, string value)
    {
        if (!eyeParameterValues.ContainsKey(paramName))
        {
            Debug.LogError($"Parameter '{paramName}' not found for Eye Parameters");
            eyeParameterValues[paramName] = new Dictionary<string, float>();
        }

        float parsedValue;

        if (string.IsNullOrEmpty(value) || !float.TryParse(value, out parsedValue))
        {
            if (paramName == "PupilSize")
                parsedValue = 0.2f;
            else if (paramName == "IrisSize")
                parsedValue = 10.0f;
            else
                parsedValue = 0f;

            Debug.LogWarning($"Could not parse '{value}' as float for Eye Parameters.{paramName}.{minMaxField}, using default: {parsedValue}");
        }

        eyeParameterValues[paramName][minMaxField] = parsedValue;
        Debug.Log($"Updated Eye Parameters.{paramName}.{minMaxField} to {parsedValue}");
    }


    /**
     * Updates a single eye property (yaw, pitch, or noise) based on user input.
     *
     * @param {string} propertyName - Name of the property to update.
     * @param {string} value - New value from the input field.
     *
     * Performs parsing and clamps or defaults invalid values.
     */
    private void OnEyePropertyValueChanged(string propertyName, string value)
    {
        if (!eyeParameterValues.ContainsKey("EyeProperties"))
        {
            Debug.LogError("EyeProperties not found for Eye Parameters");
            eyeParameterValues["EyeProperties"] = new Dictionary<string, float>();
        }

        float parsedValue;

        if (string.IsNullOrEmpty(value) || !float.TryParse(value, out parsedValue))
        {
            switch (propertyName)
            {
                case "pitch":
                case "yaw":
                    parsedValue = 0f;
                    break;
                case "pitchnoise":
                    parsedValue = 15f;
                    break;
                case "yawnoise":
                    parsedValue = 20f;
                    break;
                default:
                    parsedValue = 0f;
                    break;
            }

            Debug.LogWarning($"Could not parse '{value}' as float for Eye Properties.{propertyName}, using default: {parsedValue}");
        }

        eyeParameterValues["EyeProperties"][propertyName] = parsedValue;
        Debug.Log($"Updated Eye Properties.{propertyName} to {parsedValue}");
    }


    private void RestoreInputValues()
    {
        foreach (var cameraEntry in cameraGroupValues)
        {
            int cameraId = cameraEntry.Key;

            if (!cameraInputFields.ContainsKey(cameraId))
                continue;

            foreach (var groupEntry in cameraEntry.Value)
            {
                string groupName = groupEntry.Key;

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

        foreach (var lightEntry in lightGroupValues)
        {
            int lightId = lightEntry.Key;

            if (!lightInputFields.ContainsKey(lightId))
                continue;

            foreach (var groupEntry in lightEntry.Value)
            {
                string groupName = groupEntry.Key;

                if (!lightInputFields[lightId].ContainsKey(groupName))
                    continue;

                var inputRefs = lightInputFields[lightId][groupName];
                var values = groupEntry.Value;

                if (groupName.Contains("PositionGroupLight"))
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

        foreach (var lightEntry in lightGroupValues)
        {
            int lightId = lightEntry.Key;

            if (!lightPropertyFields.ContainsKey(lightId))
                continue;

            var properties = lightEntry.Value["Properties"];
            var propertyRefs = lightPropertyFields[lightId];

            if (propertyRefs.range != null && properties.ContainsKey("range"))
                propertyRefs.range.text = properties["range"].ToString();

            if (propertyRefs.intensity != null && properties.ContainsKey("intensity"))
                propertyRefs.intensity.text = properties["intensity"].ToString();

            if (propertyRefs.colorR != null && properties.ContainsKey("colorR"))
                propertyRefs.colorR.text = properties["colorR"].ToString();

            if (propertyRefs.colorG != null && properties.ContainsKey("colorG"))
                propertyRefs.colorG.text = properties["colorG"].ToString();

            if (propertyRefs.colorB != null && properties.ContainsKey("colorB"))
                propertyRefs.colorB.text = properties["colorB"].ToString();
        }

        foreach (var groupEntry in motionCenterValues)
        {
            string groupName = groupEntry.Key;

            if (!motionCenterInputFields.ContainsKey(groupName))
                continue;

            var inputRefs = motionCenterInputFields[groupName];
            var values = groupEntry.Value;

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


        // Restore eye parameter values
        if (eyeSizeInputFields.ContainsKey("PupilSize") && eyeParameterValues.ContainsKey("PupilSize"))
        {
            var pupilSizeFields = eyeSizeInputFields["PupilSize"];
            var pupilSizeValues = eyeParameterValues["PupilSize"];

            if (pupilSizeFields.min != null && pupilSizeValues.ContainsKey("min"))
                pupilSizeFields.min.text = pupilSizeValues["min"].ToString("0.0");

            if (pupilSizeFields.max != null && pupilSizeValues.ContainsKey("max"))
                pupilSizeFields.max.text = pupilSizeValues["max"].ToString("0.0");
        }

        if (eyeSizeInputFields.ContainsKey("IrisSize") && eyeParameterValues.ContainsKey("IrisSize"))
        {
            var irisSizeFields = eyeSizeInputFields["IrisSize"];
            var irisSizeValues = eyeParameterValues["IrisSize"];

            if (irisSizeFields.min != null && irisSizeValues.ContainsKey("min"))
                irisSizeFields.min.text = irisSizeValues["min"].ToString("0.0");

            if (irisSizeFields.max != null && irisSizeValues.ContainsKey("max"))
                irisSizeFields.max.text = irisSizeValues["max"].ToString("0.0");
        }

        if (eyeParamInputFields.ContainsKey("EyeProperties") && eyeParameterValues.ContainsKey("EyeProperties"))
        {
            var eyePropertyFields = eyeParamInputFields["EyeProperties"];
            var eyePropertyValues = eyeParameterValues["EyeProperties"];

            if (eyePropertyFields.rx != null && eyePropertyValues.ContainsKey("pitch"))
                eyePropertyFields.rx.text = eyePropertyValues["pitch"].ToString("0.0");

            if (eyePropertyFields.ry != null && eyePropertyValues.ContainsKey("yaw"))
                eyePropertyFields.ry.text = eyePropertyValues["yaw"].ToString("0.0");

            // Add pitch noise and yaw noise
            if (eyePropertyFields.x != null && eyePropertyValues.ContainsKey("pitchnoise"))
                eyePropertyFields.x.text = eyePropertyValues["pitchnoise"].ToString("0.0");

            if (eyePropertyFields.y != null && eyePropertyValues.ContainsKey("yawnoise"))
                eyePropertyFields.y.text = eyePropertyValues["yawnoise"].ToString("0.0");
        }

        if (numSamplesField != null)
        {
            numSamplesField.text = sampleCount.ToString();
        }
    }


    /**
     * Serializes the current configuration into a JSON format.
     *
     * Captures all camera, light, motion center, and eye parameter settings and writes them to a config file.
     * This is the primary method for exporting user-defined settings.
     */
    private void SaveConfiguration()
    {
        JSONNode rootNode = new JSONClass();

        if (outputPathTMP != null && !string.IsNullOrEmpty(outputPathTMP.text))
        {
            Debug.Log($"Saving configuration with output path: {outputPathTMP.text}");
            rootNode.Add("outputPath", new JSONData(outputPathTMP.text));
        }

        rootNode.Add("num_samples", new JSONData(sampleCount));

        rootNode.Add("motion_center", new JSONData(motionCenterToggle != null && motionCenterToggle.isOn ? 1 : 0));

        JSONNode centerNode = new JSONClass();
        if (motionCenterValues.ContainsKey("CameraArrayCenter"))
        {
            var centerValues = motionCenterValues["CameraArrayCenter"];
            centerNode.Add("x", new JSONData(centerValues.ContainsKey("x") ? centerValues["x"] : 0f));
            centerNode.Add("y", new JSONData(centerValues.ContainsKey("y") ? centerValues["y"] : 0f));
            centerNode.Add("z", new JSONData(centerValues.ContainsKey("z") ? centerValues["z"] : 0f));
            centerNode.Add("rx", new JSONData(centerValues.ContainsKey("rx") ? centerValues["rx"] : 0f));
            centerNode.Add("ry", new JSONData(centerValues.ContainsKey("ry") ? centerValues["ry"] : 0f));
            centerNode.Add("rz", new JSONData(centerValues.ContainsKey("rz") ? centerValues["rz"] : 0f));
        }
        rootNode.Add("camera_array_center", centerNode);

        JSONNode centerNoiseNode = new JSONClass();
        if (motionCenterValues.ContainsKey("CameraArrayCenterNoise"))
        {
            var centerNoiseValues = motionCenterValues["CameraArrayCenterNoise"];
            centerNoiseNode.Add("x", new JSONData(centerNoiseValues.ContainsKey("x") ? centerNoiseValues["x"] : 0f));
            centerNoiseNode.Add("y", new JSONData(centerNoiseValues.ContainsKey("y") ? centerNoiseValues["y"] : 0f));
            centerNoiseNode.Add("z", new JSONData(centerNoiseValues.ContainsKey("z") ? centerNoiseValues["z"] : 0f));
            centerNoiseNode.Add("rx", new JSONData(centerNoiseValues.ContainsKey("rx") ? centerNoiseValues["rx"] : 0f));
            centerNoiseNode.Add("ry", new JSONData(centerNoiseValues.ContainsKey("ry") ? centerNoiseValues["ry"] : 0f));
            centerNoiseNode.Add("rz", new JSONData(centerNoiseValues.ContainsKey("rz") ? centerNoiseValues["rz"] : 0f));
        }
        rootNode.Add("camera_array_center_noise", centerNoiseNode);

        // Create cameras array
        JSONArray camerasArray = new JSONArray();
        rootNode.Add("cameras", camerasArray);

        for (int i = 1; i <= cameraCount; i++)
        {
            if (!cameraGroupValues.ContainsKey(i))
                continue;

            JSONNode cameraNode = new JSONClass();
            cameraNode.Add("name", new JSONData($"cam{i - 1}"));
            cameraNode.Add("noise_distribution", new JSONData("uniform")); // TODO: should not be default.

            var cameraValues = cameraGroupValues[i];

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

            // JSONNode extrinsicsNoiseNode = new JSONClass();
            // if (cameraValues.ContainsKey("ExtrinsicsNoise"))
            // {
            //     var extrinsicsNoise = cameraValues["ExtrinsicsNoise"];
            //     extrinsicsNoiseNode.Add("x", new JSONData(extrinsicsNoise.ContainsKey("x") ? extrinsicsNoise["x"] : 0f));
            //     extrinsicsNoiseNode.Add("y", new JSONData(extrinsicsNoise.ContainsKey("y") ? extrinsicsNoise["y"] : 0f));
            //     extrinsicsNoiseNode.Add("z", new JSONData(extrinsicsNoise.ContainsKey("z") ? extrinsicsNoise["z"] : 0f));
            //     extrinsicsNoiseNode.Add("rx", new JSONData(extrinsicsNoise.ContainsKey("rx") ? extrinsicsNoise["rx"] : 0f));
            //     extrinsicsNoiseNode.Add("ry", new JSONData(extrinsicsNoise.ContainsKey("ry") ? extrinsicsNoise["ry"] : 0f));
            //     extrinsicsNoiseNode.Add("rz", new JSONData(extrinsicsNoise.ContainsKey("rz") ? extrinsicsNoise["rz"] : 0f));
            // }
            // cameraNode.Add("extrinsics_noise", extrinsicsNoiseNode);

            camerasArray.Add(cameraNode);
        }

        // Create lights array
        JSONArray lightsArray = new JSONArray();
        rootNode.Add("lights", lightsArray);

        for (int i = 1; i <= lightCount; i++)
        {
            if (!lightGroupValues.ContainsKey(i))
                continue;

            // TODO: Make sure all functionality is present for lights. Default values cannot be used here.
            JSONNode lightNode = new JSONClass();
            lightNode.Add("name", new JSONData($"point_light_{i}"));
            lightNode.Add("type", new JSONData("point"));
            bool isArrayMounted = lightArrayMountedStates.ContainsKey(i) ? lightArrayMountedStates[i] : true;
            lightNode.Add("array_mounted", new JSONData(isArrayMounted ? 1 : 0));

            var lightValues = lightGroupValues[i];

            JSONNode extrinsicsNode = new JSONClass();
            if (lightValues.ContainsKey("PositionGroupLight"))
            {
                var extrinsics = lightValues["PositionGroupLight"];
                extrinsicsNode.Add("x", new JSONData(extrinsics.ContainsKey("x") ? extrinsics["x"] : 0f));
                extrinsicsNode.Add("y", new JSONData(extrinsics.ContainsKey("y") ? extrinsics["y"] : 0f));
                extrinsicsNode.Add("z", new JSONData(extrinsics.ContainsKey("z") ? extrinsics["z"] : 0f));
                // extrinsicsNode.Add("rx", new JSONData(extrinsics.ContainsKey("rx") ? extrinsics["rx"] : 0f));
                // extrinsicsNode.Add("ry", new JSONData(extrinsics.ContainsKey("ry") ? extrinsics["ry"] : 0f));
                // extrinsicsNode.Add("rz", new JSONData(extrinsics.ContainsKey("rz") ? extrinsics["rz"] : 0f));
            }
            lightNode.Add("position", extrinsicsNode);

            JSONNode extrinsicsNoiseNode = new JSONClass();
            if (lightValues.ContainsKey("PositionNoiseGroup"))
            {
                var extrinsicsNoise = lightValues["PositionNoiseGroup"];
                // extrinsicsNoiseNode.Add("x", new JSONData(extrinsicsNoise.ContainsKey("x") ? extrinsicsNoise["x"] : 0f));
                // extrinsicsNoiseNode.Add("y", new JSONData(extrinsicsNoise.ContainsKey("y") ? extrinsicsNoise["y"] : 0f));
                // extrinsicsNoiseNode.Add("z", new JSONData(extrinsicsNoise.ContainsKey("z") ? extrinsicsNoise["z"] : 0f));
                // extrinsicsNoiseNode.Add("rx", new JSONData(extrinsicsNoise.ContainsKey("rx") ? extrinsicsNoise["rx"] : 0f));
                // extrinsicsNoiseNode.Add("ry", new JSONData(extrinsicsNoise.ContainsKey("ry") ? extrinsicsNoise["ry"] : 0f));
                // extrinsicsNoiseNode.Add("rz", new JSONData(extrinsicsNoise.ContainsKey("rz") ? extrinsicsNoise["rz"] : 0f));
            }
            // lightNode.Add("position_noise", extrinsicsNoiseNode); // TODO: determine logic for this

            JSONNode propertiesNode = new JSONClass();
            if (lightGroupValues[i].ContainsKey("Properties"))
            {
                var properties = lightGroupValues[i]["Properties"];

                float range = properties.ContainsKey("range") ? properties["range"] : 100.0f;
                propertiesNode.Add("range", new JSONData(range));

                float intensity = properties.ContainsKey("intensity") ? properties["intensity"] : 0.3f;
                propertiesNode.Add("intensity", new JSONData(intensity));

                JSONNode colorNode = new JSONClass();
                float r = properties.ContainsKey("colorR") ? properties["colorR"] : 1.0f;
                float g = properties.ContainsKey("colorG") ? properties["colorG"] : 1.0f;
                float b = properties.ContainsKey("colorB") ? properties["colorB"] : 1.0f;
                colorNode.Add("r", new JSONData(r));
                colorNode.Add("g", new JSONData(g));
                colorNode.Add("b", new JSONData(b));
                propertiesNode.Add("color", colorNode);

                // propertiesNode.Add("shadows", new JSONData("soft"));

                // float shadowBias = properties.ContainsKey("shadow_bias") ? properties["shadow_bias"] : 0.05f;
                // propertiesNode.Add("shadow_bias", new JSONData(shadowBias));
            }
            else
            {
                propertiesNode.Add("range", new JSONData(100.0f));
                propertiesNode.Add("intensity", new JSONData(0.3f));

                JSONNode colorNode = new JSONClass();
                colorNode.Add("r", new JSONData(1.0f));
                colorNode.Add("g", new JSONData(1.0f));
                colorNode.Add("b", new JSONData(1.0f));
                propertiesNode.Add("color", colorNode);

                // propertiesNode.Add("shadows", new JSONData("soft"));
                // propertiesNode.Add("shadow_bias", new JSONData(0.05f));
            }
            lightNode.Add("properties", propertiesNode);
            lightsArray.Add(lightNode);
        }

        JSONNode eyeParametersNode = new JSONClass();

        // TODO: Make all of these default values below into dynamic variables.

        JSONNode pupilSizeRangeNode = new JSONClass();
        if (eyeParameterValues.ContainsKey("PupilSize"))
        {
            var pupilSizeValues = eyeParameterValues["PupilSize"];
            pupilSizeRangeNode.Add("min", new JSONData(pupilSizeValues.ContainsKey("min") ? pupilSizeValues["min"] : 0.2f));
            pupilSizeRangeNode.Add("max", new JSONData(pupilSizeValues.ContainsKey("max") ? pupilSizeValues["max"] : 0.2f));
        }
        else
        {
            pupilSizeRangeNode.Add("min", new JSONData(0.2f));
            pupilSizeRangeNode.Add("max", new JSONData(0.2f));
        }
        eyeParametersNode.Add("pupil_size_range", pupilSizeRangeNode);

        JSONNode irisSizeRangeNode = new JSONClass();
        if (eyeParameterValues.ContainsKey("IrisSize"))
        {
            var irisSizeValues = eyeParameterValues["IrisSize"];
            irisSizeRangeNode.Add("min", new JSONData(irisSizeValues.ContainsKey("min") ? irisSizeValues["min"] : 10.0f));
            irisSizeRangeNode.Add("max", new JSONData(irisSizeValues.ContainsKey("max") ? irisSizeValues["max"] : 10.0f));
        }
        else
        {
            irisSizeRangeNode.Add("min", new JSONData(10.0f));
            irisSizeRangeNode.Add("max", new JSONData(10.0f));
        }
        eyeParametersNode.Add("iris_size_range", irisSizeRangeNode);

        if (eyeParameterValues.ContainsKey("EyeProperties"))
        {
            var eyeProps = eyeParameterValues["EyeProperties"];
            eyeParametersNode.Add("default_yaw", new JSONData(eyeProps.ContainsKey("yaw") ? eyeProps["yaw"] : 0f));
            eyeParametersNode.Add("default_pitch", new JSONData(eyeProps.ContainsKey("pitch") ? eyeProps["pitch"] : 0f));
            eyeParametersNode.Add("yaw_noise", new JSONData(eyeProps.ContainsKey("yawnoise") ? eyeProps["yawnoise"] : 20f));
            eyeParametersNode.Add("pitch_noise", new JSONData(eyeProps.ContainsKey("pitchnoise") ? eyeProps["pitchnoise"] : 15f));
        }
        else
        {
            eyeParametersNode.Add("default_yaw", new JSONData(0f));
            eyeParametersNode.Add("default_pitch", new JSONData(0f));
            eyeParametersNode.Add("yaw_noise", new JSONData(20f));
            eyeParametersNode.Add("pitch_noise", new JSONData(15f));
        }

        rootNode.Add("eye_parameters", eyeParametersNode);


        // Save to file
        try
        {
            // Use the user-specified base output folder or fallback to persistentDataPath
            string baseOutputFolder = !string.IsNullOrEmpty(outputPathTMP?.text)
                ? outputPathTMP.text
                : Path.Combine(Application.persistentDataPath, "imgs");

            // Use the user-specified folder name or default to "imgs"
            string folderName = !string.IsNullOrEmpty(outputFolderNameTMP?.text)
                ? outputFolderNameTMP.text
                : "imgs";

            // Combine them into one variable output_path
            string output_path = Path.Combine(baseOutputFolder, folderName);
            if (!Directory.Exists(output_path))
            {
                Directory.CreateDirectory(output_path);
                Debug.Log($"Created images directory: {output_path}");
            }

            // Save the combined output path into the JSON (for image saving)
            rootNode.Add("outputPath", new JSONData(output_path));

            // Save other configuration parameters as before…
            rootNode.Add("num_samples", new JSONData(sampleCount));
            // … (other config nodes)

            // Save the configuration JSON file to the base output folder (outside the subfolder)
            string configFilePath = Path.Combine(baseOutputFolder, "camera_config.json");
            File.WriteAllText(configFilePath, rootNode.ToJSON(4));
            Debug.Log($"Configuration saved successfully to: {configFilePath}");

            if (synthesEyesServer != null)
            {
                // Notify the server. It should use its own single variable, output_path,
                // which is now set by reading the saved JSON.
                synthesEyesServer.ReloadConfiguration(configFilePath);
                Debug.Log("Notified SynthesEyesServer to reload configuration");
            }
            else
            {
                Debug.LogWarning("SynthesEyesServer reference not set. Cannot auto-update scene.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving configuration: {e.Message}");
        }
    }
    // Helper method to test writability of a directory
    private bool IsDirectoryWritable(string path)
    {
        try
        {
            // Attempt to create and then delete a temporary file
            string testFile = Path.Combine(path, Path.GetRandomFileName());
            using (FileStream fs = File.Create(testFile, 1, FileOptions.DeleteOnClose))
            {
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private TMP_InputField FindInputField(Transform parent, string name)
    {
        Transform found = FindChildByName(parent, name);
        return found?.GetComponent<TMP_InputField>();
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
