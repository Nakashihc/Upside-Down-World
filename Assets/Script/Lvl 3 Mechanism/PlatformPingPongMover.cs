using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PointData
{
    public Transform point;
    public float pauseDuration = 1f;
}

public class PlatformPingPongMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public List<PointData> pointsData;
    public float speed = 2f;
    public bool loop = true;

    [Header("Easing Settings")]
    public bool useCustomCurve = false;
    public AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Trigger Settings")]
    public bool requireTrigger = false;
    public TriggerDetector triggerDetector;

    private int currentIndex = 0;
    private bool movingForward = true;
    private bool isMoving = false;
    private bool isPausing = false;
    private bool playerInTrigger = false;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float journeyLength;
    private float journeyTime;
    private float elapsedTime;

    private void Start()
    {
        if (!requireTrigger)
            BeginMovement();
        else if (triggerDetector == null)
            Debug.LogWarning("Require trigger is enabled, but triggerDetector is not assigned.");
        else
            triggerDetector.OnPlayerTriggerChanged += OnPlayerTriggerChanged;
    }

    private void OnDestroy()
    {
        if (triggerDetector != null)
            triggerDetector.OnPlayerTriggerChanged -= OnPlayerTriggerChanged;
    }

    private void Update()
    {
        if (isMoving && pointsData.Count > 1 && !isPausing)
        {
            MoveAlongPath();
        }
    }

    private void OnPlayerTriggerChanged(bool isInside)
    {
        if (isInside)
        {
            currentIndex = 0;
            movingForward = true;
            BeginMovement();
        }
    }

    

    private void MoveAlongPath()
    {
        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / journeyTime);

        if (useCustomCurve && movementCurve != null)
            t = movementCurve.Evaluate(t);

        transform.position = Vector3.Lerp(startPos, targetPos, t);

        if (t >= 1f)
        {
            StartCoroutine(PauseBeforeNextMove());
        }
    }

    private IEnumerator PauseBeforeNextMove()
    {
        isPausing = true;

        float pauseTime = pointsData[currentIndex].pauseDuration;
        yield return new WaitForSeconds(pauseTime);

        UpdateNextTarget();
        isPausing = false;
    }

    private void UpdateNextTarget()
    {
        if (movingForward)
        {
            currentIndex++;
            if (currentIndex >= pointsData.Count)
            {
                if (loop)
                {
                    movingForward = false;
                    currentIndex = pointsData.Count - 2;
                }
                else
                {
                    isMoving = false;
                }
            }
        }
        else
        {
            currentIndex--;
            if (currentIndex < 0)
            {
                if (loop)
                {
                    movingForward = true;
                    currentIndex = 1;
                }
                else
                {
                    isMoving = false;
                }
            }
        }

        if (isMoving)
            BeginMovement();
    }

    private void BeginMovement()
    {
        isMoving = true;
        if (pointsData == null || pointsData.Count < 2) return;

        startPos = transform.position;
        targetPos = pointsData[currentIndex].point.position;
        journeyLength = Vector3.Distance(startPos, targetPos);
        journeyTime = journeyLength / speed;
        elapsedTime = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (pointsData != null && pointsData.Count > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < pointsData.Count - 1; i++)
            {
                if (pointsData[i].point != null && pointsData[i + 1].point != null)
                    Gizmos.DrawLine(pointsData[i].point.position, pointsData[i + 1].point.position);
            }
        }
    }
}
