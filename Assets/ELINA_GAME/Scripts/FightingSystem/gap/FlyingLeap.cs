using UnityEngine;
using System.Collections;

/// <summary>
/// Flying leap class use for Gotham Style Fighting
/// </summary>
public class FlyingLeap : MonoBehaviour
{
    
    // public for debugging
    public float timeReq;    
    public float distanceFound;

    // constants i tested
    private Vector3 _gravity = 9.8f * Vector3.down;
    private float minDistanceForLeap = 3f;
    private float multiplier = 0.05f;

    public GameObject target;

    private float distanceToTime()
    {
        distanceFound = Vector3.Distance(transform.position, target.transform.position);
        timeReq = distanceFound * multiplier;
        return timeReq;
    }
    
    public void LeapToTarget(GameObject target)
    {
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance < minDistanceForLeap)
        {
            return;
        }
        transform.LookAt(target.transform);
        SetVelocityToJump(target, distanceToTime());
    }

    private void SetVelocityToJump(GameObject goToJumpTo, float timeToJump)
    {
        StartCoroutine(jumpAndFollow(goToJumpTo, timeToJump));
    }

    private IEnumerator jumpAndFollow(GameObject goToJumpTo, float timeToJump)
    {
        var startPosition = transform.position;
        var targetTransform = goToJumpTo.transform;
        var lastTargetPosition = targetTransform.position;
        var initialVelocity = getInitialVelocity(lastTargetPosition - startPosition, timeToJump);

        var progress = 0f;
        while (progress < timeToJump)
        {
            progress += Time.deltaTime;
            if (targetTransform.position != lastTargetPosition)
            {
                lastTargetPosition = targetTransform.position;
                initialVelocity = getInitialVelocity(lastTargetPosition - startPosition, timeToJump);
            }

            transform.position = startPosition + (progress * initialVelocity) + (0.5f * Mathf.Pow(progress, 2) * _gravity);
            yield return null;
        }
    }

    private Vector3 getInitialVelocity(Vector3 toTarget, float timeToJump)
    {
        return (toTarget - (0.5f * Mathf.Pow(timeToJump, 2) * _gravity)) / timeToJump;
    }
}