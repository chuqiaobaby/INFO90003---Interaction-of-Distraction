# Mon 18 May — Hand Tracking Progress Summary

I use https://github.com/homuler/MediaPipeUnityPlugin this for hand tracking.

## 1. Hand Tracking Implementation

Hand Tracking has been implemented and tested successfully using an external camera.  
Currently, the external camera feed is used as the background for testing purposes.

If the camera background is not needed during exhibition:

1. Select `ProjectorManager` in Unity
2. Uncheck:
Show External Camera As Background

## 2. Ripple Trigger Issue

Currently, ripples trigger whenever the camera detects a hand.

To disable this:

Select ProjectorManager
Open ActivateProjector
Under Hand Tracking, uncheck:
Media Pipe Hand Visible Triggers Ripple

After this, the system will fall back to mouse coordinates for testing instead of camera-triggered ripples.

## 3. Next Step

- Review Owen’s Arduino code
- Connect Arduino sensor input with Unity ripple effects-
- Continue refining the interaction effects



