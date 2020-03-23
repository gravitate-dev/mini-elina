using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyLogic : MonoBehaviour
{
    private GameObject DEBUG_ATTACK_SYMBOL;

    GameObject TargetPlayer;
    Vector3 SpawnPoint;
    public Vector3 TargetDestination;
    [HideInInspector] public Vector3 MovementDirection;
    CharacterController cc; 
    IAstarAI ai;
    [Range(0, 1)] public int EnemyType;

    [Header("Speeds")]
    //DONT TOUCH currentSpeed;
    public float CurrentSpeed;
    float Acceleration;
    public float MaxChasingSpeed;
    public float MaxRepositioningSpeed;
    public float MaxStrafingSpeed;

    [Header("Behavior")]
    public Behavior CurrentBehavior;
    float QueuedDistance = 0;
    public float BehaviorChangeTimerForSex = 0;
    public float BehaviorChangeTimerForStun = 0;
    public float BehaviorChangeTimer = 0;
    // new 
    float CurrentAttackChargeTime = 0;
    float StrafeDirectionTimer = 0;
    float AttackDistance = 0;
    float PreferredDistance = 0;
    float PreferredDistanceTimer = 0;
    float CloserThanPreferredTimer = 0;
    float avoidanceTimer = 0;
    [HideInInspector] public float AttackCooldownTimer = 0;
    [HideInInspector] public float DistanceFromPlayer = 0;

    [HideInInspector] public bool Aggro = false;
    bool InAttackRange = false;
    bool MovingToSpecifiedLocation = false;
    [HideInInspector] public bool Attacking = false;
    bool StrafingRight = false;
    public bool MovingToTargetPosition = false;

    public float TargetingRange;
    public float MaximumDistanceToPlayer;
    public float MinimumDistanceToPlayer;

    [Range(0, 1)] public float ChanceToIdle;
    [Range(0, 10)] public float DurationToIdle;

    [Range(0, 1)] public float ChanceToStrafe;
    [Range(0, 10)] public float DurationToStrafe;
    [Range(0, 10)] public float StrafeDirectionChangeTime;

    [Range(0, 1)] public float ChanceToReposition;
    [Range(0, 10)] public float DurationToReposition;
    public float TimeBetweenAttacks;

    private bool isDefeated;
    private List<System.Guid> disposables = new List<System.Guid>();
    private EnemyActionManager enemyActionManager;
    private void OnDestroy()
    {
       WickedObserver.RemoveListener(disposables);
    }
    public enum Behavior
    {
        MovingToAttack,
        Disabled,
        Idling,
        Chasing,
        Repositioning,
        Strafing
    }

    private void Start()
    {
        DEBUG_ATTACK_SYMBOL = SpecialFxRequestBuilder.newBuilder("DebugMarker")
                .setOwner(transform, true)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.PLAYER_HEIGHT, 0))
                .build().Play();
        DEBUG_ATTACK_SYMBOL.SetActive(false);
        enemyActionManager = GetComponent<EnemyActionManager>();
        cc = GetComponent<CharacterController>();
        ai = GetComponent<IAstarAI>();
        TargetPlayer = GameObject.FindWithTag("Player");
        PreferredDistance = MaximumDistanceToPlayer / 2;
        SpawnPoint = transform.position;

        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + gameObject.GetInstanceID(), (unused) =>
        {
            BehaviorChangeTimerForStun = 0;
           if (isDefeated)
               return;
           DisableForDurationBySex(float.MaxValue);
        }));
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + gameObject.GetInstanceID(), (unused) =>
        {
           if (isDefeated)
               return;
           DisableForDurationBySex(0.1f);
        }));
    }


    /*private void LateUpdate()
    {
        // used for debugging
        if (CurrentBehavior == Behavior.MovingToAttack)
        {
            DEBUG_ATTACK_SYMBOL.SetActive(true);
        }
        else
        {
            DEBUG_ATTACK_SYMBOL.SetActive(false);
        }
    }*/
    void Update()
    {
        if (isDefeated)
            return;
        if (Input.GetKeyDown(KeyCode.L))
        {
            GrabAndMoveObject(GameObject.Find("TARGET"), TargetPlayer.transform.position);
        }

        // If the player is not in range, do nothing
        if (TargetPlayer == null || !cc.enabled)
            return;

        
        // Update the pathfinding
        UpdatePath();

        if (!Aggro && Vector3.Distance(TargetPlayer.transform.position, transform.position) < TargetingRange)
        {
            Aggro = true;
        }

        if (!Aggro && !MovingToSpecifiedLocation)
        {
            if (Vector3.Distance(transform.position, SpawnPoint) > 2)
            {
                SetPathfindingLocation(SpawnPoint);
                Vector3 dir = ai.steeringTarget - transform.position;
                dir.y = 0;
                Quaternion rot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 20 * Time.deltaTime);
            }
            else
            {
                DisablePathfinding();
                MovementDirection = Vector3.zero;
                MovementDirection.y -= 10;
                cc.Move(MovementDirection * Time.deltaTime);
            }
            return;
        }

        DistanceFromPlayer = Vector3.Distance(TargetPlayer.transform.position, transform.position);

        // Drop aggro and stand still
        if (DistanceFromPlayer > TargetingRange)
        {
            Aggro = false;
            return;
        }

        ChooseBehavior();

        if (MovingToSpecifiedLocation) CurrentBehavior = Behavior.Chasing;

        if (( Attacking && CurrentAttackChargeTime  < 0 )|| CurrentBehavior == Behavior.Disabled) return;

        CurrentAttackChargeTime -= Time.deltaTime;
        TimeBetweenAttacks -= Time.deltaTime;
        MovementDirection = Vector3.zero;

        // Enemy is outside of the max range from player and needs to move closer
        if (CurrentBehavior != Behavior.MovingToAttack &&
        Vector3.Distance(TargetPlayer.transform.position, transform.position) > MaximumDistanceToPlayer && !MovingToSpecifiedLocation)
        {
            RotateTowardTarget(5);
            CurrentBehavior = Behavior.Chasing;
            BehaviorChangeTimer = 0.5f;
            MovementDirection = (ai.steeringTarget - transform.position);
            MovementDirection.y = 0;
            SetPathfindingLocation(TargetPlayer.transform.position);
        }
        // Enemy is too close to the player and needs to move away
        else if (CurrentBehavior != Behavior.MovingToAttack &&
        Vector3.Distance(TargetPlayer.transform.position, transform.position) < MinimumDistanceToPlayer && !MovingToTargetPosition)
        {
            DisablePathfinding();
            RotateTowardTarget(5);
            CurrentBehavior = Behavior.Repositioning;
            BehaviorChangeTimer = 0.5f;
            MovementDirection = (transform.position - TargetPlayer.transform.position);
        }
        // Enemy is within the min / max range
        else
        {
            if (CurrentBehavior == Behavior.Idling)
            {
                // Do any idling action you want here, could be taunting the player
                DisablePathfinding();

            }
            else if (CurrentBehavior == Behavior.MovingToAttack)
            {
                // Try to move the enemy into attack range then trigger the attack
                MoveToAttackDistance();
            }
            else if (CurrentBehavior == Behavior.Repositioning)
            {
                // Reposition the enemy closer to its desired distance
                DisablePathfinding();
                RotateTowardTarget(5);
                MoveToPreferredDistance();
            }
            else if (CurrentBehavior == Behavior.Strafing)
            {
                // Change the strafe direction if the timer is up
                DisablePathfinding();
                RotateTowardTarget(10);
                StrafeDirectionTimer -= Time.deltaTime;
                if (StrafeDirectionTimer <= 0)
                {
                    StrafeDirectionTimer = StrafeDirectionChangeTime;
                    StrafingRight ^= true;
                }

                // Set the movement direction relative to the strafe direction
                if (StrafingRight)
                    MovementDirection = transform.right;
                else
                    MovementDirection = -transform.right;
            }
        }

        if (!Attacking && CurrentBehavior != Behavior.MovingToAttack && CurrentBehavior != Behavior.Disabled)
            AvoidOtherEnemies();

        //if (MovingToSpecifiedLocation)
        //{
        //    Vector3 dir = ai.steeringTarget - transform.position;
        //    dir.y = 0;
        //    Quaternion rot = Quaternion.LookRotation(dir);
        //    transform.rotation = Quaternion.Slerp(transform.rotation, rot, 20 * Time.deltaTime);
        //    MovementDirection = dir;
        //}

        MovementDirection = MovementDirection.normalized;
            
        if (CurrentBehavior == Behavior.Chasing) CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxChasingSpeed * MovementDirection.sqrMagnitude, Time.deltaTime * 1f);
        else if (CurrentBehavior == Behavior.MovingToAttack) CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxRepositioningSpeed * MovementDirection.sqrMagnitude, Time.deltaTime * 1f);
        else if (CurrentBehavior == Behavior.Idling) CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0f * MovementDirection.sqrMagnitude, Time.deltaTime * 1f);
        else if (CurrentBehavior == Behavior.Strafing) CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxStrafingSpeed * MovementDirection.sqrMagnitude, Time.deltaTime * 1f);
        else if (CurrentBehavior == Behavior.Repositioning) CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxRepositioningSpeed * MovementDirection.sqrMagnitude, Time.deltaTime * 1f);
        else { CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Time.deltaTime * 1f); }

        MovementDirection *= CurrentSpeed;
        MovementDirection.y -= 10;

        // If we aren't moving via pathfinding move via directional calculations
        if (!MovingToTargetPosition)
        {
            //AvoidLedges();
            cc.Move(MovementDirection * Time.deltaTime);
        }
    }

    /// <summary>
    /// Chooses the behavior of the enemy while taking into consideration each behavior chance
    /// </summary>
    void ChooseBehavior()
    {
        BehaviorChangeTimerForSex -= Time.deltaTime;
        BehaviorChangeTimer -= Time.deltaTime;
        BehaviorChangeTimerForStun -= Time.deltaTime;
        if (BehaviorChangeTimer > 0) return;
        if (BehaviorChangeTimerForSex > 0) return;
        if (BehaviorChangeTimerForStun > 0) return;

        Attacking = false;
        InAttackRange = false;

        // Roll for each of the behaviors
        float rolledIdleChance = Random.Range(0, ChanceToIdle);
        float rolledRepositionChance = Random.Range(0, ChanceToReposition);
        float rolledStrafeChance = Random.Range(0, ChanceToStrafe);

        // Find the highest rolled behavior
        float[] rolls = { rolledIdleChance, rolledRepositionChance, rolledStrafeChance };
        float maxValue = rolls.Max();
        int maxIndex = rolls.ToList().IndexOf(maxValue);

        // Start the chosen behavior and set the duration
        if (maxIndex == 0)
        {
            CurrentBehavior = Behavior.Idling;
            BehaviorChangeTimer = DurationToIdle;
        }
        else if (maxIndex == 1)
        {
            CurrentBehavior = Behavior.Repositioning;
            BehaviorChangeTimer = DurationToReposition;
        }
        else if (maxIndex == 2)
        {
            CurrentBehavior = Behavior.Strafing;
            BehaviorChangeTimer = DurationToStrafe;
        }
    }

    /// <summary>
    /// A second channel to disable enemy movement
    /// </summary>
    /// <param name="Duration">Duration.</param>
    public void DisableForDurationBySex(float Duration)
    {
        CurrentBehavior = Behavior.Disabled;
        MovementDirection = Vector3.zero;
        BehaviorChangeTimerForSex = Duration;
    }

    /// <summary>
    /// A third channel to disable enemy movement
    /// </summary>
    /// <param name="Duration">Duration.</param>
    public void DisableForDurationByStun(float Duration)
    {
        CurrentBehavior = Behavior.Disabled;
        MovementDirection = Vector3.zero;
        BehaviorChangeTimerForStun = Duration;
    }

    public void TakeHit(float disableDuration)
    {
       TakeHit(disableDuration, Vector3.zero);
    }

    public void TakeHit(float disableDuration, Vector3 knockback)
    {
       if (knockback != Vector3.zero)
       {
           Knockback(knockback, 1.0f, true);
       }
       DisableForDuration(disableDuration);

    }

    public bool isStunned()
    {
        return BehaviorChangeTimerForStun > 0;
    }
    public void Defeat()
    {
       BehaviorChangeTimer = float.MaxValue;
       isDefeated = true;
       CurrentBehavior = Behavior.Disabled;
    }

    #region == Pathfinding ==
    /// <summary>
    /// Sets the pathfinding target location and enables pathfinding movement
    /// </summary>
    /// <param name="TargetPosition"></param>
    void SetPathfindingLocation(Vector3 TargetPosition)
    {
        TargetDestination = TargetPosition;
        MovingToTargetPosition = true;
    }

    /// <summary>
    /// Disables pathfinding movement
    /// </summary>
    void DisablePathfinding()
    {
        MovingToTargetPosition = false;
    }

    /// <summary>
    /// Updates the path to the current pathfinding target, this runs every frame
    /// </summary>
    void UpdatePath()
    {
        if (ai == null)
        {
            Debug.LogError("this is bad");
        }
        ai.destination = TargetDestination;
        ai.SearchPath();

        if (MovingToTargetPosition)
        {
            if (CurrentBehavior != Behavior.MovingToAttack)
                CurrentBehavior = Behavior.Repositioning;
            BehaviorChangeTimer = 1f;
            ai.maxSpeed = CurrentSpeed;
        }
        if ((ai.reachedDestination || Vector3.Distance(transform.position, ai.destination) <= 1.5) && CurrentBehavior != Behavior.MovingToAttack)
        {
            MovingToTargetPosition = false;
            MovingToSpecifiedLocation = false;
        }
        ai.canMove = MovingToTargetPosition;
    }

    /// <summary>
    /// Sets the movement direction toward the location provided, and returns a boolean representing if the enemy is at the location or not
    /// </summary>
    /// <param name="Location"></param>
    bool MoveToLocation(Vector3 Location)
    {
        // Set the movement direction toward the preferred distance
        Vector3 directionToLocation = Location - transform.position;
        float distanceToLocation = Vector3.Distance(Location, transform.position);
        if (distanceToLocation > 0.5f)
        {
            // Rotate towards the location
            directionToLocation.y = 0;
            Quaternion rot = Quaternion.LookRotation(directionToLocation);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 5 * Time.deltaTime);
            MovementDirection = directionToLocation;
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Sets a specific location for the enemy to move to, this overrides chasing the player
    /// </summary>
    /// <param name="location"></param>
    void MoveToSpecificLocation(Vector3 location)
    {
        MovingToSpecifiedLocation = true;
        SetPathfindingLocation(location);
    }
    #endregion

    #region == Attacking ==
    /// <summary>
    /// Puts the enemy into the attacking state for the specified duration
    /// </summary>
    /// <param name="AttackDuration">Duration of the attack animation</param>
    public void StartAttack(float AttackDuration)
    {
        CurrentAttackChargeTime = AttackDuration;
        BehaviorChangeTimer = AttackDuration;
        Attacking = true;
    }

    // todo set the time to attack
    public void SetAttackDistance(float Distance)
    {
        AttackDistance = Distance;
        CurrentBehavior = Behavior.MovingToAttack;
        BehaviorChangeTimer = 3; // 3 seconds to move into position
    }

    // original style GREAT FOR RANGE ATTACKS!
    /// <summary>
    /// Moves to the current attack range for the enemy with slight buffer to avoid jitter
    /// </summary>
    public void MoveToAttackDistanceThenStop()
    {
        // Set the movement direction toward the attack distance direction
        float distanceToPlayer = Vector3.Distance(TargetPlayer.transform.position, transform.position);
        if (distanceToPlayer * 0.95f > AttackDistance)
        {
            InAttackRange = false;
            SetPathfindingLocation(TargetPlayer.transform.position);
            MovementDirection = ai.steeringTarget - transform.position;
        }
        else
        {
            InAttackRange = true;
            DisablePathfinding();
            if (!Attacking)
            {
                // call to the attack
                float warningTime = enemyActionManager.StartRandomAttack();
                StartAttack(warningTime);
            }
        }
    }

    /// <summary>
    /// Moves to the current attack range for the enemy with slight buffer to avoid jitter
    /// </summary>
    public void MoveToAttackDistance()
    {
        // Set the movement direction toward the attack distance direction
        float distanceToPlayer = Vector3.Distance(TargetPlayer.transform.position, transform.position);
        if (distanceToPlayer * 0.95f > AttackDistance)
        {
            InAttackRange = false;
            SetPathfindingLocation(TargetPlayer.transform.position);
            MovementDirection = ai.steeringTarget - transform.position;
        }
        else
        {
            InAttackRange = true;

            if (!Attacking)
            {
                // call to the attack
                float warningTime = enemyActionManager.StartRandomAttack();
                StartAttack(warningTime);
            }
        }
    }
    public void GrabAndMoveObject(GameObject ObjectToGrab, Vector3 Destination)
    {
        StopAllCoroutines();
        StartCoroutine(grabAndMoveObject(ObjectToGrab, Destination));
    }

    IEnumerator grabAndMoveObject(GameObject ObjectToGrab, Vector3 Destination)
    {
        // Move to the specified object
        MoveToSpecificLocation(ObjectToGrab.transform.position);
        while (Vector3.Distance(transform.position, ObjectToGrab.transform.position) > 2)
        {
            yield return null;
        }

        // Parent the object to the enemy then make it move to the destination
        ObjectToGrab.transform.parent = transform;
        MoveToSpecificLocation(Destination);
    }
    #endregion

    #region == Non-Pathfinding Movement ==
    public void RestartMovement()
    {
        Attacking = false;
    }

    /// <summary>
    /// Disables the enemy movement for the specified duration
    /// </summary>
    /// <param name="Duration">Duration.</param>
    public void DisableForDuration(float Duration)
    {
        CurrentBehavior = Behavior.Disabled;
        MovementDirection = Vector3.zero;
        BehaviorChangeTimer = Duration;
    }

    /// <summary>
    /// Finds the preferred range for the enemy, 
    /// somewhere in between min and max distances
    /// </summary>
    void MoveToPreferredDistance()
    {
        // Adjust the preferred distance if the timer is up
        PreferredDistanceTimer -= Time.deltaTime;
        if (PreferredDistanceTimer <= 0)
        {
            PreferredDistance = Random.Range(MinimumDistanceToPlayer +1, MaximumDistanceToPlayer - 1);
            PreferredDistanceTimer = 0.5f;
        }

        // Set the movement direction toward the preferred distance
        float distanceToPlayer = Vector3.Distance(TargetPlayer.transform.position, transform.position);
        if (distanceToPlayer * 0.95f > PreferredDistance)
        {
            CloserThanPreferredTimer = 1f;
            MovementDirection = TargetPlayer.transform.position - transform.position;
        }
        else if (distanceToPlayer * 0.95f < PreferredDistance)
        {
            CloserThanPreferredTimer -= Time.deltaTime;
            if (CloserThanPreferredTimer <= 0)
                MovementDirection = -(TargetPlayer.transform.position - transform.position);
        }
    }
    #endregion

    #region == Helper Functions ==
    void RotateTowardTarget(float speed)
    {
        Vector3 dir = TargetPlayer.transform.position - transform.position;
        dir.y = 0; // keep the direction strictly horizontal
        Quaternion rot = Quaternion.LookRotation(dir);
        // slerp to the desired rotation over time
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, speed * Time.deltaTime);
    }

    public Vector2 Direction(Vector3 OtherObject)
    {
        int front;
        int right;
        float tolerance = 0.15f;
        Vector3 tmmp = new Vector3(transform.position.x, transform.position.y + (GetComponent<CharacterController>().height / 2), transform.position.z);
        if (Vector3.Dot(transform.forward, OtherObject - tmmp) < -tolerance) front = -1;
        else if (Vector3.Dot(transform.forward, OtherObject - tmmp) > tolerance) front = 1;
        else front = 0;

        if (Vector3.Dot(transform.right, OtherObject - tmmp) < -tolerance) right = -1;
        else if (Vector3.Dot(transform.right, OtherObject - tmmp) > tolerance) right = 1;
        else right = 0;

        return new Vector2(right, front);
    }
    
    /// <summary>
    /// Attempts to detect a ledge based on the intended movement
    /// </summary>
    /// <returns></returns>
    void AvoidLedges()
    {
        Vector3 center = transform.position;
        float radius = cc.radius * 3;
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius) + center;
            Vector3 middle = pos;
            middle.y += cc.height / 2;

            Debug.DrawRay(middle, -Vector3.up * (cc.height * 0.6f), Color.white);
            RaycastHit[] hits = Physics.RaycastAll(middle, -Vector3.up, cc.height * 0.6f);
            if (hits.Length <= 0)
            {
                middle.y = center.y;
                Vector3 oppositeDirection = -(middle - center);
                oppositeDirection.y = MovementDirection.y;
                MovementDirection = oppositeDirection;
                return;
            }
        }
    }
    #endregion

    #region == Enemy Avoidance == 
    /// <summary>
    /// Attempts to move away from the position provided
    /// </summary>
    /// <param name="otherPos"></param>
    public void MoveAwayFrom(Vector3 otherPos)
    {
        // Rotate around the player
        Vector3 moveDir = Vector3.zero;

        Vector3 enemyDir = Direction(otherPos);
        if (enemyDir.x < 0)
            moveDir = transform.right;
        else
            moveDir = -transform.right;
        moveDir = moveDir.normalized;

        DisablePathfinding();
        MovementDirection = moveDir;
    }

    /// <summary>
    /// Looks for nearby enemies and trys to avoid them
    /// </summary>
    public void AvoidOtherEnemies()
    {
        // Raycast to find nearby enemies
        Vector3 pos = transform.transform.position;
        pos.y += cc.height / 2;
        float radius = cc.radius;
        RaycastHit[] hits = Physics.SphereCastAll(pos, radius * 2, Vector3.forward, 0.01f);
        bool foundEnemyToAvoid = false;
        if (hits.Length > 0)
        {
            foreach (RaycastHit hit in hits)
            {

                if (!hit.transform.IsChildOf(gameObject.transform.root))
                {
                    // Enemies
                    if (hit.transform.root.tag == "Enemy")
                    {
                        foundEnemyToAvoid = true;
                        if (avoidanceTimer >= 1f)
                        {
                            avoidanceTimer = 0;
                            
                            int chosenBehavior = Random.Range(1, 4);
                            if (chosenBehavior == 1) { CurrentBehavior = Behavior.Repositioning; BehaviorChangeTimer = 1f; }
                            if (chosenBehavior == 2) { CurrentBehavior = Behavior.Idling; BehaviorChangeTimer = 2f; }
                            if (chosenBehavior == 3) { CurrentBehavior = Behavior.Strafing; BehaviorChangeTimer = 0.5f; }
                            MoveAwayFrom(hit.transform.root.position);
                            break;
                        }
                    }
                }
            }
        }
        if (foundEnemyToAvoid) avoidanceTimer += Time.deltaTime;
        else avoidanceTimer = 0;
    }
    #endregion

    #region == Knockback ==
    public void Knockback(Vector3 KnockbackForce, float Duration, bool ReduceOverDuration)
    {
        DisableForDuration(Duration);
        StartCoroutine(ApplyKnockback(KnockbackForce, Duration, ReduceOverDuration)); 
    }
    IEnumerator ApplyKnockback(Vector3 KnockbackForce, float Duration, bool ReduceOverDuration)
    {
        float exitTime = Duration;
        while (exitTime > 0)
        {
            exitTime -= Time.deltaTime;
            if (ReduceOverDuration)
            {
                float lerpAmount = 1 - (exitTime / Duration);
                Vector3.Lerp(KnockbackForce, Vector3.zero, lerpAmount); 
            }
            Vector3 forwardForce = transform.forward * KnockbackForce.z;
            Vector3 horizontalForce = transform.right * KnockbackForce.x;
            Vector3 relativeForce = forwardForce + horizontalForce;
            relativeForce.y = KnockbackForce.y;
            if (KnockbackForce.y <= 0) relativeForce.y -= 10;
            DisablePathfinding();
            cc.Move(relativeForce * Time.deltaTime);
            MovementDirection = relativeForce * 4;
            yield return null;
        }  
    }
    #endregion

    #region == React to Hit == 

    /// <summary>
    /// Gets the relative direction (Left, Right, Forward, Back) of a hit based on its direction
    /// </summary>
    /// <param name="AttacksDirection"></param>
    Vector2 GetDirectionOfHit(Vector3 AttacksDirection)
    {
        Vector2 FinalDir = Vector2.zero;
        Transform chest = transform;

        // If the enemy is using humanoid rig, use its chest bone for a more accurate direction
        if (GetComponent<Animator>().isHuman)
            chest = GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Chest);
        
        // Detect if the attack is coming from the left or right
        Vector3 cross = Vector3.Cross(chest.forward, AttacksDirection);
        float dir = Vector3.Dot(cross, chest.up);
        if (dir > 0.0f)
            FinalDir.x = 1.0f;
        else if (dir < 0.0f)
            FinalDir.x = -1.0f;
        else
            FinalDir.x = 0.0f;

        // Detect if the attack is coming from in front or behind
        if (Vector3.Dot(transform.TransformDirection(Vector3.forward), -AttacksDirection) < 0)
            FinalDir.y = 1;
        else
            FinalDir.y = -1;

        return FinalDir;
    }

    public void ReactToHit(Vector3 HitPosition, bool HeavyHit)
    {
        Vector2 DirectionOfHit = GetDirectionOfHit(HitPosition);
        
        // Hit From Left
        if(DirectionOfHit.x < 0 && DirectionOfHit.y == 0) {
            if(!HeavyHit) GetComponent<Animator>().Play("Hit Left Light");
            else GetComponent<Animator>().Play("Hit Left Strong");
        }
        // Hit From Back Left
        if (DirectionOfHit.x < 0 && DirectionOfHit.y < 0)
        {
            if (!HeavyHit) GetComponent<Animator>().Play("Hit Left Light");
            else GetComponent<Animator>().Play("Hit Left Strong");
        }
        // Hit From Front Left
        if (DirectionOfHit.x < 0 && DirectionOfHit.y > 0)
        {
            if (!HeavyHit) GetComponent<Animator>().Play("Hit Left Light");
            else GetComponent<Animator>().Play("Hit Left Strong");
        }

        //Hit From Right
        if (DirectionOfHit.x > 0 && DirectionOfHit.y == 0) {
            if (!HeavyHit) GetComponent<Animator>().Play("Hit Right Light");
            else GetComponent<Animator>().Play("Hit Right Strong");
        }
        // Hit From Back Right
        if (DirectionOfHit.x > 0 && DirectionOfHit.y < 0) {
            if (!HeavyHit) GetComponent<Animator>().Play("Hit Right Light");
            else GetComponent<Animator>().Play("Hit Right Strong");
        }
        // Hit From Front Right
        if (DirectionOfHit.x > 0 && DirectionOfHit.y > 0) {
            if (!HeavyHit) GetComponent<Animator>().Play("Hit Right Light");
            else GetComponent<Animator>().Play("Hit Right Strong");
        }

        // Hit From Back
        if (DirectionOfHit.y < 0 && DirectionOfHit.x == 0) {
            GetComponent<Animator>().Play("Hit Center");
        }
        // Hit From Front
        if (DirectionOfHit.y > 0 && DirectionOfHit.x == 0) {
            GetComponent<Animator>().Play("Hit Center");
        }
    }
    #endregion
}
