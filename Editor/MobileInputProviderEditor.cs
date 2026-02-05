using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Twinny.Mobile.Input;

namespace Twinny.Mobile.Editor.Input
{
    [CustomEditor(typeof(MobileInputProvider))]
    public class MobileInputProviderEditor : UnityEditor.Editor
    {
        private const string SETTINGS_PATH = "Assets/Resources/MobileInputSettings.asset";
        private const string SETTINGS_RESOURCE_NAME = "MobileInputSettings";

        private void OnEnable()
        {
            EnsureSettingsExistAndAssigned();
        }

        /// <summary>
        /// Verifica se o ScriptableObject de config existe em Resources.
        /// Se no, cria. Se sim, atribui ao Provider.
        /// </summary>
        private void EnsureSettingsExistAndAssigned()
        {
            serializedObject.Update();
            var settingsProp = serializedObject.FindProperty("_settings");

            // Se j estiver atribudo, no faz nada
            if (settingsProp.objectReferenceValue != null) return;

            // Tenta carregar de Resources
            var settings = Resources.Load<MobileInputSettings>(SETTINGS_RESOURCE_NAME);

            if (settings == null)
            {
                // Garante que a pasta Resources existe
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                // Verifica via AssetDatabase para ter certeza (caso Resources.Load falhe por cache)
                settings = AssetDatabase.LoadAssetAtPath<MobileInputSettings>(SETTINGS_PATH);

                if (settings == null)
                {
                    settings = CreateInstance<MobileInputSettings>();
                    AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[MobileInputProvider] Created global settings at {SETTINGS_PATH}");
                }
            }

            settingsProp.objectReferenceValue = settings;
            serializedObject.ApplyModifiedProperties();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // 1. Renderiza as propriedades padro do Provider (incluindo o campo _settings)
            var iterator = serializedObject.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.name == "m_Script")
                    {
                        var scriptField = new PropertyField(iterator.Copy());
                        scriptField.SetEnabled(false);
                        root.Add(scriptField);
                        continue;
                    }

                    var field = new PropertyField(iterator.Copy());
                    // Desabilita o campo _settings para reforar que  gerenciado automaticamente
                    if (iterator.name == "_settings")
                    {
                        field.SetEnabled(false);
                    }
                    root.Add(field);
                }
                while (iterator.NextVisible(false));
            }

            // 2. Renderiza o contedo do ScriptableObject (Settings)
            var settingsProp = serializedObject.FindProperty("_settings");
            if (settingsProp.objectReferenceValue != null)
            {
                var settingsObj = new SerializedObject(settingsProp.objectReferenceValue);
                
                var settingsContainer = new VisualElement();
                settingsContainer.style.marginTop = 15;
                settingsContainer.style.paddingTop = 10;
                settingsContainer.style.borderTopWidth = 1;
                settingsContainer.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

                var title = new Label("Global Input Settings");
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.marginBottom = 5;
                settingsContainer.Add(title);

                // Itera sobre as propriedades do ScriptableObject
                var settingsIter = settingsObj.GetIterator();
                if (settingsIter.NextVisible(true))
                {
                    do
                    {
                        if (settingsIter.name == "m_Script") continue;
                        
                        var field = new PropertyField(settingsIter.Copy());
                        field.Bind(settingsObj); // Bind direto ao SO
                        settingsContainer.Add(field);
                    }
                    while (settingsIter.NextVisible(false));
                }

                root.Add(settingsContainer);
            }

            return root;
        }
    }
}