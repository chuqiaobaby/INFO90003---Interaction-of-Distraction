using UnityEngine;

/// <summary>
/// Plays one-shot sound effects at key interaction moments:
///   1. Water Level  — each level (1 / 2 / 3) plays its sound exactly once
///                     per session, the first time that level is reached.
///   2. Grounding    — fires once when the user enters the Grounding state.
///   3. Blow / Clear — fires once when blow completes and effects are cleared.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SFXController : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════════════
    //  1. WATER LEVEL SOUNDS
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── 1. Water Level ──────────────────────────────────────────────")]

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
    //  2. GROUNDING SOUND
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── 2. Grounding ───────────────────────────────────────────────")]
    [Tooltip("Plays once when the user starts pressing down (enters Grounding state).")]
    public AudioClip groundingClip;
    [Range(0f, 1f)] public float groundingVolume = 1f;

    // ══════════════════════════════════════════════════════════════════════════
    //  3. BLOW / CLEAR SOUND
    // ══════════════════════════════════════════════════════════════════════════

    [Header("─── 3. Blow / Clear ────────────────────────────────────────────")]
    [Tooltip("Plays once when the blow clears all effects (enters Cooldown).")]
    public AudioClip blowClearClip;
    [Range(0f, 1f)] public float blowClearVolume = 1f;

    // ── Internal state ────────────────────────────────────────────────────────

    private AudioSource sfxSource;

    // Per-session one-shot flags — reset each time a new session starts
    private bool level1Played  = false;
    private bool level2Played  = false;
    private bool level3Played  = false;

    private bool wasGrounding  = false;
    private bool wasInCooldown = false;
    private bool wasExpStarted = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        sfxSource              = GetComponent<AudioSource>();
        sfxSource.playOnAwake  = false;
        sfxSource.spatialBlend = 0f;
    }

    private void Update()
    {
        DistractionManager dm = DistractionManager.Instance;
        if (dm == null) return;

        int  currentLevel = dm.HardwareLevel;
        bool grounding    = dm.IsGrounding;
        bool inCooldown   = dm.IsInCooldown;
        bool expStarted   = dm.HasExperienceStarted;

        // ── Session reset: clear flags so next session starts fresh ───────────
        if (wasExpStarted && !expStarted)
        {
            level1Played = false;
            level2Played = false;
            level3Played = false;
        }

        // ── 1. Water level sounds ─────────────────────────────────────────────
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

        // ── 2. Grounding: just entered Grounding state ────────────────────────
        if (grounding && !wasGrounding)
            Play(groundingClip, groundingVolume);

        // ── 3. Blow / clear: just entered Cooldown ────────────────────────────
        if (inCooldown && !wasInCooldown)
            Play(blowClearClip, blowClearVolume);

        // ── Store previous frame values ───────────────────────────────────────
        wasGrounding  = grounding;
        wasInCooldown = inCooldown;
        wasExpStarted = expStarted;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}
