using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialPopup : BaseSingleton<TutorialPopup>
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image icon;
    [SerializeField] private Button continueButton;

    private Action onClose;

    public bool IsShowing { get; private set; }

    protected override void Awake()
    {
        if (transform.parent != null)
            transform.SetParent(null);

        base.Awake();
        if (Instance != this)
            return;

        if (panel != null)
            panel.SetActive(false);

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(Close);
        }
    }

    public void ShowTutorial(string title,string description,Sprite tutorialIcon = null,Action callback = null)
    {
        if (IsShowing)
            return;

        IsShowing = true;

        if (panel != null)
            panel.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = description;

        if (icon != null)
        {
            bool hasIcon = tutorialIcon != null;

            icon.gameObject.SetActive(hasIcon);

            if (hasIcon)
                icon.sprite = tutorialIcon;
        }

        onClose = callback;
    }

    public void ShowTutorialOnce(string tutorialID,string title,string description,Sprite tutorialIcon = null,Action callback = null)
    {
        if (TutorialManager.Instance == null)
        {
            Debug.LogWarning("[TutorialPopup] TutorialManager no encontrado");
            return;
        }

        if (!TutorialManager.Instance.TryShowTutorial(tutorialID))
            return;

        ShowTutorial(title, description, tutorialIcon, callback);
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);

        IsShowing = false;

        onClose?.Invoke();
        onClose = null;
    }
}