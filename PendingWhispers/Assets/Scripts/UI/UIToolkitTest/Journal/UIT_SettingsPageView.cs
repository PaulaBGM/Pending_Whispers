using FMODUnity;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Página de opciones del diario (VE_SettingsPage). Sustituye a UIT_StaticJournalPageView en Page_Settings.
/// - SL_Sfx: volumen de efectos (FMOD). Al soltar el slider suena un efecto de prueba.
/// - SL_Music: volumen de música. Desactivado hasta que exista el bus "bus:/Music" en FMOD.
/// - SL_Brightness: brillo (luces globales 2D de la escena).
/// Los valores se guardan en GameSettings (PlayerPrefs).
/// </summary>
public class UIT_SettingsPageView : UIT_JournalPageView
{
    [Header("UXML")]
    [SerializeField] private string sfxSliderName = "SL_Sfx";
    [SerializeField] private string musicSliderName = "SL_Music";
    [SerializeField] private string brightnessSliderName = "SL_Brightness";

    [Header("Sonido de prueba")]
    [Tooltip("Suena al soltar el slider de efectos, para oír el volumen elegido.")]
    [SerializeField] private EventReference sfxPreviewEvent;

    private Slider sfxSlider;
    private Slider musicSlider;
    private Slider brightnessSlider;

    protected override void OnBind(VisualElement journalRoot)
    {
        sfxSlider = SetupSlider(sfxSliderName, value => GameSettings.SetSfxVolume(value / 100f));
        musicSlider = SetupSlider(musicSliderName, value => GameSettings.SetMusicVolume(value / 100f));
        brightnessSlider = SetupSlider(brightnessSliderName, value => GameSettings.SetBrightness(value / 100f));

        // Sonido de prueba al soltar (no en cada paso del arrastre).
        sfxSlider?.RegisterCallback<PointerCaptureOutEvent>(_ => PlayPreview());
    }

    private Slider SetupSlider(string sliderName, System.Action<float> onChanged)
    {
        Slider slider = FindInPage<Slider>(sliderName);
        if (slider == null)
            return null;

        slider.lowValue = 0f;
        slider.highValue = 100f;
        slider.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
        return slider;
    }

    protected override void OnHide() => GameSettings.Save();

    public override void Refresh()
    {
        // SetValueWithoutNotify: pintar los valores guardados no debe volver a aplicarlos.
        sfxSlider?.SetValueWithoutNotify(GameSettings.SfxVolume * 100f);
        musicSlider?.SetValueWithoutNotify(GameSettings.MusicVolume * 100f);
        brightnessSlider?.SetValueWithoutNotify(GameSettings.Brightness * 100f);

        // Sin música en FMOD todavía: el slider se ve pero no se puede tocar.
        musicSlider?.SetEnabled(GameSettings.HasMusicBus);
    }

    private void PlayPreview()
    {
        if (!sfxPreviewEvent.IsNull)
            RuntimeManager.PlayOneShot(sfxPreviewEvent);
    }
}
