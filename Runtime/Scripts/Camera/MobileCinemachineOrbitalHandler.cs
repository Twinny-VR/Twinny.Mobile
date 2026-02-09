using System.Reflection;
using Concept.Core;
using Twinny.Core.Input;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

namespace Twinny.Mobile.Camera
{
    /// <summary>
    /// Editor/mobile input bridge for Cinemachine Orbital Follow + Hard Look At setups.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public class MobileCinemachineOrbitalHandler : MonoBehaviour, IMobileInputCallbacks, ITwinnyMobileCallbacks
    {
        public enum PanTargetMode
        {
            CameraTransform,
            TrackingTarget,
            LookAtTarget,
            CustomTransform
        }

        [Header("Cinemachine")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private CinemachineOrbitalFollow _orbitalFollow;

        [Header("Mode")]
        [SerializeField] private int _activePriority = 20;
        [SerializeField] private int _inactivePriority = 5;

        [Header("Tuning")]
        [SerializeField] private float _rotateSpeed = 0.1f;
        [SerializeField] private float _tiltSpeed = 0.1f;
        [SerializeField] private bool _returnPanToOriginOnRelease = true;
        [SerializeField] private float _panSpeed = 6f;
        [SerializeField] private float _panReturnSpeed = 3f;
        [SerializeField] private float _zoomSpeed = 3f;
        [SerializeField] private Vector2 _verticalAxisLimits = new Vector2(-80f, 80f);
        [SerializeField] private Vector2 _radiusLimits = new Vector2(0.5f, 50f);
        [SerializeField] private PanTargetMode _panTargetMode = PanTargetMode.TrackingTarget;
        [SerializeField] private Transform _customPanTarget;

        private PropertyInfo _radiusProperty;
        private FieldInfo _radiusField;
        private bool _warnedMissingRadius;
        private bool _isPanning;
        private bool _isReturningPan;
        private Vector3 _panOriginPosition;
        private Vector3 _panReturnVelocity;
        private bool _isModeActive = true;

        private void Update()
        {
            if (!IsActiveCamera()) return;
            UpdatePanReturn();
        }

        private void OnEnable()
        {
            EnsureReferences();
            CacheOrbitalMembers();
            ApplyMode(_isModeActive);
            CallbackHub.RegisterCallback<IMobileInputCallbacks>(this);
            CallbackHub.RegisterCallback<ITwinnyMobileCallbacks>(this);
        }

        private void OnDisable()
        {
            CallbackHub.UnregisterCallback<IMobileInputCallbacks>(this);
            CallbackHub.UnregisterCallback<ITwinnyMobileCallbacks>(this);
        }

        private void OnValidate()
        {
            EnsureReferences();
            ClampLimits();
            CacheOrbitalMembers();
        }

        public void OnPrimaryDown(float x, float y) { }
        public void OnPrimaryUp(float x, float y) { }
        public void OnSelect(GameObject target) { }
        public void OnSelectHit(RaycastHit hit) { }
        public void OnCancel() { }

        public void OnPrimaryDrag(float dx, float dy)
        {
            ApplyRotation(dx, dy);
        }

        public void OnZoom(float delta)
        {
            ApplyZoom(delta);
        }

        public void OnTwoFingerTap(Vector2 position) { }
        public void OnTwoFingerLongPress(Vector2 position) { }

        public void OnTwoFingerSwipe(Vector2 direction, Vector2 startPosition)
        {
            if (!IsActiveCamera()) return;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                EndPan();
                return;
            }

            BeginPanIfNeeded();
            ApplyPan(direction);
        }

        public void OnThreeFingerTap(Vector2 position) { }
        public void OnThreeFingerSwipe(Vector2 direction, Vector2 startPosition) { }
        public void OnThreeFingerPinch(float delta) { }
        public void OnFourFingerTap() { }
        public void OnFourFingerSwipe(Vector2 direction) { }
        public void OnEdgeSwipe(EdgeDirection edge) { }
        public void OnForceTouch(float pressure) { }
        public void OnHapticTouch() { }
        public void OnBackTap(int tapCount) { }
        public void OnShake() { }
        public void OnTilt(Vector3 tiltRotation) { }
        public void OnDeviceRotated(DeviceOrientation orientation) { }
        public void OnPickUp() { }
        public void OnPutDown() { }
        public void OnAccessibilityAction(string actionName) { }
        public void OnScreenReaderGesture(string gestureType) { }
        public void OnNotificationAction(bool isQuickAction) { }

        public void OnStartInteract(GameObject gameObject) { }
        public void OnStopInteract(GameObject gameObject) { }
        public void OnStartTeleport() { }
        public void OnTeleport() { }

        public void OnPlatformInitializing() { }
        public void OnPlatformInitialized() { }
        public void OnExperienceReady() { }
        public void OnExperienceStarting() { }
        public void OnExperienceStarted() { }
        public void OnExperienceEnding() { }
        public void OnExperienceEnded(bool isRunning) { }
        public void OnExperienceLoaded() { }
        public void OnSceneLoadStart(string sceneName) { }
        public void OnSceneLoaded(Scene scene) { }
        public void OnTeleportToLandMark(int landMarkIndex) { }
        public void OnSkyboxHDRIChanged(Material material) { }

        public void OnEnterImmersiveMode() => ApplyMode(false);
        public void OnEnterMockupMode() => ApplyMode(true);

        private void ApplyRotation(float dx, float dy)
        {
            if (!IsActiveCamera()) return;
            if (_orbitalFollow == null) return;

            var horizontal = _orbitalFollow.HorizontalAxis;
            horizontal.Value += dx * _rotateSpeed;
            _orbitalFollow.HorizontalAxis = horizontal;

            var vertical = _orbitalFollow.VerticalAxis;
            float next = vertical.Value - dy * _tiltSpeed;
            vertical.Value = Mathf.Clamp(next, _verticalAxisLimits.x, _verticalAxisLimits.y);
            _orbitalFollow.VerticalAxis = vertical;
        }

        private void ApplyPan(Vector2 direction)
        {
            if (!IsActiveCamera()) return;
            Transform reference = GetPanReference();
            if (reference == null) return;

            Transform panTarget = GetPanTarget();
            if (panTarget == null) return;

            Vector3 right = reference.right;
            Vector3 up = reference.up;
            Vector3 move = (right * direction.x + up * direction.y) * (_panSpeed * Time.deltaTime);
            panTarget.position += move;
        }

        private void BeginPanIfNeeded()
        {
            if (_isPanning) return;
            _isPanning = true;
            Transform panTarget = GetPanTarget();
            if (panTarget != null)
                _panOriginPosition = panTarget.position;
        }

        private void EndPan()
        {
            if (!_isPanning) return;
            _isPanning = false;
            if (_returnPanToOriginOnRelease && GetPanTarget() != null)
                _isReturningPan = true;
        }

        private void ApplyZoom(float delta)
        {
            if (!IsActiveCamera()) return;
            if (_orbitalFollow == null) return;

            float radius = GetRadius();
            if (float.IsNaN(radius))
            {
                WarnMissingRadiusOnce();
                return;
            }

            float next = radius - delta * _zoomSpeed;
            SetRadius(Mathf.Clamp(next, _radiusLimits.x, _radiusLimits.y));
        }

        private Transform GetPanReference()
        {
            EnsureReferences();
            if (_cinemachineCamera != null) return _cinemachineCamera.transform;
            if (UnityEngine.Camera.main != null) return UnityEngine.Camera.main.transform;
            return null;
        }

        private void EnsureReferences()
        {
            if (_cinemachineCamera == null)
                _cinemachineCamera = GetComponent<CinemachineCamera>();

            if (_orbitalFollow == null && _cinemachineCamera != null)
                _orbitalFollow = _cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();

            if (_orbitalFollow == null)
                _orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        }

        private void ApplyMode(bool isActive)
        {
            _isModeActive = isActive;
            if (_cinemachineCamera != null)
                _cinemachineCamera.Priority = isActive ? _activePriority : _inactivePriority;
        }

        private void ClampLimits()
        {
            if (_verticalAxisLimits.y < _verticalAxisLimits.x)
                _verticalAxisLimits.x = _verticalAxisLimits.y;

            if (_radiusLimits.y < _radiusLimits.x)
                _radiusLimits.x = _radiusLimits.y;
        }

        private void CacheOrbitalMembers()
        {
            if (_orbitalFollow == null) return;

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = _orbitalFollow.GetType();

            _radiusProperty = type.GetProperty("Radius", flags)
                ?? type.GetProperty("OrbitRadius", flags);

            _radiusField = type.GetField("Radius", flags)
                ?? type.GetField("OrbitRadius", flags);
        }

        private float GetRadius()
        {
            if (_radiusProperty != null)
                return (float)_radiusProperty.GetValue(_orbitalFollow);

            if (_radiusField != null)
                return (float)_radiusField.GetValue(_orbitalFollow);

            return float.NaN;
        }

        private void SetRadius(float value)
        {
            if (_radiusProperty != null)
            {
                _radiusProperty.SetValue(_orbitalFollow, value);
                return;
            }

            if (_radiusField != null)
                _radiusField.SetValue(_orbitalFollow, value);
        }

        private void WarnMissingRadiusOnce()
        {
            if (_warnedMissingRadius) return;
            _warnedMissingRadius = true;
            Debug.LogWarning(
                "[MobileCinemachineOrbitalHandler] Could not find radius field on CinemachineOrbitalFollow."
            );
        }

        private void UpdatePanReturn()
        {
            if (!IsActiveCamera()) return;
            Transform panTarget = GetPanTarget();
            if (!_isReturningPan || panTarget == null) return;

            panTarget.position = Vector3.SmoothDamp(
                panTarget.position,
                _panOriginPosition,
                ref _panReturnVelocity,
                1f / Mathf.Max(0.01f, _panReturnSpeed)
            );

            if (Vector3.SqrMagnitude(panTarget.position - _panOriginPosition) <= 0.0001f)
            {
                panTarget.position = _panOriginPosition;
                _panReturnVelocity = Vector3.zero;
                _isReturningPan = false;
            }
        }

        private Transform GetPanTarget()
        {
            EnsureReferences();
            switch (_panTargetMode)
            {
                case PanTargetMode.CameraTransform:
                    return _cinemachineCamera != null ? _cinemachineCamera.transform : null;
                case PanTargetMode.TrackingTarget:
                    return _cinemachineCamera != null ? _cinemachineCamera.Follow : null;
                case PanTargetMode.LookAtTarget:
                    return _cinemachineCamera != null ? _cinemachineCamera.LookAt : null;
                case PanTargetMode.CustomTransform:
                    return _customPanTarget;
                default:
                    return null;
            }
        }

        private bool IsActiveCamera()
        {
            if (!_isModeActive) return false;
            EnsureReferences();
            if (_cinemachineCamera == null) return false;

            int count = CinemachineBrain.ActiveBrainCount;
            for (int i = 0; i < count; i++)
            {
                var brain = CinemachineBrain.GetActiveBrain(i);
                if (brain != null && brain.ActiveVirtualCamera == _cinemachineCamera)
                    return true;
            }

            return false;
        }
    }
}
