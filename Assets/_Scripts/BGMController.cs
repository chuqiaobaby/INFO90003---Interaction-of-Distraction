using System.Collections;
using UnityEngine;

/// <summary>
/// Plays background music that starts when the player first touches the water
/// and fades out when the session resets (cooldown or R-key).
///
/// Setup
/// ─────
///  1. Add this script to any GameObject (e.g. the same one as DistractionManager).
///  2. Attach an AudioSource component to the same GameObject.
///     — Assign your music clip to AudioSource.clip, tick "Loop", set Volume to 0.
///     — Untick "Play On Awake".
///  3. Optionally assign the AudioSource to the 'audioSource' slot below,
///     or leave it empty — the script will find it automatically on the same GameObject.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BGMController : MonoBehaviour
{
    [Header("─── BGM Settings ─────────────────────────────────────────────────")]

    [Tooltip("The AudioSource that holds the BGM clip. Leave empty to auto-find on this GameObject.")]
    public AudioSource audioSource;

    [Tooltip("Target volume when the music is fully playing (0–1).")]
    [Range(0f, 1f)]
    public float targetVolume = 0.7f;

    [Tooltip("Seconds to fade the music in from silence after the first touch.")]
    [Range(0f, 10f)]
    public float fadeInDuration = 2.0f;

    [Tooltip("Seconds to fade the music out when the session resets or cooldown starts.")]
    [Range(0f, 10f)]
    public float fadeOutDuration = 3.0f;

    // ── Internal state ────────────────────────────────────────────────────────

    private bool wasExpStarted = false;
    private Coroutine fadeCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Ensure the source starts silent and doesn't auto-play
        if (audioSource != null)
        {
            audioSource.volume    = 0f;
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        DistractionManager dm = DistractionManager.Instance;
        if (dm == null) return;

        bool expNow = dm.HasExperienceStarted;

        // First touch → start music
        if (!wasExpStarted && expNow)
        {
            StartMusic();
        }

        // Reset / cooldown → stop music
        if (wasExpStarted && !expNow)
        {
            StopMusic();
        }

        wasExpStarted = expNow;
    }

    // ── Public helpers (can also be called from other scripts) ────────────────

    public void StartMusic()
    {
        if (audioSource == null) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (!audioSource.isPlaying)
        {
            audioSource.volume = 0f;
            audioSource.Play();
        }

        fadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, targetVolume, fadeInDuration));
    }

    public void StopMusic()
    {
        if (audioSource == null) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, 0f, fadeOutDuration,
                                                   onComplete: () => audioSource.Stop()));
    }

    // ── Coroutine ─────────────────────────────────────────────────────────────

    private IEnumerator FadeVolume(float from, float to, float duration,
                                    System.Action onComplete = null)
    {
        if (audioSource == null) yield break;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed          += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        audioSource.volume = to;
        onComplete?.Invoke();
    }
}
