using Unity.Netcode;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject _loadingCanvas;
    [SerializeField] GameObject _mainMenuCanvas;
    [SerializeField] GameObject _inGameCanvas;
    [SerializeField] GameObject _startButtonCanvas;
    [SerializeField] GameObject _countdownCanvas;
    [SerializeField] GameObject _raceOverCanvas;

    UIMode CurrentUIMode;

    void Update()
    {
        if (ProjectSceneManager.Instance.IsLoading)
            SetUIMode(UIMode.Loading);
        else if (GameManager.Instance == null)
            SetUIMode(UIMode.MainMenu);
        else if (GameManager.Instance.CurrentState.Value == GameState.WaitingForPlayers)
            SetUIMode(UIMode.WaitingForPlayers);
        else if (GameManager.Instance.CurrentState.Value == GameState.CountDown)
            SetUIMode(UIMode.Countdown);
        else if (GameManager.Instance.CurrentState.Value == GameState.GameOver)
            SetUIMode(UIMode.PostRace);
        else if (GameManager.Instance.CurrentState.Value == GameState.Racing)
            SetUIMode(UIMode.DuringRace);

    }

    void SetUIMode(UIMode uiMode)
    {
        CurrentUIMode = uiMode;
        _loadingCanvas.SetActive(CurrentUIMode == UIMode.Loading);
        _mainMenuCanvas.SetActive(CurrentUIMode == UIMode.MainMenu);
        _startButtonCanvas.SetActive(CurrentUIMode == UIMode.WaitingForPlayers && NetworkManager.Singleton.IsHost);
        _inGameCanvas.SetActive(CurrentUIMode == UIMode.DuringRace);
        _raceOverCanvas.SetActive(CurrentUIMode == UIMode.PostRace);
        _countdownCanvas.SetActive(CurrentUIMode == UIMode.Countdown);
    }
}