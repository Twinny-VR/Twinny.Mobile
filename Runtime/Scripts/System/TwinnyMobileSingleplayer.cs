using UnityEngine;
using Twinny.Core;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using Concept.Core;

namespace Twinny.Mobile
{
    public class TwinnyMobileSingleplayer : IGameMode
    {

        private TwinnyMobileManager m_manager;
        public TwinnyMobileSingleplayer(TwinnyMobileManager managerOwner) => m_manager = managerOwner;

        public void Enter()
        {
        }
        public void Update()
        {
            throw new NotImplementedException();
        }


        public void Exit()
        {
            throw new NotImplementedException();
        }

        private async void Initialize()
        {
            await ChangeScene(1);
        }

        public async Task<Scene> ChangeScene(int buildIndex, int landMarkIndex = -1, Action<float> onSceneLoading = null)
        {
            string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(buildIndex));
            return await ChangeScene(sceneName, landMarkIndex, onSceneLoading);
        }

        public async Task<Scene> ChangeScene(string sceneName, int landMarkIndex = -1, Action<float> onSceneLoading = null)
        {
            Debug.LogWarning($"DEBUG:[GameMode] ChangeScene{sceneName}");
            Debug.LogWarning($"[GameMode] Change scene to {sceneName}");

            SceneFeatureMobile.Instance?.TeleportToLandMark(0);

            if (SceneManager.sceneCount > 1)
                await UnloadAdditivesScenes();


            AsyncOperation async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!async.isDone)
            {
                await Task.Yield();
                onSceneLoading?.Invoke(async.progress);
            }

            SceneFeatureMobile.Instance?.TeleportToLandMark(landMarkIndex);
            Scene newScene = SceneManager.GetSceneByName(sceneName);

            if (!newScene.IsValid())
            {
                Debug.LogError($"[TwinnyXRSingleplayer] Invalid '{sceneName}' scene name!");
                return default;
            }

            CallbackHub.CallAction<ITwinnyMobileCallbacks>(callback => callback.OnSceneLoaded(newScene));
            CallbackHub.CallAction<ITwinnyMobileCallbacks>(callback => callback.OnExperienceStarted());
            return newScene;
        }


        public void NavigateTo(int landMarkIndex)
        {
            throw new NotImplementedException();
        }

        public void Quit()
        {
            throw new NotImplementedException();
        }

        public void RestartExperience()
        {
            throw new NotImplementedException();
        }

        public Task StartExperience(int buildIndex, int landMarkIndex)
        {
            throw new NotImplementedException();
        }

        public Task StartExperience(string sceneName, int landMarkIndex)
        {
            throw new NotImplementedException();
        }



        public static async Task UnloadAdditivesScenes()
        {
            if (SceneManager.sceneCount <= 1) return;

            await Task.Yield(); // Similar "yield return new WaitForEndFrame()"

            for (int i = 1; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.IsValid() && loadedScene.isLoaded)
                    await SceneManager.UnloadSceneAsync(loadedScene);
            }
            await Resources.UnloadUnusedAssets();

        }

    }
}
