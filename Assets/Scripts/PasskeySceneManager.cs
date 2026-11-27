using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PasskeySceneManager : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Select Scene";
    private bool verified;
    private bool transitioning;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void GproShowPasskeyDoor();
#endif

    private void OnEnable()
    {
        PasskeyAuthBridge.AuthenticationSucceeded += OnSuccess;
        PasskeyAuthBridge.AuthenticationFailed += OnFailure;
    }

    private void OnDisable()
    {
        PasskeyAuthBridge.AuthenticationSucceeded -= OnSuccess;
        PasskeyAuthBridge.AuthenticationFailed -= OnFailure;
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        GproShowPasskeyDoor();
#else
        Debug.Log("パスキー扉はWebGLでのみ動作します");
#endif
    }

    private void OnSuccess()
    {
        verified = true;
        GoToNextScene();
    }

    private void OnFailure(string reason)
    {
        Debug.LogWarning("パスキー扉: " + reason);
    }

    public void GoToNextScene()
    {
        if (!verified || transitioning)
        {
            Debug.LogWarning("認証が成功するまで先へは進めません");
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError("シーンがビルドにありません: " + nextSceneName);
            return;
        }
        transitioning = true;
        SceneManager.LoadScene(nextSceneName);
    }

    public void RequestRealDoorAuthentication()
    {
        PasskeyAuthBridge.RequestAuthentication();
    }
}
