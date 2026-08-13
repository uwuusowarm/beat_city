using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    private enum VolumeChannel
    {
        Master,
        Music,
        Sfx
    }

    [SerializeField] private Slider slider;
    [SerializeField] private VolumeChannel channel;

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            float current = channel switch
            {
                VolumeChannel.Master => AudioManager.Instance.MasterVolume,
                VolumeChannel.Music => AudioManager.Instance.MusicVolume,
                VolumeChannel.Sfx => AudioManager.Instance.SfxVolume,
                _ => 1f
            };
            slider.SetValueWithoutNotify(current);
        }

        slider.onValueChanged.AddListener(HandleValueChanged);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(HandleValueChanged);
    }

    private void HandleValueChanged(float value)
    {
        if (AudioManager.Instance == null) return;

        switch (channel)
        {
            case VolumeChannel.Master:
                AudioManager.Instance.SetMasterVolume(value);
                break;
            case VolumeChannel.Music:
                AudioManager.Instance.SetMusicVolume(value);
                break;
            case VolumeChannel.Sfx:
                AudioManager.Instance.SetSfxVolume(value);
                break;
        }
    }
}