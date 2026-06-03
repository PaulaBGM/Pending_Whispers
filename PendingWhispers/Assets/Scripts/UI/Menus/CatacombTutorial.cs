using UnityEngine;

public class CatacombTutorial : MonoBehaviour
{
    private void Start()
    {
        TutorialPopup.Instance.ShowTutorialOnce("tracking","Rastreo espectral","Pulsa el click izquierdo o el icono de la esquina inferior derecha para activar el rastreo espectral.\n\nEsta habilidad revela rastros y pistas ocultas relacionadas con los espíritus."
        );
    }
}