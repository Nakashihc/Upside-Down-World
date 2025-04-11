using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public abstract class ColliderDetectorManager : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string[] triggerTags = { "Player" }; // Bisa diisi multiple tags
    public LayerMask triggerLayers; // Filter layer
    public bool triggerOnce = true;
    public UnityEvent onTrigger; // Event kustom

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Cek tag dan layer
        bool tagValid = false;
        foreach (string tag in triggerTags)
        {
            if (other.CompareTag(tag))
            {
                tagValid = true;
                break;
            }
        }

        bool layerValid = triggerLayers == (triggerLayers | (1 << other.gameObject.layer));

        if ((tagValid || layerValid) && (!hasTriggered || !triggerOnce))
        {
            TriggerTrap();
            onTrigger?.Invoke(); // Panggil event kustom
            hasTriggered = true;
        }
    }

    protected abstract void TriggerTrap();
}
