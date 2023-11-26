using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Login")] [SerializeField] GameObject _loginButtonsPanel;
    [SerializeField] Button _anonLoginButton;
    [SerializeField] Button _unityLoginButton;
    [SerializeField] TMP_InputField _nameInput;

    [Header("Lobby Buttons")] 
    [SerializeField] Button _hostButton;

    [SerializeField] Button _refreshLobbiesButton;

    [Header("Panels")] [SerializeField] GameObject _loginPanel;
    [SerializeField] CurrentLobbyPanel _currentPanel;
    [SerializeField] LobbyListPanel _lobbyListPanel;
    [SerializeField] GameObject _lobbiesPanel;
    [SerializeField] GameObject _carsPanel;

    void Start()
    {
        // Authentication Setup
        AuthenticationManager.Instance.OnSignedIn += HandlePlayerLoggedIn;
        AuthenticationManager.Instance.OnSigninFailed += ShowAuthenticationPanel;
        _anonLoginButton.onClick.AddListener(LoginAnon);
        _unityLoginButton.onClick.AddListener(LoginUnity);
        _loginPanel.SetActive(true);

        // Lobby Setup
        _lobbiesPanel.SetActive(false);
        _currentPanel.gameObject.SetActive(false);
        _carsPanel.SetActive(false);
        _hostButton.onClick.AddListener(LobbyManager.Instance.HostLobbyAsync);
        _refreshLobbiesButton.onClick.AddListener(HandleRefreshLobbyClick);
        LobbyManager.Instance.OnJoinedLobby += ShowCurrentLobby;
        LobbyManager.Instance.OnCountdownStarted += ShowCountdownPanel;
        // _countdownPanel.SetActive(false);

        _lobbyListPanel.Initialize();
        _currentPanel.Initialize();
    }

    async void HandleRefreshLobbyClick() => await LobbyManager.Instance.RefreshLobbies();

    void HandlePlayerLoggedIn()
    {
        _loginPanel.SetActive(false);
        _lobbiesPanel.SetActive(true);
    }

    void ShowAuthenticationPanel(RequestFailedException requestFailedException)
    {
        _loginPanel.SetActive(true);
        Debug.Log("Login Failed because " + requestFailedException.Message);
    }

    private void ShowCountdownPanel()
    {
        //_countdownPanel.SetActive(true);
    }

    void ShowCurrentLobby(Lobby obj)
    {
        _currentPanel.gameObject.SetActive(true);
        _carsPanel.SetActive(true);
    }

    async void LoginAnon()
    {
        try
        {
            _loginButtonsPanel.SetActive(false);
            await AuthenticationManager.Instance.SignInAnonFromUIAsync();
        }
        catch (AuthenticationException exception)
        {
            Debug.LogError(exception);
            _loginButtonsPanel.SetActive(true);
        }
    }

    void LoginUnity()
    {
        try
        {
            _loginButtonsPanel.SetActive(false);

            AuthenticationManager.Instance.StartSignInUnityAsync();
        }
        catch (AuthenticationException exception)
        {
            Debug.LogError(exception);
            _loginButtonsPanel.SetActive(true);
        }
    }
}