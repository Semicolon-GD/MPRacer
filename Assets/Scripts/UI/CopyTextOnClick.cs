using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CopyTextOnClick : MonoBehaviour, IPointerDownHandler
{
    string _text;
    bool _clicked;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_clicked)
            return;
        _clicked = true;
        
        _text = GetComponent<TMP_Text>().text;
        GUIUtility.systemCopyBuffer = _text;
        GetComponent<TMP_Text>().SetText("Copied...");
        Invoke(nameof(ResetText), 1f);
    }

    void ResetText()
    {
        GetComponent<TMP_Text>().text = _text;
        _clicked = false;

    }
}