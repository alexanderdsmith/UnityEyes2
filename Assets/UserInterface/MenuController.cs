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
    //[Header("Camera Controls")]
    [Header("Camera Management")]
    [SerializeField] private Button addCameraButton;       
    [SerializeField] private Button removeCameraButton;    

    private int cameraCount = 0; 

    [Header("Camera Parameter Groups")]
    [SerializeField] private GameObject cameraGroup;
    [SerializeField] private RectTransform cameraContainer;
    [SerializeField] private GameObject cameraGroupPrefab;
    [SerializeField] private RectTransform intrinsicsGroup;
    //[SerializeField] private InputField fxnput;

    [SerializeField] private Transform intrinsicsNoiseGroup;
    [SerializeField] private Transform extrinsicsGroup;
    [SerializeField] private Transform extrinsicsNoiseGroup;

    private Dictionary<string, Dictionary<string, float>> groupValues = new Dictionary<string, Dictionary<string, float>>();

    private class InputFieldRefs
    {
        // Change from InputField to TMP_InputField
        public TMP_InputField fx, fy, cx, cy, width, height;  
        public TMP_InputField x, y, z, rx, ry, rz;         
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

    //[Header("Camera Controls")]
    //[SerializeField] private Button addCameraButton;      
    //[SerializeField] private Button removeCameraButton;    

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

        cameraContainer = GameObject.Find("CameraContainer").GetComponent<RectTransform>();

        cameraGroupPrefab = Resources.Load<GameObject>("Prefabs/CameraGroup");
        // intrinsicsGroup = transform.Find("IntrinsicsGroup").GetComponent<RectTransform>();

        menuButton = GameObject.Find("MenuButton").GetComponent<Button>(); 
        closeButton = GameObject.Find("ExitButton").GetComponent<Button>();
        addCameraButton = GameObject.Find("AddCamera").GetComponent<Button>();
        removeCameraButton = GameObject.Find("RemoveCamera").GetComponent<Button>();
        saveButton = GameObject.Find("SaveButton").GetComponent<Button>();

        if (cameraGroupPrefab == null)
        {
            Debug.LogError("Failed to load CameraGroup prefab. Please check that it exists at Assets/Resources/Prefabs/CameraGroup");
            return;
        }
        
        InitializeCameraGroups();

        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false);
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(ToggleMenu);
        }
        // Add close button listener
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseMenu);
        }

        if (addCameraButton != null)
        {
            Debug.Log("Pressed Add Camera button");
            addCameraButton.onClick.AddListener(AddCamera);
        }
        if (removeCameraButton != null)
        {
            Debug.Log("Pressed Remove Camera button");
            removeCameraButton.onClick.AddListener(RemoveCamera);
        }

        if (addedCameras.Count == 0)
        {
            AddCamera();
        }

        if (saveButton != null)
        {
            // Debug.Log("Pressed this button");
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
        // Debug.Log("I'm Here");
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
        // Increment camera count
        cameraCount++;

        GameObject newCameraGroup = Instantiate(cameraGroupPrefab, cameraContainer);
        newCameraGroup.name = $"CameraGroup_{cameraCount}";

        addedCameras.Add(newCameraGroup);

        UpdateCameraLabels();

        UpdateButtonStates();

        Debug.Log($"Added camera group: {newCameraGroup.name}");
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

    private void InitializeCameraGroups()
    {
        Debug.Log("Starting to initialize camera groups");

        groupValues["Intrinsics"] = new Dictionary<string, float>();
        groupValues["IntrinsicsNoise"] = new Dictionary<string, float>();
        groupValues["Extrinsics"] = new Dictionary<string, float>();
        groupValues["ExtrinsicsNoise"] = new Dictionary<string, float>();

        GameObject intrinsicsGroupObj = GameObject.Find("IntrinsicsGroup");
        if (intrinsicsGroupObj != null)
        {
            Debug.Log("Found IntrinsicsGroup GameObject");
            intrinsicsGroup = intrinsicsGroupObj.GetComponent<RectTransform>();
            InitializeIntrinsicsFields("Intrinsics", intrinsicsGroupObj.transform);
        }
        else
        {
            Debug.LogError("Cannot find IntrinsicsGroup in the scene");
        }

        GameObject intrinsicsNoiseGroupObj = GameObject.Find("IntrinsicsNoiseGroup");
        if (intrinsicsNoiseGroupObj != null)
        {
            Debug.Log("Found IntrinsicsNoiseGroup GameObject");
            intrinsicsNoiseGroup = intrinsicsNoiseGroupObj.transform;
            InitializeIntrinsicsFields("IntrinsicsNoise", intrinsicsNoiseGroupObj.transform);
        }
        else
        {
            Debug.LogError("Cannot find IntrinsicsNoiseGroup in the scene");
        }

        GameObject extrinsicsGroupObj = GameObject.Find("ExtrinsicsGroup");
        if (extrinsicsGroupObj != null)
        {
            Debug.Log("Found ExtrinsicsGroup GameObject");
            extrinsicsGroup = extrinsicsGroupObj.GetComponent<RectTransform>();
            InitializeExtrinsicsFields("Extrinsics", extrinsicsGroupObj.transform);
        }
        else
        {
            Debug.LogError("Cannot find ExtrinsicsGroup in the scene");
        }

        GameObject extrinsicsNoiseGroupObj = GameObject.Find("ExtrinsicsNoiseGroup");
        if (extrinsicsNoiseGroupObj != null)
        {
            Debug.Log("Found ExtrinsicsNoiseGroup GameObject");
            extrinsicsNoiseGroup = extrinsicsNoiseGroupObj.transform;
            InitializeExtrinsicsFields("ExtrinsicsNoise", extrinsicsNoiseGroupObj.transform);
        }
        else
        {
            Debug.LogError("Cannot find ExtrinsicsNoiseGroup in the scene");
        }

        InitializeDefaultValues();
    }

    private void InitializeIntrinsicsFields(string groupName, Transform groupTransform)
    {
        Debug.Log($"Initializing {groupName} fields");

        groupInputs[groupName] = new InputFieldRefs();

        Transform fxInput = FindChildByName(groupTransform, "fxInput");
        if (fxInput != null)
        {
            groupInputs[groupName].fx = fxInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found fxInput: {groupInputs[groupName].fx != null}");

            if (groupInputs[groupName].fx != null)
            {
                SetupInputFieldListeners(groupInputs[groupName].fx, groupName, "fx");
            }
        }

        Transform fyInput = FindChildByName(groupTransform, "fyInput");
        if (fyInput != null)
        {
            groupInputs[groupName].fy = fyInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found fyInput: {groupInputs[groupName].fy != null}");

            if (groupInputs[groupName].fy != null)
            {
                SetupInputFieldListeners(groupInputs[groupName].fy, groupName, "fy");
            }
        }

        Transform cxInput = FindChildByName(groupTransform, "cxInput");
        if (cxInput != null)
        {
            groupInputs[groupName].cx = cxInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found cxInput: {groupInputs[groupName].cx != null}");

            if (groupInputs[groupName].cx != null)
            {
                SetupInputFieldListeners(groupInputs[groupName].cx, groupName, "cx");
            }
        }

        Transform cyInput = FindChildByName(groupTransform, "cyInput");
        if (cyInput != null)
        {
            groupInputs[groupName].cy = cyInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found cyInput: {groupInputs[groupName].cy != null}");

            if (groupInputs[groupName].cy != null)
            {
                SetupInputFieldListeners(groupInputs[groupName].cy, groupName, "cy");
            }
        }

        Transform widthInput = FindChildByName(groupTransform, "widthInput");
        if (widthInput != null)
        {
            groupInputs[groupName].width = widthInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found widthInput: {groupInputs[groupName].width != null}");

            if (groupInputs[groupName].width != null)
            {
                SetupInputFieldListeners(groupInputs[groupName].width, groupName, "width");
            }
        }

        Transform heightInput = FindChildByName(groupTransform, "heightInput");
        if (heightInput != null)
        {
            groupInputs[groupName].height = heightInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found heightInput: {groupInputs[groupName].height != null}");

            if (groupInputs[groupName].height != null)
            {
                SetupInputFieldListeners(groupInputs[groupName].height, groupName, "height");
            }
        }
    }

    private void InitializeExtrinsicsFields(string groupName, Transform groupTransform)
    {
        Debug.Log($"Initializing {groupName} fields");

        groupInputs[groupName] = new InputFieldRefs();


        Transform xInput = FindChildByName(groupTransform, "xInput");
        if (xInput != null)
        {
            groupInputs[groupName].x = xInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found xInput: {groupInputs[groupName].x != null}");

            if (groupInputs[groupName].x != null)
            {
                Debug.Log($"----------- IN HERE 1");
                SetupInputFieldListeners(groupInputs[groupName].x, groupName, "x");
            }
        }

        Transform yInput = FindChildByName(groupTransform, "yInput");
        if (yInput != null)
        {
            groupInputs[groupName].y = yInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found yInput: {groupInputs[groupName].y != null}");

            if (groupInputs[groupName].y != null)
            {
                Debug.Log($"----------- IN HERE 2");
                SetupInputFieldListeners(groupInputs[groupName].y, groupName, "y");
            }
        }

        Transform zInput = FindChildByName(groupTransform, "zInput");
        if (zInput != null)
        {
            groupInputs[groupName].z = zInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found zInput: {groupInputs[groupName].z != null}");

            if (groupInputs[groupName].z != null)
            {
                Debug.Log($"----------- IN HERE 3");
                SetupInputFieldListeners(groupInputs[groupName].z, groupName, "z");
            }
        }

        Transform rxInput = FindChildByName(groupTransform, "rxInput");
        if (rxInput != null)
        {
            groupInputs[groupName].rx = rxInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found rxInput: {groupInputs[groupName].rx != null}");

            if (groupInputs[groupName].rx != null)
            {
                Debug.Log($"----------- IN HERE 4");
                SetupInputFieldListeners(groupInputs[groupName].rx, groupName, "rx");
            }
        }

        Transform ryInput = FindChildByName(groupTransform, "ryInput");
        if (ryInput != null)
        {
            groupInputs[groupName].ry = ryInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found ryInput: {groupInputs[groupName].ry != null}");

            if (groupInputs[groupName].ry != null)
            {
                Debug.Log($"----------- IN HERE 5");
                SetupInputFieldListeners(groupInputs[groupName].ry, groupName, "ry");
            }
        }

        Transform rzInput = FindChildByName(groupTransform, "rzInput");
        if (rzInput != null)
        {
            groupInputs[groupName].rz = rzInput.GetComponent<TMP_InputField>();
            Debug.Log($"Found rzInput: {groupInputs[groupName].rz != null}");

            if (groupInputs[groupName].rz != null)
            {
                Debug.Log($"----------- IN HERE 6");
                SetupInputFieldListeners(groupInputs[groupName].rz, groupName, "rz");
            }
        }

    }

    private Transform FindChildByName(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            Debug.Log($"Found {childName} as direct child of {parent.name}");
            return child;
        }

        string childrenNames = "";
        for (int i = 0; i < parent.childCount; i++)
        {
            childrenNames += parent.GetChild(i).name + ", ";
        }
        Debug.Log($"Children of {parent.name}: {childrenNames}");

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform childTransform = parent.GetChild(i);
            Debug.Log($"Checking child: {childTransform.name}");

            if (childTransform.name == childName)
            {
                Debug.Log($"Found match: {childName}");
                return childTransform;
            }

            Transform result = FindChildByName(childTransform, childName);
            if (result != null)
                return result;
        }

        Debug.Log($"Could not find {childName} in {parent.name} or its children");
        return null;
    }


    private void SetupInputFieldListeners(TMP_InputField inputField, string groupName, string fieldName)
    {
        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();

        inputField.onValueChanged.AddListener((value) => {
            Debug.Log($"{fieldName}Input value changed to: {value}");
            OnValueChanged(groupName, fieldName, value);
        });

        inputField.onEndEdit.AddListener((value) => {
            Debug.Log($"{fieldName}Input editing ended with value: {value}");
            OnValueChanged(groupName, fieldName, value);
        });
    }

    private void InitializeDefaultValues()
    {
        foreach (string group in new[] { "Intrinsics", "IntrinsicsNoise" })
        {
            groupValues[group]["fx"] = 0f;
            groupValues[group]["fy"] = 0f;
            groupValues[group]["cx"] = 0f;
            groupValues[group]["cy"] = 0f;
            groupValues[group]["width"] = 0f;
            groupValues[group]["height"] = 0f;
        }

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
        Debug.Log($"OnValueChanged called for {groupName}.{fieldName} = {value}");

        if (!groupValues.ContainsKey(groupName))
        {
            Debug.LogError($"Group '{groupName}' not found in groupValues dictionary");
            groupValues[groupName] = new Dictionary<string, float>();
        }

        if (!groupValues[groupName].ContainsKey(fieldName))
        {
            Debug.Log($"Field '{fieldName}' not found in {groupName}, initializing to 0");
            groupValues[groupName][fieldName] = 0f;
        }

        if (string.IsNullOrEmpty(value) || !float.TryParse(value, out float parsedValue))
        {
            Debug.LogWarning($"Could not parse '{value}' as float for {groupName}.{fieldName}, using 0");
            parsedValue = 0f;
        }

        groupValues[groupName][fieldName] = parsedValue;

        Debug.Log($"Updated {groupName}.{fieldName} to {parsedValue}");

        Debug.Log($"Current values in {groupName}:");
        foreach (var pair in groupValues[groupName])
        {
            Debug.Log($"  {pair.Key}: {pair.Value}");
        }
    }


    private void RestoreInputValues()
    {
        foreach (string group in new[] { "Intrinsics", "IntrinsicsNoise" })
        {
            if (groupInputs.ContainsKey(group) && groupValues.ContainsKey(group))
            {
                var inputs = groupInputs[group];
                var values = groupValues[group];

                if (inputs.fx != null && values.ContainsKey("fx"))
                    inputs.fx.text = values["fx"].ToString();

                if (inputs.fy != null && values.ContainsKey("fy"))
                    inputs.fy.text = values["fy"].ToString();

                if (inputs.cx != null && values.ContainsKey("cx"))
                    inputs.cx.text = values["cx"].ToString();

                if (inputs.cy != null && values.ContainsKey("cy"))
                    inputs.cy.text = values["cy"].ToString();

                if (inputs.width != null && values.ContainsKey("width"))
                    inputs.width.text = values["width"].ToString();

                if (inputs.height != null && values.ContainsKey("height"))
                    inputs.height.text = values["height"].ToString();
            }
        }

        foreach (string group in new[] { "Extrinsics", "ExtrinsicsNoise" })
        {
            if (groupInputs.ContainsKey(group) && groupValues.ContainsKey(group))
            {
                var inputs = groupInputs[group];
                var values = groupValues[group];

                if (inputs.x != null && values.ContainsKey("x"))
                    inputs.x.text = values["x"].ToString();

                if (inputs.y != null && values.ContainsKey("y"))
                    inputs.y.text = values["y"].ToString();

                if (inputs.z != null && values.ContainsKey("z"))
                    inputs.z.text = values["z"].ToString();

                if (inputs.rx != null && values.ContainsKey("rx"))
                    inputs.rx.text = values["rx"].ToString();

                if (inputs.ry != null && values.ContainsKey("ry"))
                    inputs.ry.text = values["ry"].ToString();

                if (inputs.rz != null && values.ContainsKey("rz"))
                    inputs.rz.text = values["rz"].ToString();
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
