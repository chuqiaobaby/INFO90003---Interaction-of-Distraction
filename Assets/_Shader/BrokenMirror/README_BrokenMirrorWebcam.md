# Broken Mirror Webcam HLSL Effect

This is a compact URP broken mirror effect built around custom HLSL, `WebCamTexture`, and three crack mask states. It does not use Shader Graph, renderer features, mesh destruction, rigidbodies, or procedural runtime fracture simulation.

## Files

- `Assets/_Shader/BrokenMirror/BrokenMirrorWebcam.shader`
- `Assets/_Scripts/BrokenMirror/BrokenMirrorWebcamSource.cs`
- `Assets/_Scripts/BrokenMirror/BrokenMirrorStateController.cs`
- `Assets/_Scripts/BrokenMirror/Editor/BrokenMirrorSetupUtility.cs`
- `Assets/_Shader/BrokenMirror/crack_mask_cinematic.png`

## Scene Setup

1. Create or select a quad/plane that represents the mirror surface.
2. Create a material using shader `INFO90003/Broken Mirror Webcam HLSL`.
   - Optional shortcut: `INFO90003 > Broken Mirror > Create Material And Placeholder Masks`.
   - This assigns the imported cinematic crack mask used by the standalone reference effect.
3. Assign the material to the mirror renderer.
4. Add `BrokenMirrorWebcamSource` to the mirror object or a controller object.
5. Assign the mirror renderer and material.
6. Add `BrokenMirrorStateController`.
7. Assign the same `BrokenMirrorWebcamSource`.
8. Assign `crack_mask_cinematic.png` to `Cinematic Crack Mask` if it is not already assigned.

## Debug Controls

- `1` = fracture state 1
- `2` = fracture state 2
- `3` = fracture state 3
- `B` or `R` = clean mirror reset
- `Space` is left for the existing `TouchGlassEffect` by default.

## Water Level Integration

Call `SetWaterLevel(0..3)` on `BrokenMirrorStateController`, or enable `readWaterLevelFromDeviceInput` to read the existing `DeviceInputManager.Level`.

## Notes

- The effect now uses the standalone project's cinematic crack mask directly.
- Keep the material opaque on a simple quad for the most stable and fastest installation setup.
