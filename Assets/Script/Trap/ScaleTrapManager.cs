using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScaleTrapManager : ColliderDetectorManager
{
    [System.Serializable]
    public class ScaleStep
    {
        public Vector3 targetScale = Vector3.one;
        public float scaleDuration = 1f;
        public float delayBefore = 0f;
    }

    [System.Serializable]
    public class ScalableObject
    {
        public Transform objectTransform;
        public List<ScaleStep> steps = new List<ScaleStep>();
    }

    public List<ScalableObject> objectsToScale = new List<ScalableObject>();

    protected override void TriggerTrap()
    {
        foreach (var obj in objectsToScale)
        {
            StartCoroutine(ScaleObjectSequence(obj));
        }
    }

    public IEnumerator ScaleObjectSequence(ScalableObject obj)
    {
        foreach (var step in obj.steps)
        {
            if (step.delayBefore > 0f)
                yield return new WaitForSeconds(step.delayBefore);

            Vector3 startScale = obj.objectTransform.localScale;
            Vector3 endScale = step.targetScale;
            float elapsed = 0f;

            while (elapsed < step.scaleDuration)
            {
                float t = elapsed / step.scaleDuration;
                obj.objectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            obj.objectTransform.localScale = endScale;
        }
    }
}