using System;
using System.Collections.Generic;
using Inventory.Model;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

// =========================================================
// Traduce ScriptableObjects <-> ID estable (GUID del asset).
// El GUID no cambia aunque se renombre o mueva el asset,
// así que las partidas guardadas siguen siendo válidas.
//
// Se rellena desde el editor (clic derecho en el asset > Rebuild Registry)
// y se reconstruye automáticamente antes de cada build.
// =========================================================

[CreateAssetMenu(fileName = "SaveAssetRegistry", menuName = "Save/Save Asset Registry")]
public class SaveAssetRegistrySO : ScriptableObject
{
    [Serializable]
    private struct Entry
    {
        public string id;
        public ScriptableObject asset;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, ScriptableObject> assetsById;
    private Dictionary<ScriptableObject, string> idsByAsset;

    private void OnEnable() => ClearLookup();

    public string GetId(ScriptableObject asset)
    {
        if (asset == null)
            return null;

        EnsureLookup();

        if (idsByAsset.TryGetValue(asset, out string id))
            return id;

        Debug.LogWarning(
            $"[SaveRegistry] '{asset.name}' no está registrado y no se guardará. " +
            $"Haz 'Rebuild Registry' en '{name}'.", asset);

        return null;
    }

    public T Get<T>(string id) where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(id))
            return null;

        EnsureLookup();

        if (assetsById.TryGetValue(id, out ScriptableObject asset) && asset is T typed)
            return typed;

        Debug.LogWarning($"[SaveRegistry] No existe ningún {typeof(T).Name} con id '{id}'.");
        return null;
    }

    private void EnsureLookup()
    {
        if (assetsById != null)
            return;

        assetsById = new Dictionary<string, ScriptableObject>();
        idsByAsset = new Dictionary<ScriptableObject, string>();

        foreach (Entry entry in entries)
        {
            if (entry.asset == null || string.IsNullOrEmpty(entry.id))
                continue;

            assetsById[entry.id] = entry.asset;
            idsByAsset[entry.asset] = entry.id;
        }
    }

    private void ClearLookup()
    {
        assetsById = null;
        idsByAsset = null;
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Registry")]
    public void Rebuild()
    {
        entries.Clear();

        // Añade aquí cualquier otro tipo de SO que haya que guardar.
        Collect<FlagSO>();
        Collect<ItemSO>();
        Collect<CaseData>();

        ClearLookup();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SaveRegistry] {entries.Count} assets registrados.", this);
    }

    private void Collect<T>() where T : ScriptableObject
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

            if (asset != null)
                entries.Add(new Entry { id = guid, asset = asset });
        }
    }
#endif
}

#if UNITY_EDITOR
// Reconstruye el registro antes de cada build para que no se quede ningún asset fuera.
public class SaveAssetRegistryBuildStep : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(SaveAssetRegistrySO)}"))
        {
            var registry = AssetDatabase.LoadAssetAtPath<SaveAssetRegistrySO>(AssetDatabase.GUIDToAssetPath(guid));

            if (registry != null)
                registry.Rebuild();
        }
    }
}
#endif
