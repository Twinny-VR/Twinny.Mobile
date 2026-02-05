using Concept.Core;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Twinny.Mobile.Input
{
    [System.Serializable]
    public class InputAction
    {
        public enum ActionType
        {
            // Core Actions
            PrimaryDown,
            PrimaryUp,
            PrimaryDrag,
            Select,
            Cancel,
            Zoom,

            // Mobile-Specific Actions
            TwoFingerTap,
            TwoFingerLongPress,
            TwoFingerSwipe,
            ThreeFingerTap,
            ThreeFingerSwipe,
            ThreeFingerPinch,
            FourFingerTap,
            FourFingerSwipe,
            EdgeSwipe,
            ForceTouch,
            HapticTouch,
            BackTap,
            Shake,
            Tilt,
            DeviceRotated,
            PickUp,
            PutDown,
            AccessibilityAction,
            ScreenReaderGesture,
            NotificationAction
        }

        public ActionType type;
        public UnityEvent onTriggered;
    }

    public class MobileInputActionTriggers : MonoBehaviour, IMobileInputCallbacks
    {
        [SerializeField] private List<InputAction> _inputActions = new List<InputAction>();

        private void OnEnable() => CallbackHub.RegisterCallback<IMobileInputCallbacks>(this);
        private void OnDisable() => CallbackHub.UnregisterCallback<IMobileInputCallbacks>(this);

        #region Trigger Methods
        private void TriggerAction(InputAction.ActionType type)
        {
            Debug.LogWarning($"[MobileInputActionTriggers] {type} triggered.");
            foreach (var action in _inputActions)
            {
                if (action.type == type)
                {
                    action.onTriggered?.Invoke();
                }
            }
        }

        // Helper method to add actions in code
        public void AddAction(InputAction.ActionType type, UnityAction callback)
        {
            var action = new InputAction { type = type };
            action.onTriggered.AddListener(callback);
            _inputActions.Add(action);
        }

        public void ClearActions()
        {
            foreach (var action in _inputActions)
            {
                action.onTriggered.RemoveAllListeners();
            }
            _inputActions.Clear();
        }
        #endregion

        #region IMobileInputCallbacks Implementation
        // Core Callbacks
        public void OnPrimaryDown(float x, float y)
        {
            TriggerAction(InputAction.ActionType.PrimaryDown);
        }

        public void OnPrimaryUp(float x, float y)
        {
            TriggerAction(InputAction.ActionType.PrimaryUp);
        }

        public void OnPrimaryDrag(float dx, float dy)
        {
            TriggerAction(InputAction.ActionType.PrimaryDrag);
        }

        public void OnSelect(GameObject target)
        {
            TriggerAction(InputAction.ActionType.Select);
        }

        public void OnSelectHit(RaycastHit hit)
        {
            TriggerAction(InputAction.ActionType.Select);
        }

        public void OnCancel()
        {
            TriggerAction(InputAction.ActionType.Cancel);
        }

        public void OnZoom(float delta)
        {
            TriggerAction(InputAction.ActionType.Zoom);
        }

        // Mobile-Specific Callbacks
        public void OnTwoFingerTap(Vector2 position)
        {
            TriggerAction(InputAction.ActionType.TwoFingerTap);
        }

        public void OnTwoFingerLongPress(Vector2 position)
        {
            TriggerAction(InputAction.ActionType.TwoFingerLongPress);
        }

        public void OnTwoFingerSwipe(Vector2 direction, Vector2 startPosition)
        {
            TriggerAction(InputAction.ActionType.TwoFingerSwipe);
        }

        public void OnThreeFingerTap(Vector2 position)
        {
            TriggerAction(InputAction.ActionType.ThreeFingerTap);
        }

        public void OnThreeFingerSwipe(Vector2 direction, Vector2 startPosition)
        {
            TriggerAction(InputAction.ActionType.ThreeFingerSwipe);
        }

        public void OnThreeFingerPinch(float delta)
        {
            TriggerAction(InputAction.ActionType.ThreeFingerPinch);
        }

        public void OnFourFingerTap()
        {
            TriggerAction(InputAction.ActionType.FourFingerTap);
        }

        public void OnFourFingerSwipe(Vector2 direction)
        {
            TriggerAction(InputAction.ActionType.FourFingerSwipe);
        }

        public void OnEdgeSwipe(EdgeDirection edge)
        {
            TriggerAction(InputAction.ActionType.EdgeSwipe);
        }

        public void OnForceTouch(float pressure)
        {
            TriggerAction(InputAction.ActionType.ForceTouch);
        }

        public void OnHapticTouch()
        {
            TriggerAction(InputAction.ActionType.HapticTouch);
        }

        public void OnBackTap(int tapCount)
        {
            TriggerAction(InputAction.ActionType.BackTap);
        }

        public void OnShake()
        {
            TriggerAction(InputAction.ActionType.Shake);
        }

        public void OnTilt(Vector3 tiltRotation)
        {
            TriggerAction(InputAction.ActionType.Tilt);
        }

        public void OnDeviceRotated(DeviceOrientation orientation)
        {
            TriggerAction(InputAction.ActionType.DeviceRotated);
        }

        public void OnPickUp()
        {
            TriggerAction(InputAction.ActionType.PickUp);
        }

        public void OnPutDown()
        {
            TriggerAction(InputAction.ActionType.PutDown);
        }

        public void OnAccessibilityAction(string actionName)
        {
            TriggerAction(InputAction.ActionType.AccessibilityAction);
        }

        public void OnScreenReaderGesture(string gestureType)
        {
            TriggerAction(InputAction.ActionType.ScreenReaderGesture);
        }

        public void OnNotificationAction(bool isQuickAction)
        {
            TriggerAction(InputAction.ActionType.NotificationAction);
        }
        #endregion
    }
}
