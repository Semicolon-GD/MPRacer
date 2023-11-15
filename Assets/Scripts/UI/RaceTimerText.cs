using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using TMPro;
using UnityEngine;

public class RaceTimerText : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    void OnValidate() => _text = GetComponent<TMP_Text>();

    void Update()
    {
        if (GameManager.Instance.CurrentState.Value == GameState.Racing)
        {
            string formatted = FormatTimeFromSeconds(GameManager.Instance.RaceTime);
            _text.SetText(formatted);
        }
            //_text.SetText(GameManager.Instance.RaceTime.ToString("N2"));
    }
    
    public static string FormatTimeFromSeconds(float totalSeconds)
    {
        // Calculate minutes, seconds, and hundredths of a second.
        int minutes = (int)totalSeconds / 60;
        int seconds = (int)totalSeconds % 60;
        int hundredths = (int)((totalSeconds - (int)totalSeconds) * 100); // Getting the fractional part

        // Create a byte buffer for UTF-8 formatting. Size based on "mm:ss.ff" format.
        Span<byte> byteBuffer = stackalloc byte[9]; // Size 9 to handle up to "mm:ss.ff".

        // Position in the buffer we're writing to. Start at the beginning.
        int position = 0;

        // Format minutes into the buffer, then advance the position by the number of bytes written.
        position += Utf8Formatter.TryFormat(minutes, byteBuffer.Slice(position), out int bytesWritten, new StandardFormat('D', 2)) ? bytesWritten : 0;
        byteBuffer[position++] = (byte)':';

        // Format seconds into the buffer, then advance the position.
        position += Utf8Formatter.TryFormat(seconds, byteBuffer.Slice(position), out bytesWritten, new StandardFormat('D', 2)) ? bytesWritten : 0;
        byteBuffer[position++] = (byte)'.';

        // Format hundredths of a second into the buffer. No need to advance the position after this.
        Utf8Formatter.TryFormat(hundredths, byteBuffer.Slice(position), out bytesWritten, new StandardFormat('D', 2));

        // Create a char buffer for the final string format. Size based on "mm:ss.ff" format.
        Span<char> charBuffer = stackalloc char[8]; // "mm:ss.ff" length is 8.

        // Decode the UTF-8 byte buffer into our char buffer.
        Encoding.UTF8.GetChars(byteBuffer.Slice(0, position + bytesWritten), charBuffer);

        // Construct the final string from the char buffer without additional allocations.
        return new string(charBuffer);
    }
}