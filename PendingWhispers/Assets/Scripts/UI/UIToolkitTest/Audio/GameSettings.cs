using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Ajustes del juego (menú de opciones del diario): volumen de efectos, de música y brillo.
/// Se guardan en PlayerPrefs y se aplican solos al arrancar y en cada escena.
///
/// Audio (FMOD):
/// - Efectos: bus "bus:/SFX" si existe en FMOD Studio; si no, el bus maestro "bus:/".
/// - Música: bus "bus:/Music". Mientras no exista, el slider de música sale desactivado.
///   Cuando metáis la música, cread en FMOD Studio los grupos/buses "SFX" y "Music" (en el Mixer),
///   enviad cada evento a su bus y haced Build: esto los encuentra sin tocar código.
///
/// También se asegura de que cada escena tenga un FMOD Studio Listener en la cámara
/// (ninguna lo tenía, así que los sonidos con posición se oían mal o no se oían).
/// </summary>
public static class GameSettings
{
    public const string SfxBusPath = "bus:/SFX";
    public const string MusicBusPath = "bus:/Music";
    private const string MasterBusPath = "bus:/";

    private const string SfxKey = "settings.sfxVolume";
    private const string MusicKey = "settings.musicVolume";
    private const string BrightnessKey = "settings.brightness";

    /// <summary>0..1</summary>
    public static float SfxVolume { get; private set; } = 1f;
    /// <summary>0..1</summary>
    public static float MusicVolume { get; private set; } = 1f;
    /// <summary>0..1 (0.5 = brillo original de la escena).</summary>
    public static float Brightness { get; private set; } = 0.5f;

    // Multiplicador de la luz global con el brillo al mínimo y al máximo (0.5 = x1).
    private const float MinBrightnessMultiplier = 0.4f;
    private const float MaxBrightnessMultiplier = 1.6f;

    // Intensidad original de cada luz global, para no ir multiplicando sobre lo ya multiplicado.
    private static readonly Dictionary<Light2D, float> baseIntensities = new();

    // =========================
    // ARRANQUE
    // =========================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
        Brightness = PlayerPrefs.GetFloat(BrightnessKey, 0.5f);

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        ApplyAudio();
        PrepareScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Los buses no cambian entre escenas, pero se reaplica por si FMOD aún no había cargado los bancos.
        ApplyAudio();
        PrepareScene();
    }

    private static void PrepareScene()
    {
        baseIntensities.Clear();
        EnsureListener();
        ApplyBrightness();
    }

    // =========================
    // API (la usa UIT_SettingsPageView)
    // =========================
    public static void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        ApplyAudio();
    }

    public static void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        ApplyAudio();
    }

    public static void SetBrightness(float value)
    {
        Brightness = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(BrightnessKey, Brightness);
        ApplyBrightness();
    }

    /// <summary>Guarda en disco (al cerrar la página de opciones).</summary>
    public static void Save() => PlayerPrefs.Save();

    public static bool HasMusicBus => TryGetBus(MusicBusPath, out _);

    // =========================
    // AUDIO
    // =========================
    private static void ApplyAudio()
    {
        if (TryGetBus(SfxBusPath, out FMOD.Studio.Bus sfx) || TryGetBus(MasterBusPath, out sfx))
            sfx.setVolume(SfxVolume);

        if (TryGetBus(MusicBusPath, out FMOD.Studio.Bus music))
            music.setVolume(MusicVolume);
    }

    private static bool TryGetBus(string path, out FMOD.Studio.Bus bus)
    {
        bus = default;

        try
        {
            return RuntimeManager.StudioSystem.isValid()
                   && RuntimeManager.StudioSystem.getBus(path, out bus) == FMOD.RESULT.OK
                   && bus.isValid();
        }
        catch (System.Exception)
        {
            // FMOD aún sin inicializar (p. ej. al salir del Play).
            return false;
        }
    }

    // =========================
    // LISTENER
    // =========================
    private static void EnsureListener()
    {
        if (Object.FindFirstObjectByType<StudioListener>() != null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        var listener = cam.gameObject.AddComponent<StudioListener>();

        // La cámara está a z = -10: la distancia (volumen) se mide desde Lyra, la posición
        // (izquierda/derecha) desde la cámara. Sin jugador (mapa, menú) se usa la cámara.
        var player = Object.FindFirstObjectByType<PlayerController_Actions>();
        if (player != null)
            listener.AttenuationObject = player.gameObject;
    }

    // =========================
    // BRILLO
    // =========================
    private static void ApplyBrightness()
    {
        float multiplier = Mathf.Lerp(MinBrightnessMultiplier, MaxBrightnessMultiplier, Brightness);

        foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (light == null || light.lightType != Light2D.LightType.Global)
                continue;

            if (!baseIntensities.TryGetValue(light, out float baseIntensity))
            {
                baseIntensity = light.intensity;
                baseIntensities[light] = baseIntensity;
            }

            light.intensity = baseIntensity * multiplier;
        }
    }
}
