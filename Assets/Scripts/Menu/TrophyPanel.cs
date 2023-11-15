using TMPro;
using UnityEngine;

public class TrophyPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _trophyCountText;

    void Update() => _trophyCountText.SetText(CarUnlockManager.Instance.TrophyCount.ToString());
}
