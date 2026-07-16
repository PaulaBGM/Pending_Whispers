using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PeopleEntryUI : JournalEntryUI<PersonJournalEntry>
{
    [Header("UI")]
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text title;
    [SerializeField] private Image borderImage;

    public void SetData(PersonJournalEntry entry)
    {
        if (entry == null)
        {
            ResetData();
            return;
        }

        SetEntry(entry);

        if (portrait != null)
        {
            portrait.sprite = entry.portrait;
            portrait.color = Color.white;
            portrait.enabled = true;
            portrait.gameObject.SetActive(true);
        }

        if (title != null)
            title.text = entry.personName;
    }

    public override void ResetData()
    {
        base.ResetData();

        if (portrait != null)
        {
            portrait.sprite = null;
            portrait.gameObject.SetActive(false);
        }

        if (title != null)
            title.text = "";
    }
}