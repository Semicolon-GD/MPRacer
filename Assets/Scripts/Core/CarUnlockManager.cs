using System.Collections;
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
    public static bool IsInitialized { get; private set; }

    List<PlayersInventoryItem> Inventory { get; set; }
    public long TrophyCount { get; private set; }
    public List<VirtualPurchaseDefinition> AllPurchases { get; private set; }

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

    IEnumerator Start()
    {
        while (AuthenticationService.Instance.IsSignedIn == false)
        {
            // Debug.Log("Waiting for Authentication");
            yield return null;
        }

        yield return GetItems();
        //yield return Add20Trophies();
        IsInitialized = true;
    }

    async Awaitable GetItems()
    {
        await EconomyService.Instance.Configuration.SyncConfigurationAsync();
        AllPurchases = EconomyService.Instance.Configuration.GetVirtualPurchases();

        var itemsResult = await EconomyService.Instance.PlayerInventory.GetInventoryAsync();
        Inventory = itemsResult.PlayersInventoryItems;
        var balancesResult = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
        TrophyCount = balancesResult.Balances.First(t => t.CurrencyId == "TROPHY").Balance;
    }

    public bool IsCarUnlocked(string carItemId) => true;
        //Inventory?.Any(t => t.InventoryItemId == carItemId) == true;


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

    public async Task<bool> TryUnlockCar(string carItemId)
    {
        try
        {
            var carPurchaseId = carItemId;
            Debug.Log($"Attempting to purchase {carPurchaseId}");
            var result = await EconomyService.Instance.Purchases.MakeVirtualPurchaseAsync(carPurchaseId);
            var itemsResult = await EconomyService.Instance.PlayerInventory.GetInventoryAsync();
            Inventory = itemsResult.PlayersInventoryItems;
            var balancesResult = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
            TrophyCount = balancesResult.Balances.First(t => t.CurrencyId == "TROPHY").Balance;

            return true;
        }
        catch (EconomyException e)
        {
            Debug.LogError(e);
        }

        return false;
    }
}