using UnityEngine.SceneManagement;

/// <summary>
/// The app routes scene loads through SceneRouter (Build Settings or Addressables). The match server loads from Build
/// Settings only; this keeps code shared with the app (CasinoTableScreen, ServerConnection.PlayerApiRequest) compiling.
/// Never runs on a dedicated server in practice.
/// </summary>
public static class SceneRouter
{
    public static void LoadScene(string scene) => SceneManager.LoadScene(scene);
    public static void LoadScene(int buildIndex) => SceneManager.LoadScene(buildIndex);
    public static void LoadSceneAsync(int buildIndex) => SceneManager.LoadSceneAsync(buildIndex);
    public static void LoadSceneAsync(string scene) => SceneManager.LoadSceneAsync(scene);
}
