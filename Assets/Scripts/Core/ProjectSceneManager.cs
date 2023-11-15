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

    void Awake()
    {
        if (Instance != null)
        {
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
        if (!IsServer)
        {
            Debug.LogError("SceneManager Events registered on client, this should not be called.");
            return;
        }

        Debug.Log("SetupSceneManagementAndLoadNextTrack - Registering for Scene events on SERVER");
        NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = VerifySceneBeforeLoading;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleLoadEventCompletedForAllPlayers;
        NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleLoadCompleteForIndividualPlayer;

        LoadNextTrack();
    }

    public void LoadNextTrack()
    {
        var nextTrackName = GetNextTrack();
        UIConsoleManager.AddLog($"SetupSceneManagement Loading Track {nextTrackName}");

        StartCoroutine(LoadTrackAsync(nextTrackName));
    }

    void LogSceneEvent(SceneEvent sceneevent)
    {
        Debug.Log("SceneEvent " + sceneevent.SceneEventType + " " + sceneevent.SceneName + " for player " +
                  sceneevent.ClientId);
        Debug.Log(sceneevent.ClientsThatCompleted?.Count + " clients completed" +
                  sceneevent.ClientsThatTimedOut?.Count + " clients timed out");
        if (sceneevent.ClientsThatTimedOut != null)
            foreach (var client in sceneevent.ClientsThatTimedOut)
            {
                Debug.Log(client + " timed out");
            }
    }

    static bool VerifySceneBeforeLoading(int sceneindex, string scenename, LoadSceneMode loadscenemode)
    {
        Debug.Log("Doing Verification for " + scenename + " (filtering out UserInterface, everything else passes verification");

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

    void HandleLoadCompleteForIndividualPlayer(ulong clientid, string scenename, LoadSceneMode loadscenemode)
    {
        Debug.LogError($"4. HandleLoadCompleteAndAddPlayerCar for {clientid} in {scenename} on SERVER ONLY");

        if (scenename.StartsWith("Track") == false)
            return;

        var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientid);
        var player = playerNetworkObject.GetComponent<NetworkPlayer>();
        string playerName = player.PlayerName.Value.Value;

        var allCars = FindObjectsByType<CarClientMovementController>(FindObjectsSortMode.None);
        var existingCar = allCars.FirstOrDefault(t => t.OwnerName.Value.Value.Equals(playerName));
        if (existingCar != null)
        {
            Debug.LogError($"4.5 Retaking Ownership of Car {existingCar} for {clientid} in {scenename} on SERVER ONLY");
            existingCar.GetComponent<NetworkObject>().ChangeOwnership(player.OwnerClientId);
        }
        else
            player.SpawnCarOnServer();
    }

    void HandleLoadEventCompletedForAllPlayers(string sceneName, LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        Debug.Log(
            $"HandleLoadEventCompletedForAllPlayers {sceneName} {loadSceneMode} Completed:{clientsCompleted.Count} TimedOut:{clientsTimedOut.Count}");
        IsLoading = false;
        // NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompletedForAllPlayers;
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