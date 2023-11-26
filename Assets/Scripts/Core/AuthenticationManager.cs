using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    const float MESSAGE_LIMIT_RATE = 1.2f;
    
    PlayerInfo _playerInfo;
    public static AuthenticationManager Instance { get; private set; }

    public bool IsAuthenticated => AuthenticationService.Instance.IsSignedIn;

    public async Task<string> GetPlayerNameAsync()
    {
        if (!string.IsNullOrWhiteSpace(LocalPlayerName))
            return LocalPlayerName;
        LocalPlayerName = await AuthenticationService.Instance.GetPlayerNameAsync();
        return LocalPlayerName;
    }

    public static string LocalPlayerName { get; private set; }

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
        PlayerAccountService.Instance.SignedIn += PlayerAccountAnonLoginComplete;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            AuthenticationService.Instance.SignedIn -= HandleSignedInAnon;
            AuthenticationService.Instance.SignedOut -= HandleSignedOut;
            AuthenticationService.Instance.SignInFailed -= HandleSignInFailed;
            PlayerAccountService.Instance.SignedIn -= PlayerAccountAnonLoginComplete;

            Instance = null;
        }
    }

    void HandleSignedInAnon()
    {
        Debug.Log("Signed In");
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

    public async Awaitable SignInAnonAsync(string playerName)
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            await Awaitable.WaitForSecondsAsync(MESSAGE_LIMIT_RATE);
        }

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
            AuthenticationService.Instance.SignedIn += HandlePlayerSignedIn;
        }
    }

    async void HandlePlayerSignedIn()
    {
        AuthenticationService.Instance.SignedIn -= HandlePlayerSignedIn;

        var currentName = await GetPlayerNameAsync();
        if (String.IsNullOrWhiteSpace(currentName))
        {
            Debug.Log($"Set Playername to {LocalPlayerName}");
            await AuthenticationService.Instance.UpdatePlayerNameAsync(LocalPlayerName);
        }
        else
        {
            Debug.LogError($"Playername already set to {currentName}");
        }
    }

    public async void StartSignInUnityAsync()
    {
        if (PlayerAccountService.Instance.IsSignedIn)
        {
            await SignInUnityAsync();
        }
        else
        {
            try
            {
                await PlayerAccountService.Instance.StartSignInAsync();
            }
            catch (RequestFailedException ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    async void PlayerAccountAnonLoginComplete()
    {
        await SignInUnityAsync();
    }

    async Task SignInUnityAsync()
    {
        Debug.Log("Signing In With Unity");
        var token = PlayerAccountService.Instance.AccessToken;

        await AuthenticationService.Instance.SignInWithUnityAsync(token, new SignInOptions() { CreateAccount = true });
    }

    async void UpgradeAccountFromAnonToUnityAsync()
    {
        var token = AuthenticationService.Instance.AccessToken;
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

    public async Task SignInAnonFromUIAsync()
    {
        await SignInAnonAsync( LocalPlayerName);
    }
}