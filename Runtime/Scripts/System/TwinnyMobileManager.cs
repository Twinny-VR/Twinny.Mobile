using System.Threading.Tasks;
using Concept.Core;
using Twinny.Core;
using Twinny.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Twinny.Mobile
{
    public class TwinnyMobileManager : MonoBehaviour, IMobileUICallbacks
    {
        private const string START_SCENE_NAME = "MobileStartScene";
        private const string MOCKUP_SCENE_NAME = "MobileMockupScene";

        private void OnEnable()
        {
            CallbackHub.RegisterCallback<IMobileUICallbacks>(this);
        }

        private void OnDisable()
        {
            CallbackHub.UnregisterCallback<IMobileUICallbacks>(this);
        }

        private async void Start()
        {
            await InitializeAsync();
        }

        private void Update()
        {
        }

        public async Task InitializeAsync()
        {
            StateMachine.ChangeState(new IdleState(this));
            await LoadSceneWithProgressAsync(START_SCENE_NAME, LoadSceneMode.Additive);
        }

        public async void OnImmersiveRequested()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!WebGLGyroAPI.IsInitialized)
                WebGLGyroAPI.RequestGyroPermission();
#endif
            await CanvasTransition.FadeScreenAsync(true,1f,renderMode:RenderMode.ScreenSpaceOverlay);
            StateMachine.ChangeState(new MobileImmersiveState(this));
            await CanvasTransition.FadeScreenAsync(false,1f,renderMode:RenderMode.ScreenSpaceOverlay);

        }

        public async void OnMockupRequested()
        {
            await CanvasTransition.FadeScreenAsync(true,1f,renderMode:RenderMode.ScreenSpaceOverlay);
            StateMachine.ChangeState(new MobileMockupState(this));
            await CanvasTransition.FadeScreenAsync(false,1f,renderMode:RenderMode.ScreenSpaceOverlay);

        }

        public async void OnStartExperienceRequested()
        {
            await StartExperienceSequenceAsync();
        }

        public void OnLoadingProgressChanged(float progress) { }
        public void OnExperienceLoaded() { }
        public void OnGyroscopeToggled(bool enabled) { }

        private static async Task LoadSceneWithProgressAsync(string sceneName, LoadSceneMode mode)
        {
            CallbackHub.CallAction<ITwinnyMobileCallbacks>(callback => callback.OnSceneLoadStart(sceneName));
            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnLoadingProgressChanged(0f));
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (loadOperation == null)
                return;

            while (!loadOperation.isDone)
            {
                float normalized = Mathf.Clamp01(loadOperation.progress / 0.9f);
                CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnLoadingProgressChanged(normalized));
                await Task.Yield();
            }

            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnLoadingProgressChanged(1f));

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid())
                CallbackHub.CallAction<ITwinnyMobileCallbacks>(callback => callback.OnSceneLoaded(loadedScene));

            await CanvasTransition.FadeScreenAsync(false, 1f, renderMode: RenderMode.ScreenSpaceOverlay);
        }

        private async Task StartExperienceSequenceAsync()
        {
            await LoadSceneWithProgressAsync(MOCKUP_SCENE_NAME, LoadSceneMode.Additive);
            StateMachine.ChangeState(new MobileMockupState(this));
            CallbackHub.CallAction<ITwinnyMobileCallbacks>(callback => callback.OnExperienceLoaded());
        }
    }
}
