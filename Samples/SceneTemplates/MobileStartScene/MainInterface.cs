using Concept.Core;
using Twinny.Mobile;
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
        private const string LoadingOverlayName = "LoadingOverlay";
        private const string LoadingBarFillName = "LoadingBarFill";
        private const string CutoffSliderName = "CutoffSlider";

        [SerializeField] private UIDocument _document;
        private Button _startButton;
        private Button _immersiveButton;
        private Button _mockupButton;
        private Button _gyroToggleButton;
        private VisualElement _mainUi;
        private VisualElement _experienceUi;
        private VisualElement _loadingOverlay;
        private VisualElement _loadingBarFill;
        private Slider _cutoffSlider;
        private bool _warnedMissingRoot;
        private bool _warnedMissingStart;
        private bool _warnedMissingImmersive;
        private bool _warnedMissingMockup;
        private bool _warnedMissingGyroToggle;
        private bool _warnedMissingMainUi;
        private bool _warnedMissingExperienceUi;
        private bool _warnedMissingLoadingOverlay;
        private bool _warnedMissingLoadingBar;
        private bool _warnedMissingCutoffSlider;
        private bool _gyroEnabled = true;

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
            _loadingOverlay = root.Q<VisualElement>(LoadingOverlayName);
            _loadingBarFill = root.Q<VisualElement>(LoadingBarFillName);
            _cutoffSlider = root.Q<Slider>(CutoffSliderName);

            if (_startButton == null) WarnMissingStart();
            if (_immersiveButton == null) WarnMissingImmersive();
            if (_mockupButton == null) WarnMissingMockup();
            if (_gyroToggleButton == null) WarnMissingGyroToggle();
            if (_mainUi == null) WarnMissingMainUi();
            if (_experienceUi == null) WarnMissingExperienceUi();
            if (_loadingOverlay == null) WarnMissingLoadingOverlay();
            if (_loadingBarFill == null) WarnMissingLoadingBar();
            if (_cutoffSlider == null) WarnMissingCutoffSlider();
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

        public void OnImmersiveRequested()
        {
            SetModeButtons(isMockup: false);
        }

        public void OnMockupRequested()
        {
            SetModeButtons(isMockup: true);
        }

        public void OnStartExperienceRequested() { }

        public void OnEnterImmersiveMode(){ }
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
        public void OnSceneLoadStart(string sceneName) { }
        public void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene) { }
        public void OnTeleportToLandMark(int landMarkIndex) { }
        public void OnSkyboxHDRIChanged(Material material) { }

        public void OnLoadingProgressChanged(float progress)
        {
            if (_loadingBarFill == null || _loadingOverlay == null)
                return;

            float clamped = Mathf.Clamp01(progress);
            _loadingBarFill.style.width = Length.Percent(clamped * 100f);
            _loadingOverlay.style.display = (clamped >= 0.01f && clamped < 1f) ? DisplayStyle.Flex : DisplayStyle.None;
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

        private void WarnMissingLoadingOverlay()
        {
            if (_warnedMissingLoadingOverlay) return;
            _warnedMissingLoadingOverlay = true;
            Debug.LogWarning("[MainInterface] LoadingOverlay not found in UXML.");
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
            if (_immersiveButton != null)
                _immersiveButton.style.display = isMockup ? DisplayStyle.Flex : DisplayStyle.None;

            if (_mockupButton != null)
                _mockupButton.style.display = isMockup ? DisplayStyle.None : DisplayStyle.Flex;

            if (_gyroToggleButton != null)
                _gyroToggleButton.style.display = isMockup ? DisplayStyle.None : DisplayStyle.Flex;

            if (_cutoffSlider != null)
                _cutoffSlider.style.display = isMockup ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateGyroToggleLabel()
        {
            if (_gyroToggleButton == null) return;
            _gyroToggleButton.text = _gyroEnabled ? "Gyro On" : "Gyro Off";
        }
    }
}
