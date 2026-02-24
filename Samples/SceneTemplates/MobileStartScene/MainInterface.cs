using Concept.Core;
using Twinny.Mobile;
using Twinny.Mobile.Navigation;
using Twinny.Shaders;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Mobile.Samples
{
    [RequireComponent(typeof(UIDocument))]
    public class MainInterface : MonoBehaviour, IMobileUICallbacks, ITwinnyMobileCallbacks
    {
        private const string StartButtonName = "StartButton";
        private const string ImmersiveButtonName = "ImmersiveButton";
        private const string MockupButtonName = "MockupButton";
        private const string GyroToggleButtonName = "GyroToggleButton";
        private const string MainUiName = "MainUI";
        private const string ExperienceUiName = "ExperienceUI";
        private const string LoadingOverlayRootName = "LoadingOverlayRoot";
        private const string LoadingBarFillName = "LoadingBarFill";
        private const string CutoffSliderName = "CutoffSlider";


        [SerializeField] private UIDocument _document;
        private Button _startButton;
        private Button _immersiveButton;
        private Button _mockupButton;
        private Button _gyroToggleButton;
        private VisualElement _mainUi;
        private VisualElement _experienceUi;
        private VisualElement _loadingOverlayRoot;
        private VisualElement _loadingBarFill;
        private Slider _cutoffSlider;
        private bool _isCutoffPointerDragging;
        private int _cutoffPointerId = -1;
        private bool _warnedMissingRoot;
        private bool _warnedMissingStart;
        private bool _warnedMissingImmersive;
        private bool _warnedMissingMockup;
        private bool _warnedMissingGyroToggle;
        private bool _warnedMissingMainUi;
        private bool _warnedMissingExperienceUi;
        private bool _warnedMissingLoadingOverlayRoot;
        private bool _warnedMissingLoadingBar;
        private bool _warnedMissingCutoffSlider;
        private bool _gyroEnabled = true;
        private bool _isMockupMode;

        private void OnEnable()
        {
            EnsureDocument();
            CacheElements();
            RegisterCallbacks();
            CallbackHub.RegisterCallback<ITwinnyMobileCallbacks>(this);
            CallbackHub.RegisterCallback<IMobileUICallbacks>(this);
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
            CallbackHub.UnregisterCallback<IMobileUICallbacks>(this);
            CallbackHub.UnregisterCallback<ITwinnyMobileCallbacks>(this);
        }

        private void EnsureDocument()
        {
            if (_document == null)
                _document = GetComponent<UIDocument>();
        }

        private void CacheElements()
        {
            if (_document == null || _document.rootVisualElement == null)
            {
                WarnMissingRoot();
                return;
            }

            var root = _document.rootVisualElement;
            _startButton = root.Q<Button>(StartButtonName);
            _immersiveButton = root.Q<Button>(ImmersiveButtonName);
            _mockupButton = root.Q<Button>(MockupButtonName);
            _gyroToggleButton = root.Q<Button>(GyroToggleButtonName);
            _mainUi = root.Q<VisualElement>(MainUiName);
            _experienceUi = root.Q<VisualElement>(ExperienceUiName);
            _loadingOverlayRoot = root.Q<VisualElement>(LoadingOverlayRootName);
            _loadingBarFill = root.Q<VisualElement>(LoadingBarFillName);
            _cutoffSlider = root.Q<Slider>(CutoffSliderName);

            if (_startButton == null) WarnMissingStart();
            if (_immersiveButton == null) WarnMissingImmersive();
            if (_mockupButton == null) WarnMissingMockup();
            if (_gyroToggleButton == null) WarnMissingGyroToggle();
            if (_mainUi == null) WarnMissingMainUi();
            if (_experienceUi == null) WarnMissingExperienceUi();
            if (_loadingOverlayRoot == null) WarnMissingLoadingOverlayRoot();
            if (_loadingBarFill == null) WarnMissingLoadingBar();
            if (_cutoffSlider == null) WarnMissingCutoffSlider();

            if (_cutoffSlider != null)
            {
                _cutoffSlider.lowValue = 0f;
                _cutoffSlider.pageSize = 0.001f;
            }

            if (_loadingOverlayRoot != null)
                _loadingOverlayRoot.style.display = DisplayStyle.None;

            if (_loadingBarFill != null)
                _loadingBarFill.style.width = Length.Percent(0f);
        }

        private void RegisterCallbacks()
        {
            if (_startButton != null)
                _startButton.clicked += HandleStartClicked;

            if (_immersiveButton != null)
                _immersiveButton.clicked += HandleImmersiveClicked;

            if (_mockupButton != null)
                _mockupButton.clicked += HandleMockupClicked;

            if (_gyroToggleButton != null)
                _gyroToggleButton.clicked += HandleGyroToggleClicked;

            if (_cutoffSlider != null)
                _cutoffSlider.RegisterValueChangedCallback(HandleCutoffChanged);

            if (_cutoffSlider != null)
            {
                _cutoffSlider.RegisterCallback<PointerDownEvent>(HandleCutoffPointerDown, TrickleDown.TrickleDown);
                _cutoffSlider.RegisterCallback<PointerMoveEvent>(HandleCutoffPointerMove, TrickleDown.TrickleDown);
                _cutoffSlider.RegisterCallback<PointerUpEvent>(HandleCutoffPointerUp, TrickleDown.TrickleDown);
                _cutoffSlider.RegisterCallback<PointerCancelEvent>(HandleCutoffPointerCancel, TrickleDown.TrickleDown);
            }
        }

        private void UnregisterCallbacks()
        {
            if (_startButton != null)
                _startButton.clicked -= HandleStartClicked;

            if (_immersiveButton != null)
                _immersiveButton.clicked -= HandleImmersiveClicked;

            if (_mockupButton != null)
                _mockupButton.clicked -= HandleMockupClicked;

            if (_gyroToggleButton != null)
                _gyroToggleButton.clicked -= HandleGyroToggleClicked;

            if (_cutoffSlider != null)
                _cutoffSlider.UnregisterValueChangedCallback(HandleCutoffChanged);

            if (_cutoffSlider != null)
            {
                _cutoffSlider.UnregisterCallback<PointerDownEvent>(HandleCutoffPointerDown, TrickleDown.TrickleDown);
                _cutoffSlider.UnregisterCallback<PointerMoveEvent>(HandleCutoffPointerMove, TrickleDown.TrickleDown);
                _cutoffSlider.UnregisterCallback<PointerUpEvent>(HandleCutoffPointerUp, TrickleDown.TrickleDown);
                _cutoffSlider.UnregisterCallback<PointerCancelEvent>(HandleCutoffPointerCancel, TrickleDown.TrickleDown);
                ReleaseCutoffPointer();
            }
        }

        private void HandleStartClicked()
        {
            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnStartExperienceRequested());
        }

        private void HandleImmersiveClicked()
        {
            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnImmersiveRequested());
        }

        private void HandleMockupClicked()
        {
            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnMockupRequested());
        }

        private void HandleGyroToggleClicked()
        {
            _gyroEnabled = !_gyroEnabled;
            UpdateGyroToggleLabel();
            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnGyroscopeToggled(_gyroEnabled));
        }

        private void HandleCutoffChanged(ChangeEvent<float> evt)
        {
            Shader.SetGlobalFloat("_CutoffHeight", evt.newValue);
        }

        private void HandleCutoffPointerDown(PointerDownEvent evt)
        {
            if (_cutoffSlider == null)
                return;

            _isCutoffPointerDragging = true;
            _cutoffPointerId = evt.pointerId;
            _cutoffSlider.CapturePointer(_cutoffPointerId);
            SetCutoffFromPointer(evt.position);
            evt.StopImmediatePropagation();
        }

        private void HandleCutoffPointerMove(PointerMoveEvent evt)
        {
            if (_cutoffSlider == null || !_isCutoffPointerDragging || evt.pointerId != _cutoffPointerId)
                return;

            SetCutoffFromPointer(evt.position);
            evt.StopImmediatePropagation();
        }

        private void HandleCutoffPointerUp(PointerUpEvent evt)
        {
            if (_cutoffSlider == null || evt.pointerId != _cutoffPointerId)
                return;

            SetCutoffFromPointer(evt.position);
            ReleaseCutoffPointer();
            evt.StopImmediatePropagation();
        }

        private void HandleCutoffPointerCancel(PointerCancelEvent evt)
        {
            if (_cutoffSlider == null || evt.pointerId != _cutoffPointerId)
                return;

            ReleaseCutoffPointer();
            evt.StopImmediatePropagation();
        }

        private void SetCutoffFromPointer(Vector2 pointerPosition)
        {
            if (_cutoffSlider == null)
                return;

            VisualElement dragContainer = _cutoffSlider.Q("unity-drag-container");
            Rect dragRect = dragContainer != null ? dragContainer.worldBound : _cutoffSlider.worldBound;
            if (dragRect.height <= 0f)
                return;

            float t = Mathf.InverseLerp(dragRect.yMax, dragRect.yMin, pointerPosition.y);
            float value = Mathf.Lerp(_cutoffSlider.lowValue, _cutoffSlider.highValue, t);
            _cutoffSlider.value = Mathf.Clamp(value, _cutoffSlider.lowValue, _cutoffSlider.highValue);
        }

        private void ReleaseCutoffPointer()
        {
            if (_cutoffSlider != null && _cutoffPointerId >= 0 && _cutoffSlider.HasPointerCapture(_cutoffPointerId))
                _cutoffSlider.ReleasePointer(_cutoffPointerId);

            _isCutoffPointerDragging = false;
            _cutoffPointerId = -1;
        }

        public void OnMaxWallHeightRequested(float height)
        {
            if (_cutoffSlider == null)
                return;

            float maxHeight = Mathf.Max(_cutoffSlider.lowValue, height);
            _cutoffSlider.highValue = maxHeight;
            _cutoffSlider.SetValueWithoutNotify(maxHeight);
            Shader.SetGlobalFloat("_CutoffHeight", maxHeight);
        }

        public void OnImmersiveRequested()
        {
            SetModeButtons(isMockup: false);
        }

        public void OnMockupRequested()
        {
            SetModeButtons(isMockup: true);
        }

        public void OnStartExperienceRequested() { }

        public void OnEnterImmersiveMode()
        {
            if (_cutoffSlider == null)
                return;

            float maxHeight = _cutoffSlider.highValue;
            _cutoffSlider.SetValueWithoutNotify(maxHeight);
            Shader.SetGlobalFloat("_CutoffHeight", maxHeight);
        }
        public void OnExitImmersiveMode(){ }

        public void OnEnterMockupMode()
        {
            SetModeButtons(isMockup: true);
            if (_cutoffSlider != null)
                _cutoffSlider.SetValueWithoutNotify(Shader.GetGlobalFloat("_CutoffHeight"));
        }
        public void OnExitMockupMode()
        {
            SetModeButtons(isMockup: false);
        }

        public void OnExperienceLoaded()
        {
            if (_mainUi != null)
                _mainUi.style.display = DisplayStyle.None;

            if (_experienceUi != null)
                _experienceUi.style.display = DisplayStyle.Flex;

            SetModeButtons(isMockup: true);
            UpdateGyroToggleLabel();
        }

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
        public void OnSceneLoadStart(string sceneName)
        {
            if (_loadingOverlayRoot != null)
                _loadingOverlayRoot.style.display = DisplayStyle.Flex;

            if (_loadingBarFill != null)
                _loadingBarFill.style.width = Length.Percent(0f);
        }

        public void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene)
        {
            if (_loadingOverlayRoot != null)
                _loadingOverlayRoot.style.display = DisplayStyle.None;

            ApplyModeButtons();
        }
        public void OnTeleportToLandMark(int landMarkIndex) { }
        public void OnSkyboxHDRIChanged(Material material) { }

        public void OnLoadingProgressChanged(float progress)
        {
            if (_loadingBarFill == null)
                return;

            float clamped = Mathf.Clamp01(progress);
            _loadingBarFill.style.width = Length.Percent(clamped * 100f);
        }

        public void OnGyroscopeToggled(bool enabled) { }

        private void WarnMissingRoot()
        {
            if (_warnedMissingRoot) return;
            _warnedMissingRoot = true;
            Debug.LogWarning("[MainInterface] UIDocument or rootVisualElement not found.");
        }

        private void WarnMissingImmersive()
        {
            if (_warnedMissingImmersive) return;
            _warnedMissingImmersive = true;
            Debug.LogWarning("[MainInterface] ImmersiveButton not found in UXML.");
        }

        private void WarnMissingStart()
        {
            if (_warnedMissingStart) return;
            _warnedMissingStart = true;
            Debug.LogWarning("[MainInterface] StartButton not found in UXML.");
        }

        private void WarnMissingMockup()
        {
            if (_warnedMissingMockup) return;
            _warnedMissingMockup = true;
            Debug.LogWarning("[MainInterface] MockupButton not found in UXML.");
        }

        private void WarnMissingGyroToggle()
        {
            if (_warnedMissingGyroToggle) return;
            _warnedMissingGyroToggle = true;
            Debug.LogWarning("[MainInterface] GyroToggleButton not found in UXML.");
        }

        private void WarnMissingMainUi()
        {
            if (_warnedMissingMainUi) return;
            _warnedMissingMainUi = true;
            Debug.LogWarning("[MainInterface] MainUI not found in UXML.");
        }

        private void WarnMissingExperienceUi()
        {
            if (_warnedMissingExperienceUi) return;
            _warnedMissingExperienceUi = true;
            Debug.LogWarning("[MainInterface] ExperienceUI not found in UXML.");
        }

        private void WarnMissingLoadingOverlayRoot()
        {
            if (_warnedMissingLoadingOverlayRoot) return;
            _warnedMissingLoadingOverlayRoot = true;
            Debug.LogWarning("[MainInterface] LoadingOverlayRoot not found in UXML.");
        }

        private void WarnMissingLoadingBar()
        {
            if (_warnedMissingLoadingBar) return;
            _warnedMissingLoadingBar = true;
            Debug.LogWarning("[MainInterface] LoadingBarFill not found in UXML.");
        }

        private void WarnMissingCutoffSlider()
        {
            if (_warnedMissingCutoffSlider) return;
            _warnedMissingCutoffSlider = true;
            Debug.LogWarning("[MainInterface] CutoffSlider not found in UXML.");
        }

        private void SetModeButtons(bool isMockup)
        {
            _isMockupMode = isMockup;
            ApplyModeButtons();
        }

        private void ApplyModeButtons()
        {
            bool canShowFpsControls = _isMockupMode && MobileFpsNavigation.HasActiveInstance;
            bool canShowAlphaSlider = _isMockupMode && AlphaClipper.HasActiveInstance;
            bool canShowGyroToggle = canShowFpsControls && IsMobileWebGlRuntime();
            if (_immersiveButton != null)
                _immersiveButton.style.display = canShowFpsControls ? DisplayStyle.Flex : DisplayStyle.None;

            if (_mockupButton != null)
                _mockupButton.style.display = _isMockupMode ? DisplayStyle.None : DisplayStyle.Flex;

            if (_gyroToggleButton != null)
                _gyroToggleButton.style.display = canShowGyroToggle ? DisplayStyle.Flex : DisplayStyle.None;

            if (_cutoffSlider != null)
                _cutoffSlider.style.display = canShowAlphaSlider ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateGyroToggleLabel()
        {
            if (_gyroToggleButton == null) return;
            _gyroToggleButton.text = _gyroEnabled ? "Gyro On" : "Gyro Off";
        }

        private static bool IsMobileWebGlRuntime()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
#else
            return false;
#endif
        }
    }
}
