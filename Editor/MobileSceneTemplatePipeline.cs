#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MobileTemplatePipeline : ISceneTemplatePipeline
{
    private string m_userChosenPath;
    private string m_lastScene;

    public virtual bool IsValidTemplateForInstantiation(SceneTemplateAsset sceneTemplateAsset)
    {
        return true;
        
    }

    public virtual void BeforeTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, bool isAdditive, string sceneName)
    {
        m_lastScene = EditorSceneManager.GetActiveScene().path;
        string assetName = sceneTemplateAsset.name;
        string defaultName = assetName.EndsWith("Template") ? assetName[..^"Template".Length] : assetName;

        string initialDirectory = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(initialDirectory)) initialDirectory = "Assets";


        m_userChosenPath = EditorUtility.SaveFilePanelInProject(
            $"Create new {sceneTemplateAsset.templateName} template scene.",
            defaultName,
            "unity",
            "Escolha o local e nome da nova cena",
            initialDirectory
        );

    }

    public virtual void AfterTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, Scene scene, bool isAdditive, string sceneName)
    {
        if (string.IsNullOrEmpty(m_userChosenPath))
        {
            if (m_lastScene != null)
                EditorSceneManager.OpenScene(m_lastScene);
            return;
        }

        // salva cena no caminho escolhido
        EditorSceneManager.SaveScene(scene, m_userChosenPath);

        string templateName = sceneTemplateAsset.name;
        bool addToBuilderList = EditorUtility.DisplayDialog(
    templateName,
    "Do you want add the new scene in Scenes in Build List?",
    "Yes",
    "No"
);


        if (addToBuilderList) AddSceneToBuildList(m_userChosenPath, templateName);
    }
    public static void AddSceneToBuildList(string scenePath, string templateName)
    {
        var buildScenes = EditorBuildSettings.scenes;
        var newScene = new EditorBuildSettingsScene(scenePath, true);

        int insertIndex = buildScenes.Length; // padrão: adicionar no final

        switch (templateName)
        {
            case "MobilePlatformSceneTemplate":
                insertIndex = 0;
                break;
            case "MobileStartSceneTemplate":
                insertIndex = 1;
                break;
        }

        // garante que não ultrapasse o tamanho do array
        if (insertIndex > buildScenes.Length)
            insertIndex = buildScenes.Length;

        var newBuildScenes = new EditorBuildSettingsScene[buildScenes.Length + 1];

        // copia cenas antes do índice de inserção
        for (int i = 0; i < insertIndex; i++)
            newBuildScenes[i] = buildScenes[i];

        // insere a nova cena
        newBuildScenes[insertIndex] = newScene;

        // copia o restante das cenas
        for (int i = insertIndex; i < buildScenes.Length; i++)
            newBuildScenes[i + 1] = buildScenes[i];

        EditorBuildSettings.scenes = newBuildScenes;

        Debug.Log($"Cena {scenePath} adicionada na Build List na posição {insertIndex}");
    }



}
#endif