using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    const float MESSAGE_LIMIT_RATE = 1.2f;

    PlayerInfo _playerInfo;
    public static AuthenticationManager Instance { get; private set; }

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

    public string GetPlayerName()
    {
        return AuthenticationService.Instance.PlayerName;
    }

    public event Action OnSignedIn;
    public event Action<RequestFailedException> OnSigninFailed;

    public async Awaitable<SigninResult> SignInAnonAsync(string playerName)
    {
        try
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
                SignInOptions options = new SignInOptions() {CreateAccount = true};
                await AuthenticationService.Instance.SignInAnonymouslyAsync(options);
            }

            return SigninResult.Successful;
        }
        catch (Exception ex)
        {
            return new SigninResult(false, ex.Message);
        }
    }

    public async Task<SigninResult> StartSignInUnityAsync()
    {
        try
        {
            if (PlayerAccountService.Instance.IsSignedIn)
            {
                await FinishSignInUnityAsync();
            }
            else
            {
                PlayerAccountService.Instance.SignedIn += PlayerAccountServiceSignedIn;
                await PlayerAccountService.Instance.StartSignInAsync();
            }
        }
        catch (Exception e)
        {
            return new SigninResult(false, e.Message);
        }

        return SigninResult.Successful;
    }

    async void PlayerAccountServiceSignedIn()
    {
        PlayerAccountService.Instance.SignedIn -= PlayerAccountServiceSignedIn;
        await FinishSignInUnityAsync();
    }

    static async Task FinishSignInUnityAsync()
    {
        var token = PlayerAccountService.Instance.AccessToken;
        Debug.Log("Signing In With Unity + " + token);
        await AuthenticationService.Instance.SignInWithUnityAsync(token,
            new SignInOptions() {CreateAccount = true});

        var info = await AuthenticationService.Instance.GetPlayerInfoAsync();
        Debug.Log(info);
    }

    public string GetPlayerId() => AuthenticationService.Instance.PlayerId;

    public async Task<SigninResult> SignInWithUserNameAndPassword(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            await AuthenticationService.Instance.UpdatePlayerNameAsync(username);

            var currentData = await CloudSaveService.Instance.Data.Player.LoadAllAsync();
            Dictionary<string, object> data = new();
            data["logins"] = 1;
            if (currentData.TryGetValue("logins", out var logins))
            {
                data["logins"] = logins.Value.GetAs<int>() + 1;
            }

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            
            return new SigninResult(true, String.Empty);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return new SigninResult(false, ex.Message);
        }
    }

    public async Task<SigninResult> RegisterWithUserNameAndPassword(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            return new SigninResult(true, String.Empty);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return new SigninResult(false, ex.Message);
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

    // async void HandlePlayerSignedIn()
    // {
    //     AuthenticationService.Instance.SignedIn -= HandlePlayerSignedIn;
    //
    //     var currentName = await GetPlayerNameAsync();
    //     if (String.IsNullOrWhiteSpace(currentName))
    //     {
    //         Debug.Log($"Set Playername to {LocalPlayerName}");
    //         await AuthenticationService.Instance.UpdatePlayerNameAsync(LocalPlayerName);
    //     }
    //     else
    //     {
    //         Debug.LogError($"Playername already set to {currentName}");
    //     }
    // }

    // async void PlayerAccountAnonLoginComplete()
    // {
    //     await SignInUnityAsync();
    // }
}