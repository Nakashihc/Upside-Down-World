using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveTrapManager : ColliderDetectorManager
{
    [System.Serializable]
    public class MoveStep
    {
        public Transform targetTransform;
        public float moveDuration = 1f;
        public float delayBefore = 0f;
        public AudioClip stepSound;
    }

    [System.Serializable]
    public class MovableObject
    {
        public Transform objectTransform;
        public AudioSource audioSource;
        public Color gizmoColor = Color.cyan;
        public List<MoveStep> steps = new List<MoveStep>();
    }

    [Header("Gizmo Settings")]
    public bool showGizmos = true;
    public List<MovableObject> objectsToMove = new List<MovableObject>();

    protected override void TriggerTrap()
    {
        foreach (var obj in objectsToMove)
        {
            StartCoroutine(MoveObjectSequence(obj));
        }
    }

    public IEnumerator MoveObjectSequence(MovableObject obj)
    {
        foreach (var step in obj.steps)
        {
            if (step.delayBefore > 0f)
                yield return new WaitForSeconds(step.delayBefore);

            Vector3 startPos = obj.objectTransform.position;
            Vector3 endPos = step.targetTransform.position;

            if (step.stepSound != null && obj.audioSource != null)
            {
                obj.audioSource.clip = step.stepSound;
                obj.audioSource.Play();
            }

            float elapsed = 0f;

            while (elapsed < step.moveDuration)
            {
                float t = elapsed / step.moveDuration;
                obj.objectTransform.position = Vector3.Lerp(startPos, endPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            obj.objectTransform.position = endPos;
        }
    }
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        foreach (var obj in objectsToMove)
        {
            if (obj.objectTransform == null || obj.steps == null) continue;
            Gizmos.color = obj.gizmoColor;

            Vector3 previousPosition = obj.objectTransform.position;

            foreach (var step in obj.steps)
            {
                if (step.targetTransform != null)
                {
                    Gizmos.DrawLine(previousPosition, step.targetTransform.position);
                    Gizmos.DrawWireSphere(step.targetTransform.position, 0.2f);
                    previousPosition = step.targetTransform.position;
                }
            }
        }
    }
}