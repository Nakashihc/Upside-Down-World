using UnityEngine;
using System;

public class TriggerDetector : MonoBehaviour
{
    public event Action<bool> OnPlayerTriggerChanged;
    public string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerTriggerChanged?.Invoke(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerTriggerChanged?.Invoke(false);
        }
    }
}
