using System;
using System.Collections;
using System.Linq;
using Unity.Multiplayer.Playmode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.PlayerAccounts;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MPPMManager : MonoBehaviour
{
    #if UNITY_EDITOR
    int _randomId;
    string profileName;

    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        _randomId = UnityEngine.Random.Range(1000, 9999);

        Debug.Log("MPPM");
        yield return new WaitForSeconds(1f);
        Debug.Log("MPPM2");

        yield return InitializeMPPM();
    }

    const string HOST = "Host";
    const string CLIENT = "Client";
    const string SERVER = "Server";
    const string LOBBY_HOST = "Lobby Host";
    const string LOBBY_CLIENT = "Lobby Client";

    IEnumerator InitializeMPPM()
    {
        Debug.Log("InitializeMPPM");
        profileName = CurrentPlayer.ReadOnlyTags().Except(new[]
            {
                LOBBY_HOST, LOBBY_CLIENT, HOST, CLIENT, SERVER
            })
            .FirstOrDefault();

        Debug.Log("ProfileName " + profileName);
        if (profileName != default)
        {
            AuthenticationService.Instance.SwitchProfile(profileName);
            Debug.Log($"Starting as {profileName}");
        }

        if (CurrentPlayer.ReadOnlyTags().Contains(LOBBY_HOST))
            yield return LobbyHost();
        if (CurrentPlayer.ReadOnlyTags().Contains(LOBBY_CLIENT))
            yield return LobbyClient();
        if (CurrentPlayer.ReadOnlyTags().Contains(HOST))
            yield return Host();
        if (CurrentPlayer.ReadOnlyTags().Contains(CLIENT))
            yield return Client();
        if (CurrentPlayer.ReadOnlyTags().Contains(SERVER))
            Server();
    }

    static void Server()
    {
        NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7777);
        NetworkManager.Singleton.StartServer();
    }

    IEnumerator Client()
    {
        LobbyManager.Instance.gameObject.SetActive(false);
        Debug.Log("Signing in");
        yield return AuthenticationManager.Instance.SignInAnonAsync("Client" + profileName);

        Debug.Log("Start Client");

        NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7777);
        PlayerConnectionsManager.Instance.StartClient();
        NetworkManager.Singleton.OnClientDisconnectCallback += args =>
        {
            Debug.LogError($"Client Disconnected {args}");
            Debug.LogError(NetworkManager.Singleton.DisconnectReason);
        };
    }

    IEnumerator Host()
    {
        LobbyManager.Instance.gameObject.SetActive(false);
        Debug.Log("Starting Host");
        yield return AuthenticationManager.Instance.SignInAnonAsync("Host" + profileName);

        NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7777);
        yield return PlayerConnectionsManager.Instance.StartHostOnServer();

        yield return ReloadExistingTrackOrLoadDefault();
    }

    static bool ReloadExistingTrackOrLoadDefault()
    {
        for (int i = 0; i < SceneManager.loadedSceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name.StartsWith("Track "))
            {
                Debug.Log($"Loading {scene.name}");
                UIConsoleManager.AddLog($"MPPM ReloadExistingTrackOrLoadDefault Loading Trac");

                ProjectSceneManager.Instance.LoadTrack(scene.name);
                return true;
            }
        }

        Debug.Log("Loading Default Track");
        ProjectSceneManager.Instance.SetupSceneManagementAndLoadNextTrack();
        return false;
    }

    IEnumerator LobbyClient()
    {
        string prefix = "LobbyClient";
        yield return AuthenticateRandomUser(prefix);

        while (PlayerPrefs.HasKey("LobbyId") == false)
        {
            // Debug.Log("Waiting for lobbyId");
            yield return null;
        }

        string lobbyId = PlayerPrefs.GetString("LobbyId");
        Debug.Log($"Joining Lobby {lobbyId}");
        LobbyManager.Instance.JoinLobbyById(lobbyId);
    }

    async Awaitable AuthenticateRandomUser(string prefix)
    {
        string randomProfileName = profileName + prefix + _randomId;

        try
        {
            AuthenticationService.Instance.SwitchProfile(randomProfileName);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }

        Debug.Log("Signing in");

        SignInOptions options = new SignInOptions() { CreateAccount = true };
        await AuthenticationService.Instance.SignInAnonymouslyAsync(options);
        await AuthenticationService.Instance.UpdatePlayerNameAsync(randomProfileName);
        UIConsoleManager.AddLog($"Updating Playername to {randomProfileName}");
        Debug.Log($"Set Name to {randomProfileName}");
    }

    IEnumerator LobbyHost()
    {
        yield return AuthenticationManager.Instance.SignInAnonAsync();
        yield return new WaitUntil(() => AuthenticationManager.Instance.IsAuthenticated);

        var lobbyTask = LobbyManager.Instance.HostLobby();
        yield return new WaitUntil(() => lobbyTask.IsCompleted);

        try
        {
            var lobby = lobbyTask.Result;
            PlayerPrefs.SetString("LobbyId", lobby.Id);
            Debug.Log($"Set LobbyId to {lobby.Id}");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
    }

    void OnDestroy()
    {
        PlayerPrefs.DeleteKey("LobbyId");
    }
    #endif
}

public enum GameState
{
    WaitingForPlayers,
    CountDown,
    Racing,
    GameOver
}