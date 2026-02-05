using System;
using UnityEngine;

namespace Twinny.Mobile.Input
{
    public static class MobileInputEvents
    {
        #region Static Callback Events (Alternative to IMobileInputCallbacks)
        // Single finger events
        public static event Action<Vector2> OnTapEvent; // Screen position
        public static event Action OnHapticTouchEvent;
        public static event Action<float> OnForceTouchEvent; // Pressure (0-1)
        public static event Action<Vector2, Vector2> OnDragEvent; // delta, currentPosition
        public static event Action OnLongPressEvent; // Single finger long press

        // Two finger events
        public static event Action<Vector2> OnTwoFingerTapEvent; // Center position
        public static event Action<Vector2, Vector2> OnTwoFingerSwipeEvent; // direction, center
        public static event Action<Vector2> OnTwoFingerLongPressEvent; // Center position
        public static event Action<float> OnPinchZoomEvent; // delta (-1 to 1)

        // Three finger events
        public static event Action<Vector2> OnThreeFingerTapEvent; // Center position
        public static event Action<Vector2, Vector2> OnThreeFingerSwipeEvent; // direction, center
        public static event Action<float> OnThreeFingerPinchEvent; // delta

        // Four finger events
        public static event Action OnFourFingerTapEvent;
        public static event Action<Vector2> OnFourFingerSwipeEvent; // direction

        // Device sensor events
        public static event Action OnShakeEvent;
        public static event Action<Vector3> OnTiltEvent; // delta rotation
        public static event Action<DeviceOrientation> OnDeviceRotatedEvent;
        public static event Action OnPickUpEvent;
        public static event Action OnPutDownEvent;

        // Edge gestures
        public static event Action<EdgeDirection> OnEdgeSwipeEvent;

        // Accessibility events
        public static event Action<string> OnAccessibilityActionEvent;
        public static event Action<string> OnScreenReaderGestureEvent;
        public static event Action<bool> OnNotificationActionEvent; // fromTop

        #endregion

        #region Static Invokers

        // Single finger
        public static void Tap(Vector2 pos) => OnTapEvent?.Invoke(pos);
        public static void HapticTouch() => OnHapticTouchEvent?.Invoke();
        public static void ForceTouch(float pressure) => OnForceTouchEvent?.Invoke(pressure);
        public static void Drag(Vector2 delta, Vector2 currentPos) => OnDragEvent?.Invoke(delta, currentPos);
        public static void LongPress() => OnLongPressEvent?.Invoke();

        // Two finger
        public static void TwoFingerTap(Vector2 center) => OnTwoFingerTapEvent?.Invoke(center);
        public static void TwoFingerSwipe(Vector2 direction, Vector2 center) => OnTwoFingerSwipeEvent?.Invoke(direction, center);
        public static void TwoFingerLongPress(Vector2 center) => OnTwoFingerLongPressEvent?.Invoke(center);
        public static void PinchZoom(float delta) => OnPinchZoomEvent?.Invoke(delta);

        // Three finger
        public static void ThreeFingerTap(Vector2 center) => OnThreeFingerTapEvent?.Invoke(center);
        public static void ThreeFingerSwipe(Vector2 direction, Vector2 center) => OnThreeFingerSwipeEvent?.Invoke(direction, center);
        public static void ThreeFingerPinch(float delta) => OnThreeFingerPinchEvent?.Invoke(delta);

        // Four finger
        public static void FourFingerTap() => OnFourFingerTapEvent?.Invoke();
        public static void FourFingerSwipe(Vector2 direction) => OnFourFingerSwipeEvent?.Invoke(direction);

        // Device sensors
        public static void Shake() => OnShakeEvent?.Invoke();
        public static void Tilt(Vector3 deltaRotation) => OnTiltEvent?.Invoke(deltaRotation);
        public static void DeviceRotated(DeviceOrientation orientation) => OnDeviceRotatedEvent?.Invoke(orientation);
        public static void PickUp() => OnPickUpEvent?.Invoke();
        public static void PutDown() => OnPutDownEvent?.Invoke();

        // Edge gestures
        public static void EdgeSwipe(EdgeDirection edge) => OnEdgeSwipeEvent?.Invoke(edge);

        // Accessibility
        public static void AccessibilityAction(string action) => OnAccessibilityActionEvent?.Invoke(action);
        public static void ScreenReaderGesture(string gesture) => OnScreenReaderGestureEvent?.Invoke(gesture);
        public static void NotificationAction(bool fromTop) => OnNotificationActionEvent?.Invoke(fromTop);

        #endregion

        #region Utility
        // Clear all subscribers (useful for scene transitions)
        public static void ClearAllSubscribers()
        {
            OnTapEvent = null;
            OnHapticTouchEvent = null;
            OnForceTouchEvent = null;
            OnDragEvent = null;
            OnLongPressEvent = null;
            OnTwoFingerTapEvent = null;
            OnTwoFingerSwipeEvent = null;
            OnTwoFingerLongPressEvent = null;
            OnPinchZoomEvent = null;
            OnThreeFingerTapEvent = null;
            OnThreeFingerSwipeEvent = null;
            OnThreeFingerPinchEvent = null;
            OnFourFingerTapEvent = null;
            OnFourFingerSwipeEvent = null;
            OnShakeEvent = null;
            OnTiltEvent = null;
            OnDeviceRotatedEvent = null;
            OnPickUpEvent = null;
            OnPutDownEvent = null;
            OnEdgeSwipeEvent = null;
            OnAccessibilityActionEvent = null;
            OnScreenReaderGestureEvent = null;
            OnNotificationActionEvent = null;
        }
        #endregion

    }
}
