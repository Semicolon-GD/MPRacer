using System;
using UnityEngine;

public class CarPanel : MonoBehaviour
{
    void Start() => CarUnlockManager.Instance.OnItemsUpdated += UpdateCarButtons;

    void OnDestroy() => CarUnlockManager.Instance.OnItemsUpdated -= UpdateCarButtons;

    void OnEnable() => UpdateCarButtons();

    void UpdateCarButtons()
    {
        if (!CarUnlockManager.Instance || CarUnlockManager.Instance.AllPurchases?.Count > 0 != true)
            return;
        
        var allButtons = GetComponentsInChildren<CarButton>();
        for (var i = 0; i < CarUnlockManager.Instance.AllPurchases.Count; i++)
        {
            var purchase = CarUnlockManager.Instance.AllPurchases[i];
            allButtons[i].Bind(purchase);
        }

        for (int i = CarUnlockManager.Instance.AllPurchases.Count; i < allButtons.Length; i++)
        {
            allButtons[i].gameObject.SetActive(false);
        }
    }
}