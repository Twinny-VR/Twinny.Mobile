using Concept.Core;
using Twinny.Core.Input;
using Unity.Cinemachine;
using UnityEngine;

namespace Twinny.Mobile.Camera
{
    /// <summary>
    /// Mobile input bridge for Cinemachine Pan Tilt-based FPS cameras.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    [RequireComponent(typeof(CinemachinePanTilt))]
    public class MobileCinemachineFpsHandler : MonoBehaviour, IMobileInputCallbacks
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
        [SerializeField] private CinemachinePanTilt _panTilt;

        [Header("Tuning")]
        [SerializeField] private float _rotateSpeed = 0.1f;
        [SerializeField] private float _tiltSpeed = 0.1f;
        [SerializeField] private Vector2 _verticalAxisLimits = new Vector2(-80f, 80f);
        [SerializeField] private float _panSpeed = 6f;
        [SerializeField] private float _panReturnSpeed = 3f;
        [SerializeField] private bool _returnPanToOriginOnRelease = true;
        [SerializeField] private PanTargetMode _panTargetMode = PanTargetMode.TrackingTarget;
        [SerializeField] private Transform _customPanTarget;
        [SerializeField] private float _zoomFov = 45f;
        [SerializeField] private float _zoomSpeed = 90f;
        [SerializeField] private float _zoomReleaseDelay = 0.15f;

        private bool _hasDefaultFov;
        private float _defaultFov;
        private float _lastZoomInputTime;
        private bool _zoomRequested;
        private bool _isPanning;
        private bool _isReturningPan;
        private Vector3 _panOriginPosition;
        private Vector3 _panOriginLocalPosition;
        private bool _panUseLocalSpace;
        private Vector3 _panReturnVelocity;
        private Transform _panTarget;
        private Transform _originalFollow;
        private Transform _panPivot;

        private void Update()
        {
            if (!IsActiveCamera()) return;
            UpdateZoom();
            UpdatePanReturn();
#if UNITY_EDITOR
            UpdateEditorPanRelease();
#endif
        }

        private void OnEnable()
        {
            EnsureReferences();
            ClampLimits();
            CacheDefaultFov();
            SetupPanPivot();
            CallbackHub.RegisterCallback<IMobileInputCallbacks>(this);
        }

        private void OnDisable()
        {
            RestorePanPivot();
            CallbackHub.UnregisterCallback<IMobileInputCallbacks>(this);
        }

        private void OnValidate()
        {
            EnsureReferences();
            ClampLimits();
        }

        public void OnPrimaryDown(float x, float y) { }
        public void OnPrimaryUp(float x, float y) { }
        public void OnSelect(GameObject target) { }
        public void OnSelectHit(RaycastHit hit) { }
        public void OnCancel() { }
        public void OnPrimaryDrag(float dx, float dy) => ApplyRotation(dx, dy);
        public void OnZoom(float delta) => RegisterZoomInput(delta);
        public void OnTwoFingerTap(Vector2 position) => EndPan();
        public void OnTwoFingerLongPress(Vector2 position) { }
        public void OnTwoFingerSwipe(Vector2 direction, Vector2 startPosition)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
#if UNITY_EDITOR
                if (UnityEngine.Input.GetMouseButton(0) || UnityEngine.Input.GetMouseButton(1))
                    return;
#endif
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

        private void ApplyRotation(float dx, float dy)
        {
            if (!IsActiveCamera()) return;
            if (_panTilt == null) return;

            var horizontal = _panTilt.PanAxis;
            horizontal.Value += dx * _rotateSpeed;
            _panTilt.PanAxis = horizontal;

            var vertical = _panTilt.TiltAxis;
            float next = vertical.Value - dy * _tiltSpeed;
            vertical.Value = Mathf.Clamp(next, _verticalAxisLimits.x, _verticalAxisLimits.y);
            _panTilt.TiltAxis = vertical;
        }

        private void ApplyPan(Vector2 direction)
        {
            if (!IsActiveCamera()) return;
            Transform reference = GetPanReference();
            if (reference == null) return;

            Transform panTarget = _panTarget ?? ResolvePanTarget();
            if (panTarget == null) return;

            Vector3 right = reference.right;
            Vector3 up = reference.up;
            Vector3 move = (right * direction.x + up * direction.y) * (_panSpeed * Time.deltaTime);
            if (_panUseLocalSpace && panTarget.parent != null)
            {
                Vector3 localMove = panTarget.parent.InverseTransformVector(move);
                panTarget.localPosition += localMove;
            }
            else
            {
                panTarget.position += move;
            }
        }

        private void BeginPanIfNeeded()
        {
            if (_isPanning) return;
            _isPanning = true;
            _panTarget = ResolvePanTarget();
            _panUseLocalSpace = _panTarget == _panPivot && _panTarget != null && _panTarget.parent != null;
            if (_panTarget != null)
            {
                _panOriginPosition = _panTarget.position;
                _panOriginLocalPosition = _panUseLocalSpace ? Vector3.zero : _panTarget.localPosition;
            }
        }

        private void EndPan()
        {
            if (!_isPanning) return;
            _isPanning = false;
            if (_returnPanToOriginOnRelease && _panTarget != null)
                _isReturningPan = true;
        }

        private void EnsureReferences()
        {
            if (_cinemachineCamera == null)
                _cinemachineCamera = GetComponent<CinemachineCamera>();

            if (_panTilt == null && _cinemachineCamera != null)
                _panTilt = _cinemachineCamera.GetComponent<CinemachinePanTilt>();

            if (_panTilt == null)
                _panTilt = GetComponent<CinemachinePanTilt>();
        }

        private void ClampLimits()
        {
            if (_verticalAxisLimits.y < _verticalAxisLimits.x)
                _verticalAxisLimits.x = _verticalAxisLimits.y;
        }

        private Transform GetPanReference()
        {
            if (_cinemachineCamera != null) return _cinemachineCamera.transform;
            if (UnityEngine.Camera.main != null) return UnityEngine.Camera.main.transform;
            return null;
        }

        private Transform GetPanTarget()
        {
            switch (_panTargetMode)
            {
                case PanTargetMode.CameraTransform:
                    return _cinemachineCamera != null ? _cinemachineCamera.transform : transform;
                case PanTargetMode.TrackingTarget:
                    return _panPivot != null ? _panPivot : _cinemachineCamera != null ? _cinemachineCamera.Follow : null;
                case PanTargetMode.LookAtTarget:
                    return _cinemachineCamera != null ? _cinemachineCamera.LookAt : null;
                case PanTargetMode.CustomTransform:
                    return _customPanTarget;
                default:
                    return null;
            }
        }

        private Transform ResolvePanTarget()
        {
            var target = GetPanTarget();
            if (target != null) return target;
            if (_cinemachineCamera != null) return _cinemachineCamera.transform;
            return transform;
        }

        private void SetupPanPivot()
        {
            if (_panTargetMode != PanTargetMode.TrackingTarget) return;
            if (_cinemachineCamera == null || _cinemachineCamera.Follow == null) return;
            if (_panPivot != null) return;

            _originalFollow = _cinemachineCamera.Follow;
            _panPivot = CreatePanPivot(_originalFollow);
            _cinemachineCamera.Follow = _panPivot;
        }

        private void RestorePanPivot()
        {
            if (_cinemachineCamera != null && _originalFollow != null && _cinemachineCamera.Follow == _panPivot)
                _cinemachineCamera.Follow = _originalFollow;

            if (_panPivot != null)
                Destroy(_panPivot.gameObject);

            _panPivot = null;
            _originalFollow = null;
        }

        private static Transform CreatePanPivot(Transform parent)
        {
            var pivot = new GameObject("PanPivot").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = Vector3.zero;
            pivot.localRotation = Quaternion.identity;
            return pivot;
        }

        private void CacheDefaultFov()
        {
            if (_cinemachineCamera == null) return;
            _defaultFov = _cinemachineCamera.Lens.FieldOfView;
            _hasDefaultFov = true;
        }

        private void RegisterZoomInput(float delta)
        {
            if (!IsActiveCamera()) return;
            if (Mathf.Abs(delta) <= 0.0001f) return;
            _zoomRequested = true;
            _lastZoomInputTime = Time.unscaledTime;
        }

        private void UpdateZoom()
        {
            if (!IsActiveCamera()) return;
            if (_cinemachineCamera == null) return;
            if (!_hasDefaultFov) CacheDefaultFov();

            bool zoomActive = _zoomRequested &&
                (Time.unscaledTime - _lastZoomInputTime) <= _zoomReleaseDelay;

#if UNITY_EDITOR
            if (UnityEngine.Input.GetMouseButton(2))
                zoomActive = true;
#endif

            float target = zoomActive ? _zoomFov : _defaultFov;
            var lens = _cinemachineCamera.Lens;
            float current = lens.FieldOfView;
            float step = _zoomSpeed * Time.unscaledDeltaTime;
            lens.FieldOfView = Mathf.MoveTowards(current, target, step);
            _cinemachineCamera.Lens = lens;

            if (!zoomActive && Mathf.Approximately(lens.FieldOfView, _defaultFov))
                _zoomRequested = false;
        }

        private void UpdatePanReturn()
        {
            if (!IsActiveCamera()) return;
            Transform panTarget = _panTarget ?? ResolvePanTarget();
            if (!_isReturningPan || panTarget == null) return;

            float smoothTime = 1f / Mathf.Max(0.01f, _panReturnSpeed);
            if (_panUseLocalSpace && panTarget.parent != null)
            {
                panTarget.localPosition = Vector3.SmoothDamp(
                    panTarget.localPosition,
                    _panOriginLocalPosition,
                    ref _panReturnVelocity,
                    smoothTime
                );
            }
            else
            {
                panTarget.position = Vector3.SmoothDamp(
                    panTarget.position,
                    _panOriginPosition,
                    ref _panReturnVelocity,
                    smoothTime
                );
            }

            Vector3 currentPos = _panUseLocalSpace ? panTarget.localPosition : panTarget.position;
            Vector3 targetPos = _panUseLocalSpace ? _panOriginLocalPosition : _panOriginPosition;
            if (Vector3.SqrMagnitude(currentPos - targetPos) <= 0.0001f)
            {
                if (_panUseLocalSpace)
                    panTarget.localPosition = _panOriginLocalPosition;
                else
                    panTarget.position = _panOriginPosition;
                _panReturnVelocity = Vector3.zero;
                _isReturningPan = false;
                _panTarget = null;
            }
        }

#if UNITY_EDITOR
        private void UpdateEditorPanRelease()
        {
            if (!IsActiveCamera()) return;
            if (!_isPanning) return;
            bool left = UnityEngine.Input.GetMouseButton(0);
            bool right = UnityEngine.Input.GetMouseButton(1);
            if (!left || !right)
                EndPan();
        }
#endif

        private bool IsActiveCamera()
        {
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
