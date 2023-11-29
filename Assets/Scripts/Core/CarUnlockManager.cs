using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using UnityEngine;

public class CarUnlockManager : MonoBehaviour
{
    public static CarUnlockManager Instance { get; private set; }
    public event Action OnItemsUpdated;
    List<PlayersInventoryItem> Inventory { get; set; }
    public long TrophyCount { get; private set; }
    public List<VirtualPurchaseDefinition> AllPurchases { get; private set; } = new();


    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ShortcutManager.Add("+20 Trophies", () => StartCoroutine(Add20Trophies()));
    }

    void Start() => AuthenticationService.Instance.SignedIn += GetItems;

    void OnDestroy() => AuthenticationService.Instance.SignedIn -= GetItems;

    async void GetItems()
    {
        await EconomyService.Instance.Configuration.SyncConfigurationAsync();
        AllPurchases = EconomyService.Instance.Configuration.GetVirtualPurchases();
        
        RefreshCustomCarData();
       
        await RefreshPlayersInventory();
    }

    async Task RefreshPlayersInventory()
    {
        var itemsResult = await EconomyService.Instance.PlayerInventory.GetInventoryAsync();
        Inventory = itemsResult.PlayersInventoryItems;
        var balancesResult = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
        TrophyCount = balancesResult.Balances.First(t => t.CurrencyId == "TROPHY").Balance;

        OnItemsUpdated?.Invoke();
    }

    void RefreshCustomCarData()
    {
        var allItems = EconomyService.Instance.Configuration.GetInventoryItems();
        foreach (var item in allItems)
        {
            var carData = item.CustomDataDeserializable.GetAs<CarCustomData>();
            if (carData != default)
            {
                CarDatas[item.Id] = carData;
            }
        }
    }

    public CarCustomData GetCarCustomData(string carItemId)
    {
        if (CarDatas.TryGetValue(carItemId, out var data))
            return data;
        return default;
    }

    Dictionary<string, CarCustomData> CarDatas = new();

    public bool IsCarUnlocked(string carItemId) => Inventory?.Any(t => 
        t.InventoryItemId == carItemId) == true;

    [ContextMenu(nameof(Add20Trophies))]
    public async Awaitable Add20Trophies()
    {
        try
        {
            Debug.Log("Adding Trophy");
            var newBalance = await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync("TROPHY", 20);
            TrophyCount = newBalance.Balance;
            Debug.Log($" Trophy Balance = {TrophyCount}");
        }
        catch (EconomyException e)
        {
            Debug.LogError(e);
        }
    }

    public async Task<bool> TryUnlockCar(string carPurchaseId)
    {
        try
        {
            Debug.Log($"Attempting to purchase {carPurchaseId}");
            var result = await EconomyService.Instance.Purchases.MakeVirtualPurchaseAsync(carPurchaseId);
            Debug.Log($"Result = {result.Rewards.Inventory.Count} items & {result.Rewards.Currency.Count} currency");

            await RefreshPlayersInventory();

            return true;
        }
        catch (EconomyException e)
        {
            Debug.LogError(e.Message);
        }

        return false;
    }

    public async Task TryLockCarAsync(string itemId)
    {
        var itemInstance = Inventory.FirstOrDefault(t => t.InventoryItemId == itemId);
        if (itemInstance == null)
        {
            Debug.LogError($"Unable to lock car {itemId}");
            return;
        }

        await EconomyService.Instance.PlayerInventory.DeletePlayersInventoryItemAsync(itemInstance
            .PlayersInventoryItemId);

        await RefreshPlayersInventory();
    }

    public async void SetLocalPlayerCar(string carItemId)
    {
        SelectedCarItemId = carItemId;
        await LobbyManager.Instance.UpdateLocalPlayerCar();
    }

    public string SelectedCarItemId { get; private set; }
}