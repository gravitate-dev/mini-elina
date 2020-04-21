﻿using Animancer;
using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

/* Copyright High Latency Games - All Rights Reserved
 * This License grants to the END-USER a non-exclusive, worldwide, and perpetual license to this file and its contents to integrate only as 
 * incorporated and embedded components of electronic games and interactive media and distribute such electronic game and interactive media. 
 * END-USER may otherwise not reproduce, distribute, sublicense, rent, lease or lend this file or its contents.
 * Written by Lee Griffiths <leegriffithsdesigns@gmail.com>, April 9, 2019
 */

public class EnemyLogic : MonoBehaviour
{
    public bool HitsInteruptStun;
    public bool Stunned;
    [HideInInspector] public GameObject TargetPlayer;
    Vector3 SpawnPoint;
    Vector3 TargetDestination;
    [HideInInspector] public Vector3 MovementDirection;
    [HideInInspector] public Vector3 BlendedMovementDirection;
    CharacterController cc;
    RagdollEnabler rd;
    IAstarAI ai;

    public UnityEvent OnStartChargeUp;
    public UnityEvent OnStopChargeUp;

    [Header("Enemy type: 0 = Ranged, 1 = Melee")]
    [Range(0, 1)] public int EnemyType;
    [Tooltip("Whether this Enemy has strafing animations configured, if this is set to false the enemy will turn towards their strafe direction instead.")]
    public bool HasStrafingAnimations;

    float Acceleration;
    [Header("Movement Speeds")]
    [Tooltip("The maximum speed the enemy will reach while Chasing.")]
    [Range(0, 20)] public float MaxChasingSpeed;
    [Tooltip("The maximum speed the enemy will reach while Repositioning.")]
    [Range(0, 20)] public float MaxRepositioningSpeed;
    [Tooltip("The maximum speed the enemy will reach while Strafing.")]
    [Range(0, 20)] public float MaxStrafingSpeed;
    [Tooltip("The maximum speed the enemy will reach while Moving into Attack Range.")]
    [Range(0, 20)] public float MaxMovingToAttackSpeed;
    [Tooltip("The percent of the Current speed to use if the enemy detects an incoming attack. Setting this to a lower percent will make it easier to land hits on enemies.")]
    [Range(0, 200)] public float PercentSpeedWhileBeingAttacked;
    [Tooltip("The maximum distance from the enemy the player can be to influence the enemy speed while attacking.")]
    public float MaxDistanceForSpeedChange;
    [HideInInspector] public float CurrentSpeed;

    [Header("AI Behavior")]
    public Behavior CurrentBehavior;
    float QueuedDistance = 0;
    float BehaviorChangeTimer = 0;
    float StrafeDirectionTimer = 0;
    float StrafeRepositionTimer = 0;
    float AttackDistance = 0;
    float PreferredDistance = 0;
    float PreferredDistanceTimer = 0;
    float CloserThanPreferredTimer = 0;
    float avoidanceTimer = 0;
    float ChaseTimer = 0;
    float BehaviourWaitTimer = 0;
    float DropAggroTimer = 0;
    float StoredCurrentSpeed = -1000;
    bool StrafingRight = false;
    bool MovementInfluenced = false;
    public bool ChargingAttack = false;
    public float ChargingAttackDuration = 0;
    [HideInInspector] public bool MovingToTargetPosition = false;
    [HideInInspector] public bool MovingToSpecifiedLocation = false;
    [HideInInspector] public bool Attacking = false;
    public float StoredAttackDuration = 0;
    public float AttackDuration = 0;
    [HideInInspector] public float AttackCooldownTimer = 0;
    [HideInInspector] public float DistanceFromPlayer = 0;
    [HideInInspector] public bool Aggro = false;
    [HideInInspector] public bool InAttackRange = false;

    [Tooltip("The range at which the enemy will become aggro from.")]
    public float TargetingRange;
    [Tooltip("The distance the enemy will alert nearby enemies when being aggro.")]
    public float AlertNearbyEnemiesRange;
    [Tooltip("The Maximum distance from the player before the enemy begins Chasing.")]
    public float MaximumDistanceToPlayer;
    [Tooltip("The Minumum distance from the player before the enemy begins Moving Away.")]
    public float MinimumDistanceToPlayer;
    [Tooltip("The Maximum time the enemy can be outside of the targeting range before dropping Aggro.")]
    [Range(1, 10)] public float TimeUntilAggroDrop;
    [Tooltip("The Delay until the enemy begins chasing the player.")]
    [Range(0, 10)] public float DelayToChase;
    [Tooltip("The amount of time the enemy should idle in between switching behaviors.")]
    [Range(0, 10)] public float IdleTimeBetweenBehaviors;
    [Tooltip("The chance the enemy has to idle in place.")]
    [Range(0, 1)] public float ChanceToIdle;
    [Tooltip("The duration the enemy will idle in place.")]
    [Range(0, 10)] public float DurationToIdle;
    [Tooltip("The chance the enemy has begin strafing around the player.")]
    [Range(0, 1)] public float ChanceToStrafe;
    [Tooltip("The duration the enemy will strafe around the player.")]
    [Range(0, 10)] public float DurationToStrafe;
    [Tooltip("The time between changing strafing directions.")]
    [Range(0, 10)] public float StrafeDirectionChangeTime;
    [Tooltip("Whether or not the enemy should also move diagonally while strafing.")]
    public bool RepositionsWhileStrafing;
    [Tooltip("The chance the enemy has to randomly move closer or further from the player.")]
    [Range(0, 1)] public float ChanceToReposition;
    [Tooltip("The duration the enemy will randomly move closer or further from the player.")]
    [Range(0, 10)] public float DurationToReposition;


    [Header("Avoidance Behaviour")]
    [Tooltip("The distance at which an enemy will attempt to avoid other enemies.")]
    [Range(0, 10)] public float RangeToBeginAvoiding;
    [Tooltip("The duration an enemy will wait until trying to move away from other enemies that are too close.")]
    [Range(0, 10)] public float BufferTimeTilAvoiding;


    [Header("Custom Behaviour")]
    [Tooltip("Whether or not the enemy will perform the custom behaviors in the list of custom behaviors.")]
    public bool CanPerformCustomBehaviors;
    [Tooltip("The list of custom behaviors the enemy can choose from, or be overriden by.")]
    public List<CustomEnemyBehavior> CustomBehaviors;
    bool PerformingCustomBehavior = false;
    CustomEnemyBehavior CurrentCustomBehavior;

    [Header("Attacks")]
    [Tooltip("The minimum time that must pass between attacks regardless of each individual attacks cooldown.")]
    [Range(0, 30)] public float MinimumTimeBetweenAttacks;
    [Tooltip("The distance from the player the enemy must be to begin performing ranged attacks. If this is set to 0 or if all melee attacks are on cooldown then the enemy will move into ranged attack distance anyways.")]
    public float RangedAttackDistance;
    [Tooltip("The list of attacks the enemy can choose from.")]
    public List<Attack> Attacks;
    public Attack SelectedAttack = null;

    private AstarPath astarpath;

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
        cc = GetComponent<CharacterController>();
        ai = GetComponent<IAstarAI>();
        //rd = GetComponent<RagdollEnabler>();
        TargetPlayer = IAmElina.ELINA;
        PreferredDistance = MaximumDistanceToPlayer / 2;
        SpawnPoint = transform.position;
        astarpath = GameObject.Find("AStar").GetComponent<AstarPath>();
        foreach (CustomEnemyBehavior customEnemyBehavior in CustomBehaviors)
        {
            customEnemyBehavior.Enemy = this;
        }
    }

    void Update()
    {
        // Only reset the movement if it isn't being influenced externally
        if (!MovementInfluenced)
        {
            MovementDirection = Vector3.zero;
        }

        ProgressAttack();
        if (!Attacking)
        {
            DecreaseAttackCooldowns();
        }

        // If the player is not in range, do nothing
        if (TargetPlayer == null || !cc.enabled || (rd != null && (rd.state != RagdollEnabler.CurrentState.Enabled || rd.animRagdollFlag)))
        {
            ai.canMove = false;
            BlendedMovementDirection = Vector3.Lerp(BlendedMovementDirection, MovementDirection, Time.deltaTime * 8);
            return;
        }

        // Update the pathfinding
        UpdatePath();

        if (!Aggro && Vector3.Distance(TargetPlayer.transform.position, transform.position) < TargetingRange)
        {
            Aggro = true;
            AlertNearbyEnemies();
        }

        if (!Aggro && !MovingToSpecifiedLocation)
        {
            if (Vector3.Distance(transform.position, SpawnPoint) > 2 && DropAggroTimer >= TimeUntilAggroDrop)
            {
                SetPathfindingLocation(SpawnPoint);
                Vector3 dir = ai.steeringTarget - transform.position;
                dir.y = 0;
                Quaternion rot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 20 * Time.deltaTime);
            }
            else
            {
                if (DropAggroTimer > 0)
                {
                    DropAggroTimer = 0;
                    CurrentBehavior = Behavior.Idling;
                }
                DisablePathfinding();
                MovementDirection = Vector3.zero;
                MovementDirection.y -= 10;
                cc.Move(MovementDirection * Time.deltaTime);

                // Here we could make the enemy patrol if we wanted to
            }
            return;
        }

        DistanceFromPlayer = Vector3.Distance(TargetPlayer.transform.position, transform.position);

        // Drop aggro
        if (DistanceFromPlayer > TargetingRange && !MovingToSpecifiedLocation)
        {
            DropAggroTimer += Time.deltaTime;
            if (DropAggroTimer >= TimeUntilAggroDrop)
            {
                Aggro = false;
                Attacking = false;
                ChargingAttack = false;
                CurrentBehavior = Behavior.Repositioning;
                return;
            }
        }

        ChooseBehavior();

        if (MovingToSpecifiedLocation) CurrentBehavior = Behavior.Chasing;

        if (Attacking) GetComponent<HybridAnimancerComponent>().Animator.applyRootMotion = true;
        else GetComponent<HybridAnimancerComponent>().Animator.applyRootMotion = false;

        if (Attacking || CurrentBehavior == Behavior.Disabled)
        {
            if (Attacking && ChargingAttack)
            {
                CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxMovingToAttackSpeed * 0.75f, Time.deltaTime);
                RotateTowardTarget(TargetPlayer.transform.position, 5f);
                MoveToAttackDistance();

                MovementDirection = MovementDirection.normalized;
                MovementDirection.x *= CurrentSpeed;
                MovementDirection.z *= CurrentSpeed;
                MovementDirection.y -= 10;
                BlendedMovementDirection = Vector3.Lerp(BlendedMovementDirection, MovementDirection, Time.deltaTime * 8);
            }
            if (Attacking)
                RotateTowardTarget(TargetPlayer.transform.position, SelectedAttack.RotationSpeed);
            return;
        }

        // Enemy is outside of the max range from player and needs to move closer
        if (CurrentBehavior != Behavior.MovingToAttack &&
        Vector3.Distance(TargetPlayer.transform.position, transform.position) > MaximumDistanceToPlayer && !MovingToSpecifiedLocation)
        {
            if (ChaseTimer < DelayToChase)
                ChaseTimer += Time.deltaTime;

            if (ChaseTimer >= DelayToChase)
            {
                RotateTowardTarget(TargetPlayer.transform.position, 5f);
                CurrentBehavior = Behavior.Chasing;
                BehaviorChangeTimer = 0.5f;
                MovementDirection = (ai.steeringTarget - transform.position);
                MovementDirection.y = 0;
                SetPathfindingLocation(TargetPlayer.transform.position);
            }
        }
        // Enemy is too close to the player and needs to move away
        else if (CurrentBehavior != Behavior.MovingToAttack &&
        Vector3.Distance(TargetPlayer.transform.position, transform.position) < MinimumDistanceToPlayer && !MovingToTargetPosition)
        {
            ChaseTimer = 0;
            DisablePathfinding();
            RotateTowardTarget(TargetPlayer.transform.position, 5f);
            CurrentBehavior = Behavior.Repositioning;
            BehaviorChangeTimer = 0.5f;
            MovementDirection = (transform.position - TargetPlayer.transform.position);
        }
        // Enemy is within the min / max range
        else
        {
            ChaseTimer -= Time.deltaTime;
            if (ChaseTimer < 0) ChaseTimer = 0;

            if (CurrentBehavior == Behavior.Idling)
            {
                // Do any idling action you want here, could be taunting the player
                DisablePathfinding();
                RotateTowardTarget(TargetPlayer.transform.position, 0.5f);
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
                RotateTowardTarget(TargetPlayer.transform.position, 5f);
                MoveToPreferredDistance();
            }
            else if (CurrentBehavior == Behavior.Strafing)
            {
                // Change the strafe direction if the timer is up
                DisablePathfinding();

                if (HasStrafingAnimations)
                    RotateTowardTarget(TargetPlayer.transform.position, 5f);

                StrafeDirectionTimer -= Time.deltaTime;
                if (StrafeDirectionTimer <= 0)
                {
                    StrafeDirectionTimer = StrafeDirectionChangeTime;
                    int strafeDirRoll = UnityEngine.Random.Range(0, 3);
                    StrafingRight = strafeDirRoll == 0;
                }

                // Handle repositioning while strafing
                Vector3 priorToRepositionDir = MovementDirection;
                MoveToPreferredDistance();
                Vector3 repositionDir = MovementDirection;

                // Set the movement direction relative to the strafe direction
                if (StrafingRight)
                {
                    MovementDirection = TargetPlayer.transform.position - transform.position;
                    var right = new Vector3(MovementDirection.z, MovementDirection.y, -MovementDirection.x);
                    var left = -right;
                    MovementDirection = Vector3.Lerp(priorToRepositionDir, right, Time.deltaTime * 5);
                }
                else
                {
                    MovementDirection = TargetPlayer.transform.position - transform.position;
                    var right = new Vector3(MovementDirection.z, MovementDirection.y, -MovementDirection.x);
                    var left = -right;
                    MovementDirection = Vector3.Lerp(priorToRepositionDir, left, Time.deltaTime * 5);
                }

                // Only add repositioning forward/backward dir to the strafe dir only if the stars align
                if (RepositionsWhileStrafing)
                    MovementDirection += repositionDir;

                if (!HasStrafingAnimations)
                {
                    RotateTowardTarget(MovementDirection * 999, 2.5f);
                }
            }
        }

        if (!Attacking && CurrentBehavior != Behavior.MovingToAttack && CurrentBehavior != Behavior.Disabled)
            AvoidOtherEnemies();

        if (MovingToSpecifiedLocation || MovingToTargetPosition)
        {
            if (HasStrafingAnimations && TargetPlayer != null && (Vector3.Distance(transform.position, TargetPlayer.transform.position) < MaximumDistanceToPlayer))
            {
                Vector3 dir = ai.steeringTarget - transform.position;
                dir.y = 0;
                MovementDirection = dir;
                RotateTowardTarget(TargetPlayer.transform.position, 2.5f);
            }
            else
            {
                Vector3 dir = ai.steeringTarget - transform.position;
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    Quaternion rot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rot, 2 * Time.deltaTime);
                }
                
                MovementDirection = dir;
            }
        }

        MovementDirection = MovementDirection.normalized;
        Vector3 SpeedScaling = MovementDirection;
        SpeedScaling.y = 0;
        if (CurrentBehavior == Behavior.Chasing) CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxChasingSpeed, Time.deltaTime);
        else if (CurrentBehavior == Behavior.MovingToAttack) CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxMovingToAttackSpeed, Time.deltaTime);
        else if (CurrentBehavior == Behavior.Idling) CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0f, Time.deltaTime);
        else if (CurrentBehavior == Behavior.Strafing) CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxStrafingSpeed, Time.deltaTime);
        else if (CurrentBehavior == Behavior.Repositioning) CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxRepositioningSpeed, Time.deltaTime);
        else { CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Time.deltaTime); }
        if (Attacking) { CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Time.deltaTime); }

        AdjustSpeedForIncomingAttacks();

        MovementDirection.x *= CurrentSpeed;
        MovementDirection.z *= CurrentSpeed;
        MovementDirection.y -= 10;

        // If we aren't moving via pathfinding move via directional calculations
        if (!MovingToTargetPosition)
        {
            AvoidLedges();
            BlendedMovementDirection = Vector3.Lerp(BlendedMovementDirection, MovementDirection, Time.deltaTime * 8);
            cc.Move(BlendedMovementDirection * Time.deltaTime);
        }
        else
        {
            BlendedMovementDirection = Vector3.Lerp(BlendedMovementDirection, MovementDirection, Time.deltaTime * 8);
        }
    }

    void AlertNearbyEnemies()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, AlertNearbyEnemiesRange);
        foreach (Collider hit in hitColliders)
        {
            if (hit.transform.root.tag == "Enemy" && hit.GetComponent<EnemyLogic>() != null)
            {
                EnemyLogic enemy = hit.GetComponent<EnemyLogic>();
                enemy.Aggro = true;
                enemy.TargetPlayer = TargetPlayer;
                enemy.DropAggroTimer = -2;
            }
        }
    }

    /// <summary>
    /// Chooses the behavior of the enemy while taking into consideration each behavior chance
    /// </summary>
    void ChooseBehavior()
    {
        BehaviorChangeTimer -= Time.deltaTime;
        if (PerformingCustomBehavior) CurrentCustomBehavior.CustomUpdate();
        CheckForBehaviorOverride();
        if (BehaviorChangeTimer > 0) return;
        Stunned = false;
        OnStopChargeUp.Invoke();
        // Increment the wait time between behaviours and only change behaviours when the time is up
        if (BehaviourWaitTimer < IdleTimeBetweenBehaviors)
        {
            BehaviourWaitTimer += Time.deltaTime;
            CurrentBehavior = Behavior.Idling;
            return;
        }
        else BehaviourWaitTimer = 0;

        Attacking = false;
        ChargingAttack = false;
        InAttackRange = false;

        // Create a key value list of behaviors and their chances
        List<Tuple<string, float>> rolls = new List<Tuple<string, float>>();
        rolls.Add(Tuple.Create("Idling", UnityEngine.Random.Range(0, ChanceToIdle)));
        rolls.Add(Tuple.Create("Repositioning", UnityEngine.Random.Range(0, ChanceToReposition)));
        rolls.Add(Tuple.Create("Strafing", UnityEngine.Random.Range(0, ChanceToStrafe)));

        // Add in the custom behaviors
        if (CanPerformCustomBehaviors)
        {
            foreach (CustomEnemyBehavior behavior in CustomBehaviors)
            {
                float rolledChance = UnityEngine.Random.Range(0, behavior.ChanceToPerformBehavior);
                rolls.Add(Tuple.Create(behavior.BehaviorName, rolledChance));
            }
        }

        string ChosenBehaviorName = rolls.OrderByDescending(t => t.Item2).First().Item1;

        PerformingCustomBehavior = false;

        // Start the chosen behavior and set the duration
        if (ChosenBehaviorName == "Idling")
        {
            CurrentBehavior = Behavior.Idling;
            BehaviorChangeTimer = DurationToIdle;
        }
        else if (ChosenBehaviorName == "Repositioning")
        {
            CurrentBehavior = Behavior.Repositioning;
            BehaviorChangeTimer = DurationToReposition;
        }
        else if (ChosenBehaviorName == "Strafing")
        {
            CurrentBehavior = Behavior.Strafing;
            BehaviorChangeTimer = DurationToStrafe;
        }
        else
        {
            // Find the chosen CustomBehavior
            foreach (CustomEnemyBehavior behavior in CustomBehaviors)
            {
                if (ChosenBehaviorName == behavior.BehaviorName)
                {
                    BehaviorChangeTimer = behavior.DurationToPerformBehavior;
                    CurrentBehavior = Behavior.Disabled;
                    CurrentCustomBehavior = behavior;
                    PerformingCustomBehavior = true;
                    break;
                }
            }
        }
    }

    void CheckForBehaviorOverride()
    {
        if (CanPerformCustomBehaviors)
            foreach (CustomEnemyBehavior behavior in CustomBehaviors)
            {
                if (behavior.OverridesOtherBehaviors && behavior.CheckOverride())
                {
                    // The behavior has met the criteria required to override the decision making and should become the current custom behavior
                    behavior.CustomUpdate();
                }
            }
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
        ai.destination = TargetDestination;
        if (!ai.pathPending)
            ai.SearchPath();

        // Not able to reach path so pick an area nearby
        //if (!Pathfinding.PathUtilities.IsPathPossible(astarpath.GetNearest(transform.position).node, astarpath.GetNearest(TargetDestination).node))
        //{
        //    float radius = UnityEngine.Random.Range(0, 5);
        //    Vector3 originPoint = transform.position;
        //    originPoint.x += UnityEngine.Random.Range(-radius, radius);
        //    originPoint.z += UnityEngine.Random.Range(-radius, radius);
        //    MoveToSpecificLocation(originPoint);
        //}

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
        PerformingCustomBehavior = false;
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
        BehaviorChangeTimer = AttackDuration + SelectedAttack.ChargeUpTime;
        StoredAttackDuration = AttackDuration;
        PerformingCustomBehavior = false;
        Attacking = true;

        CurrentSpeed = 0;
        MovementDirection = Vector3.zero;
        BlendedMovementDirection = Vector3.zero;

        foreach (Attack attk in Attacks)
        {
            if (attk.AnimationName == SelectedAttack.AnimationName)
            {
                attk.CurrentCooldown = attk.Cooldown;
                ChargingAttack = true;
                ChargingAttackDuration = attk.ChargeUpTime;
                OnStartChargeUp.Invoke();
                //GetComponent<Animator>().Play("Idle");
                break;
            }
        }

        // Add the minimum time between all attacks
        foreach (Attack attk in Attacks) attk.CurrentCooldown += MinimumTimeBetweenAttacks;

        // Shuffle the attacks so we get a different one next time
        Attacks = Attacks.OrderBy(a => Guid.NewGuid()).ToList();
    }

    public void SetAttackDistance(float Distance)
    {
        AttackDistance = Distance;
        CurrentBehavior = Behavior.MovingToAttack;
        BehaviorChangeTimer = 30;
        PerformingCustomBehavior = false;
    }

    /// <summary>
    /// Moves to the current attack range for the enemy with slight buffer to avoid jitter
    /// </summary>
    float switchToRangeBuffer = 0;
    public void MoveToAttackDistance()
    {
        // Set the movement direction toward the attack distance direction
        float distanceToPlayer = Vector3.Distance(TargetPlayer.transform.position, transform.position);

        if (!Attacking)
        {
            if (!SelectedAttack.IsRangedAttack && distanceToPlayer >= RangedAttackDistance)
            {
                switchToRangeBuffer += Time.deltaTime;
                if (switchToRangeBuffer > 0.5f)
                {
                    ChooseAttack();
                    switchToRangeBuffer = 0;
                }
            }
            else
            {
                switchToRangeBuffer = 0;
            }
        }

        if (distanceToPlayer * 0.95f > AttackDistance)
        {
            InAttackRange = false;
            SetPathfindingLocation(TargetPlayer.transform.position);
            MovementDirection = ai.steeringTarget - transform.position;
        }
        else
        {
            if (!Attacking)
            {
                InAttackRange = true;
                DisablePathfinding();
                // Get attack length here
                GetComponent<HybridAnimancerComponent>().SetFloat("Attack Speed", SelectedAttack.AttackSpeed);
                float attackDuration = GetAttackAnimationDuration(SelectedAttack.AnimationName);
                StartAttack(attackDuration);
            }
        }
    }

    [System.Serializable]
    public class Attack
    {
        [Tooltip("Whether the attack can be interupted or not.")]
        public bool CanBeInterupted = true;
        public bool disabled;
        [Tooltip("The name of the animation to play for the attack. IMPORTANT: The animation STATE and the name of the animation CLIP MUST be named the same.")]
        public string AnimationName;
        [Tooltip("The amount of damage the attack will deal.")]
        public float Damage;
        [Tooltip("The Minimum amount of damage to the players guard metre the attack deals.")]
        public float MinGuardBreak;
        [Tooltip("The Maximum amount of damage to the players guard metre the attack deals.")]
        public float MaxGuardBreak;
        [Tooltip("The speed the enemy should rotate towards the player while attacking. Set this to 0 if you do not wish to make the enemy rotate.")]
        [Range(0, 100)] public float RotationSpeed;
        [Tooltip("The speed of the attack. This adjusts the animation playback speed to dynamically change an attacks speed.")]
        [Range(1, 2)] public float AttackSpeed;
        [Tooltip("The distance the enemy can be from the player before using this attack.")]
        [Range(1, 20)] public float MinimumDistance;
        [Tooltip("How long the attack should go on cooldown after usage.")]
        [Range(1, 20)] public float Cooldown;
        [Tooltip("Charge up time.")]
        [Range(0, 20)] public float ChargeUpTime;
        [Tooltip("The name of the animation to play while charging. IMPORTANT: The animation STATE and the name of the animation CLIP MUST be named the same.")]
        public string ChargingAnimationName;
        [Tooltip("Whether the attack is ranged or not.")]
        public bool IsRangedAttack;
        [Tooltip("The projectile prefab to spawn when attacking.")]
        public GameObject ProjectilePrefab;
        [Tooltip("The playback point of the animation to spawn the projectile.")]
        [Range(0, 1)] public float ProjectileSpawnTime;
        [Tooltip("The position where the projectile should be spawned.")]
        public Transform ProjectileSpawnPoint;
        [HideInInspector] public float CurrentCooldown;
        [HideInInspector] public bool ProjectileSpawned;
    }

    public void ProgressAttack()
    {
        if (Attacking && ChargingAttack)
        {

            if (ChargingAttackDuration > 0)
            {
                if (!string.IsNullOrWhiteSpace(SelectedAttack.ChargingAnimationName))
                    GetComponent<HybridAnimancerComponent>().CrossFade(SelectedAttack.ChargingAnimationName, 0.1f);
                ChargingAttackDuration -= Time.deltaTime;
            }

            // Don't touch this
            if (ChargingAttackDuration <= 0)
            {
                ChargingAttack = false;
                DisablePathfinding();
                CurrentSpeed = 0;

                GetComponent<HybridAnimancerComponent>().CrossFade(SelectedAttack.AnimationName, 0.1f);
            }
        }
        if (Attacking && !ChargingAttack)
        {
            OnStopChargeUp.Invoke();
            AttackDuration += Time.deltaTime;

            // Detect and release projectile
            if (!SelectedAttack.ProjectileSpawned && SelectedAttack.ProjectilePrefab != null && SelectedAttack.ProjectileSpawnPoint != null)
            {
                // Spawn the projectile
                if (AttackDuration / StoredAttackDuration >= SelectedAttack.ProjectileSpawnTime)
                {
                    SelectedAttack.ProjectileSpawned = true;
                    Vector3 TargetPosition = TargetPlayer.transform.position;

                    float height = 0;
                    if (TargetPlayer.GetComponent<CapsuleCollider>())
                    {
                        height = TargetPlayer.GetComponent<CapsuleCollider>().height;
                    }
                    else
                    {
                        height = TargetPlayer.GetComponent<CharacterController>().height;
                    }
                    TargetPosition.y += height / 2;
                    GameObject tmp = Instantiate(SelectedAttack.ProjectilePrefab, SelectedAttack.ProjectileSpawnPoint.position, Quaternion.LookRotation(TargetPosition - SelectedAttack.ProjectileSpawnPoint.position));

                    tmp.GetComponent<Projectile>().CastedBy = gameObject;
                    /*tmp.GetComponent<Spell>().CastedBy = gameObject;
                    tmp.GetComponent<Spell>().CheckAugments();*/
                }
            }
        }
        else
        {
            AttackDuration = 0;
        }
    }

    public bool AllAttacksOnCooldown()
    {
        foreach (Attack attack in Attacks)
        {
            if (attack.disabled)
                continue;

            if (attack.CurrentCooldown <= 0) return false;
            else attack.ProjectileSpawned = false;
        }
        return true;
    }

    public void ChooseAttack()
    {
        Attack possibleMeleeAttack = null;
        Attack possibleRangedAttack = null;
        foreach (Attack attack in Attacks)
        {
            if (attack.disabled)
                continue;
            if (attack.CurrentCooldown <= 0)
            {
                // We have found a possible ranged attack, store it for later
                if (possibleRangedAttack == null && attack.IsRangedAttack)
                    possibleRangedAttack = attack;
                // We have found a possible melee attack, store it for later
                else if (possibleMeleeAttack == null && !attack.IsRangedAttack)
                    possibleMeleeAttack = attack;
            }
        }

        // Choose the ranged attack first if in ranged distance
        if (possibleRangedAttack != null && Vector3.Distance(TargetPlayer.transform.position, transform.position) >= RangedAttackDistance)
        {
            SelectedAttack = possibleRangedAttack;
            return;
        }
        // If ranged isn't an option, choose the melee attack
        else if (possibleMeleeAttack != null)
        {
            SelectedAttack = possibleMeleeAttack;
            return;
        }
        // If melee attack wasn't an option, but we have a possible ranged attack but we were outside of the ranged distance fall back to that anyways.
        else if (possibleRangedAttack != null)
        {
            SelectedAttack = possibleRangedAttack;
            return;
        }
    }

    public void DecreaseAttackCooldowns()
    {
        foreach (Attack attack in Attacks)
        {
            if (attack.disabled) continue;
            if (attack.CurrentCooldown > 0) attack.CurrentCooldown -= Time.deltaTime;
        }
    }
    #endregion

    #region == Non-Pathfinding Movement ==
    public void RestartMovement()
    {
        Attacking = false;
        ChargingAttack = false;
    }

    /// <summary>
    /// Disables the enemy movement for the specified duration
    /// </summary>
    /// <param name="Duration">Duration.</param>
    public void DisableForDuration(float Duration)
    {
        OnStopChargeUp.Invoke();
        ChargingAttack = false;
        PerformingCustomBehavior = false;
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
            PreferredDistance = UnityEngine.Random.Range(MinimumDistanceToPlayer, MaximumDistanceToPlayer);
            PreferredDistanceTimer = 0.5f;
        }

        // Set the movement direction toward the preferred distance
        float distanceToPlayer = Vector3.Distance(TargetPlayer.transform.position, transform.position);
        if (distanceToPlayer * 0.9f > PreferredDistance)
        {
            CloserThanPreferredTimer = 1f;
            MovementDirection = TargetPlayer.transform.position - transform.position;
        }
        else if (distanceToPlayer * 0.9f < PreferredDistance)
        {
            CloserThanPreferredTimer -= Time.deltaTime;
            if (CloserThanPreferredTimer <= 0)
                MovementDirection = -(TargetPlayer.transform.position - transform.position);
        }
    }

    #endregion

    #region == Helper Functions ==
    void RotateTowardTarget(Vector3 TargetPosition, float speed)
    {
        Vector3 dir = TargetPosition - transform.position;
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

    void RotateTorsoTowardTarget(float speed)
    {
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

    float GetAttackAnimationDuration(string attackName)
    {
        HybridAnimancerComponent animancer = GetComponent<HybridAnimancerComponent>();

        AnimancerState state = animancer.States[attackName];
        if (state!=null && state.Clip != null)
        {
            float calculatedSpeed = SelectedAttack.AttackSpeed;
            if (calculatedSpeed >= 1) return state.Clip.length / calculatedSpeed;
            else if (calculatedSpeed < 1) return state.Clip.length * (1 + (1 - calculatedSpeed));
        } else
        {
            Debug.LogError("MISSING ATTACK ANIMATION! " + attackName + " ON " + gameObject.name);
        }
        return 0;
    }
    #endregion

    #region == Enemy Avoidance ==
    /// <summary>
    /// Attempts to detect a ledge based on the intended movement
    /// </summary>
    /// <returns></returns>
    void AvoidLedges()
    {
        Vector3 center = transform.position;
        float radius = cc.radius * 3f;
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

    /// <summary>
    /// Attempts to move away from the position provided
    /// </summary>
    /// <param name="otherPos"></param>
    public void MoveAwayFromEnemy(Vector3 otherPos)
    {
        int MaxTrys = 10;
        while (MaxTrys > 0)
        {
            float radius = UnityEngine.Random.Range(0, RangeToBeginAvoiding);
            Vector3 originPoint = transform.position;
            originPoint.x += UnityEngine.Random.Range(-radius, radius);
            originPoint.z += UnityEngine.Random.Range(-radius, radius);

            // If the new location is impossible to move to try again
            if (!Pathfinding.PathUtilities.IsPathPossible(astarpath.GetNearest(transform.position).node, astarpath.GetNearest(originPoint).node))
            {
                MaxTrys--;
                continue;
            }

            bool TooClose = false;
            float avoidanceRad = RangeToBeginAvoiding;
            RaycastHit[] hits = Physics.SphereCastAll(originPoint, avoidanceRad, Vector3.forward, 0.01f);
            if (hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (!hit.transform.IsChildOf(gameObject.transform.root))
                    {
                        // Enemies
                        if (hit.transform.root.tag == "Enemy")
                        {
                            TooClose = true;
                        }
                    }
                }
            }
            if (!TooClose)
            {
                MoveToSpecificLocation(originPoint);
                break;
            }
            MaxTrys--;
        }
    }

    /// <summary>
    /// Looks for nearby enemies and trys to avoid them
    /// </summary>
    public void AvoidOtherEnemies()
    {
        // Raycast to find nearby enemies
        Vector3 pos = transform.transform.position;
        pos.y += cc.height / 2;
        float radius = RangeToBeginAvoiding;
        RaycastHit[] hits = Physics.SphereCastAll(pos, radius, Vector3.forward, 0.01f);
        bool foundEnemyToAvoid = false;
        if (hits.Length > 0)
        {
            foreach (RaycastHit hit in hits)
            {

                if (!hit.transform.IsChildOf(gameObject.transform.root))
                {
                    // Enemies
                    if (hit.transform.root.tag == "Enemy" && hit.transform.root.GetComponent<HealthSystem>().CurrentHealth > 0)
                    {
                        foundEnemyToAvoid = true;
                        if (avoidanceTimer >= BufferTimeTilAvoiding)
                        {
                            avoidanceTimer = 0;
                            MoveAwayFromEnemy(hit.transform.root.position);
                            break;
                        }
                    }
                }
            }
        }
        if (foundEnemyToAvoid) avoidanceTimer += Time.deltaTime;
        else avoidanceTimer = 0;
    }

    /// <summary>
    /// Detects if the target player is currently attacking the enemy and applys any speed changes
    /// </summary>
    /// <returns></returns>
    public void AdjustSpeedForIncomingAttacks()
    {
        if (Vector3.Distance(TargetPlayer.transform.position, transform.position) <= MaxDistanceForSpeedChange
            // && TargetPlayer.GetComponent<MeleeHandler>().AttackDuration > 0 
            && CurrentBehavior != Behavior.Disabled
            && CurrentBehavior != Behavior.MovingToAttack
            && !Attacking)
        {
            if (StoredCurrentSpeed == -1000)
                StoredCurrentSpeed = CurrentSpeed;

            CurrentSpeed = Mathf.Lerp(CurrentSpeed, (StoredCurrentSpeed * (PercentSpeedWhileBeingAttacked / 100)), Time.deltaTime * 20f);
            MovementDirection *= (PercentSpeedWhileBeingAttacked / 100);
        }
        else
            StoredCurrentSpeed = -1000;
    }
    #endregion

    #region == Knockback ==
    public void Knockback(Vector3 KnockbackDir, float Force, float StunDuration)
    {
        if (!Stunned) DisableForDuration(10f);
        StartCoroutine(ApplyKnockback(KnockbackDir, Force, StunDuration));
    }
    IEnumerator ApplyKnockback(Vector3 KnockbackDir, float Force, float StunDuration)
    {
        MovementInfluenced = true;
        Vector3 KnockbackForce = Vector3.zero;
        if (KnockbackDir.y < 0) KnockbackDir.y = -KnockbackDir.y; // reflect down force on the ground
        KnockbackForce += KnockbackDir.normalized * Force;

        // Cancel the attack animation
        Attacking = false;
        ChargingAttack = false;
        GetComponent<Animator>().SetBool("Attacking", false);
        // store stun time for after
        float existingStunTime = 0;
        if (Stunned && !HitsInteruptStun) { existingStunTime = BehaviorChangeTimer; }
        while (KnockbackForce.magnitude > 0.2f)
        {
            DisableForDuration(10f);
            Vector3 forwardForce = transform.forward * KnockbackForce.z;
            Vector3 horizontalForce = transform.right * KnockbackForce.x;
            Vector3 relativeForce = forwardForce + horizontalForce;
            relativeForce.y = KnockbackForce.y;
            relativeForce = KnockbackForce;
            relativeForce.y -= 10f;
            DisablePathfinding();

            CurrentSpeed = Mathf.Lerp(CurrentSpeed, Mathf.Max(Mathf.Abs(relativeForce.x), Mathf.Abs(relativeForce.z)), Time.deltaTime);
            MovementDirection = relativeForce;
            BlendedMovementDirection = Vector3.Lerp(BlendedMovementDirection, MovementDirection, Time.deltaTime * 8);
            cc.Move(BlendedMovementDirection * Time.deltaTime);
            CloserThanPreferredTimer = 0;
            KnockbackForce = Vector3.Lerp(KnockbackForce, Vector3.zero, 5 * Time.deltaTime);
            yield return null;
        }
        MovementInfluenced = false;
        if (StunDuration > 0)
        {
            DisableForDuration(StunDuration);
            Stunned = true;
        }
        else
        {
            DisableForDuration(0.1f + existingStunTime);
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
        //if (GetComponent<HybridAnimancerComponent>().isHuman)
        //    chest = GetComponent<HybridAnimancerComponent>().GetBoneTransform(HumanBodyBones.Chest);

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
        if (DirectionOfHit.x < 0 && DirectionOfHit.y == 0)
        {
            if (!HeavyHit) GetComponent<HybridAnimancerComponent>().CrossFade("Hit Left Light", 0.15f);
            else GetComponent<HybridAnimancerComponent>().CrossFade("Hit Left Strong", 0.15f);
        }
        // Hit From Back Left
        if (DirectionOfHit.x < 0 && DirectionOfHit.y < 0)
        {
            if (!HeavyHit) GetComponent<HybridAnimancerComponent>().CrossFade("Hit Left Light", 0.15f);
            else GetComponent<HybridAnimancerComponent>().CrossFade("Hit Left Strong", 0.15f);
        }
        // Hit From Front Left
        if (DirectionOfHit.x < 0 && DirectionOfHit.y > 0)
        {
            if (!HeavyHit) GetComponent<HybridAnimancerComponent>().CrossFade("Hit Left Light", 0.15f);
            else GetComponent<HybridAnimancerComponent>().CrossFade("Hit Left Strong", 0.15f);
        }

        //Hit From Right
        if (DirectionOfHit.x > 0 && DirectionOfHit.y == 0)
        {
            if (!HeavyHit) GetComponent<HybridAnimancerComponent>().CrossFade("Hit Right Light", 0.15f);
            else GetComponent<HybridAnimancerComponent>().CrossFade("Hit Right Strong", 0.15f);
        }
        // Hit From Back Right
        if (DirectionOfHit.x > 0 && DirectionOfHit.y < 0)
        {
            if (!HeavyHit) GetComponent<HybridAnimancerComponent>().CrossFade("Hit Right Light", 0.15f);
            else GetComponent<HybridAnimancerComponent>().CrossFade("Hit Right Strong", 0.15f);
        }
        // Hit From Front Right
        if (DirectionOfHit.x > 0 && DirectionOfHit.y > 0)
        {
            if (!HeavyHit) GetComponent<HybridAnimancerComponent>().CrossFade("Hit Right Light", 0.15f);
            else GetComponent<HybridAnimancerComponent>().CrossFade("Hit Right Strong", 0.15f);
        }

        // Hit From Back
        if (DirectionOfHit.y < 0 && DirectionOfHit.x == 0)
        {
            GetComponent<HybridAnimancerComponent>().CrossFade("Hit Center", 0.15f);
        }
        // Hit From Front
        if (DirectionOfHit.y > 0 && DirectionOfHit.x == 0)
        {
            GetComponent<HybridAnimancerComponent>().CrossFade("Hit Center", 0.15f);
        }

        // Cancel the attack animation
        Attacking = false;
        ChargingAttack = false;
        GetComponent<HybridAnimancerComponent>().SetBool("Attacking", false);
        GetComponent<HybridAnimancerComponent>().CrossFade("Cancel Attack", 0.4f);
    }
    #endregion
#if UNITY_EDITOR
    [Header("Debug Info")]
    public bool DrawRangeDistances = false;
    public bool DrawAttackDistances = false;
    /// <summary>
    /// Visualize values used in <see cref="EnemyLogic"/>
    /// </summary>
    [CustomEditor(typeof(EnemyLogic))]
    public class EnemyLogicHandle : Editor
    {
        EnemyLogic component;
        private int labelsDrawn;
        void OnSceneGUI()
        {
            labelsDrawn = 0;
            component = (EnemyLogic)target;
            if (component == null)
            {
                return;
            }

            if (!component.DrawAttackDistances && !component.DrawRangeDistances) return;

            if (component.DrawRangeDistances)
            {
                DrawRadius(component.TargetingRange, "Target Range", Color.red);
                DrawRadius(component.AlertNearbyEnemiesRange, "Alert Range", Color.yellow);
                DrawRadius(component.MaximumDistanceToPlayer, "Max Dist Player", Color.green);
                DrawRadius(component.MinimumDistanceToPlayer, "Min Dist Player", Color.blue);
            }

            if (component.DrawAttackDistances)
            {
                foreach (Attack attack in component.Attacks)
                {
                    DrawRadius(attack.MinimumDistance, attack.AnimationName, Color.cyan);
                }
            }
        }

        private void DrawRadius(float value, string label, Color color)
        {
            Handles.color = color;
            Vector3 pos = component.transform.position;
            Handles.DrawWireArc(pos,
                component.transform.up,
                -component.transform.right,
                360,
                value);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = color;

            Handles.BeginGUI();
            Vector2 pos2D = HandleUtility.WorldToGUIPoint(pos);
            string msg = label + " = " + value;
            GUI.Label(new Rect(pos2D.x, pos2D.y + (labelsDrawn * 30) + 10, 100, 30), msg, style);
            Handles.EndGUI();

            labelsDrawn += 1;
        }
    }
#endif
}