#if UNITY_EDITOR || UNITY_WEBGL
using Concept.Core;
using Twinny.Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Twinny.Mobile.Input
{
    public class MobileInputEmulator : MonoBehaviour
    {
        private const string SettingsResourceName = "MobileInputSettings";
        private const float TapMaxTime = 0.25f;
        private const float TwoFingerTapMaxTime = 0.3f;
        private const float MousePinchSensitivity = 0.005f;

        private MobileInputSettings _settings;
        private bool _warnedMissingSettings;
        private bool _warnedMissingRouter;
        [SerializeField] private bool _logTapDebug = false;
        [SerializeField] private bool _ignoreUiBlocking = true;
        private bool _loggedStartup;

        private bool _singleDown;
        private bool _singleDragging;
        private bool _suppressTap;
        private Vector3 _singleStartPos;
        private float _singleStartTime;
        private Vector3 _lastSinglePos;

        private bool _twoFingerDown;
        private bool _twoFingerDragging;
        private Vector3 _lastTwoFingerPos;
        private float _twoFingerStartTime;
        private bool _twoFingerLongPressDetected;
        private bool _suppressSingleUntilRelease;

        private bool _threeFingerDown;
        private bool _threeFingerDragging;
        private Vector3 _lastThreeFingerPos;
        private float _threeFingerStartTime;

        private bool _mousePinchDown;
        private Vector3 _lastMousePinchPos;

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld)
            {
                Destroy(gameObject);
                return;
            }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld) return;
#endif
            if (FindAnyObjectByType<MobileInputEmulator>() != null) return;
            var emulatorObject = new GameObject("MobileInputEmulator");
            emulatorObject.AddComponent<MobileInputEmulator>();
            DontDestroyOnLoad(emulatorObject);
        }

        private void Update()
        {
            if (_logTapDebug && !_loggedStartup)
                _loggedStartup = true;
            EnsureSettingsLoaded();
            ForceReleaseIfButtonsUp();
            if (_suppressSingleUntilRelease && !UnityEngine.Input.GetMouseButton(0))
                _suppressSingleUntilRelease = false;
            if (!_ignoreUiBlocking && EventSystem.current?.IsPointerOverGameObject() == true)
                return;

            if (HandleThreeFinger()) return;
            if (HandleTwoFinger()) return;
            if (HandleMousePinch()) return;

            HandleSingleFinger();
        }

        private bool HandleTwoFinger()
        {
            bool twoPressed = UnityEngine.Input.GetMouseButton(0) && UnityEngine.Input.GetMouseButton(1);
            if (!twoPressed && !_twoFingerDown) return false;

            if (twoPressed && !_twoFingerDown) BeginTwoFinger();
            if (twoPressed) UpdateTwoFinger();
            if (!twoPressed && _twoFingerDown) EndTwoFinger();
            return true;
        }

        private void BeginTwoFinger()
        {
            _twoFingerDown = true;
            _twoFingerDragging = false;
            _twoFingerStartTime = Time.time;
            _twoFingerLongPressDetected = false;
            _lastTwoFingerPos = UnityEngine.Input.mousePosition;
            _suppressSingleUntilRelease = true;
            _suppressTap = true;
            _singleDown = false;
            _singleDragging = false;
        }

        private void UpdateTwoFinger()
        {
            Vector3 current = UnityEngine.Input.mousePosition;
            Vector2 delta = (Vector2)(current - _lastTwoFingerPos);
            if (delta.sqrMagnitude > 0f)
            {
                _twoFingerDragging = true;
                _suppressTap = true;
                Vector2 direction = delta.normalized;
                Vector2 center = current;
                CallbackHub.CallAction<IMobileInputCallbacks>(
                    cb => cb.OnTwoFingerSwipe(direction, center)
                );
                MobileInputEvents.Drag(direction, center);
            }

            if (!_twoFingerLongPressDetected &&
                Time.time - _twoFingerStartTime > _settings.TwoFingerLongPressTime)
            {
                _twoFingerLongPressDetected = true;
                Vector2 center = current;
                CallbackHub.CallAction<IMobileInputCallbacks>(
                    cb => cb.OnTwoFingerLongPress(center)
                );
                MobileInputEvents.Tap(center);
            }

            _lastTwoFingerPos = current;
        }

        private void EndTwoFinger()
        {
            Vector2 center = _lastTwoFingerPos;
            float elapsed = Time.time - _twoFingerStartTime;
            if (!_twoFingerDragging && elapsed <= TwoFingerTapMaxTime)
            {
                CallbackHub.CallAction<IMobileInputCallbacks>(
                    cb => cb.OnTwoFingerTap(center)
                );
                MobileInputEvents.Tap(center);
            }
            else if (_twoFingerDragging)
            {
                CallbackHub.CallAction<IMobileInputCallbacks>(
                    cb => cb.OnTwoFingerSwipe(Vector2.zero, center)
                );
                MobileInputEvents.TwoFingerSwipe(Vector2.zero, center);
                MobileInputEvents.Drag(Vector2.zero, center);
            }
            _twoFingerDown = false;
            _twoFingerDragging = false;
        }

        private bool HandleThreeFinger()
        {
            bool threePressed = UnityEngine.Input.GetMouseButton(2) &&
                (UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt));
            if (!threePressed && !_threeFingerDown) return false;

            if (threePressed && !_threeFingerDown) BeginThreeFinger();
            if (threePressed) UpdateThreeFinger();
            if (!threePressed && _threeFingerDown) EndThreeFinger();
            return true;
        }

        private void BeginThreeFinger()
        {
            _threeFingerDown = true;
            _threeFingerDragging = false;
            _threeFingerStartTime = Time.time;
            _lastThreeFingerPos = UnityEngine.Input.mousePosition;
        }

        private void UpdateThreeFinger()
        {
            Vector3 current = UnityEngine.Input.mousePosition;
            Vector2 delta = (Vector2)(current - _lastThreeFingerPos);
            if (delta.sqrMagnitude > 0f)
            {
                _threeFingerDragging = true;
                Vector2 direction = delta.normalized;
                Vector2 center = current;
                CallbackHub.CallAction<IMobileInputCallbacks>(
                    cb => cb.OnThreeFingerSwipe(direction, center)
                );
                MobileInputEvents.Drag(direction, center);
            }
            _lastThreeFingerPos = current;
        }

        private void EndThreeFinger()
        {
            Vector2 center = _lastThreeFingerPos;
            float elapsed = Time.time - _threeFingerStartTime;
            if (!_threeFingerDragging && elapsed <= TapMaxTime)
            {
                CallbackHub.CallAction<IMobileInputCallbacks>(
                    cb => cb.OnThreeFingerTap(center)
                );
                MobileInputEvents.Tap(center);
            }
            _threeFingerDown = false;
            _threeFingerDragging = false;
            _suppressTap = false;
        }

        private bool HandleMousePinch()
        {
            bool pinchPressed = UnityEngine.Input.GetMouseButton(2) &&
                !UnityEngine.Input.GetKey(KeyCode.LeftAlt) &&
                !UnityEngine.Input.GetKey(KeyCode.RightAlt);
            if (!pinchPressed && !_mousePinchDown) return false;

            if (pinchPressed && !_mousePinchDown) BeginMousePinch();
            if (pinchPressed) UpdateMousePinch();
            if (!pinchPressed && _mousePinchDown) EndMousePinch();
            return true;
        }

        private void BeginMousePinch()
        {
            _mousePinchDown = true;
            _lastMousePinchPos = UnityEngine.Input.mousePosition;
            _suppressSingleUntilRelease = true;
            _singleDown = false;
            _singleDragging = false;
        }

        private void UpdateMousePinch()
        {
            var router = TryGetRouter();
            if (router == null) return;

            Vector3 current = UnityEngine.Input.mousePosition;
            float deltaY = current.y - _lastMousePinchPos.y;
            if (Mathf.Abs(deltaY) > 0.01f)
            {
                float pinch = deltaY * MousePinchSensitivity;
                router.Zoom(pinch);
                MobileInputEvents.PinchZoom(pinch);
            }
            _lastMousePinchPos = current;
        }

        private void EndMousePinch()
        {
            _mousePinchDown = false;
        }

        private void HandleSingleFinger()
        {
            var router = TryGetRouter();
            if (router == null) return;

            if (_suppressSingleUntilRelease)
            {
                if (!UnityEngine.Input.GetMouseButton(0))
                    _suppressSingleUntilRelease = false;
                return;
            }

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _singleDown = true;
                _singleDragging = false;
                _singleStartPos = UnityEngine.Input.mousePosition;
                _lastSinglePos = _singleStartPos;
                _singleStartTime = Time.time;
                router.PrimaryDown(_singleStartPos.x, _singleStartPos.y);
            }

            if (_singleDown && UnityEngine.Input.GetMouseButton(0))
            {
                Vector3 current = UnityEngine.Input.mousePosition;
                Vector2 delta = (Vector2)(current - _lastSinglePos);
                if (delta.sqrMagnitude > _settings.DragThreshold * _settings.DragThreshold)
                {
                    _singleDragging = true;
                    _suppressTap = true;
                    router.PrimaryDrag(delta.x, delta.y);
                    MobileInputEvents.Drag(delta, current);
                }
                _lastSinglePos = current;
            }

            if (_singleDown && UnityEngine.Input.GetMouseButtonUp(0))
            {
                Vector3 current = UnityEngine.Input.mousePosition;
                router.PrimaryUp(current.x, current.y);
                if (!_singleDragging && !_suppressTap && Time.time - _singleStartTime <= TapMaxTime)
                {
                    TrySelect(current, router);
                    MobileInputEvents.Tap(current);
                }
                _singleDown = false;
                _singleDragging = false;
                _suppressTap = false;
            }
        }

        private void ForceReleaseIfButtonsUp()
        {
            if (_twoFingerDown)
            {
                bool left = UnityEngine.Input.GetMouseButton(0);
                bool right = UnityEngine.Input.GetMouseButton(1);
                if (!left || !right)
                    EndTwoFinger();
            }

            if (_threeFingerDown && !UnityEngine.Input.GetMouseButton(2))
                EndThreeFinger();

            if (_mousePinchDown && !UnityEngine.Input.GetMouseButton(2))
                EndMousePinch();

            if (_singleDown && !UnityEngine.Input.GetMouseButton(0))
            {
                var router = TryGetRouter();
                if (router != null && !_singleDragging && !_suppressTap && Time.time - _singleStartTime <= TapMaxTime)
                {
                    TrySelect(_lastSinglePos, router);
                    MobileInputEvents.Tap(_lastSinglePos);
                }
                _singleDown = false;
                _singleDragging = false;
                _suppressSingleUntilRelease = false;
                _suppressTap = false;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) return;
            ResetAllStates();
        }

        private void OnDisable()
        {
            ResetAllStates();
        }

        private void ResetAllStates()
        {
            _singleDown = false;
            _singleDragging = false;
            _twoFingerDown = false;
            _twoFingerDragging = false;
            _twoFingerLongPressDetected = false;
            _threeFingerDown = false;
            _threeFingerDragging = false;
            _suppressSingleUntilRelease = false;
        }

        private void HandleScroll()
        {
            var router = TryGetRouter();
            if (router == null) return;

            if (UnityEngine.Input.GetMouseButton(2))
                return;

            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) <= 0.0001f) return;

            router.Zoom(scroll);
            MobileInputEvents.PinchZoom(scroll);
        }

        private void TrySelect(Vector3 screenPosition, InputRouter router)
        {
            var camera = GetRaycastCamera();
            if (camera == null)
            {
                router.Cancel();
                return;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log($"[MobileInputEmulator] Click hit {hit.collider.name} at {hit.point}");
                router.Select(hit.collider.gameObject);
                CallbackHub.CallAction<IMobileInputCallbacks>(cb => cb.OnSelectHit(hit));
            }
            else
            {
                Debug.Log("[MobileInputEmulator] Click hit nothing.");
                router.Cancel();
            }
        }

        private void EnsureSettingsLoaded()
        {
            if (_settings != null) return;

            _settings = Resources.Load<MobileInputSettings>(SettingsResourceName);
            if (_settings != null) return;

            if (!_warnedMissingSettings)
            {
                _warnedMissingSettings = true;
                Debug.LogWarning(
                    $"[MobileInputEmulator] Missing settings asset. " +
                    $"Expected Resources/{SettingsResourceName}.asset. Using in-memory defaults."
                );
            }

            _settings = ScriptableObject.CreateInstance<MobileInputSettings>();
        }

        private InputRouter TryGetRouter()
        {
            var router = InputRouter.Instance;
            if (router == null && !_warnedMissingRouter)
            {
                _warnedMissingRouter = true;
                Debug.LogWarning("[MobileInputEmulator] InputRouter.Instance is null. Input routing disabled.");
            }
            return router;
        }

        private Camera GetRaycastCamera()
        {
            if (Camera.main != null) return Camera.main;
            return FindAnyObjectByType<Camera>();
        }
    }
}
#endif
