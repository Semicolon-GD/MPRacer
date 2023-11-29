using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Login - Main Buttons")] [SerializeField]
    GameObject _loginButtonsPanel;

    [SerializeField] Button _anonLoginButton;
    [SerializeField] Button _unityLoginButton;

    [Header("Login - UserName and Password")] [SerializeField]
    Button _usernameAndPasswordShowPanelButton;

    [SerializeField] Button _usernameAndPasswordLoginButton;
    [SerializeField] Button _usernameAndPasswordCancelButton;
    [SerializeField] Button _usernameAndPasswordRegisterButton;
    [SerializeField] Button _accountButton;
    [SerializeField] TMP_InputField _nameInput;
    [SerializeField] TMP_InputField _passwordInput;
    [SerializeField] TMP_Text _statusText;
    [SerializeField] TMP_Text _playerIdText;

    [Header("Lobby Buttons")] [SerializeField]
    Button _hostButton;

    [SerializeField] Button _refreshLobbiesButton;

    [Header("Panels")] [SerializeField] GameObject _loginPanel;
    [SerializeField] CurrentLobbyPanel _currentPanel;
    [SerializeField] LobbyListPanel _lobbyListPanel;
    [SerializeField] GameObject _lobbiesPanel;
    [SerializeField] GameObject _carsPanel;
    [SerializeField] GameObject _userNameAndPasswordPanel;

    [SerializeField] Button _economyButton;

    void Start()
    {
        // Authentication Setup
        AuthenticationManager.Instance.OnSignedIn += HandlePlayerLoggedIn;
        AuthenticationManager.Instance.OnSigninFailed += ShowAuthenticationPanel;
        _anonLoginButton.onClick.AddListener(LoginAnon);
        _unityLoginButton.onClick.AddListener(LoginUnity);
        _usernameAndPasswordShowPanelButton.onClick.AddListener(ShowUserNameAndPasswordPanel);
        _usernameAndPasswordCancelButton.onClick.AddListener(HideUserNameAndPasswordPanel);
        _usernameAndPasswordLoginButton.onClick.AddListener(LoginUserNameAndPassword);
        _usernameAndPasswordRegisterButton.onClick.AddListener(SignUpWithUsernameAndPassword);
        _accountButton.onClick.AddListener(ShowDeleteAccountPage);
        _economyButton.onClick.AddListener(ShowCarsPanel);

        HideUserNameAndPasswordPanel();
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

    void ShowCarsPanel()
    {
        _carsPanel.SetActive(true);
    }

    void ShowDeleteAccountPage()
    {
        Application.OpenURL("https://player-account.unity.com/");
    }


    async void HandleRefreshLobbyClick() => await LobbyManager.Instance.RefreshLobbies();

    void HandlePlayerLoggedIn()
    {
        _loginPanel.SetActive(false);
        _lobbiesPanel.SetActive(true);
        _playerIdText.SetText(AuthenticationService.Instance.PlayerId);
    }

    void ShowAuthenticationPanel(RequestFailedException requestFailedException)
    {
        _loginPanel.SetActive(true);
        Debug.Log("Login Failed because " + requestFailedException.Message);
    }

    void ShowCountdownPanel()
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
        _loginButtonsPanel.SetActive(false);
        var result = await AuthenticationManager.Instance.SignInAnonAsync(default);
        if (!result.Success)
        {
            _loginButtonsPanel.SetActive(true);
            Debug.LogError(result.Message);
        }
    }

    void ShowUserNameAndPasswordPanel()
    {
        var username = PlayerPrefs.GetString("username");

#if UNITY_EDITOR
        username = FindFirstObjectByType<MPPMManager>().profileName;
        var password = PlayerPrefs.GetString("password");
        _passwordInput.text = password;
#endif

        if (!string.IsNullOrWhiteSpace(username))
            _nameInput.text = username;
        _userNameAndPasswordPanel.SetActive(true);
    }

    void HideUserNameAndPasswordPanel() => _userNameAndPasswordPanel.SetActive(false);

    async void SignUpWithUsernameAndPassword()
    {
        _usernameAndPasswordLoginButton.gameObject.SetActive(false);

        var result = await AuthenticationManager.Instance.RegisterWithUserNameAndPassword(
            _nameInput.text,
            _passwordInput.text);

        if (!result.Success)
        {
            _statusText.SetText(result.Message);
            _usernameAndPasswordLoginButton.gameObject.SetActive(true);
        }
        else
            SaveUsernameAndPassword();
    }

    async void LoginUserNameAndPassword()
    {
        _usernameAndPasswordLoginButton.gameObject.SetActive(false);

        var result =
            await AuthenticationManager.Instance.SignInWithUserNameAndPassword(_nameInput.text, _passwordInput.text);

        if (!result.Success)
        {
            _statusText.SetText(result.Message);
            _usernameAndPasswordLoginButton.gameObject.SetActive(true);
        }
        else
            SaveUsernameAndPassword();
    }

    void SaveUsernameAndPassword()
    {
        PlayerPrefs.SetString("username", _nameInput.text);
        #if UNITY_EDITOR
        PlayerPrefs.SetString("password", _passwordInput.text);
        #endif
    }

    async void LoginUnity()
    {
        _loginButtonsPanel.SetActive(false);

        var result = await AuthenticationManager.Instance.StartSignInUnityAsync();
        if (!result.Success)
        {
            Debug.LogError(result.Message);
            _loginButtonsPanel.SetActive(true);
        }
    }
}