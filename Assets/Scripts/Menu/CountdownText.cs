using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CountdownText : MonoBehaviour
{
    TMP_Text _text;

    void Start() => _text = GetComponent<TMP_Text>();

    void Update()
    {
        if (GameManager.Instance.CurrentState.Value == GameState.CountDown)
        {
            var timeRemaining = GameManager.Instance.TimeToStart.Value - NetworkManager.Singleton.ServerTime.Time;
            _text.SetText(timeRemaining.ToString("n1"));
        }

        if (GameManager.Instance.CurrentState.Value == GameState.WaitingForPlayers)
            _text.SetText("Press START");
    }
}