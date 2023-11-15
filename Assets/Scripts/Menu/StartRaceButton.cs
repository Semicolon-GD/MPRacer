using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class StartRaceButton : MonoBehaviour
{
    Button _button;

    void Start()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(TryStartRace);
    }

    void TryStartRace()
    {
        if (NetworkManager.Singleton.IsHost)
            GameManager.Instance.StartRace();
        else
            Debug.LogError("Only the Host can start a race");
    }
}