using UnityEngine;

public class CatacombTutorial : MonoBehaviour
{

    [Header("Flags")]
    [SerializeField] private FlagSO entercatacombs;
    private void Start()
    {
        if (!GameProgress.Instance.HasFlag(entercatacombs))
        {
            GameProgress.Instance.AddFlag(entercatacombs); 
        }
        TutorialPopup.Instance.ShowTutorialOnce("tracking", "Spectral Wave", "Press the left mouse button or click the icon in the bottom-right corner to activate Spectral Tracking.\n\nUse it to uncover hidden traces and clues left behind by spirits.");        
    }
}