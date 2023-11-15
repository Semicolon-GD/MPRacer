using System.Collections;
using UnityEngine;

public class CarPanel : MonoBehaviour
{
    IEnumerator Start()
    {
        while (CarUnlockManager.IsInitialized == false)
            yield return null;
        UpdateCarButtons();
    }

    void UpdateCarButtons()
    {
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