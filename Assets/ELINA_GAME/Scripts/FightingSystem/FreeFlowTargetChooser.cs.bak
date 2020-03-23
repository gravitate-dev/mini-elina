using Invector.vCharacterController;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class FreeFlowTargetChooser : MonoBehaviour
{

    private const float MAX_TARGET_DISTANCE = 11.0f;// prod value 11.0f;

    public const int TARGET_REASON_ATTACK = 0;
    public const int TARGET_REASON_COUNTER = 1;
    public const int TARGET_REASON_SEX = 2;
    // returns the primary target


    private Vector3 GetInput()
    {
        Vector3 tempInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        tempInput = Camera.main.transform.TransformDirection(tempInput);
        tempInput = Vector3.ProjectOnPlane(tempInput, Vector3.up).normalized;
        return tempInput;
    }

    /// <summary>
    /// Returns are target based on directional influence
    /// </summary>
    /// <param name="directionTolerance"> 0 = 90 degrees, 0.5 = 45 degrees, -1 = 180 degrees</param>
    /// <returns></returns>
    private Transform GetClosestTargetRotation(List<Transform> availableTargets, Vector3 inputDir, Vector3 playerPos, float directionTolerance)
    {
        float closestDot = -Mathf.Infinity;
        Transform closestTarget = null;
        for (int i = 0; i < availableTargets.Count; i++)
        {
            Vector3 planarTargetPos = Vector3.ProjectOnPlane(availableTargets[i].position, Vector3.up);
            Vector3 dirToTarget = (planarTargetPos - playerPos).normalized;
            float dot = Vector3.Dot(inputDir.normalized, dirToTarget);
            if (dot > closestDot)
            {
                closestDot = dot;
                closestTarget = availableTargets[i];
            }
        }


        if (closestTarget != null)
        {
            Debug.DrawRay(closestTarget.position, Vector3.up * 5f, Color.green, 1f);
        }

        return closestTarget;
    }

    private Transform GetClosestTargetDistance(List<Transform> availableTargets, Vector3 playerPos)
    {
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;
        for (int i = 0; i < availableTargets.Count; i++)
        {
            Vector3 planarTargetPos = Vector3.ProjectOnPlane(availableTargets[i].position, Vector3.up);
            float dist = Vector3.Distance(playerPos, planarTargetPos);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestTarget = availableTargets[i];
            }
        }

        if (closestTarget != null)
        {
            Debug.DrawRay(closestTarget.position, Vector3.up * 5f, Color.green, 1f);
        }
        return closestTarget;
    }

    //@author brum
    public GameObject getTarget(int reason)
    {
        List<Transform> targets = new List<Transform>();
        foreach (Collider collider in Physics.OverlapSphere(transform.position, MAX_TARGET_DISTANCE))
        {
            EnemyActionManager enemyMeleeActionManager = collider.gameObject.GetComponent<EnemyActionManager>();
            FreeFlowTargetable freeFlowTargetable = collider.gameObject.GetComponent<FreeFlowTargetable>();
            if (enemyMeleeActionManager == null || collider.gameObject == this.gameObject)
            {
                continue;
            }



            if (reason == TARGET_REASON_ATTACK && !freeFlowTargetable.isTargetableForAttack()
                || reason == TARGET_REASON_COUNTER && !enemyMeleeActionManager.isWishingMeleeAttack()
                || reason == TARGET_REASON_SEX && !freeFlowTargetable.isTargetSexable())
            {
                continue;
            }
            targets.Add(collider.transform);
        }

        Vector3 wishDir = GetInput();
        if (wishDir.sqrMagnitude > 0f)
        {
            Debug.DrawRay(this.transform.position, wishDir * 5f, Color.yellow, 1f);
            Transform result = GetClosestTargetRotation(targets, wishDir, this.transform.position, /* testTolerance= */0);
            if (result != null)
            {
                return result.gameObject;
            }
        }
        else
        {
            Transform result = GetClosestTargetDistance(targets, transform.position);
            if (result != null)
            {
                return result.gameObject;
            }
        }
        return null;

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, MAX_TARGET_DISTANCE);
    }
}
