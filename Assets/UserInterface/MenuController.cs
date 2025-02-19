using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject settingsMenu;  // The "Settings Menu" GameObject in your Canvas
    [SerializeField] private Button menuButton;        // The "Menu Button" that will open/close the dialog

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
    // Multi-Camera Support
    // ---------------------------------------------------------
    [Header("Multi-Camera Settings")]
    [SerializeField] private Toggle multiCameraToggle;
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
    [SerializeField] private InputField outputPathField;
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

    private void Start()
    {
        settingsMenu = GameObject.Find("SettingsMenu"); // Find the GameObject by name
        menuButton = GameObject.Find("MenuButton").GetComponent<Button>(); // Find the Button component
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