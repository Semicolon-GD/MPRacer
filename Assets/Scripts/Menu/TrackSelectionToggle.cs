using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class TrackSelectionToggle : MonoBehaviour
{
    Toggle _toggle;
    [field:SerializeField] public string TrackName { get; private set; }

    void Awake()
    {
        _toggle = GetComponent<Toggle>();
        _toggle.onValueChanged.AddListener(ToggleChanged);
    }

    void ToggleChanged(bool isOn)
    {
        if (LobbyManager.Instance.CurrentLobby.HostId != AuthenticationService.Instance.PlayerId)
            return;

        if (isOn)
            LobbyManager.Instance.SetTrackInLobby(TrackName);
    }

    public void SetInteractable(bool interactable) => _toggle.interactable = interactable;
}