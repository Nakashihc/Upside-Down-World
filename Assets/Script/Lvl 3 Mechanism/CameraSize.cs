using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;

public class CameraSize : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private bool lastFlipXState;

    [Header("Cinemachine References")]
    public CinemachineCamera virtualCamera;
    public CinemachineFollow framingTransposer;
    
    [Header("Smoothing Options")]
    public bool useSmoothing = true;
    public float smoothSpeed = 5f; // Semakin tinggi, semakin cepat transisinya

    [Header("Settings Saat Flipped Right (flipX = true)")]
    public float orthoSizeRight = 6f;
    public Vector3 followOffsetRight = new Vector3(0f, 0f, -10f);
    public Vector3 positionDampingRight = new Vector3(1f, 1f, 1f);
    public Vector3 rotationDampingRight = new Vector3(1f, 1f, 1f);

    [Header("Settings Saat Flipped Left (flipX = false)")]
    public float orthoSizeLeft = 5f;
    public Vector3 followOffsetLeft = new Vector3(0f, 0f, -10f);
    public Vector3 positionDampingLeft = new Vector3(1f, 1f, 1f);
    public Vector3 rotationDampingLeft = new Vector3(1f, 1f, 1f);

    [Header("Optional Events")]
    public UnityEvent onFlippedRight;
    public UnityEvent onFlippedLeft;

    // Target values untuk lerp
    private float targetOrthoSize;
    private Vector3 targetFollowOffset;
    private Vector3 targetPositionDamping;
    private Vector3 targetRotationDamping;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer tidak ditemukan.");
            enabled = false;
            return;
        }

        if (virtualCamera == null)
        {
            Debug.LogError("VirtualCamera belum diassign.");
            enabled = false;
            return;
        }

        framingTransposer = virtualCamera.GetComponent<CinemachineFollow>();
        if (framingTransposer == null)
        {
            Debug.LogError("FramingTransposer tidak ditemukan di VirtualCamera.");
            enabled = false;
            return;
        }

        lastFlipXState = spriteRenderer.flipX;
        SetTargetSettingsImmediate();
    }

    void Update()
    {
        if (spriteRenderer.flipX != lastFlipXState)
        {
            lastFlipXState = spriteRenderer.flipX;
            SetTargetSettings();
        }

        if (useSmoothing)
        {
            SmoothUpdate();
        }
    }

    private void SetTargetSettings()
    {
        if (spriteRenderer.flipX)
        {
            targetOrthoSize = orthoSizeRight;
            targetFollowOffset = followOffsetRight;
            targetPositionDamping = positionDampingRight;
            targetRotationDamping = rotationDampingRight;
            onFlippedRight?.Invoke();
        }
        else
        {
            targetOrthoSize = orthoSizeLeft;
            targetFollowOffset = followOffsetLeft;
            targetPositionDamping = positionDampingLeft;
            targetRotationDamping = rotationDampingLeft;
            onFlippedLeft?.Invoke();
        }

        if (!useSmoothing)
        {
            ApplyTargetSettings();
        }
    }

    private void SetTargetSettingsImmediate()
    {
        if (spriteRenderer.flipX)
        {
            targetOrthoSize = orthoSizeRight;
            targetFollowOffset = followOffsetRight;
            targetPositionDamping = positionDampingRight;
            targetRotationDamping = rotationDampingRight;
        }
        else
        {
            targetOrthoSize = orthoSizeLeft;
            targetFollowOffset = followOffsetLeft;
            targetPositionDamping = positionDampingLeft;
            targetRotationDamping = rotationDampingLeft;
        }

        ApplyTargetSettings();
    }

    private void ApplyTargetSettings()
    {
        virtualCamera.Lens.OrthographicSize = targetOrthoSize;
        framingTransposer.FollowOffset = targetFollowOffset;
        framingTransposer.TrackerSettings.PositionDamping = targetPositionDamping;
        framingTransposer.TrackerSettings.RotationDamping = targetRotationDamping;
        Debug.Log("Setting Cinemachine untuk flipped RIGHT diterapkan.");
    }

    private void SmoothUpdate()
    {
        // Smooth OrthoSize
        virtualCamera.Lens.OrthographicSize = Mathf.Lerp(virtualCamera.Lens.OrthographicSize, targetOrthoSize, Time.deltaTime * smoothSpeed);

        // Smooth Follow Offset
        framingTransposer.FollowOffset = Vector3.Lerp(framingTransposer.FollowOffset, targetFollowOffset, Time.deltaTime * smoothSpeed);

        // Smooth Damping
        framingTransposer.TrackerSettings.PositionDamping.x = Mathf.Lerp(framingTransposer.TrackerSettings.PositionDamping.x, targetPositionDamping.x, Time.deltaTime * smoothSpeed);
        framingTransposer.TrackerSettings.PositionDamping.y = Mathf.Lerp(framingTransposer.TrackerSettings.PositionDamping.y, targetPositionDamping.y, Time.deltaTime * smoothSpeed);
        framingTransposer.TrackerSettings.RotationDamping.x = Mathf.Lerp(framingTransposer.TrackerSettings.RotationDamping.x, targetRotationDamping.x, Time.deltaTime * smoothSpeed);
        framingTransposer.TrackerSettings.RotationDamping.y = Mathf.Lerp(framingTransposer.TrackerSettings.RotationDamping.y, targetRotationDamping.y, Time.deltaTime * smoothSpeed);
    }

}
