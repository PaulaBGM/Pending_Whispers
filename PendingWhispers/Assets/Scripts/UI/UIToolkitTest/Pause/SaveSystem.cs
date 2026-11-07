using System;
using System.Collections.Generic;
using System.IO;
using Inventory.Model;
using UnityEngine;
using UnityEngine.SceneManagement;

// =========================================================
// Guardado / carga por slots (de momento se usa solo el 0).
// - Save(slot): captura el estado y lo escribe en disco.
// - Load(slot): carga la escena guardada y, cuando termina de
//   cargarse (antes de los Start), restaura el estado.
// - GetSlotInfo(slot): cabecera ligera para una futura pantalla de slots.
// =========================================================

public class SaveSystem : BaseSingleton<SaveSystem>
{
    private const int CurrentVersion = 1;
    private const string FilePrefix = "save_";
    private const string FileExtension = ".json";

    [SerializeField] private SaveAssetRegistrySO registry;
    [SerializeField] private InventorySO inventory;

    private SaveData pendingLoad;
    private int pendingSlot = -1;

    public event Action<int> OnGameSaved;
    public event Action<int> OnGameLoaded;

    /// <summary>True mientras se aplica una partida cargada (p. ej. para que el HUD no avise de cada pista restaurada).</summary>
    public bool IsRestoring { get; private set; }

    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        base.OnDestroy();
    }

    // =========================================================
    // SLOTS
    // =========================================================

    public bool HasSave(int slot) => File.Exists(GetSlotPath(slot));

    public SaveSlotInfo GetSlotInfo(int slot) => ReadFromDisk(slot)?.info;

    public void DeleteSave(int slot)
    {
        string path = GetSlotPath(slot);

        if (File.Exists(path))
            File.Delete(path);
    }

    private static string GetSlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"{FilePrefix}{slot}{FileExtension}");

    // =========================================================
    // SAVE
    // =========================================================

    public bool Save(int slot)
    {
        if (registry == null)
        {
            Debug.LogError("[SaveSystem] Falta asignar el SaveAssetRegistrySO.");
            return false;
        }

        try
        {
            SaveData data = CaptureState();
            WriteToDisk(slot, JsonUtility.ToJson(data, true));

            Debug.Log($"[SaveSystem] Partida guardada en slot {slot}: {GetSlotPath(slot)}");
            OnGameSaved?.Invoke(slot);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Error al guardar en slot {slot}: {e}");
            return false;
        }
    }

    private SaveData CaptureState()
    {
        CaseData currentCaseData = CaseManager.Instance != null
            ? CaseManager.Instance.GetCurrentCaseData()
            : null;

        var data = new SaveData
        {
            version = CurrentVersion,
            info = new SaveSlotInfo
            {
                sceneName = SceneManager.GetActiveScene().name,
                caseTitle = currentCaseData != null ? currentCaseData.caseTitle : string.Empty,
                savedAtUtc = DateTime.UtcNow.ToString("o")
            },
            mapNodeID = MapState.Instance != null ? MapState.Instance.GetCurrentNode() : null,
            currentCaseID = registry.GetId(currentCaseData)
        };

        // Flags
        if (GameProgress.Instance != null)
        {
            foreach (FlagSO flag in GameProgress.Instance.GetFlags())
            {
                string id = registry.GetId(flag);
                if (id != null)
                    data.flagIDs.Add(id);
            }
        }

        // Inventario
        if (inventory != null)
        {
            foreach (var slot in inventory.GetCurrentInventoryState())
            {
                string id = registry.GetId(slot.Value.item);
                if (id == null)
                    continue;

                data.inventory.Add(new InventorySlotSaveData
                {
                    index = slot.Key,
                    itemID = id,
                    quantity = slot.Value.quantity
                });
            }
        }

        // Dialogos ya hablados
        if (DialogueManager.Instance != null)
            data.seenDialogueTopics.AddRange(DialogueManager.Instance.GetSeenDialogueTopics());

        // Casos (las pistas encontradas no se guardan: se reconstruyen desde los flags)
        if (CaseJournalSystem.Instance != null)
        {
            foreach (CaseRuntime runtime in CaseJournalSystem.Instance.GetAllCases())
            {
                string id = registry.GetId(runtime.data);
                if (id == null)
                    continue;

                data.cases.Add(new CaseSaveData
                {
                    caseID = id,
                    isResolved = runtime.isResolved,
                    chosenOutcome = runtime.chosenOutcome
                });
            }
        }

        return data;
    }

    // =========================================================
    // LOAD
    // =========================================================

    public bool Load(int slot)
    {
        SaveData data = ReadFromDisk(slot);

        if (data == null || string.IsNullOrEmpty(data.info?.sceneName))
        {
            Debug.LogWarning($"[SaveSystem] No hay partida válida en el slot {slot}.");
            return false;
        }

        pendingLoad = data;
        pendingSlot = slot;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        LoadScene(data.info.sceneName);
        return true;
    }

    // sceneLoaded se lanza tras Awake/OnEnable de la escena y antes de los Start,
    // así MapManager, el HUD, etc. leen ya el estado restaurado en su Start.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingLoad == null || scene.name != pendingLoad.info.sceneName)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        IsRestoring = true;
        try
        {
            ApplyState(pendingLoad);
        }
        finally
        {
            IsRestoring = false;
        }

        int slot = pendingSlot;
        pendingLoad = null;
        pendingSlot = -1;

        Debug.Log($"[SaveSystem] Partida del slot {slot} restaurada.");
        OnGameLoaded?.Invoke(slot);
    }

    private void ApplyState(SaveData data)
    {
        if (data.version > CurrentVersion)
            Debug.LogWarning($"[SaveSystem] La partida es de una versión más nueva ({data.version}).");

        // 1. Flags primero: inventario y casos dependen de ellos.
        var flags = new List<FlagSO>();
        foreach (string id in data.flagIDs)
        {
            FlagSO flag = registry.Get<FlagSO>(id);
            if (flag != null)
                flags.Add(flag);
        }

        if (GameProgress.Instance != null)
            GameProgress.Instance.RestoreFlags(flags);

        // 2. Mapa
        if (MapState.Instance != null)
            MapState.Instance.SetCurrentNode(data.mapNodeID ?? string.Empty);

        // 3. Inventario
        if (inventory != null)
        {
            var slots = new List<(int index, ItemSO item, int quantity)>();

            foreach (InventorySlotSaveData slot in data.inventory)
            {
                ItemSO item = registry.Get<ItemSO>(slot.itemID);
                if (item != null)
                    slots.Add((slot.index, item, slot.quantity));
            }

            inventory.RestoreSlots(slots);
        }

        // 4. Casos
        RestoreCases(data);

        // 5. Dialogos ya hablados
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.RestoreSeenDialogueTopics(data.seenDialogueTopics);
    }

    private void RestoreCases(SaveData data)
    {
        CaseData currentData = registry.Get<CaseData>(data.currentCaseID);
        CaseRuntime currentRuntime = null;

        if (CaseJournalSystem.Instance != null)
        {
            CaseJournalSystem.Instance.Clear();

            foreach (CaseSaveData saved in data.cases)
            {
                CaseData caseData = registry.Get<CaseData>(saved.caseID);
                if (caseData == null)
                    continue;

                var runtime = new CaseRuntime(caseData);

                // Pistas: se reconstruyen desde los flags ya restaurados.
                if (caseData.requiredClues != null && GameProgress.Instance != null)
                {
                    foreach (FlagSO clue in caseData.requiredClues)
                    {
                        if (clue != null && GameProgress.Instance.HasFlag(clue))
                            runtime.AddClue(clue);
                    }
                }

                // Se asigna el estado directamente: Resolve() no se llama
                // para no repetir efectos (flags, reputación, feedback).
                if (saved.isResolved)
                {
                    runtime.isResolved = true;
                    runtime.chosenOutcome = saved.chosenOutcome;
                }

                CaseJournalSystem.Instance.TryAddCase(runtime);

                if (caseData == currentData)
                    currentRuntime = runtime;
            }
        }

        if (CaseManager.Instance != null)
            CaseManager.Instance.RestoreCurrentCase(currentData, currentRuntime);
    }

    // =========================================================
    // NEW GAME
    // =========================================================

    // Llamar al empezar partida nueva desde el menú principal:
    // los singletons persistentes conservan el estado de la partida anterior.
    public void ResetRuntimeState()
    {
        if (GameProgress.Instance != null)
            GameProgress.Instance.RestoreFlags(null);

        if (inventory != null)
            inventory.RestoreSlots(null);

        if (CaseJournalSystem.Instance != null)
            CaseJournalSystem.Instance.Clear();

        if (CaseManager.Instance != null)
            CaseManager.Instance.RestoreCurrentCase(null, null);

        if (MapState.Instance != null)
        {
            MapState.Instance.SetCurrentNode(string.Empty);
            MapState.Instance.ClearRevealed();
        }

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.RestoreSeenDialogueTopics(null);
    }

    // =========================================================
    // DISK
    // =========================================================

    private static void WriteToDisk(int slot, string json)
    {
        string path = GetSlotPath(slot);
        string tempPath = path + ".tmp";

        // Se escribe primero en un temporal para no corromper la partida
        // existente si algo falla a mitad de escritura.
        File.WriteAllText(tempPath, json);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(tempPath, path);
    }

    private static SaveData ReadFromDisk(int slot)
    {
        string path = GetSlotPath(slot);

        if (!File.Exists(path))
            return null;

        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] No se pudo leer el slot {slot}: {e}");
            return null;
        }
    }

    private static void LoadScene(string sceneName)
    {
        if (SceneController.Instance != null)
            SceneController.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
