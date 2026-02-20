using UnityEngine;
using System.Runtime.InteropServices;

// Agora é uma classe estática pura, sem herdar de MonoBehaviour
public static class WebGLGyroAPI
{
    [DllImport("__Internal")]
    private static extern void InitGyroscope();

    [DllImport("__Internal")]
    private static extern float GetGyroAlpha();

    [DllImport("__Internal")]
    private static extern float GetGyroBeta();

    [DllImport("__Internal")]
    private static extern float GetGyroGamma();

    // Propriedade pública para checar se o gyro já foi liberado
    public static bool IsInitialized { get; private set; } = false;

    // O seu CallbackHub vai chamar esse método aqui
    public static void RequestGyroPermission()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InitGyroscope();
        IsInitialized = true;
#endif
    }

    // Qualquer script do seu jogo pode chamar isso para pegar a rotação atual
    public static Quaternion GetRotation()
    {
        if (!IsInitialized)
            return Quaternion.identity; // Retorna rotação zerada se não inicializou

#if UNITY_WEBGL && !UNITY_EDITOR
        float alpha = GetGyroAlpha(); 
        float beta = GetGyroBeta();   
        float gamma = GetGyroGamma(); 

        return Quaternion.Euler(beta, -alpha, -gamma);
#else
        return Quaternion.identity;
#endif
    }
}