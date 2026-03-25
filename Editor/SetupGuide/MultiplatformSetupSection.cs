#if UNITY_EDITOR
using System.Linq;
using Twinny.Multiplatform;
using Twinny.Multiplatform.Input;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Twinny.Editor
{
    [InitializeOnLoad]
    public static class MultiplatformSetupSectionRegister
    {
        private const string PackageName = "com.twinny.multiplatform";
        private const string IconsAtlasPath = "Packages/com.twinny.twe26/Editor/SetupGuide/Resources/Sprites/Icons.png";
        private const string IconSpriteName = "Ico_Multi";

        static MultiplatformSetupSectionRegister()
        {
            SetupGuideWindow.RegisterModule(new ModuleInfo
            {
                sortOrder = 20,
                moduleName = PackageName,
                moduleDisplayName = "Multiplatform",
                moduleIcon = LoadIcon(),
                moduleInstallPath = "https://github.com/Twinny-VR/Twinny.Multiplatform.git"
            }, typeof(MultiplatformSetupSection));
        }

        private static Sprite LoadIcon()
        {
            return AssetDatabase.LoadAllAssetsAtPath(IconsAtlasPath)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == IconSpriteName);
        }
    }

    [UxmlElement]
    public partial class MultiplatformSetupSection : VisualElement, IModuleSetup
    {
        private const string UxmlAssetPath = "Packages/com.twinny.multiplatform/Editor/SetupGuide/MultiplatformSetupSection.uxml";
        private const string UssAssetPath = "Packages/com.twinny.multiplatform/Editor/SetupGuide/MultiplatformSetupSection.uss";
        private const string InputSettingsAssetPath = "Assets/Resources/InputSettings.asset";
        private readonly Label _title;
        private readonly Label _description;
        private readonly Button _runtimeTabButton;
        private readonly Button _inputTabButton;
        private readonly VisualElement _runtimeTabContent;
        private readonly VisualElement _inputTabContent;
        private readonly VisualElement _runtimeInspectorRoot;
        private readonly VisualElement _inputInspectorRoot;
        private SerializedObject _runtimeSerializedObject;
        private SerializedObject _inputSerializedObject;

        public MultiplatformSetupSection()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlAssetPath);
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssAssetPath);

            if (styleSheet != null)
                styleSheets.Add(styleSheet);

            if (visualTree != null)
            {
                visualTree.CloneTree(this);
                _title = this.Q<Label>("MultiplatformTitle");
                _description = this.Q<Label>("MultiplatformDescription");
                _runtimeTabButton = this.Q<Button>("RuntimeTabButton");
                _inputTabButton = this.Q<Button>("InputTabButton");
                _runtimeTabContent = this.Q<VisualElement>("RuntimeTabContent");
                _inputTabContent = this.Q<VisualElement>("InputTabContent");
                _runtimeInspectorRoot = this.Q<VisualElement>("RuntimeInspectorRoot");
                _inputInspectorRoot = this.Q<VisualElement>("InputInspectorRoot");
                RegisterTabCallbacks();
            }
            else
            {
                AddToClassList("content");
                Add(new Label("Multiplatform setup layout not found."));
            }

            if (_title != null)
                _title.text = "Twinny Multiplatform";

            if (_description != null)
            {
                _description.text =
                    "Configure the screen-based Twinny runtime for Windows, WebGL, Android, and iOS. " +
                    "This package centralizes input, navigation, cameras, UI, and scene flow outside XR.";
            }
        }

        public void OnShowSection(SetupGuideWindow guideWindow, int tabIndex = 0)
        {
            RebuildRuntimeSection();
            RebuildInputSection();
            ShowTab(tabIndex == 1 ? "input" : "runtime");
        }

        public void OnApply()
        {
        }

        private void RebuildRuntimeSection()
        {
            if (_runtimeInspectorRoot == null)
                return;

            _runtimeInspectorRoot.Clear();

            PlatformRuntime runtimePreset = PlatformRuntime.GetInstance(true);
            if (runtimePreset == null)
            {
                _runtimeInspectorRoot.Add(new Label("PlatformRuntimePreset could not be loaded."));
                return;
            }

            _runtimeSerializedObject = new SerializedObject(runtimePreset);
            SerializedProperty iterator = _runtimeSerializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                    continue;

                PropertyField field = new PropertyField(iterator.Copy());
                field.Bind(_runtimeSerializedObject);
                field.AddToClassList("multiplatform-runtime-field");
                _runtimeInspectorRoot.Add(field);
            }
        }

        private void RebuildInputSection()
        {
            if (_inputInspectorRoot == null)
                return;

            _inputInspectorRoot.Clear();

            InputSettings inputSettings = AssetDatabase.LoadAssetAtPath<InputSettings>(InputSettingsAssetPath);
            if (inputSettings == null)
            {
                _inputInspectorRoot.Add(new Label($"InputSettings could not be loaded at '{InputSettingsAssetPath}'."));
                return;
            }

            _inputSerializedObject = new SerializedObject(inputSettings);
            SerializedProperty iterator = _inputSerializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                    continue;

                PropertyField field = new PropertyField(iterator.Copy());
                field.Bind(_inputSerializedObject);
                field.AddToClassList("multiplatform-runtime-field");
                _inputInspectorRoot.Add(field);
            }
        }

        private void RegisterTabCallbacks()
        {
            if (_runtimeTabButton != null)
                _runtimeTabButton.clicked += () => ShowTab("runtime");

            if (_inputTabButton != null)
                _inputTabButton.clicked += () => ShowTab("input");
        }

        private void ShowTab(string tabName)
        {
            bool showInput = tabName == "input";

            if (_runtimeTabContent != null)
                _runtimeTabContent.style.display = showInput ? DisplayStyle.None : DisplayStyle.Flex;

            if (_inputTabContent != null)
                _inputTabContent.style.display = showInput ? DisplayStyle.Flex : DisplayStyle.None;

            _runtimeTabButton?.EnableInClassList("active", !showInput);
            _inputTabButton?.EnableInClassList("active", showInput);
        }
    }
}
#endif
