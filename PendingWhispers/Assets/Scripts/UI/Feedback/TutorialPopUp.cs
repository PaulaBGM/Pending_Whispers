using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialPopup : MonoBehaviour
{
    public static TutorialPopup Instance;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    //[SerializeField] private Image icon;
    [SerializeField] private Button continueButton;

    private System.Action onClose;
    private PlayerController player;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);

        continueButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
    }

    public void ShowTutorial(
        string title,
        string description,
        Sprite tutorialIcon = null,
        System.Action callback = null)
    {
        panel.SetActive(true);

        titleText.text = title;
        bodyText.text = description;

       /* if (icon != null)
        {
            icon.gameObject.SetActive(tutorialIcon != null);

            if (tutorialIcon != null)
                icon.sprite = tutorialIcon;
        }*/

        onClose = callback;

        if (player != null)
            player.canMove = false;
    }

    private void Close()
    {
        panel.SetActive(false);

        if (player != null)
            player.canMove = true;

        onClose?.Invoke();
    }
}