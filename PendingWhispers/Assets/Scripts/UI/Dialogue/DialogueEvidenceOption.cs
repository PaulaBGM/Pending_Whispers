using Inventory.Model;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueEvidenceOption
{
    public ItemSO item;
    public string nextNodeID;
    [Header("Efectos opcionales al acertar")]
    public List<FlagSO> addFlags;
    public GameEventSO onSelectedEvent;
}