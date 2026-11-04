using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class PasskeyAuthBridge : MonoBehaviour
{
    public static PasskeyAuthBridge Instance { get; private set; }

    public static event Action AuthenticationSucceeded;
    public static event Action<string> AuthenticationFailed;

    [DllImport("__Internal")]
    private static extern void GproRequestPasskeyAuth();

    // シーンを編集せず、自動で連携オブジェクトを作る
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null) return;

        GameObject obj = new GameObject("PasskeyAuthBridge");
        obj.AddComponent<PasskeyAuthBridge>();
        DontDestroyOnLoad(obj);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 将来、本物のドアから呼び出すメソッド
    public static void RequestAuthentication()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        GproRequestPasskeyAuth();
#else
        Debug.Log("パスキー認証はWebGLブラウザで実行します");
#endif
    }

    // JavaScriptから認証成功時に呼び出される
    public void OnPasskeySuccess()
    {
        Debug.Log("パスキー認証成功！");
        AuthenticationSucceeded?.Invoke();
    }

    // JavaScriptから認証失敗時に呼び出される
    public void OnPasskeyFailure(string message)
    {
        Debug.LogWarning("パスキー認証失敗: " + message);
        AuthenticationFailed?.Invoke(message);
    }
}
