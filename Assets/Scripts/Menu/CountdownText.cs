using TMPro;
using UnityEngine;

public class CountdownText : MonoBehaviour
{
    TMP_Text _text;

    void Start() => _text = GetComponent<TMP_Text>();

    void Update()
    {
        if (GameManager.Instance.CurrentState.Value == GameState.CountDown)
            _text.SetText(GameManager.Instance.TimeToStart.ToString("n1"));
           
        if (GameManager.Instance.CurrentState.Value == GameState.WaitingForPlayers)
            _text.SetText("Press START");
    }
}