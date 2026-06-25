using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TriggerSound : MonoBehaviour
{
    [SerializeField] private AudioClip soundToPlay;

    private AudioSource audioSource;
    private bool hasPlayed = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPlayed)
            return;

        Debug.Log("Beep");
        audioSource.PlayOneShot(soundToPlay);
        hasPlayed = true;
    }
}