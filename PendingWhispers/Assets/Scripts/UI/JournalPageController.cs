using System.Collections;
using UnityEngine;

public abstract class JournalPageController : MonoBehaviour
{
    [SerializeField] protected GameObject root;

    protected virtual void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    protected virtual void OnEnable()
    {
        Show();
        StartCoroutine(RefreshNextFrame());
    }

    protected virtual void OnDisable()
    {
        Hide();
    }

    protected IEnumerator RefreshNextFrame()
    {
        yield return null;
        Refresh();
    }

    public virtual void Show()
    {
        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public abstract void Refresh();
}