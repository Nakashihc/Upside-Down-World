using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class PlayerColliderTrigger2D : MonoBehaviour
{
    [Header("Tag yang harus dimiliki objek pemain")]
    public string playerTag = "Player";

    [Header("Event Unity")]
    public UnityEvent onEnter;
    public UnityEvent onStay;
    public UnityEvent onExit;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            onEnter?.Invoke();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            onStay?.Invoke();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            onExit?.Invoke();
        }
    }
}
