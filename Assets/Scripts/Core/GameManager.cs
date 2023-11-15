using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : NetworkBehaviour
{
    public NetworkVariable<GameState> CurrentState = new();
    public NetworkVariable<FixedString128Bytes> Winner = new();
    public NetworkVariable<double> TimeToStart = new();
    public static GameManager Instance { get; private set; }

 //   public float TimeToStart { get; set; }
    public float RaceTime { get; set; }

    void Awake()
    {
        Instance = this;
            CurrentState.OnValueChanged += GameState_OnValueChanged;
    }

    void GameState_OnValueChanged(GameState previousValue, GameState newValue)
    {
        Debug.Log("GameState = " + newValue);
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

    public void StartRace()
    {
        if (IsHost)
        {
            CurrentState.Value = GameState.CountDown;
            TimeToStart.Value = NetworkManager.Singleton.NetworkTimeSystem.ServerTime + 3;
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

    void RacingUpdate() => RaceTime += Time.deltaTime;

    void CountDownUpdate()
    {
        if (TimeToStart.Value == 0)
            return;

        var timeRemaining = TimeToStart.Value - NetworkManager.ServerTime.Time;
        if (timeRemaining <= 0)
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

        if (lapsCompleteValue < 2) // one lap to win for testing
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

        ProjectSceneManager.Instance.LoadNextTrack();
    }

    public void RequestNextTrack()
    {
        SwitchToNextTrackOnServerRpc();
    }
}