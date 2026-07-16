using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PeopleEntryUI : JournalEntryUI<PersonJournalEntry>
{
    [Header("UI")]
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text title;

    public void SetData(PersonJournalEntry entry)
    {
        if (entry == null)
        {
            ResetData();
            return;
        }

        SetEntry(entry);

        portrait.gameObject.SetActive(true);
        portrait.sprite = entry.portrait;
        portrait.color = Color.white;
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