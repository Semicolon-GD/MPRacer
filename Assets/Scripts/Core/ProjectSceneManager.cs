using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProjectSceneManager : NetworkBehaviour
{
    public static ProjectSceneManager Instance { get; private set; }

    bool _unloading;
    string _track;

#if UNITY_EDITOR
    [SerializeField] List<SceneAsset> Tracks;

    void OnValidate()
    {
        TrackNames = Tracks.Select(t => t.name).ToList();
    }
#endif
    [SerializeField] List<string> TrackNames;


    public bool IsLoading { get; private set; }
    public Scene CurrentTrackScene => GetCurrentTrack().Item1;
    public bool IsAnyTrackLoaded() => GetCurrentTrack() != default;

    public static event Action<ulong> OnPlayerJoinedTrack;


    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Duplicate ProjectSceneManager, destroying new one");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        NetworkManager.Singleton.OnServerStarted += SetupSceneManagementAndLoadNextTrack;
        ShortcutManager.Add("List Scenes", ListScenes);
    }

    void ListScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            UIConsoleManager.AddLog(SceneManager.GetSceneAt(i).name);
        }
    }

    [ContextMenu(nameof(SetupSceneManagementAndLoadNextTrack))]
    public void SetupSceneManagementAndLoadNextTrack()
    {
        NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = VerifySceneBeforeLoading;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoaded;
        //NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += HandlePlayerJoined;
        NetworkManager.Singleton.SceneManager.OnLoadComplete += HandlePlayerJoinedAtStart;

        var nextTrackName = GetNextTrack();
        UIConsoleManager.AddLog($"SetupSceneManagement Loading Trac");

        StartCoroutine(LoadTrackAsync(nextTrackName));
    }

    static bool VerifySceneBeforeLoading(int sceneindex, string scenename, LoadSceneMode loadscenemode)
    {
        if (scenename == "UserInterface")
            return false;
        return true;
    }

    public void LoadTrack(string trackName) => StartCoroutine(LoadTrackAsync(trackName));

    IEnumerator LoadTrackAsync(string trackName = default)
    {
        IsLoading = true;
        var currentTrack = GetCurrentTrack();

        if (currentTrack != default)
            yield return UnloadScene(currentTrack.Item1);

        if (trackName == default && LobbyManager.Instance &&
            LobbyManager.Instance.CurrentLobby?.Data.TryGetValue("track", out var trackData) == true)
            trackName = trackData.Value;

        NetworkManager.Singleton.SceneManager.LoadScene(trackName, LoadSceneMode.Additive);
        UIConsoleManager.AddLog($"Loading Track {trackName}");
    }

    void HandlePlayerJoinedAtStart(ulong clientid, string scenename, LoadSceneMode loadscenemode)
    {
        if (scenename.StartsWith("Track") == false)
            return;

        var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientid);
        var player = playerNetworkObject.GetComponent<NetworkPlayer>();
        player.SpawnCar();
    }

    void HandlePlayerJoined(ulong clientid)
    {
        Debug.Log($"HandlePlayerJoinedLate {clientid}");
        var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientid);
        var player = playerNetworkObject.GetComponent<NetworkPlayer>();
        player.SpawnCar();
    }

    void HandleSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        Debug.Log($"Loading Scene {sceneName}");
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleSceneLoaded;
        IsLoading = false;
        ListScenes();
    }

    Tuple<Scene, string> GetCurrentTrack()
    {
        for (int i = 0; i < SceneManager.loadedSceneCount; i++)
        {
            Scene currentTrack = SceneManager.GetSceneAt(i);
            _track = TrackNames.FirstOrDefault(t => t == currentTrack.name);
            if (_track != null)
                return new Tuple<Scene, string>(currentTrack, _track);
        }

        return default;
    }

    string GetNextTrack()
    {
        var current = GetCurrentTrack();
        if (current == null || current.Item2 == null)
        {
            return TrackNames[0];
        }

        int index = TrackNames.IndexOf(current.Item2) + 1;
        if (index < TrackNames.Count)
            return TrackNames[index];


        return TrackNames[0];
    }

    IEnumerator UnloadScene(Scene scene)
    {
        // Assure only the server calls this when the NetworkObject is
        // spawned and the scene is loaded.
        if (!IsHost || !IsSpawned || !scene.IsValid() || !scene.isLoaded)
        {
            yield break;
        }

        _unloading = true;

        // Unload the scene
        NetworkManager.SceneManager.OnUnloadComplete += UnloadComplete;
        NetworkManager.SceneManager.UnloadScene(scene);
        while (_unloading)
            yield return null;
    }

    void UnloadComplete(ulong clientid, string scenename)
    {
        NetworkManager.SceneManager.OnUnloadComplete -= UnloadComplete;
        _unloading = false;
    }

    public IEnumerator LoadMenu()
    {
        var currentTrack = GetCurrentTrack();
        yield return UnloadScene(currentTrack.Item1);
    }
}