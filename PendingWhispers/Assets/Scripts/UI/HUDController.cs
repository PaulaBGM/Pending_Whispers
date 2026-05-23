using UnityEngine;

public class HUDController : MonoBehaviour
{
    private void Start()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (UIManager.Instance == null)
        {
            Invoke(nameof(TrySubscribe), 0.1f);
            return;
        }

        UIManager.Instance.OnJournalStateChanged += HandleJournal;
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.OnJournalStateChanged -= HandleJournal;
    }

    private void HandleJournal(bool open)
    {
        gameObject.SetActive(!open);
    }
}
