using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public NetworkVariable<GameState> CurrentState = new();
    public NetworkVariable<FixedString128Bytes> Winner = new();
    public static GameManager Instance { get; private set; }

    public float TimeToStart { get; set; }
    public float RaceTime { get; set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (IsHost)
        {
            CurrentState.Value = GameState.WaitingForPlayers;
        }
    }

    void Update()
    {
        switch (CurrentState.Value)
        {
            case GameState.WaitingForPlayers:
                break;
            case GameState.CountDown:
                CountDownUpdate();
                break;
            case GameState.Racing:
                RacingUpdate();
                break;
            case GameState.GameOver:
                break;
        }
    }

    public override void OnDestroy()
    {
        if (IsHost)
            NetworkManager.OnClientConnectedCallback -= AddPlayerCar;

        base.OnDestroy();
    }

    void AddPlayerCar(ulong clientId)
    {
        UIConsoleManager.AddLog($"Add Player car {clientId}");
        var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        var networkPlayer = playerNetworkObject.GetComponent<NetworkPlayer>();
        networkPlayer.SpawnCar();
    }

    public void StartRace()
    {
        if (IsHost)
        {
            CurrentState.Value = GameState.CountDown;
            TimeToStart = 3;
            StartRaceClientRpc();
            MoveCarsToStartPointsOnServer();
        }
    }

    void MoveCarsToStartPointsOnServer()
    {
        var starts = new Queue<GameObject>(GameObject.FindGameObjectsWithTag("Respawn"));
        foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            var start = starts.Dequeue();
            player.transform.SetPositionAndRotation(start.transform.position, start.transform.rotation);
        }
    }

    [ClientRpc]
    void StartRaceClientRpc() => TimeToStart = 3f;

    void RacingUpdate() => RaceTime += Time.deltaTime;

    void CountDownUpdate()
    {
        TimeToStart -= Time.deltaTime;
        if (TimeToStart <= 0)
        {
            if (IsServer)
                CurrentState.Value = GameState.Racing;

            RaceTime = 0;
        }
    }

    public void FinishLap(byte lapsCompleteValue, CarLapCounter carLapCounter)
    {
        if (IsHost == false)
            return;

        if (lapsCompleteValue < 1) // one lap to win for testing
            return;

        var allPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        var player = allPlayers.FirstOrDefault(t => t.OwnerClientId == carLapCounter.OwnerClientId);
        if (player == null)
        {
            Debug.LogError($"Unable to map player to car {carLapCounter}");
            return;
        }

        SetWinner(player);
    }

    void SetWinner(NetworkPlayer networkPlayer)
    {
        CurrentState.Value = GameState.GameOver;
        Winner.Value = networkPlayer.name;
    }

    [ServerRpc]
    void SwitchToNextTrackOnServerRpc()
    {
        foreach (var car in FindObjectsByType<CarClientMovementController>(FindObjectsSortMode.None).ToList())
        {
            Destroy(car.gameObject);
        }

        ProjectSceneManager.Instance.SetupSceneManagementAndLoadNextTrack();
    }

    public void RequestNextTrack()
    {
        SwitchToNextTrackOnServerRpc();
    }
}