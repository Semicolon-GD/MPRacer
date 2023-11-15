using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.PlayerAccounts;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    PlayerInfo _playerInfo;
    public static AuthenticationManager Instance { get; private set; }

    public bool IsAuthenticated => AuthenticationService.Instance.IsSignedIn;
    public Task<string> GetPlayerNameAsync() => AuthenticationService.Instance.GetPlayerNameAsync();

    public event Action OnSignedIn;
    public event Action<RequestFailedException> OnSigninFailed;

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

    async void Start()
    {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += HandleSignedInAnon;
        AuthenticationService.Instance.SignedOut += HandleSignedOut;
        AuthenticationService.Instance.SignInFailed += HandleSignInFailed;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            AuthenticationService.Instance.SignedIn -= HandleSignedInAnon;
            AuthenticationService.Instance.SignedOut -= HandleSignedOut;
            AuthenticationService.Instance.SignInFailed -= HandleSignInFailed;
            Instance = null;
        }
    }

    void HandleSignedInAnon()
    {
        Debug.Log("Signed In Anon");
        OnSignedIn?.Invoke();
    }

    void HandleSignedOut()
    {
        Debug.Log("Signed Out");
    }

    void HandleSignInFailed(RequestFailedException obj)
    {
        Debug.Log($"Sign In Failed: {obj.Message}");
        OnSigninFailed?.Invoke(obj);
    }

    public async Awaitable SignInAnonAsync(string playerName = default)
    {
        if (playerName == default && AuthenticationService.Instance.SessionTokenExists)
        {
            Debug.Log("Session Token Exists");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        else
        {
            Debug.Log("Signing In Anonymously");
            SignInOptions options = new SignInOptions() { CreateAccount = true };
            await AuthenticationService.Instance.SignInAnonymouslyAsync(options);
        }

        if (playerName != default)
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            Debug.Log($"Set Playername to {playerName}");
        }
    }

    public async Task SignInUnityAsync()
    {
        Debug.Log("Signing In With Unity");
        await PlayerAccountService.Instance.StartSignInAsync();
    }

    async void UpgradeAccountFromAnonToUnityAsync()
    {
        var token = PlayerAccountService.Instance.AccessToken;
        await AuthenticationService.Instance.LinkWithUnityAsync(token);
        Debug.Log("Link Sent");
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
    }

    public async Task<string> GetPlayerIdAsync()
    {
        _playerInfo = await AuthenticationService.Instance.GetPlayerInfoAsync();
        return _playerInfo.Id;
    }

    public FixedString32Bytes GetPlayerIdCached()
    {
        return _playerInfo.Id;
    }
}