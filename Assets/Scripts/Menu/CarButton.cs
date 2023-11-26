using System;
using System.Collections;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Economy.Model;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CarButton : MonoBehaviour
{
    [SerializeField] Button _selectButton;
    [SerializeField] Button _unlockButton;
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
    }

    async void TryUnlockCar()
    {
        var unlockResult = await CarUnlockManager.Instance.TryUnlockCar(_carPurchaseId);
        Debug.Log($"Unlock result {unlockResult}");
        if (unlockResult)
        {
            _selectButton.interactable = true;
            _unlockButton.gameObject.SetActive(false);
        }
        
    }

    void Update()
    {
        if (CarUnlockManager.Instance != null)
            _selectButton.interactable = CarUnlockManager.Instance.IsCarUnlocked(_carItemId);
    }

    async void TrySelectCar()
    {
        bool isUnlocked = true;
        if (!isUnlocked)
            return;

        await LobbyManager.Instance.SetLocalPlayerCar(_carDefinition.Name);
    }

    public void Bind(VirtualPurchaseDefinition purchase)
    {
        _carPurchaseId = purchase.Id;
        _carItemId = purchase.Rewards.FirstOrDefault()?.Item.GetReferencedConfigurationItem().Id;
        _carDefinition = CarDefinition.GetCarDefinition(purchase.Id);
        _unlockButton.gameObject.SetActive(true);
        _selectButton.interactable = false;
        _costText.SetText(purchase.Costs.FirstOrDefault()?.Amount.ToString());
        _nameText.SetText(purchase.Name);
        _frontImage.sprite = _carDefinition.FrontShot;
        _sideImage.sprite = _carDefinition.SideShot;
        var carStats = _carDefinition.Prefab.GetComponent<CarClientMovementController>();
        if (carStats != null)
        {
            _speedText.SetText(carStats.MaxSpeed.ToString());
            _turnText.SetText(carStats.TurnSpeed.ToString());
        }
    }
}