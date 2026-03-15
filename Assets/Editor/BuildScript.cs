#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildScript
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Lobby/LobbyScene.unity",
        "Assets/Scenes/Transitions/GameBootstrapScene.unity",
        "Assets/Scenes/Transitions/NetworkDespawnerScene.unity",
        "Assets/Scenes/Testbeds/TB_ArenaDemo.unity",
    };

    #region Editor menu items

    [MenuItem("Build/Windows/DevLocal")]
    public static void BuildDevLocalWindows() => Build(LoadConfig("DevLocal"), BuildTarget.StandaloneWindows64);

    [MenuItem("Build/Windows/Dev")]
    public static void BuildDevWindows() => Build(LoadConfig("Dev"), BuildTarget.StandaloneWindows64);

    [MenuItem("Build/Windows/Production")]
    public static void BuildProductionWindows() => Build(LoadConfig("Production"), BuildTarget.StandaloneWindows64);

    [MenuItem("Build/Mac/DevLocal")]
    public static void BuildDevLocalMac() => Build(LoadConfig("DevLocal"), BuildTarget.StandaloneOSX);

    [MenuItem("Build/Mac/Dev")]
    public static void BuildDevMac() => Build(LoadConfig("Dev"), BuildTarget.StandaloneOSX);

    [MenuItem("Build/Mac/Production")]
    public static void BuildProductionMac() => Build(LoadConfig("Production"), BuildTarget.StandaloneOSX);

    #endregion


    #region CLI entry point
    /// <summary>
    /// Invoked via: /path/to/Unity -executeMethod BuildScript.BuildCLI -buildConfig AppConfig_Staging -buildTarget Windows64
    /// Supported -buildTarget values: Windows64, OSX
    /// </summary>
    public static void BuildCLI()
    {
        string configName = ReadArg("-buildConfig")
            ?? throw new System.Exception("Missing -buildConfig argument. Usage: -buildConfig <AssetName>");

        string targetName = ReadArg("-buildTarget") ?? "Windows64";
        BuildTarget target = targetName switch
        {
            "Windows64" => BuildTarget.StandaloneWindows64,
            "OSX" => BuildTarget.StandaloneOSX,
            _ => throw new System.Exception($"Unknown -buildTarget '{targetName}'. Supported: Windows64, OSX"),
        };

        Build(LoadConfig(configName), target);
    }
    #endregion

    #region Internal
    static AppConfig LoadConfig(string assetName)
    {
        string path = $"Assets/Resources/Build/{assetName}.asset";
        var config = AssetDatabase.LoadAssetAtPath<AppConfig>(path);
        if (config == null)
        {
            throw new System.Exception($"Could not load AppConfig at '{path}'. Check the asset name.");
        }
        return config;
    }

    static void Build(AppConfig config, BuildTarget target)
    {
        InjectConfig(config);

        bool isDev = !config.enableSteamLobby && !config.useProductionRelay;
        string ext = target == BuildTarget.StandaloneWindows64 ? ".exe" : ".app";
        string targetFolder = target == BuildTarget.StandaloneWindows64 ? "Windows" : "Mac";

        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = $"Builds/{config.name}/{targetFolder}/Resonance{ext}",
            target = target,
            options = isDev ? BuildOptions.Development : BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new System.Exception($"Build failed for config '{config.name}' target '{target}'");
        }
    }

    static void InjectConfig(AppConfig config)
    {
        const string lobbyScenePath = "Assets/Scenes/Lobby/LobbyScene.unity";

        bool wasAlreadyLoaded = false;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path == lobbyScenePath)
            {
                wasAlreadyLoaded = true;
                break;
            }
        }

        var scene = wasAlreadyLoaded
            ? EditorSceneManager.GetSceneByPath(lobbyScenePath)
            : EditorSceneManager.OpenScene(lobbyScenePath, OpenSceneMode.Additive);

        var configurator = Object.FindFirstObjectByType<SceneConfigurator>();
        if (configurator == null)
        {
            Debug.LogWarning($"[BuildScript] SceneConfigurator not found in {lobbyScenePath}. Config not injected.");
            if (!wasAlreadyLoaded)
                EditorSceneManager.CloseScene(scene, true);
            return;
        }

        var so = new SerializedObject(configurator);
        so.FindProperty("config").objectReferenceValue = config;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);

        if (!wasAlreadyLoaded)
            EditorSceneManager.CloseScene(scene, true);
    }

    static string ReadArg(string flag)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == flag)
            {
                return args[i + 1];
            }
        }
        return null;
    }
    #endregion
}
#endif
