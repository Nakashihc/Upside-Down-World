using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ToggleActiveTrapManager : ColliderDetectorManager
{
    [System.Serializable]
    public class ToggleStep
    {
        public GameObject targetObject;
        public bool setActiveState = true;
        public float delayBefore = 0f;
    }

    public List<ToggleStep> steps = new List<ToggleStep>();

    protected override void TriggerTrap()
    {
        StartCoroutine(ToggleSequence());
    }

    public IEnumerator ToggleSequence()
    {
        foreach (var step in steps)
        {
            if (step.delayBefore > 0f)
                yield return new WaitForSeconds(step.delayBefore);

            if (step.targetObject != null)
                step.targetObject.SetActive(step.setActiveState);
        }
    }
}