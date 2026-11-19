using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Inventory.Model;

namespace Inventory
{
    /// <summary>
    /// Objeto que Lyra lleva en la mano tras pulsar "Use" en el diario.
    ///
    /// Es el único sitio que guarda ese estado: ni el diario ni el jugador lo guardan,
    /// solo lo leen o lo cambian a través de aquí (evita un "modo usar" con flags).
    /// - Hold(item): pone el objeto en la mano.
    /// - Clear(): lo suelta (al usarlo en un hotspot, al cancelar, al cambiar de escena...).
    /// - OnChanged: avisa a la vista (PlayerHeldItemView) y a quien lo necesite.
    ///
    /// No se guarda en la partida: al cambiar de escena o cargar, la mano queda vacía.
    /// </summary>
    public static class HeldItemState
    {
        public static ObjectItemSO Current { get; private set; }
        public static bool IsHolding => Current != null;

        public static event Action<ObjectItemSO> OnChanged;

        public static void Hold(ObjectItemSO item)
        {
            if (item == Current)
                return;

            Current = item;
            Debug.Log(item != null ? $"[HeldItem] Lyra coge '{item.Name}'" : "[HeldItem] Mano vacía");
            OnChanged?.Invoke(Current);
        }

        public static void Clear() => Hold(null);

        // Se resetea al arrancar (también con Domain Reload desactivado) y en cada cambio de escena.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            OnChanged = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene _, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
                Clear();
        }
    }
}
