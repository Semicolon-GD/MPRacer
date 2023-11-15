using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    [SerializeField] TMP_Text _winnerText;
    [SerializeField] Button _quitButton;
    [SerializeField] Button _nextRaceButton;
    [SerializeField] GameObject _panelRoot;
    
    void Start()
    {
        _quitButton.onClick.AddListener(LoadMenu);
        _nextRaceButton.onClick.AddListener(SwitchToNextTrackOnServer);
        _panelRoot.SetActive(false);
    }

    void LoadMenu() => StartCoroutine(ProjectSceneManager.Instance.LoadMenu());
    void SwitchToNextTrackOnServer() => GameManager.Instance.RequestNextTrack();//SwitchToNextTrackOnServer();

    void Update()
    {
        if (_panelRoot.activeSelf == false && 
            GameManager.Instance.CurrentState.Value == GameState.GameOver)
        {
            _panelRoot.SetActive(true);
            _winnerText.text = $"Player {GameManager.Instance.Winner.Value} Wins!";
        }
        else if (_panelRoot.activeSelf &&
                 GameManager.Instance.CurrentState.Value != GameState.GameOver)
        {
            _panelRoot.SetActive(false);
        }
    }
}