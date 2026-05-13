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

    private int  _lastLevel   = -1;
    private bool _wasBlowing  = false;
    private bool _wasShielded = false;

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
            _lastLevel = level; // prevent same-frame re-crack
            _wasShielded = false;
            return;
        }

        // Cache shielded state for next frame's blow check
        _wasShielded = shieldedNow;

        // Drive mirror state from water level
        if (level != _lastLevel)
        {
            _lastLevel = level;
            mirrorFractureController.SetFractureState(level);
        }
    }
}
