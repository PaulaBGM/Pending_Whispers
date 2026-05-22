using UnityEngine;

public class PeoplePageController : MonoBehaviour
{
    [SerializeField] private GameObject listPanel;
    [SerializeField] private GameObject detailPanel;

    public void SelectPerson()
    {
        // aquí luego conectas diálogo
        Debug.Log("Mostrar diálogo persona");
    }

    public void ShowList()
    {
        listPanel.SetActive(true);
        detailPanel.SetActive(false);
    }

    public void ShowDetail()
    {
        listPanel.SetActive(false);
        detailPanel.SetActive(true);
    }
}