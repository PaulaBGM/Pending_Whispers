using UnityEngine;
using Inventory.Model;

public class InventoryRuntime : MonoBehaviour
{
    public static InventoryRuntime Instance { get; private set; }

    [SerializeField] private InventorySO inventory;
    
    [SerializeField] private PeopleJournalSystem peopleJournal;
    [SerializeField] private CaseJournalSystem caseJournal;

    private bool initialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);

            if (!initialized)
            {
                inventory.Initialize();
                initialized = true;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public InventorySO GetInventory()
    {
        return inventory;
    }
    
    public PeopleJournalSystem GetPeopleJournal()
    {
        return peopleJournal;
    }

    public CaseJournalSystem GetCaseJournal()
    {
        return caseJournal;
    }
}