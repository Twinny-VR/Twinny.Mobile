using Twinny.Core.Input;
using UnityEngine;

namespace Twinny.Mobile
{
    /// <summary>
    /// Interface for handling mobile-specific input events.
    /// Extends the core IInputCallbacks for multitouch, gestures, sensors, and system interactions.
    /// </summary>
    public interface IMobileInputCallbacks : IInputCallbacks
    {
        #region Core Extensions

        /// <summary>
        /// Triggered when a tap hits a world collider.
        /// </summary>
        /// <param name="hit">Raycast hit information.</param>
        void OnSelectHit(RaycastHit hit);

        #endregion

        #region Multitouch Gestures

        // 2 Fingers

        /// <summary>
        /// Triggered when a two-finger tap is detected.
        /// </summary>
        /// <param name="position">Screen position where the tap occurred.</param>
        void OnTwoFingerTap(Vector2 position);

        /// <summary>
        /// Triggered when a two-finger long press is detected.
        /// </summary>
        /// <param name="position">Screen position where the long press occurred.</param>
        void OnTwoFingerLongPress(Vector2 position);

        /// <summary>
        /// Triggered when a two-finger swipe is detected.
        /// Use for specific commands; for camera or list navigation, map to core input instead.
        /// </summary>
        /// <param name="direction">Normalized swipe direction vector.</param>
        /// <param name="startPosition">Screen position where the swipe started.</param>
        void OnTwoFingerSwipe(Vector2 direction, Vector2 startPosition);

        // 3 Fingers

        /// <summary>
        /// Triggered when a three-finger tap is detected.
        /// </summary>
        /// <param name="position">Screen position where the tap occurred.</param>
        void OnThreeFingerTap(Vector2 position);

        /// <summary>
        /// Triggered when a three-finger swipe is detected.
        /// </summary>
        /// <param name="direction">Normalized swipe direction vector.</param>
        /// <param name="startPosition">Screen position where the swipe started.</param>
        void OnThreeFingerSwipe(Vector2 direction, Vector2 startPosition);

        /// <summary>
        /// Triggered when a three-finger pinch gesture is detected.
        /// </summary>
        /// <param name="delta">Change in pinch scale (distance between fingers).</param>
        void OnThreeFingerPinch(float delta);

        // 4 Fingers

        /// <summary>
        /// Triggered when a four-finger tap is detected.
        /// </summary>
        void OnFourFingerTap();

        /// <summary>
        /// Triggered when a four-finger swipe is detected.
        /// </summary>
        /// <param name="direction">Normalized swipe direction vector.</param>
        void OnFourFingerSwipe(Vector2 direction);

        #endregion

        #region Edge & Special Touch

        // Edge swipes (usually mapped to system or UI actions)

        /// <summary>
        /// Triggered when the user swipes from the edge of the screen.
        /// </summary>
        /// <param name="edge">Edge direction where the swipe originated.</param>
        void OnEdgeSwipe(EdgeDirection edge);

        // Force & haptic touch

        /// <summary>
        /// Triggered when a force touch (pressure touch) is detected.
        /// </summary>
        /// <param name="pressure">Pressure value of the touch.</param>
        void OnForceTouch(float pressure);

        /// <summary>
        /// Triggered when a haptic touch is detected.
        /// Can be used to provide vibration feedback or UI response.
        /// </summary>
        void OnHapticTouch();

        /// <summary>
        /// Triggered when the back of the device is tapped.
        /// </summary>
        /// <param name="tapCount">Number of consecutive taps detected.</param>
        void OnBackTap(int tapCount);

        #endregion

        #region Device Sensors

        /// <summary>
        /// Triggered when the device is shaken.
        /// </summary>
        void OnShake();

        /// <summary>
        /// Triggered when the device is tilted.
        /// </summary>
        /// <param name="tiltRotation">Current rotation vector representing tilt.</param>
        void OnTilt(Vector3 tiltRotation);

        /// <summary>
        /// Triggered when the device orientation changes.
        /// </summary>
        /// <param name="orientation">New device orientation.</param>
        void OnDeviceRotated(DeviceOrientation orientation);

        /// <summary>
        /// Triggered when the device is picked up.
        /// </summary>
        void OnPickUp();

        /// <summary>
        /// Triggered when the device is put down.
        /// </summary>
        void OnPutDown();

        #endregion

        #region Accessibility & System

        /// <summary>
        /// Triggered by an accessibility-related action.
        /// </summary>
        /// <param name="actionName">Name of the accessibility action.</param>
        void OnAccessibilityAction(string actionName);

        /// <summary>
        /// Triggered by a screen reader gesture.
        /// </summary>
        /// <param name="gestureType">Type of gesture recognized by the screen reader.</param>
        void OnScreenReaderGesture(string gestureType);

        /// <summary>
        /// Triggered when a notification action is performed.
        /// </summary>
        /// <param name="isQuickAction">True if the action is a quick action (e.g., swipe or button).</param>
        void OnNotificationAction(bool isQuickAction);

        #endregion
    }

    /// <summary>
    /// Directions representing the edges of the screen for edge swipe gestures.
    /// </summary>
    public enum EdgeDirection
    {
        Left,   // Left edge of the screen
        Right,  // Right edge of the screen
        Top,    // Top edge of the screen
        Bottom  // Bottom edge of the screen
    }
}
