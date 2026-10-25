using UnityEngine;
using UnityEngine.SceneManagement;

public class PasskeyGameFlow : MonoBehaviour
{
    // 次に移動するシーン名（後で設定）
    private const string GameSceneName = "SampleScene";

    public static bool IsAuthenticated { get; private set; }

    private static PasskeyGameFlow instance;
    private bool isTransitioning = false;

    // シーンを編集せず、自動でオブジェクトを作成
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;

        GameObject obj = new GameObject("PasskeyGameFlow");
        DontDestroyOnLoad(obj);
        obj.AddComponent<PasskeyGameFlow>();
    }

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        PasskeyAuthBridge.AuthenticationSucceeded
            += OnAuthenticationSucceeded;
    }

    private void OnDisable()
    {
        PasskeyAuthBridge.AuthenticationSucceeded
            -= OnAuthenticationSucceeded;
    }

    private void OnAuthenticationSucceeded()
    {
        IsAuthenticated = true;

        Debug.Log("パスキー認証完了！ゲーム進行を開始します");

        if (isTransitioning) return;

        if (string.IsNullOrEmpty(GameSceneName))
        {
            Debug.LogWarning(
                "移動先のシーン名がまだ設定されていません"
            );
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(GameSceneName))
        {
            Debug.LogError(
                "シーンがBuild Profilesに登録されていません: "
                + GameSceneName
            );
            return;
        }

        isTransitioning = true;

        Debug.Log("ゲーム本編へ移動: " + GameSceneName);
        SceneManager.LoadScene(GameSceneName);
    }
}
