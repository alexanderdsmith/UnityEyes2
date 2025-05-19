using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuControllerHelp : MonoBehaviour
{
    [SerializeField] private GameObject helpPopup;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button exitHelpButton;
    [SerializeField] private TextMeshProUGUI helpText;

    private void Start()
    {
        // Locate UI elements
        helpButton = GameObject.Find("HelpButton").GetComponent<Button>();
        // helpPopup = GameObject.Find("HelpPopup");
        exitHelpButton = GameObject.Find("HelpPopup/ExitButton").GetComponent<Button>();
        helpText = GameObject.Find("HelpPopup/HelpText").GetComponent<TextMeshProUGUI>();

        // Set initial visibility
        if (helpPopup != null)
            helpPopup.SetActive(false);

        // Assign listeners
        if (helpButton != null)
            helpButton.onClick.AddListener(ShowHelpPopup);

        if (exitHelpButton != null)
            exitHelpButton.onClick.AddListener(CloseHelpPopup);

        // Set help content
        string hotkeys =
            "s: save image\n" +
            "c: randomize camera and eye parameters\n" +
            "r: randomize scene parameters\n" +
            "l: randomize lighting parameters\n" +
            "h: hide UI button overlays\n" +
            "p: toggle preview mode\n" +
            "→: increment camera index\n" +
            "←: decrement camera index\n" +
            "Esc: close the UE2 application";

        if (helpText != null)
            helpText.text = hotkeys;
    }

    private void ShowHelpPopup()
    {
        helpPopup?.SetActive(true);
    }

    private void CloseHelpPopup()
    {
        helpPopup?.SetActive(false);
    }
}
