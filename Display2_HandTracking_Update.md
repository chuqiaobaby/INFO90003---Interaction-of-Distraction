# Display 2 Hand Tracking Update

## Summary

This update adds a Display 2 projection layer that can place the pastel ripple effect at a tracked hand position.

The intended interaction is:

```text
Arduino touch state
+ hand coordinate input
-> Display 2 ripple appears at the hand position
```

The existing Arduino input flow is not replaced. Unity still reads `DeviceInputManager.Instance.isTouching` to decide whether the interaction should trigger. The new hand tracking input only provides the visual position.

## Updated Files

```text
Assets/_Scripts/ActivateProjector.cs
Assets/_Scripts/PastelClassicRippleController.cs
Assets/_Scripts/HandTrackingUdpReceiver.cs
Assets/_Scripts/HandTrackingUdpReceiver.cs.meta
Assets/_Shader/PastelClassicRipple.shader
Assets/_Shader/PastelClassicRipple.shader.meta
```

## What Changed

- `ActivateProjector` now manages the Display 2 pastel projection and triggers ripples from one place.
- Display 2 is limited to the `ProjectionContent` layer so it does not render the webcam background.
- `PastelClassicRippleController` now supports triggering a ripple at a normalized screen position.
- `PastelClassicRipple.shader` now uses `_RippleCenter` so the ripple can start from the hand position instead of always starting from the center.
- `HandTrackingUdpReceiver` listens for external hand coordinates on UDP port `5052`.
- A Unity Editor mouse fallback was added for testing before the real camera coordinate sender is ready.

## UDP Coordinate Format

The receiver expects a UTF-8 text packet:

```text
x,y,visible
```

Example:

```text
0.42,0.67,1
```

Where:

```text
x       horizontal normalized coordinate, 0 to 1
y       vertical normalized coordinate, 0 to 1
visible 1 when a hand is detected, 0 when no hand is detected
```

Default port:

```text
5052
```

## Runtime Logic

```text
if Arduino isTouching == 1
and UDP hand visible == 1
    trigger ripple at UDP hand position
else if Arduino isTouching == 1
    trigger fallback ripple at center
```

In the Unity Editor only, holding the manual trigger key can use the mouse position as the hand position. This is only for local testing.

## Editor Test Method

Use this before the external camera coordinate sender is ready.

1. Open `Assets/_Scenes/Main.unity`.
2. Wait for Unity scripts to compile.
3. Press Play.
4. In the Game view, select `Display 2`.
5. Move the mouse to a clear position in the Game view, such as top-left or bottom-right.
6. Hold `Space`.
7. The ripple should appear around the mouse position and repeat about every `0.45s` while held.

If the ripple still appears at the center, click inside the Game view once so it has keyboard/mouse focus, then test again.

## External Camera Test Method

Use this after the external hand tracking system can send coordinates.

1. Make sure the coordinate sender sends UDP packets to the Unity machine on port `5052`.
2. Use the packet format `x,y,visible`, for example `0.42,0.67,1`.
3. Open `Assets/_Scenes/Main.unity`.
4. Press Play.
5. Select `Display 2` in the Game view.
6. Trigger touch through Arduino.
7. Move the tracked hand. The ripple should appear at the tracked hand position.

## Build / Projection Notes

For real dual-display projection, test with a built Unity app, not only the Unity Editor.

Before running the build:

1. Connect the projector.
2. Set the projector as the laptop's second display.
3. Run the Unity build.
4. Display 2 should output the projection layer.

The Display 2 background is transparent in the shader and does not render the webcam layer. On a real projector or Game view, empty transparent areas may still look black because there is no rendered light/color in those pixels.
