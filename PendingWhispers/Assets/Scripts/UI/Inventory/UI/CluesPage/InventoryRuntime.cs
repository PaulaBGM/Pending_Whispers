using UnityEngine;
using Inventory.Model;

public class InventoryRuntime : BaseSingleton<InventoryRuntime>
{
    [SerializeField] private InventorySO inventory;   
    [SerializeField] private PeopleJournalSystem peopleJournal;
    [SerializeField] private CaseJournalSystem caseJournal;

    private bool initialized = false;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        inventory.Initialize(); 
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