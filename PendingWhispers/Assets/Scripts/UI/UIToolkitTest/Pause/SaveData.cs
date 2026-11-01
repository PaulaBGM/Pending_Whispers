using System;
using System.Collections.Generic;
using System.Globalization;

// =========================================================
// Datos planos que se serializan a JSON (JsonUtility).
// Los ScriptableObjects se guardan por ID (GUID del asset),
// nunca por referencia, y se resuelven con SaveAssetRegistrySO.
// =========================================================

[Serializable]
public class SaveData
{
    // Sube este número si cambias la estructura, para poder migrar partidas antiguas.
    public int version;

    // Cabecera ligera: es lo que mostraría una futura pantalla de slots.
    public SaveSlotInfo info = new();

    // Mundo
    public string mapNodeID;

    // Progreso
    public List<string> flagIDs = new();
    public List<InventorySlotSaveData> inventory = new();

    // Casos
    public string currentCaseID;
    public List<CaseSaveData> cases = new();
}

[Serializable]
public class SaveSlotInfo
{
    public string sceneName;
    public string caseTitle;
    public string savedAtUtc; // ISO 8601 ("o")

    public DateTime SavedAtLocal =>
        DateTime.TryParse(savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date)
            ? date.ToLocalTime()
            : DateTime.MinValue;
}

[Serializable]
public class InventorySlotSaveData
{
    public int index;
    public string itemID;
    public int quantity;
}

[Serializable]
public class CaseSaveData
{
    public string caseID;
    public bool isResolved;
    public string chosenOutcome;
}
