using Twinny.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Twinny.Multiplatform
{
    [CreateAssetMenu(fileName = "TwinnyMultiplatformRuntimePreset", menuName = "Twinny/Multiplatform Runtime")]
    public class PlatformRuntime : TwinnyRuntime
    {
        private const string MobileDefaultSceneName = "MobileMockupScene";
        private static PlatformRuntime _instance;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EnsurePresetExistsOnEditorLoad()
        {
            GetInstance(true);
        }
#endif

        public static PlatformRuntime GetInstance(bool forceCreate = false)
        {
            if (_instance == null)
                _instance = Resources.Load<PlatformRuntime>("PlatformRuntimePreset");

            if (_instance == null && forceCreate)
            {
                _instance = TwinnyRuntime.CreateAsset<PlatformRuntime>();
                if (_instance != null)
                {
                    _instance.defaultSceneName = MobileDefaultSceneName;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(_instance);
                    AssetDatabase.SaveAssets();
#endif
                }
            }

            return _instance;
        }

        public static string GetDefaultSceneName()
        {
            _instance ??= GetInstance();

            if (_instance != null && !string.IsNullOrWhiteSpace(_instance.defaultSceneName))
                return _instance.defaultSceneName;

            TwinnyRuntime runtime = TwinnyRuntime.GetInstance();
            if (runtime != null && !string.IsNullOrWhiteSpace(runtime.defaultSceneName))
                return runtime.defaultSceneName;

            return MobileDefaultSceneName;
        }
    }
}
