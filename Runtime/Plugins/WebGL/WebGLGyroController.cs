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

    [DllImport("__Internal")]
    private static extern int GetGyroHasData();

    [DllImport("__Internal")]
    private static extern int GetGyroPermissionState();

    private static bool _requestedPermission;

    // 0 = not requested/unknown, 1 = granted, 2 = denied, 3 = unsupported, 4 = error
    public static int PermissionState
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!_requestedPermission) return 0;
            return GetGyroPermissionState();
#else
            return 0;
#endif
        }
    }

    // Initialized means the browser granted access and at least one sensor event arrived.
    public static bool IsInitialized
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!_requestedPermission) return false;
            return PermissionState == 1 && GetGyroHasData() == 1;
#else
            return false;
#endif
        }
    }

    // O seu CallbackHub vai chamar esse método aqui
    public static void RequestGyroPermission()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        _requestedPermission = true;
        InitGyroscope();
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
