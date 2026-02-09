using System.Collections;
using Concept.Core;
using Twinny.Core;
using Twinny.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Twinny.Mobile
{
    public class TwinnyMobileManager : MonoBehaviour, IMobileUICallbacks
    {
        private const string MobileStartSceneName = "MobileStartScene";

        private void OnEnable()
        {
            CallbackHub.RegisterCallback<IMobileUICallbacks>(this);
        }

        private void OnDisable()
        {
            CallbackHub.UnregisterCallback<IMobileUICallbacks>(this);
        }

        private void Start()
        {
          Initialize();
        }

        private void Update()
        {
        }

        public void Initialize()
        {
            StateMachine.ChangeState(new IdleState(this));
            //TODO Decidir como fazer o carregamento das cenas
            StartCoroutine(LoadSceneWithProgress(MobileStartSceneName, LoadSceneMode.Additive));
        }

        public async void OnImmersiveRequested()
        {
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

        public void OnStartExperienceRequested()
        {
            StartCoroutine(StartExperienceSequence());
        }

        public void OnLoadingProgressChanged(float progress) { }
        public void OnExperienceLoaded() { }
        public void OnGyroscopeToggled(bool enabled) { }

        private static IEnumerator LoadSceneWithProgress(string sceneName, LoadSceneMode mode)
        {
            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnLoadingProgressChanged(0f));
            AsyncOperation async = SceneManager.LoadSceneAsync(sceneName, mode);
            if (async == null)
                yield break;

            while (!async.isDone)
            {
                float normalized = Mathf.Clamp01(async.progress / 0.9f);
                CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnLoadingProgressChanged(normalized));
                yield return null;
            }

            CallbackHub.CallAction<IMobileUICallbacks>(callback => callback.OnLoadingProgressChanged(1f));
            _ = CanvasTransition.FadeScreenAsync(false, 1f, renderMode: RenderMode.ScreenSpaceOverlay);

        }

        private IEnumerator StartExperienceSequence()
        {
            yield return LoadSceneWithProgress("MobileMockupScene", LoadSceneMode.Additive);
            StateMachine.ChangeState(new MobileMockupState(this));
            CallbackHub.CallAction<ITwinnyMobileCallbacks>(callback => callback.OnExperienceLoaded());
        }
    }
}
