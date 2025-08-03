using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [System.Serializable]
    public enum SceneLoadState
    {
        Idle,
        Loading,
        Unloading
    }

    [System.Serializable]
    public class SceneData
    {
        public string sceneName;
        public bool isLoaded;
        public bool isActive;

        public SceneData(string name)
        {
            sceneName = name;
            isLoaded = false;
            isActive = false;
        }
    }

    [Header("Scene Management")]
    public SceneLoadState currentState = SceneLoadState.Idle;
    public string currentScene = "";

    [Header("Default Scenes")]
    [SerializeField] private string playerScene = "PlayerScene";
    [SerializeField] private string startScene = "StartScene";



    public float progressValue { get; private set; }

    // Scene tracking
    private List<SceneData> managedScenes = new List<SceneData>();


    // Events
    public static event Action<string> OnSceneLoaded;
    public static event Action<string> OnSceneUnloaded;
    public static event Action<string, float> OnLoadingProgress;
   
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

    private void Start()
    {
        StartCoroutine(InitializeScenes());
    }

    private IEnumerator InitializeScenes()
    {
        yield return RefreshActiveScenes();
        yield return LoadSceneWithSkybox(playerScene);
        yield return LoadSceneWithSkybox(startScene);

    }

    #region Public Scene Management Methods

    /// <summary>
    /// Load a scene additively with optional skybox
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Unload a scene
    /// </summary>
    public void UnloadScene(string sceneName)
    {
        StartCoroutine(UnloadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Hide/Show scene by deactivating/activating all root GameObjects
    /// </summary>
    public void SetSceneActive(string sceneName, bool active)
    {
        StartCoroutine(SetSceneActiveCoroutine(sceneName, active));
    }

    /// <summary>
    /// Check if a scene is currently loaded
    /// </summary>
    public bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }



    #endregion

    #region Enhanced Scene Management

    /// <summary>
    /// Load multiple scenes at once
    /// </summary>
    public void LoadScenes(string[] sceneNames)
    {
        StartCoroutine(LoadScenesCoroutine(sceneNames));
    }

    /// <summary>
    /// Unload multiple scenes at once
    /// </summary>
    public void UnloadScenes(string[] sceneNames)
    {
        StartCoroutine(UnloadScenesCoroutine(sceneNames));
    }

    /// <summary>
    /// Hide all scenes except specified ones
    /// </summary>
    public void HideAllScenesExcept(string[] exceptScenes)
    {
        StartCoroutine(HideAllScenesExceptCoroutine(exceptScenes));
    }



    #endregion

    #region Core Scene Loading Logic

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        yield return WaitForIdleState();

        if (IsSceneLoaded(sceneName))
        {
            Debug.Log($"Scene '{sceneName}' is already loaded.");
            currentScene = sceneName;
            yield break;
        }

        yield return LoadSceneWithSkybox(sceneName);
    }

    private IEnumerator LoadSceneWithSkybox(string sceneName)
    {
        currentState = SceneLoadState.Loading;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!operation.isDone)
        {
            yield return null;
        }

        UpdateSceneData(sceneName, true, true);
        currentScene = sceneName;
        currentState = SceneLoadState.Idle;

        OnSceneLoaded?.Invoke(sceneName);
        Debug.Log($"Scene '{sceneName}' loaded successfully.");
    }

    private IEnumerator UnloadSceneCoroutine(string sceneName)
    {
        yield return WaitForIdleState();

        if (!IsSceneLoaded(sceneName))
        {
            Debug.Log($"Scene '{sceneName}' is not loaded.");
            yield break;
        }

        currentState = SceneLoadState.Unloading;

        AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            yield return null;
        }

        RemoveSceneData(sceneName);

        if (currentScene == sceneName)
        {
            currentScene = "";
        }

        currentState = SceneLoadState.Idle;

        OnSceneUnloaded?.Invoke(sceneName);
        Debug.Log($"Scene '{sceneName}' unloaded successfully.");
    }

 

    #endregion

    #region Enhanced Coroutines

    private IEnumerator LoadScenesCoroutine(string[] sceneNames)
    {
        foreach (string sceneName in sceneNames)
        {
            if (!IsSceneLoaded(sceneName))
            {
                yield return LoadSceneCoroutine(sceneName);
            }
        }
    }

    private IEnumerator UnloadScenesCoroutine(string[] sceneNames)
    {
        foreach (string sceneName in sceneNames)
        {
            if (IsSceneLoaded(sceneName))
            {
                yield return UnloadSceneCoroutine(sceneName);
            }
        }
    }

 

    private IEnumerator HideAllScenesExceptCoroutine(string[] exceptScenes)
    {
        foreach (SceneData sceneData in managedScenes)
        {
            bool shouldKeepVisible = false;

            if (exceptScenes != null)
            {
                foreach (string exceptScene in exceptScenes)
                {
                    if (sceneData.sceneName == exceptScene)
                    {
                        shouldKeepVisible = true;
                        break;
                    }
                }
            }

            if (!shouldKeepVisible)
            {
                yield return SetSceneActiveCoroutine(sceneData.sceneName, false);
            }
        }
    }

    #endregion

 

    #region Scene Visibility Management

    private IEnumerator SetSceneActiveCoroutine(string sceneName, bool active)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning($"Scene '{sceneName}' is not loaded or invalid.");
            yield break;
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            obj.SetActive(active);
        }

        UpdateSceneData(sceneName, true, active);
        Debug.Log($"Scene '{sceneName}' {(active ? "activated" : "deactivated")}.");
    }

    public void HideScene(string sceneName)
    {
        SetSceneActive(sceneName, false);
    }

    public void ShowScene(string sceneName)
    {
        SetSceneActive(sceneName, true);
    }

    #endregion

    #region Utility Methods

    private IEnumerator WaitForIdleState()
    {
        while (currentState != SceneLoadState.Idle)
        {
            yield return null;
        }
    }

    public IEnumerator RefreshActiveScenes()
    {
        managedScenes.Clear();

        if (SceneManager.sceneCount > 0)
        {
            // Start from index 1 to skip the main scene (like in your original code)
            for (int i = 1; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    SceneData data = new SceneData(scene.name);
                    data.isLoaded = true;
                    data.isActive = true; // Assume active if loaded
                    managedScenes.Add(data);
                }
            }
        }

        yield return null;
    }

    private void UpdateSceneData(string sceneName, bool isLoaded, bool isActive)
    {
        SceneData existingData = managedScenes.Find(s => s.sceneName == sceneName);

        if (existingData == null)
        {
            SceneData newData = new SceneData(sceneName);
            newData.isLoaded = isLoaded;
            newData.isActive = isActive;
            managedScenes.Add(newData);
        }
        else
        {
            existingData.isLoaded = isLoaded;
            existingData.isActive = isActive;
        }
    }

    private void RemoveSceneData(string sceneName)
    {
        managedScenes.RemoveAll(s => s.sceneName == sceneName);
    }

    public void ReloadCurrentScene()
    {
      if(!string.IsNullOrEmpty(currentScene))
        {
            StartCoroutine(ReloadSceneCoroutine(currentScene));
        }
        else
        {
            Debug.Log("No current scene to reload.");
        }
    }

   private IEnumerator ReloadSceneCoroutine(string sceneName)
    {
        yield return UnloadSceneCoroutine(sceneName);
        yield return LoadSceneWithSkybox(sceneName);
    }

    #endregion


}