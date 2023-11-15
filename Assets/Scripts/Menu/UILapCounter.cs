using System.Text;
using TMPro;
using UnityEngine;

public class UILapCounter : MonoBehaviour
{
    void Update()
    {
        StringBuilder builder = new();
        var allCars = FindObjectsByType<CarLapCounter>(FindObjectsSortMode.None);
        foreach (var car in allCars)
        {
            builder.AppendLine($"{car.PlayerName} {car.LapsComplete.Value}");
        }
        GetComponent<TMP_Text>().SetText(builder);
    }
}
