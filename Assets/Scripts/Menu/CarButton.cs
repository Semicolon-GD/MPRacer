using System;
using System.Linq;
using TMPro;
using Unity.Services.Economy.Model;
using UnityEngine;
using UnityEngine.UI;

public class CarButton : MonoBehaviour
{
    [SerializeField] Button _selectButton;
    [SerializeField] Button _unlockButton;
    [SerializeField] Button _lockButton;
    [SerializeField] Image _sideImage;
    [SerializeField] Image _frontImage;
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _costText;
    [SerializeField] TMP_Text _speedText;
    [SerializeField] TMP_Text _turnText;

    [SerializeField] CarDefinition _carDefinition; // can be pulled from economy once setup
    string _carPurchaseId;
    string _carItemId;

    void OnValidate()
    {
        _selectButton = GetComponent<Button>();
    }

    void Awake()
    {
        _selectButton.onClick.AddListener(TrySelectCar);
        _unlockButton.onClick.AddListener(TryUnlockCar);
        _lockButton.onClick.AddListener(TryLockCar);
    }

    async void TryLockCar() => await CarUnlockManager.Instance.TryLockCarAsync(_carItemId);

    async void TryUnlockCar()
    {
        var unlockResult = await CarUnlockManager.Instance.TryUnlockCar(_carPurchaseId);
        Debug.Log($"Unlock result {unlockResult}");
        if (unlockResult)
        {
            _selectButton.interactable = true;
            _unlockButton.gameObject.SetActive(false);
            _lockButton.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (CarUnlockManager.Instance != null)
            _selectButton.interactable = CarUnlockManager.Instance.IsCarUnlocked(_carItemId);
    }

    void TrySelectCar()
    {
        bool isUnlocked = true;
        if (!isUnlocked)
            return;

        CarUnlockManager.Instance.SetLocalPlayerCar(_carItemId);
    }

    public void Bind(VirtualPurchaseDefinition purchase)
    {
        _carPurchaseId = purchase.Id;
        var item = purchase.Rewards.FirstOrDefault()?.Item.GetReferencedConfigurationItem();
        if (item == null)
        {
            Debug.LogError($"Invalid Item for purchase {purchase.Id}");
            return;
        }
        
        _carItemId = item.Id;

        string definition = item.Name.Replace(" Car", "");
        if (item.CustomDataDeserializable != null)
        {
            try
            {
                string customCarDefinition = item.CustomDataDeserializable.GetAs<CarCustomData>()?.cardefinition;
                if (!string.IsNullOrWhiteSpace(customCarDefinition))
                    definition = customCarDefinition;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Unable to read custom data from {_carItemId} " + ex.Message);
            }
        }

        _carDefinition = CarDefinition.GetCarDefinition(definition);

        bool isCarOwned = CarUnlockManager.Instance.IsCarUnlocked(_carItemId);
        _unlockButton.gameObject.SetActive(!isCarOwned);
        _lockButton.gameObject.SetActive(isCarOwned);
        _selectButton.interactable = isCarOwned;

        _costText.SetText(purchase.Costs.FirstOrDefault()?.Amount.ToString());
        _nameText.SetText(purchase.Name);
        _frontImage.sprite = _carDefinition.FrontShot;
        _sideImage.sprite = _carDefinition.SideShot;

        var carStats = _carDefinition.Prefab.GetComponent<CarClientMovementController>();
        if (carStats != null)
        {
            var carCustomData = item.CustomDataDeserializable.GetAs<CarCustomData>();
            if (carCustomData != default)
            {
                _speedText.SetText(carCustomData.maxspeed.ToString());
                _turnText.SetText(carCustomData.turnspeed.ToString());
            }
            else
            {
                _speedText.SetText(carStats.MaxSpeed.ToString());
                _turnText.SetText(carStats.TurnSpeed.ToString());
            }
        }
    }
}

public class CarCustomData
{
    public int maxspeed;
    public int turnspeed;
    public string cardefinition;
}