using Invector.vCharacterController;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class FreeFlowTargetChooser: MonoBehaviour
{

    private float distance = 11.0f;// prod value 11.0f;

    // directional angles
    private float MAX_ANGLE_ALLOWED = 90.0f; // this is both positive and negative
    // MAX_ANGLE_PRIORITY_ALLOWED is picked first over MAX_ANGLE_ALLOWED
    private float MAX_ANGLE_PRIORITY_ALLOWED = 30.0f; // this is both positive and negative
    private bool directionalInfluence;
    private float MIN_DISTANCE_FOR_DIRECTIONAL = 0.8f; // 1.0f is default
    private int directionalKey = -1;
    private const int DIRECTION_NONE = -1;
    private const int DIRECTION_FORWARD = 0;
    private const int DIRECTION_LEFT = 1;
    private const int DIRECTION_RIGHT = 2;
    private const int DIRECTION_BACK = 3;

    private FreeFlowTarget lastTarget;

    private vShooterMeleeInput shooterMeleeInput;

    void Awake()
    {
        shooterMeleeInput = GetComponent<vShooterMeleeInput>();
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {
            if (Input.GetKey(KeyCode.W))
            {
                directionalKey = DIRECTION_FORWARD;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                directionalKey = DIRECTION_LEFT;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                directionalKey = DIRECTION_RIGHT;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                directionalKey = DIRECTION_BACK;
            }
            directionalInfluence = true;
        } else
        {
            directionalKey = -1;
            directionalInfluence = false;
        }
    }

    class ProximityTarget
    {
        private Collider collider;
        public float distance;
        public float angle;

        public ProximityTarget(Collider collider, float distance, float angle)
        {
            this.collider = collider;
            this.distance = distance;
            this.angle = angle;
        }
        public static int SortByDistance(ProximityTarget p1, ProximityTarget p2)
        {
            return p1.distance.CompareTo(p2.distance);
        }

        public static int SortByAngle(ProximityTarget p1, ProximityTarget p2)
        {
            return p1.angle.CompareTo(p2.angle);
        }

        public GameObject getGameObject()
        {
            if (collider == null)
            {
                return null;
            }
            return collider.gameObject;
        }
    }

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
            float dist = Vector3.Distance(playerPos, planarTargetPos);
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

        if (closestTarget == null)
        {
            //Debug.Log("No viable targets for attack");
        }
        else
        {
            Debug.DrawRay(closestTarget.position, Vector3.up * 5f, Color.green, 1f);
        }
        return closestTarget;
    }

    //@author brum
    public FreeFlowTarget getTarget(int reason)
    {
        List<ProximityTarget> possibleTargets = new List<ProximityTarget>();
        Collider[] cols = Physics.OverlapSphere(transform.position, distance);

        
        List<Transform> targets = new List<Transform>(); 
        foreach (Collider collider in cols)
        {
            // sometimes bosses dont have stuns so we ignore
            FreeFlowTargetable freeFlowTargetable = collider.gameObject.GetComponent<FreeFlowTargetable>();
            if (freeFlowTargetable == null)
            {
                continue;
            }

            if (collider.gameObject == this.gameObject)
            {
                continue;
            }

            

            if (reason == TARGET_REASON_ATTACK)
            {
                if (!freeFlowTargetable.isTargetableForAttack())
                {
                    continue;
                }
            }
            else if (reason == TARGET_REASON_COUNTER)
            {
                if (!freeFlowTargetable.isTargetableForCounter())
                {
                    continue;
                }
            }
            else if (reason == TARGET_REASON_SEX)
            {
                if (!freeFlowTargetable.isTargetSexable())
                {
                    continue;
                }
            }
            targets.Add(collider.transform);
        }

        // brum code

        Vector3 wishDir = GetInput();
        if (wishDir.sqrMagnitude > 0f)
        {
            Debug.DrawRay(this.transform.position, wishDir * 5f, Color.yellow, 1f);
            Transform result = GetClosestTargetRotation(targets, wishDir, this.transform.position, /* testTolerance= */0);
            if (result != null)
            {
                return fromTransform(result);
            }
        } else
        {
            Transform result = GetClosestTargetDistance(targets, transform.position);
            if (result != null)
            {
                return fromTransform(result);
            }
        }
        return null;

    }


    //@author louck
    public FreeFlowTarget getTargetLouck(int reason)
    {
        List<ProximityTarget> possibleTargets = new List<ProximityTarget>();
        Collider[] cols = Physics.OverlapSphere(transform.position, distance);
        Vector3 characterToCollider;
        float dot;

        foreach (Collider collider in cols)
        {
            // not himself
            if (collider.gameObject == this.gameObject)
            {
                continue;
            }

            // sometimes bosses dont have stuns so we ignore
            FreeFlowTargetable freeFlowTargetable = collider.gameObject.GetComponent<FreeFlowTargetable>();
            if (freeFlowTargetable == null)
            {
                continue;
            }

            if (reason == TARGET_REASON_ATTACK)
            {
                if (!freeFlowTargetable.isTargetableForAttack())
                {
                    continue;
                }
            }
            else if (reason == TARGET_REASON_COUNTER)
            {
                if (!freeFlowTargetable.isTargetableForCounter())
                {
                    continue;
                }
            }
            else if (reason == TARGET_REASON_SEX)
            {
                if (!freeFlowTargetable.isTargetSexable())
                {
                    continue;
                }
            }

            characterToCollider = (collider.transform.position - transform.position).normalized;
            Camera cam = Camera.main;
            Transform reference = cam.transform;

            float horizontalAxis = Input.GetAxis("Horizontal");
            float verticalAxis = Input.GetAxis("Vertical");
            Vector3 inputVector = new Vector3(horizontalAxis, 0f, verticalAxis);
            Debug.Log(inputVector);
            var debugDir = reference.forward;
            if (directionalKey == DIRECTION_LEFT)
            {
                dot = Vector3.Dot(characterToCollider, -reference.right);
                debugDir = Vector3.Cross(characterToCollider, -reference.right);
            }
            else if (directionalKey == DIRECTION_RIGHT)
            {
                dot = Vector3.Dot(characterToCollider, reference.right);
                debugDir = Vector3.Cross(characterToCollider, reference.right);
            }
            else if (directionalKey == DIRECTION_BACK)
            {
                dot = Vector3.Dot(characterToCollider, -reference.forward);
                debugDir = Vector3.Cross(characterToCollider, -reference.forward);
            }
            else
            {
                // also the case where player is in control
                dot = Vector3.Dot(characterToCollider, reference.forward);
                debugDir = Vector3.Cross(characterToCollider, reference.forward);
            }

            float angle = Mathf.Rad2Deg * Mathf.Acos(dot);

            possibleTargets.Add(new ProximityTarget(collider, distance, angle));
        }

        if (possibleTargets.Count() == 0)
        {
            lastTarget = null;
            return null;
        }

        // Priority on angle
        var targetByPriorities = possibleTargets.Where(t => t.angle <= MAX_ANGLE_PRIORITY_ALLOWED);

        // If no priority, take all
        if (targetByPriorities.Count() == 0)
        {
            targetByPriorities = possibleTargets;
        }

        ProximityTarget res = targetByPriorities
            .OrderBy(t => t.angle)
            .ThenBy(t => t.distance)
            .FirstOrDefault();
        if (res == null)
        {
            lastTarget = null;
        } else
        {
            lastTarget = fromProximityTarget(res);
        }
        return lastTarget;
    }
    private FreeFlowTarget fromTransform(Transform transform)
    {
        FreeFlowTarget fft = new FreeFlowTarget();
        GameObject go = transform.gameObject;
        fft.distance = 10;
        fft.gameObject = go;
        return fft;
    }

    private FreeFlowTarget fromProximityTarget(ProximityTarget pt)
    {
        FreeFlowTarget fft = new FreeFlowTarget();
        GameObject go = pt.getGameObject();
        fft.distance = pt.distance;
        fft.gameObject = go;
        if (pt.angle > 90)
        {
            fft.isBehindMe = true;
        }
        return fft;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
         //Use the same vars you use to draw your Overlap SPhere to draw your Wire Sphere.
         Gizmos.DrawWireSphere(transform.position, distance);
    }
}
