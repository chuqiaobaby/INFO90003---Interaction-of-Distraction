using UnityEngine;

/// <summary>
/// Reads DeviceInputManager.Level (0-3) and drives MirrorFractureController.
/// Blow resets the mirror only after grounding is complete (Shielded state).
///
/// Uses _wasShielded (previous frame) instead of current frame IsShielded to avoid a
/// script-execution-order race: DistractionManager.Update() may process the blow and
/// exit Shielded in the same frame before this script runs, making IsShielded false
/// by the time we check it.
/// </summary>
[DisallowMultipleComponent]
public sealed class BrokenMirrorLevelBridge : MonoBehaviour
{
    [SerializeField] private MirrorFractureController mirrorFractureController;

    private int  _lastLevel         = -1;
    private bool _wasBlowing        = false;
    private bool _wasShielded       = false;
    // After a successful blow, block level-driven re-cracking until Level returns to 0.
    // This lets staff physically remove objects from the water without the mirror
    // re-cracking as the level drops 3→2→1→0 between users.
    private bool _awaitingLevelReset = false;

    private void Update()
    {
        if (mirrorFractureController == null) return;

        DeviceInputManager hw = DeviceInputManager.Instance;
        DistractionManager dm = DistractionManager.Instance;

        int  level          = hw != null ? hw.Level          : 0;
        bool blowingNow     = hw != null ? hw.isBlowing == 1  : Input.GetKeyDown(KeyCode.B);
        bool blowRisingEdge = blowingNow && !_wasBlowing;
        bool shieldedNow    = dm != null && dm.IsShielded;

        _wasBlowing = blowingNow;

        // Use _wasShielded (previous frame) — guards against the case where
        // DistractionManager already reset to Normal earlier this same frame.
        if (blowRisingEdge && _wasShielded)
        {
            mirrorFractureController.ResetMirror();
            _lastLevel           = level;
            _wasShielded         = false;
            // In hardware mode, wait for physical water to drain back to 0.
            // In keyboard/sim mode, allow immediate re-cracking.
            bool isHardwareMode  = hw != null && hw.useHardwareInput;
            _awaitingLevelReset  = isHardwareMode && (level > 0);
            return;
        }

        // Cache shielded state for next frame's blow check
        _wasShielded = shieldedNow;

        // While waiting for the water level to physically return to 0 (staff removing
        // objects between users), ignore level changes so the mirror stays clean.
        if (_awaitingLevelReset)
        {
            if (level == 0)
            {
                _awaitingLevelReset = false;
                _lastLevel          = 0;
            }
            return;
        }

        // Drive mirror state from water level: only advances forward, never retreats.
        // Shielded state also blocks changes. Only ResetMirror() (via blow) clears it.
        if (level != _lastLevel)
        {
            _lastLevel = level;
            if (!shieldedNow && level > mirrorFractureController.ActiveState)
                mirrorFractureController.SetFractureState(level);
        }
    }
}
