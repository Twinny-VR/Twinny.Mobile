using System.Reflection;
using System.Collections.Generic;
using System.Collections;
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
        [SerializeField] private bool _returnTrackingTargetToOriginOnRelease = false;
        [SerializeField] private float _panSpeed = 6f;
        [SerializeField] private float _panReturnSpeed = 3f;
        [SerializeField] private float _zoomSpeed = 3f;
        [SerializeField] private bool _lockRotationWhileTwoFingerPan = true;
        [SerializeField] private float _hardLookRestoreDelay = 0.08f;
        [SerializeField] private bool _enablePanLimit;
        [SerializeField] private float _maxPanDistance = 10f;
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
        private Vector3 _initialPanTargetPosition;
        private bool _hasInitialPosition;
        private Vector3 _lastValidPanForward = Vector3.forward;
        private float _panLockHorizontalAxis;
        private float _panLockVerticalAxis;
        private bool _hasPanLockAxes;
        private readonly List<SuspendedHardLookState> _suspendedHardLookStates = new List<SuspendedHardLookState>();
        private bool _isHardLookSuspended;
        private Transform _suspendedLookAtTarget;
        private Coroutine _hardLookRestoreRoutine;

        private struct SuspendedHardLookState
        {
            public Behaviour behaviour;
            public bool wasEnabled;
        }

        private void Update()
        {
            if (!IsActiveCamera()) return;
            EnforceRotationLockWhilePanning();
            UpdatePanReturn();
        }

        private void OnEnable()
        {
            EnsureReferences();
            CacheOrbitalMembers();
            InitializePanLimit();
            ApplyMode(_isModeActive);
            CallbackHub.RegisterCallback<IMobileInputCallbacks>(this);
            CallbackHub.RegisterCallback<ITwinnyMobileCallbacks>(this);
        }

        private void OnDisable()
        {
            if (_hardLookRestoreRoutine != null)
            {
                StopCoroutine(_hardLookRestoreRoutine);
                _hardLookRestoreRoutine = null;
            }
            RestoreHardLookAfterPan();
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
            RestoreHardLookAfterPan();
            if (_lockRotationWhileTwoFingerPan && _isPanning) return;

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

            Transform panTarget = GetTrackingTarget();
            if (panTarget == null) return;

            if (!TryGetStablePanAxes(out Vector3 right, out Vector3 forward))
                return;
            
            // Initialize limit origin if not set yet
            if (_enablePanLimit && !_hasInitialPosition)
            {
                _initialPanTargetPosition = panTarget.position;
                _hasInitialPosition = true;
            }

            // Normalize sensitivity based on screen height (Reference: 1080p)
            float screenScale = 1080f / Mathf.Max(Screen.height, 1);

            // Dynamic speed based on zoom (radius)
            float zoomFactor = 1f;
            float currentRadius = GetRadius();
            // Use max radius as baseline to preserve the "perfect" speed at distance
            if (!float.IsNaN(currentRadius) && _radiusLimits.y > 0.001f)
            {
                zoomFactor = Mathf.Clamp(currentRadius / _radiusLimits.y, 0.01f, 1f);
            }

            // Invert input for natural "drag world" feel and scale for pixel coordinates
            Vector3 move = (right * -direction.x + forward * -direction.y) * (_panSpeed * 0.002f * screenScale * zoomFactor);
            Vector3 finalPos = panTarget.position + move;
            finalPos.y = panTarget.position.y;

            if (_enablePanLimit)
            {
                Vector3 offset = finalPos - _initialPanTargetPosition;
                finalPos = _initialPanTargetPosition + Vector3.ClampMagnitude(offset, _maxPanDistance);
                finalPos.y = panTarget.position.y;
            }

            panTarget.position = finalPos;
        }

        private bool TryGetStablePanAxes(out Vector3 right, out Vector3 forward)
        {
            right = Vector3.right;
            forward = Vector3.forward;

            // Prefer orbital yaw: it's stable even when camera tilt approaches 90 degrees.
            if (_orbitalFollow != null)
            {
                float yaw = _orbitalFollow.HorizontalAxis.Value;
                Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
                forward = yawRot * Vector3.forward;
                right = yawRot * Vector3.right;
                _lastValidPanForward = forward;
                return true;
            }

            Transform reference = GetPanReference();
            if (reference == null) return false;

            Vector3 flatForward = Vector3.ProjectOnPlane(reference.forward, Vector3.up);
            if (flatForward.sqrMagnitude > 0.0001f)
            {
                forward = flatForward.normalized;
                _lastValidPanForward = forward;
            }
            else if (_lastValidPanForward.sqrMagnitude > 0.0001f)
            {
                forward = _lastValidPanForward.normalized;
            }
            else
            {
                forward = Vector3.forward;
            }

            right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.right;
                forward = Vector3.forward;
            }

            return true;
        }

        private void BeginPanIfNeeded()
        {
            // Se estava voltando, cancela o retorno para dar prioridade ao dedo do usuário.
            bool wasReturning = _isReturningPan;
            _isReturningPan = false;

            if (_isPanning) return;
            _isPanning = true;
            Transform panTarget = GetTrackingTarget();
            // Só redefine a origem se não estivesse no meio de um retorno (evita drift da origem)
            if (panTarget != null && !wasReturning)
                _panOriginPosition = panTarget.position;

            CachePanLockAxes();
            SuspendHardLookWhilePanning();
        }

        private void EndPan()
        {
            if (!_isPanning) return;
            _isPanning = false;
            _hasPanLockAxes = false;
            if (_hardLookRestoreRoutine != null)
                StopCoroutine(_hardLookRestoreRoutine);
            if (_returnTrackingTargetToOriginOnRelease && GetTrackingTarget() != null)
                _isReturningPan = true;
        }

        private IEnumerator RestoreHardLookAfterPanDelayed()
        {
            float delay = Mathf.Max(0f, _hardLookRestoreDelay);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            RestoreHardLookAfterPan();
            _hardLookRestoreRoutine = null;
        }

        private void SuspendHardLookWhilePanning()
        {
            if (_cinemachineCamera == null) return;
            if (_isHardLookSuspended) return;

            _isHardLookSuspended = true;
            _suspendedLookAtTarget = _cinemachineCamera.LookAt;
            _cinemachineCamera.LookAt = null;

            var components = _cinemachineCamera.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null) continue;

                // Avoid hard dependency on a specific Cinemachine version/type name.
                if (!component.GetType().Name.Contains("HardLookAt")) continue;
                if (component is not Behaviour behaviour) continue;

                SuspendedHardLookState state = new SuspendedHardLookState
                {
                    behaviour = behaviour,
                    wasEnabled = behaviour.enabled
                };

                _suspendedHardLookStates.Add(state);
                behaviour.enabled = false;
            }
        }

        private void RestoreHardLookAfterPan()
        {
            if (!_isHardLookSuspended) return;

            if (_cinemachineCamera != null) _cinemachineCamera.LookAt = _suspendedLookAtTarget;
            _suspendedLookAtTarget = null;
            _isHardLookSuspended = false;

            for (int i = 0; i < _suspendedHardLookStates.Count; i++)
            {
                SuspendedHardLookState state = _suspendedHardLookStates[i];
                Behaviour behaviour = state.behaviour;
                if (behaviour == null) continue;
                behaviour.enabled = state.wasEnabled;
            }

            _suspendedHardLookStates.Clear();
        }

        private void CachePanLockAxes()
        {
            if (_orbitalFollow == null) return;
            _panLockHorizontalAxis = _orbitalFollow.HorizontalAxis.Value;
            _panLockVerticalAxis = _orbitalFollow.VerticalAxis.Value;
            _hasPanLockAxes = true;
        }

        private void EnforceRotationLockWhilePanning()
        {
            if (!_lockRotationWhileTwoFingerPan || !_isPanning) return;
            if (_orbitalFollow == null || !_hasPanLockAxes) return;

            var horizontal = _orbitalFollow.HorizontalAxis;
            horizontal.Value = _panLockHorizontalAxis;
            _orbitalFollow.HorizontalAxis = horizontal;

            var vertical = _orbitalFollow.VerticalAxis;
            vertical.Value = _panLockVerticalAxis;
            _orbitalFollow.VerticalAxis = vertical;
        }

        private void ApplyZoom(float delta)
        {
            if (!IsActiveCamera()) return;
            if (_orbitalFollow == null) return;
            RestoreHardLookAfterPan();

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
            Transform panTarget = GetTrackingTarget();
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

        private void InitializePanLimit()
        {
            Transform target = GetTrackingTarget();
            if (target != null && !_hasInitialPosition)
            {
                _initialPanTargetPosition = target.position;
                _hasInitialPosition = true;
            }
        }

        private Transform GetTrackingTarget()
        {
            EnsureReferences();
            return _cinemachineCamera != null ? _cinemachineCamera.Follow : null;
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
