using Concept.Core;
using Twinny.Core;
using Twinny.Core.Input;
using Twinny.Mobile;
using Twinny.Mobile.Interactables;
using Twinny.Mobile.Navigation;
using Twinny.Shaders;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Mobile.Samples
{
    [RequireComponent(typeof(UIDocument))]
    public class MainInterface : MonoBehaviour, IMobileUICallbacks, ITwinnyMobileCallbacks, IMobileInputCallbacks
    {
        private const string StartButtonName = "StartButton";
        private const string HomeButtonName = "HomeButton";
        private const string ImmersiveButtonName = "ImmersiveButton";
        private const string MockupButtonName = "MockupButton";
        private const string GyroToggleButtonName = "GyroToggleButton";
        private const string MainUiName = "MainUI";
        private const string ExperienceUiName = "ExperienceUI";
        private const string GlobalUiRootName = "GlobalUIRoot";
        private const string SceneOverlayRootName = "SceneOverlayRoot";
        private const string LoadingOverlayRootName = "LoadingOverlayRoot";
        private const string LoadingBarFillName = "LoadingBarFill";
        private const string CutoffSliderName = "CutoffSlider";
        private const string InjectedContentRootName = "InjectedContentRoot";
        private const string FloorHintRootName = "FloorHintRoot";
        private const float LoadingSortingOrder = 1000f;


        [SerializeField] private UIDocument _document;
        [SerializeField] private float _floorHintScreenPadding = 16f;
        private float _defaultSortingOrder;
        private bool _hasDefaultSortingOrder;
        private float _defaultPanelSortingOrder;
        private bool _hasDefaultPanelSortingOrder;
        private Button _startButton;
        private Button _immersiveButton;
        private Button _mockupButton;
        private Button _homeButton;
        private Button _gyroToggleButton;
        private VisualElement _mainUi;
        private VisualElement _experienceUi;
        private VisualElement _globalUiRoot;
        private VisualElement _sceneOverlayRoot;
        private VisualElement _loadingOverlayRoot;
        private VisualElement _loadingBarFill;
        private VisualElement _injectedContentRoot;
        private FloorHintWidget _floorHintWidget;
        private Slider _cutoffSlider;
        private Floor _selectedFloor;
        private bool _isCutoffPointerDragging;
        private int _cutoffPointerId = -1;
        private bool _warnedMissingRoot;
        private bool _warnedMissingStart;
        private bool _warnedMissingImmersive;
        private bool _warnedMissingMockup;
        private bool _warnedMissingHome;
        private bool _warnedMissingGyroToggle;
        private bool _warnedMissingMainUi;
        private bool _warnedMissingExperienceUi;
        private bool _warnedMissingLoadingOverlayRoot;
        private bool _warnedMissingLoadingBar;
        private bool _warnedMissingCutoffSlider;
        private bool _warnedMissingInjectedRoot;
        private bool _gyroEnabled = true;
        private bool _isMockupMode;
        private bool _isDemoModeActive;

        private void OnEnable()
        {
            EnsureDocument();
            CaptureDefaultSortingOrder();
            CacheElements();
            RegisterCallbacks();
            CallbackHub.RegisterCallback<ITwinnyMobileCallbacks>(this);
            CallbackHub.RegisterCallback<IMobileUICallbacks>(this);
            CallbackHub.RegisterCallback<IMobileInputCallbacks>(this);
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
            CallbackHub.UnregisterCallback<IMobileInputCallbacks>(this);
            CallbackHub.UnregisterCallback<IMobileUICallbacks>(this);
            CallbackHub.UnregisterCallback<ITwinnyMobileCallbacks>(this);
        }

        private void Update()
        {
            UpdateFloorHintPosition();
        }

        private void EnsureDocument()
        {
            if (_document == null)
                _document = GetComponent<UIDocument>();
        }

        private void CaptureDefaultSortingOrder()
        {
            if (_document == null || _hasDefaultSortingOrder)
                return;

            _defaultSortingOrder = _document.sortingOrder;
            _hasDefaultSortingOrder = true;

            if (_document.panelSettings != null)
            {
                _defaultPanelSortingOrder = _document.panelSettings.sortingOrder;
                _hasDefaultPanelSortingOrder = true;
            }
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
            _homeButton = root.Q<Button>(HomeButtonName);
            _gyroToggleButton = root.Q<Button>(GyroToggleButtonName);
            _mainUi = root.Q<VisualElement>(MainUiName);
            _experienceUi = root.Q<VisualElement>(ExperienceUiName);
            _globalUiRoot = root.Q<VisualElement>(GlobalUiRootName);
            _sceneOverlayRoot = root.Q<VisualElement>(SceneOverlayRootName);
            _loadingOverlayRoot = root.Q<VisualElement>(LoadingOverlayRootName);
            _loadingBarFill = root.Q<VisualElement>(LoadingBarFillName);
            _cutoffSlider = root.Q<Slider>(CutoffSliderName);
            _injectedContentRoot = root.Q<VisualElement>(InjectedContentRootName);

            if (_startButton == null) WarnMissingStart();
            if (_immersiveButton == null) WarnMissingImmersive();
            if (_mockupButton == null) WarnMissingMockup();
            if (_homeButton == null) WarnMissingHome();
            if (_gyroToggleButton == null) WarnMissingGyroToggle();
            if (_mainUi == null) WarnMissingMainUi();
            if (_experienceUi == null) WarnMissingExperienceUi();
            if (_loadingOverlayRoot == null) WarnMissingLoadingOverlayRoot();
            if (_loadingBarFill == null) WarnMissingLoadingBar();
            if (_cutoffSlider == null) WarnMissingCutoffSlider();
            if (_injectedContentRoot == null) WarnMissingInjectedRoot();

            if (_cutoffSlider != null)
            {
                _cutoffSlider.lowValue = 0f;
                _cutoffSlider.pageSize = 0.001f;
            }

            if (_loadingOverlayRoot != null)
                _loadingOverlayRoot.style.display = DisplayStyle.None;

            if (_loadingBarFill != null)
                _loadingBarFill.style.width = Length.Percent(0f);

            // Initialize mode UI before first callback to avoid one-frame button flicker.
            _isMockupMode = true;
            ApplyModeButtons();
            UpdateHomeButtonVisibility(TwinnyMobileRuntime.GetDefaultSceneName());
        }

        private void HandleFloorSelected(Floor floor)
        {
            if (floor == null) return;
            _selectedFloor = floor;
            EnsureFloorHintCreated();
            RefreshFloorHintContent();
            SetFloorHintVisibility(true);
            UpdateFloorHintPosition();
        }

        private void HandleFloorUnselected(Floor floor)
        {
            if (_selectedFloor != floor) return;
            _selectedFloor = null;
            SetFloorHintVisibility(false);
        }

        private void EnsureFloorHintCreated()
        {
            if (_floorHintWidget != null) return;
            if (_injectedContentRoot == null) return;

            _floorHintWidget = new FloorHintWidget { name = FloorHintRootName };
            _floorHintWidget.style.position = Position.Absolute;
            _floorHintWidget.RegisterCallback<ClickEvent>(_ => HandleFloorHintClicked());
            _injectedContentRoot.Add(_floorHintWidget);
            SetFloorHintVisibility(false);
        }

        private void RefreshFloorHintContent()
        {
            if (_selectedFloor == null || _floorHintWidget == null) return;
            _floorHintWidget.SetFloor(_selectedFloor);
        }

        private void UpdateFloorHintPosition()
        {
            if (_floorHintWidget == null || _selectedFloor == null || _document == null) return;
            if (_document.rootVisualElement?.panel == null) return;

            if (_isDemoModeActive)
            {
                SetFloorHintVisibility(false);
                return;
            }

            Camera cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();
            if (cam == null) return;

            if (!_selectedFloor.TryGetScreenRect(cam, out Rect screenRect))
            {
                SetFloorHintVisibility(false);
                return;
            }

            Vector3 anchorScreen = cam.WorldToScreenPoint(_selectedFloor.TargetPosition);
            if (anchorScreen.z <= 0f)
            {
                SetFloorHintVisibility(false);
                return;
            }

            SetFloorHintVisibility(true);

            float x = screenRect.xMax + _floorHintScreenPadding;
            float y = anchorScreen.y;
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(_document.rootVisualElement.panel, new Vector2(x, y));

            float hintWidth = _floorHintWidget.resolvedStyle.width > 0f ? _floorHintWidget.resolvedStyle.width : 220f;
            float hintHeight = _floorHintWidget.resolvedStyle.height > 0f ? _floorHintWidget.resolvedStyle.height : 64f;
            float panelWidth = _document.rootVisualElement.resolvedStyle.width;
            float panelHeight = _document.rootVisualElement.resolvedStyle.height;

            float left = Mathf.Clamp(panelPos.x, 0f, Mathf.Max(0f, panelWidth - hintWidth));
            float top = Mathf.Clamp(panelPos.y - (hintHeight * 0.5f), 0f, Mathf.Max(0f, panelHeight - hintHeight));

            _floorHintWidget.style.left = left;
            _floorHintWidget.style.top = top;
        }

        private void SetFloorHintVisibility(bool visible)
        {
            if (_floorHintWidget == null) return;
            _floorHintWidget.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HandleFloorHintClicked()
        {
            if (_selectedFloor == null) return;
            if (!_selectedFloor.HasImmersionScene) return;

            Floor selectedFloor = _selectedFloor;
            HandleFloorUnselected(selectedFloor);
            SetFloorHintVisibility(false);
            Debug.Log($"[MainInterface] Floor hint clicked: {selectedFloor.ImmersionSceneName}");
            CallbackHub.CallAction<IMobileUICallbacks>(callback =>
            {
                if (selectedFloor.SceneOpenMode == Floor.FloorSceneOpenMode.Mockup)
                    callback.OnMockupRequested(selectedFloor.ImmersionSceneName);
                else
                    callback.OnImmersiveRequested(selectedFloor.ImmersionSceneName);
            });
        }

        private void RegisterCallbacks()
        {
            if (_startButton != null)
                _startButton.clicked += HandleStartClicked;

            if (_immersiveButton != null)
                _immersiveButton.clicked += HandleImmersiveClicked;

            if (_mockupButton != null)
                _mockupButton.clicked += HandleMockupClicked;

            if (_homeButton != null)
                _homeButton.clicked += HandleHomeClicked;

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

            if (_homeButton != null)
                _homeButton.clicked -= HandleHomeClicked;

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
            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnStartExperienceRequested(TwinnyMobileRuntime.GetDefaultSceneName()));
        }

        private void HandleImmersiveClicked()
        {
            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnImmersiveRequested());
        }

        private void HandleMockupClicked()
        {
            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnMockupRequested());
        }

        private void HandleHomeClicked()
        {
            UpdateHomeButtonVisibility(TwinnyMobileRuntime.GetDefaultSceneName());
            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnStartExperienceRequested());
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

        public void OnImmersiveRequested(string sceneName)
        {
            SetModeButtons(isMockup: false);
        }

        public void OnMockupRequested(string sceneName)
        {
            SetModeButtons(isMockup: true);
        }

        public void OnStartExperienceRequested(string sceneName) { }

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
        public void OnEnterDemoMode()
        {
            _isDemoModeActive = true;
            SetFloorHintVisibility(false);
        }
        public void OnExitDemoMode()
        {
            _isDemoModeActive = false;
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
            SetDocumentSortingOrder(LoadingSortingOrder);
            SetSceneRootsVisibility(false);

            if (_loadingOverlayRoot != null)
                _loadingOverlayRoot.style.display = DisplayStyle.Flex;

            if (_loadingBarFill != null)
                _loadingBarFill.style.width = Length.Percent(0f);
        }

        public void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene)
        {
            SetSceneRootsVisibility(true);

            if (_loadingOverlayRoot != null)
                _loadingOverlayRoot.style.display = DisplayStyle.None;

            RestoreSortingOrder();
            UpdateHomeButtonVisibility(scene.name);
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

        public void OnPrimaryDown(float x, float y) { }
        public void OnPrimaryUp(float x, float y) { }
        public void OnPrimaryDrag(float dx, float dy) { }
        public void OnSelect(SelectionData selection)
        {
            if (selection.Target == null)
            {
                if (_selectedFloor != null)
                    HandleFloorUnselected(_selectedFloor);
                return;
            }

            Floor floor = selection.Target.GetComponentInParent<Floor>();
            if (floor == null)
            {
                if (_selectedFloor != null)
                    HandleFloorUnselected(_selectedFloor);
                return;
            }

            HandleFloorSelected(floor);
        }
        public void OnCancel()
        {
            if (_selectedFloor != null)
                HandleFloorUnselected(_selectedFloor);
        }
        public void OnZoom(float delta) { }
        public void OnTwoFingerTap(Vector2 position) { }
        public void OnTwoFingerLongPress(Vector2 position) { }
        public void OnTwoFingerSwipe(Vector2 direction, Vector2 startPosition)
        {
            if (_selectedFloor != null)
                HandleFloorUnselected(_selectedFloor);
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

        private void WarnMissingHome()
        {
            if (_warnedMissingHome) return;
            _warnedMissingHome = true;
            Debug.LogWarning("[MainInterface] HomeButton not found in UXML.");
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

        private void WarnMissingInjectedRoot()
        {
            if (_warnedMissingInjectedRoot) return;
            _warnedMissingInjectedRoot = true;
            Debug.LogWarning("[MainInterface] InjectedContentRoot not found in UXML.");
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

        private void UpdateHomeButtonVisibility(string loadedSceneName)
        {
            if (_homeButton == null)
                return;

            bool shouldShow = !string.IsNullOrWhiteSpace(loadedSceneName) &&
                              !string.Equals(loadedSceneName, TwinnyMobileRuntime.GetDefaultSceneName(), System.StringComparison.Ordinal);
            _homeButton.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetSceneRootsVisibility(bool visible)
        {
            DisplayStyle display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (_globalUiRoot != null)
                _globalUiRoot.style.display = display;

            if (_injectedContentRoot != null)
                _injectedContentRoot.style.display = display;

            if (_sceneOverlayRoot != null)
                _sceneOverlayRoot.style.display = display;
        }

        private void SetDocumentSortingOrder(float sortingOrder)
        {
            if (_document == null)
                return;

            _document.sortingOrder = sortingOrder;

            if (_document.panelSettings != null)
                _document.panelSettings.sortingOrder = sortingOrder;
        }

        private void RestoreSortingOrder()
        {
            SetDocumentSortingOrder(_defaultSortingOrder);

            if (_document == null || _document.panelSettings == null || !_hasDefaultPanelSortingOrder)
                return;

            _document.panelSettings.sortingOrder = _defaultPanelSortingOrder;
        }
    }
}
