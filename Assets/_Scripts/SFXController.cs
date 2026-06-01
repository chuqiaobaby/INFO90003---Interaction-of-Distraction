using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays one-shot sound effects at key interaction moments:
///   1. Touch Effect — starts when a touch effect appears, fades out when it disappears.
///   2. Water Level  — each level (1 / 2 / 3) plays its sound exactly once
///                     per session, the first time that level is reached.
///   3. Grounding    — fires once per grounding pulse ring.
///   4. Blow Prompt  — fires when the blow instruction prompt appears.
///   5. Blow / Clear — fires once when blow completes and effects are cleared.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SFXController : MonoBehaviour
{
    [System.Serializable]
    public class TouchEffectClip
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public static SFXController Instance { get; private set; }

    // ══════════════════════════════════════════════════════════════════════════
    //  1. TOUCH EFFECT SOUNDS
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── 1. Touch Effect ───────────────────────────────────────────")]
    [Tooltip("Random pool used when each touch effect appears. Each entry has its own volume.")]
    public TouchEffectClip[] touchEffectClips = new TouchEffectClip[3];
    [Range(0f, 1f)] public float touchEffectVolume = 1f;
    [Tooltip("Seconds for a touch effect sound to fade out after its visual effect disappears.")]
    [Range(0.05f, 5f)] public float touchEffectFadeOutDuration = 0.75f;

    // ══════════════════════════════════════════════════════════════════════════
    //  2. WATER LEVEL SOUNDS
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── 2. Water Level ──────────────────────────────────────────────")]

    [Tooltip("Plays the first time the water reaches Level 1 this session.")]
    public AudioClip waterLevel1Clip;
    [Range(0f, 1f)] public float waterLevel1Volume = 1f;

    [Tooltip("Plays the first time the water reaches Level 2 this session.")]
    public AudioClip waterLevel2Clip;
    [Range(0f, 1f)] public float waterLevel2Volume = 1f;

    [Tooltip("Plays the first time the water reaches Level 3 this session.")]
    public AudioClip waterLevel3Clip;
    [Range(0f, 1f)] public float waterLevel3Volume = 1f;

    // ══════════════════════════════════════════════════════════════════════════
    //  3. GROUNDING SOUND
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── 3. Grounding ───────────────────────────────────────────────")]
    [Tooltip("Random pool played once per grounding pulse ring. Add about 3 clips here.")]
    public AudioClip[] groundingClips = new AudioClip[3];
    [Range(0f, 3f)] public float groundingVolume = 1f;

    // ══════════════════════════════════════════════════════════════════════════
    //  4. BLOW PROMPT SOUND
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── 4. Blow Prompt ────────────────────────────────────────────")]
    [Tooltip("Plays when the blow instruction prompt appears.")]
    public AudioClip blowPromptClip;
    [Range(0f, 1f)] public float blowPromptVolume = 1f;

    // ══════════════════════════════════════════════════════════════════════════
    //  5. BLOW / CLEAR SOUND
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── 5. Blow / Clear ────────────────────────────────────────────")]
    [Tooltip("Primary sound played when the blow clears all effects (enters Cooldown).")]
    public AudioClip blowClearClip;
    [Range(0f, 1f)] public float blowClearVolume = 1f;
    [Tooltip("Secondary sound played at the same time as Blow Clear Clip.")]
    public AudioClip blowClearClip2;
    [Range(0f, 1f)] public float blowClearVolume2 = 1f;
    [Tooltip("Seconds at the end of each Blow / Clear clip used for fade out.")]
    [Range(0.05f, 10f)] public float blowClearFadeOutDuration = 2.0f;

    // ── Internal state ────────────────────────────────────────────────────────

    private AudioSource sfxSource;
    private class ActiveTouchSource
    {
        public AudioSource source;
        public float baseVolume;
    }

    private readonly List<ActiveTouchSource> activeTouchSources = new List<ActiveTouchSource>();
    private float touchEffectGroundingDucking = 0f;

    // Per-session one-shot flags — reset each time a new session starts
    private bool level1Played  = false;
    private bool level2Played  = false;
    private bool level3Played  = false;

    private bool wasInCooldown = false;
    private bool wasExpStarted = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        sfxSource              = GetComponent<AudioSource>();
        sfxSource.playOnAwake  = false;
        sfxSource.spatialBlend = 0f;
    }

    private void Update()
    {
        DistractionManager dm = DistractionManager.Instance;
        if (dm == null) return;

        int  currentLevel = dm.HardwareLevel;
        bool inCooldown   = dm.IsInCooldown;
        bool expStarted   = dm.HasExperienceStarted;

        // ── Session reset: clear flags so next session starts fresh ───────────
        if (wasExpStarted && !expStarted)
        {
            level1Played = false;
            level2Played = false;
            level3Played = false;
        }

        // ── 2. Water level sounds ─────────────────────────────────────────────
        // Gated on HasExperienceStarted AND grounding prompt not yet triggered.
        // Once the grounding prompt appears, water level sounds are permanently
        // silenced for the rest of this session.
        bool groundingFrozen    = GroundingPrompt.Instance != null &&
                               GroundingPrompt.Instance.HasTriggered;
        bool instructionShowing = InstructionDisplay.Instance != null &&
                                  InstructionDisplay.Instance.IsVisible;

        if (expStarted && !groundingFrozen && !instructionShowing)
        {
            if (currentLevel >= 1 && !level1Played)
            {
                Play(waterLevel1Clip, waterLevel1Volume);
                level1Played = true;
            }
            if (currentLevel >= 2 && !level2Played)
            {
                Play(waterLevel2Clip, waterLevel2Volume);
                level2Played = true;
            }
            if (currentLevel >= 3 && !level3Played)
            {
                Play(waterLevel3Clip, waterLevel3Volume);
                level3Played = true;
            }
        }

        // ── 3. Grounding sounds are triggered by InteractionVFXController
        // once per spawned pulse ring, so timing stays synced with ringsPerSecond.

        // ── 5. Blow / clear: just entered Cooldown ────────────────────────────
        if (inCooldown && !wasInCooldown)
        {
            PlayWithEndFadeOut(blowClearClip, blowClearVolume, blowClearFadeOutDuration, "BlowClearSFX_1");
            PlayWithEndFadeOut(blowClearClip2, blowClearVolume2, blowClearFadeOutDuration, "BlowClearSFX_2");
        }

        // ── Store previous frame values ───────────────────────────────────────
        wasInCooldown = inCooldown;
        wasExpStarted = expStarted;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    public void PlayGroundingPulse()
    {
        AudioClip clip = PickRandomClip(groundingClips);
        if (clip != null) Play(clip, groundingVolume);
    }

    public AudioSource StartTouchEffect()
    {
        TouchEffectClip entry = PickRandomTouchEffectClip();
        if (entry == null || entry.clip == null) return null;

        GameObject go = new GameObject("TouchEffectSFX");
        go.transform.SetParent(transform, false);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = true;
        source.clip = entry.clip;

        float baseVolume = touchEffectVolume * Mathf.Clamp01(entry.volume);
        source.volume = GetTouchEffectDuckedVolume(baseVolume);
        source.Play();
        activeTouchSources.Add(new ActiveTouchSource
        {
            source = source,
            baseVolume = baseVolume
        });
        return source;
    }

    public void FadeOutTouchEffect(AudioSource source, float fadeOutDuration = -1f)
    {
        if (source == null) return;
        RemoveActiveTouchSource(source);
        float duration = fadeOutDuration > 0f ? fadeOutDuration : touchEffectFadeOutDuration;
        StartCoroutine(FadeOutAndDestroy(source, duration));
    }

    public void SetTouchEffectGroundingDucking(float amount)
    {
        touchEffectGroundingDucking = Mathf.Clamp01(amount);

        for (int i = activeTouchSources.Count - 1; i >= 0; i--)
        {
            ActiveTouchSource active = activeTouchSources[i];
            if (active == null || active.source == null)
            {
                activeTouchSources.RemoveAt(i);
                continue;
            }

            active.source.volume = GetTouchEffectDuckedVolume(active.baseVolume);
        }
    }

    public void PlayBlowPrompt()
    {
        Play(blowPromptClip, blowPromptVolume);
    }

    private AudioClip PickRandomClip(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0)
        {
            int validCount = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null) validCount++;
            }

            if (validCount > 0)
            {
                int target = Random.Range(0, validCount);
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] == null) continue;
                    if (target == 0) return clips[i];
                    target--;
                }
            }
        }

        return null;
    }

    private TouchEffectClip PickRandomTouchEffectClip()
    {
        if (touchEffectClips != null && touchEffectClips.Length > 0)
        {
            int validCount = 0;
            for (int i = 0; i < touchEffectClips.Length; i++)
            {
                if (touchEffectClips[i] != null && touchEffectClips[i].clip != null) validCount++;
            }

            if (validCount > 0)
            {
                int target = Random.Range(0, validCount);
                for (int i = 0; i < touchEffectClips.Length; i++)
                {
                    if (touchEffectClips[i] == null || touchEffectClips[i].clip == null) continue;
                    if (target == 0) return touchEffectClips[i];
                    target--;
                }
            }
        }

        return null;
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    private void PlayWithEndFadeOut(AudioClip clip, float volume, float fadeOutDuration, string objectName)
    {
        if (clip == null) return;

        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, false);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
        source.clip = clip;
        source.volume = volume;
        source.Play();

        StartCoroutine(FadeOutAtClipEndAndDestroy(source, fadeOutDuration));
    }

    private float GetTouchEffectDuckedVolume(float baseVolume)
    {
        return baseVolume * (1f - touchEffectGroundingDucking);
    }

    private void RemoveActiveTouchSource(AudioSource source)
    {
        for (int i = activeTouchSources.Count - 1; i >= 0; i--)
        {
            if (activeTouchSources[i] == null || activeTouchSources[i].source == null || activeTouchSources[i].source == source)
                activeTouchSources.RemoveAt(i);
        }
    }

    private IEnumerator FadeOutAndDestroy(AudioSource source, float duration)
    {
        if (source == null) yield break;

        float startVolume = source.volume;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration && source != null)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        if (source != null)
            Destroy(source.gameObject);
    }

    private IEnumerator FadeOutAtClipEndAndDestroy(AudioSource source, float fadeOutDuration)
    {
        if (source == null || source.clip == null) yield break;

        float fadeDuration = Mathf.Clamp(fadeOutDuration, 0.01f, source.clip.length);
        float waitBeforeFade = Mathf.Max(0f, source.clip.length - fadeDuration);

        if (waitBeforeFade > 0f)
            yield return new WaitForSeconds(waitBeforeFade);

        yield return FadeOutAndDestroy(source, fadeDuration);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
