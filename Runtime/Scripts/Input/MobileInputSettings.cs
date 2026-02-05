using UnityEngine;

namespace Twinny.Mobile.Input
{
    public class MobileInputSettings : ScriptableObject
    {
        [Header("Thresholds")]
        [SerializeField] private float _dragThreshold = 5f;
        [SerializeField] private float _longPressTime = 0.8f;
        [SerializeField] private float _edgeThreshold = 50f;
        [SerializeField] private float _shakeThreshold = 3.0f;
        [SerializeField] private float _twoFingerLongPressTime = 1.0f;
        [SerializeField] private float _pickupAccelerationThreshold = 0.5f;
        [SerializeField] private float _putDownStableTime = 2.0f;

        public float DragThreshold => _dragThreshold;
        public float LongPressTime => _longPressTime;
        public float EdgeThreshold => _edgeThreshold;
        public float ShakeThreshold => _shakeThreshold;
        public float TwoFingerLongPressTime => _twoFingerLongPressTime;
        public float PickupAccelerationThreshold => _pickupAccelerationThreshold;
        public float PutDownStableTime => _putDownStableTime;
    }
}