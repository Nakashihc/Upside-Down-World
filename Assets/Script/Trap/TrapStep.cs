using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class TrapStep : ColliderDetectorManager
{
    public enum TrapType { Move, Scale, ToggleActive }
    public enum ExecutionMode { Parallel, Sequential }

    [System.Serializable]
    public class TrapAction
    {
        public TrapType trapType;
        public ExecutionMode executionMode;
        [Header("Gizmo Settings")]
        public bool showGizmos = true;
        
        // Konfigurasi untuk setiap jenis trap
        public MoveTrapManager.MovableObject moveTrapConfig;
        public ScaleTrapManager.ScalableObject scaleTrapConfig;
        public ToggleActiveTrapManager.ToggleStep toggleTrapConfig;
        
        [Tooltip("Delay sebelum aksi ini dijalankan")]
        public float delayBeforeAction = 0f;
    }

    [Header("Trap Actions")]
    public List<TrapAction> trapActions = new List<TrapAction>();

    protected override void TriggerTrap()
    {
        StartCoroutine(RunTrapActions());
    }

    private IEnumerator RunTrapActions()
    {
        foreach (var action in trapActions)
        {
            if (action.delayBeforeAction > 0f)
                yield return new WaitForSeconds(action.delayBeforeAction);

            switch (action.trapType)
            {
                case TrapType.Move:
                    if (action.executionMode == ExecutionMode.Parallel)
                        StartCoroutine(RunMoveTrap(action.moveTrapConfig));
                    else
                        yield return RunMoveTrap(action.moveTrapConfig);
                    break;

                case TrapType.Scale:
                    if (action.executionMode == ExecutionMode.Parallel)
                        StartCoroutine(RunScaleTrap(action.scaleTrapConfig));
                    else
                        yield return RunScaleTrap(action.scaleTrapConfig);
                    break;

                case TrapType.ToggleActive:
                    if (action.executionMode == ExecutionMode.Parallel)
                        StartCoroutine(RunToggleTrap(action.toggleTrapConfig));
                    else
                        yield return RunToggleTrap(action.toggleTrapConfig);
                    break;
            }
        }
    }

    private IEnumerator RunMoveTrap(MoveTrapManager.MovableObject config)
    {
        MoveTrapManager moveTrap = gameObject.AddComponent<MoveTrapManager>();
        moveTrap.objectsToMove = new List<MoveTrapManager.MovableObject> { config };
        yield return StartCoroutine(moveTrap.MoveObjectSequence(config));
        Destroy(moveTrap);
    }

    private IEnumerator RunScaleTrap(ScaleTrapManager.ScalableObject config)
    {
        ScaleTrapManager scaleTrap = gameObject.AddComponent<ScaleTrapManager>();
        scaleTrap.objectsToScale = new List<ScaleTrapManager.ScalableObject> { config };
        yield return StartCoroutine(scaleTrap.ScaleObjectSequence(config));
        Destroy(scaleTrap);
    }

    private IEnumerator RunToggleTrap(ToggleActiveTrapManager.ToggleStep config)
    {
        ToggleActiveTrapManager toggleTrap = gameObject.AddComponent<ToggleActiveTrapManager>();
        toggleTrap.steps = new List<ToggleActiveTrapManager.ToggleStep> { config };
        yield return StartCoroutine(toggleTrap.ToggleSequence());
        Destroy(toggleTrap);
    }

    private void OnDrawGizmos()
    {
        foreach (var action in trapActions)
        {
            if (!action.showGizmos) return;

            switch (action.trapType)
            {
                case TrapType.Move:
                    DrawMoveGizmos(action.moveTrapConfig);
                    break;
                // case TrapType.Scale:
                //     DrawScaleGizmos(action.scaleTrapConfig);
                //     break;
                // case TrapType.ToggleActive:
                //     DrawToggleGizmos(action.toggleTrapConfig);
                //     break;
            }
        }
    }

    // Gizmo untuk Move Trap
    private void DrawMoveGizmos(MoveTrapManager.MovableObject config)
    {
        if (config.objectTransform == null || config.steps == null) return;
        
        Gizmos.color = config.gizmoColor;
        Vector3 previousPosition = config.objectTransform.position;

        foreach (var step in config.steps)
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

