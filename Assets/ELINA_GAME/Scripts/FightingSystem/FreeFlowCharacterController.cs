using Animancer;
using Invector.vCharacterController;
using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static PlayerSpellLibrary;

/// <summary>
/// Controls invector too!
/// </summary>
public class FreeFlowCharacterController : MonoBehaviour, FreeFlowGapListener
{
    private bool ActionsEnabled;
    private GameObject currentTarget;
    private PlayerSpellLibrary playerSpellLibrary;
    private FreeFlowMovePicker freeFlowMovePicker;
    private FreeFlowTargetChooser freeFlowEnemyPicker;
    private FreeFlowAnimatorController freeFlowAnimatorController;
    private HentaiSexCoordinator hentaiSexCoordinator;
    private vThirdPersonInput invectorControllerInput;
    private HybridAnimancerComponent animancer;
    private int GO_ID;
    private Queue<FreeFlowAttackMove> actionQueue = new Queue<FreeFlowAttackMove>();
    private List<System.Guid> disposables = new List<System.Guid>();
    private HashSet<string> lockInputReasons = new HashSet<string>();
	public bool debugMode;

    private float MAX_ASTAR_TRAVEL_TIME = 4000f;
    private float astarTravelTime;

    private int nextAction = 0;
    private int ACTION_NULL = 0;
    private int ACTION_ATTACK = 1;
    private int ACTION_COUNTER = 2;
    private int ACTION_EVADE = 3;
    private int ACTION_MAGIC_SPELL = 4;
    private float ACTION_TIMEOUT_TIME = 1f; // game will listen for 0.3 seconds to do the next action otherwise it will ignore
    private List<Spell> spells;
    private Spell chosenSpell;


    #region == Variables Victim Hit Routines ==
    public const int HIT_RESULT_NORMAL = 0;
    public const int HIT_RESULT_STUN = 2;

    public int orgasmTimesForDefeat = 3;
    private float hitDisableTime = 1;
    private int currentHits;

    [HideInInspector]
    public bool canBeAttacked;
    [HideInInspector]
    public bool canBeSexed; // meaning that cna the player be forced to switch to a new move.
    [HideInInspector]
    public bool canBeCarried;
    #endregion
    private void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();
        playerSpellLibrary = GetComponent<PlayerSpellLibrary>();
        freeFlowAnimatorController = GetComponent<FreeFlowAnimatorController>();
        invectorControllerInput = GetComponent<vThirdPersonInput>();
        freeFlowMovePicker = FindObjectOfType<FreeFlowMovePicker>();
        animancer = GetComponent<HybridAnimancerComponent>();
        freeFlowEnemyPicker = GetComponent<FreeFlowTargetChooser>();
        ai = GetComponent<IAstarAI>();

        ActionsEnabled = true;
        canBeAttacked = true;
        canBeSexed = true;
        canBeCarried = false;

        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_START_H_MOVE_LOCAL + GO_ID, (unused) =>
        {
            canBeSexed = false;
            canBeAttacked = false;
            invectorControllerInput.jumpInput.useInput = false;
            CancelInvoke();
        }));

        WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (unused) =>
        {
            canBeSexed = true;
            canBeAttacked = true;
            Invoke("AllowJump", 0.1f);
        });


        //todo set freeflow mode when appropriateGetClipByName
        animancer.SetBool("FreeFlowMode", true);
    }

    private void Start()
    {
        spells = playerSpellLibrary.GetSpells();
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    void Update()
    {
        CapturePlayersInput();

        MonitorVictimForNull();

        UpdatePath();

       

        if (ACTION_TIMEOUT_TIME < Time.time)
        {
            nextAction = ACTION_NULL;
        }
        
        if (nextAction == ACTION_NULL || actionQueue.Count > 0)
        {
            // if i am busy or doing nothing return
            return;
        }

        // while attacking do not allow sending other attacks
        if (!ActionsEnabled)
            return;

        if (nextAction == ACTION_ATTACK)
        {
            Attack();
        } else if (nextAction == ACTION_COUNTER)
        {
            Counter();
        } else if (nextAction == ACTION_MAGIC_SPELL)
        {
            MagicSpell(chosenSpell);
        } else if (nextAction == ACTION_EVADE)
        {
            Evade();
        }
        nextAction = 0;
    }

    private void MonitorVictimForNull()
    {
        if (actionQueue.Count==0)
        {
            return;
        }

        FreeFlowAttackMove attackMove = actionQueue.Peek();
        // bug fix
        // 1. start an attack for an enemy far away
        // 2. Destroy(enemyGameobject);
        // 3. Your player gets stuck unless you use this fix here.
        if (attackMove.victim == null)
        {
            Debug.Log("TROLLNABLE: Nullvictim");
            EnableActions();
            actionQueue.Clear();
            UnlockPlayerInput("FreeFlowCharacterController");
        }
    }
    #region == Capture Input ==
    private void CapturePlayersInput()
    {
        bool wasSpell = false;
        if (spells != null)
        {
            foreach (Spell spell in spells)
            {
                if (Input.GetKeyDown(spell.hotkey))
                {
                    if (spell.isOnCooldown())
                    {
                        continue;
                    }
                    wasSpell = true;
                    chosenSpell = spell;
                    break;
                }
            }
        }
        if (wasSpell)
        {
            nextAction = ACTION_MAGIC_SPELL;
            ACTION_TIMEOUT_TIME = Time.time + ACTION_TIMEOUT_TIME;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            nextAction = ACTION_ATTACK;
            ACTION_TIMEOUT_TIME = Time.time + ACTION_TIMEOUT_TIME;
        }
        else if (Input.GetMouseButtonDown(1))
        {
            nextAction = ACTION_COUNTER;
            ACTION_TIMEOUT_TIME = Time.time + ACTION_TIMEOUT_TIME;
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            nextAction = ACTION_EVADE;
            ACTION_TIMEOUT_TIME = Time.time + ACTION_TIMEOUT_TIME;
        }
    }
# endregion
    private void Attack()
    {
        UnlockPlayerInput("FreeFlowCharacterController");  // fix for a glitch where we get stuck
        GameObject target = freeFlowEnemyPicker.getTarget(FreeFlowTargetChooser.TARGET_REASON_ATTACK);
        if (target == null)
        {
            return;
        }

        FreeFlowAttackMove attackMove = freeFlowMovePicker.PickMoveRandomly(transform,target, false);
        if (attackMove == null)
        {
            return;
        }
        ActionsEnabled = false;
        currentTarget = target;
        WickedObserver.SendMessage("PlayerAttacked"); /// used by <see cref="MusicController"/>
        LockPlayerInput("FreeFlowCharacterController");
        attackMove.victimGO_ID = target.gameObject.GetInstanceID();
        attackMove.attacker = gameObject;
        attackMove.victim = target;
        
        actionQueue.Enqueue(attackMove);
    }

    private void Counter()
    {
        UnlockPlayerInput("FreeFlowCharacterController"); // fix for a glitch where we get stuck
        GameObject target = freeFlowEnemyPicker.getTarget(FreeFlowTargetChooser.TARGET_REASON_COUNTER);
        if (target == null)
        {
            return;
        }
        CounterEnemy(target);

        WickedObserver.SendMessage("PlayerAttacked"); /// used by <see cref="MusicController"/>
        

        FreeFlowAttackMove attackMove = freeFlowMovePicker.PickMoveRandomly(transform,target, true);
        if (attackMove == null)
        {
            return;
        }
        attackMove.isCounter = true;
        attackMove.victimGO_ID = target.gameObject.GetInstanceID();
        attackMove.attacker = gameObject;
        attackMove.victim = target;

        ActionsEnabled = false;
        actionQueue.Enqueue(attackMove);
        LockPlayerInput("FreeFlowCharacterController");
        currentTarget = target;
    }

    private void CounterEnemy(GameObject enemy)
    {
        EnemyLogic enemyLogic = enemy.GetComponent<EnemyLogic>();
        if (enemyLogic.ChargingAttack)
        {
            enemyLogic.ReactToHit(Vector3.zero, true);

            //OR you could do this
            //enemy.GetComponent<HealthSystem>().TakeDamage(0, -enemy.transform.forward, 1f, -enemy.transform.forward);

            enemyLogic.DisableForDuration(1);
        }
    }
    private void MagicSpell(Spell spell)
    {
        if (spell.name == "Summon Succubus")
        {
            DoSexSuccubusSpell(spell);
        }
    }
    private void DoSexSuccubusSpell(Spell spell) {
        GameObject target = freeFlowEnemyPicker.getTarget(FreeFlowTargetChooser.TARGET_REASON_SEX);
        if (target == null)
        {
            return;
        }
        playerSpellLibrary.OnCastSpell(spell);

        GameObject newSuccubus = SpecialFxRequestBuilder.newBuilder("MasterSexSuccubusClone")
                .setOwner(transform, false)
                .setOffsetPosition(new Vector3(0, 0.5f, 0) + transform.forward)
                .setOffsetRotation(new Vector3(0, 0, 0))
                .build().Play();
        newSuccubus.transform.rotation = transform.rotation;
        SexShadowClone sexShadowClone = newSuccubus.GetComponent<SexShadowClone>();
        sexShadowClone.SetTarget(target);
        sexShadowClone.MoveThenSex();
    }

    private void Evade()
    {
        //todo jump
    }

    private float calculateDelayTillNextAttack(FreeFlowAttackMove currentAttack)
    {
        return (currentAttack.victimAnimationDelay + currentAttack.attackerLockTimeAfterHit) / currentAttack.attackerAnimationSpeed;
    }


    public void onReachedDestination()
    {
        if (actionQueue.Count == 0)
        {
            return;
        }
        FreeFlowAttackMove action = actionQueue.Dequeue();

        FreeFlowAnimatorController victimFreeFlowAnimatorController = action.victim.GetComponent<FreeFlowAnimatorController>();
        FreeFlowTargetable victimTargetable = action.victim.GetComponent<FreeFlowTargetable>();
        int result = victimTargetable.hit(action);
        action.victimReactionId = result;

        // victim
        victimFreeFlowAnimatorController.startFreeFlowAttack(action);
        victimTargetable.VictimHitRoutines(action);

        // attacker
        freeFlowAnimatorController.startFreeFlowAttack(action);

        float actionTotalTime = calculateDelayTillNextAttack(action);
        // we disable the character controller during this time!
        StartCoroutine(RegainPlayerControl(actionTotalTime));
        
        MovingToTargetPosition = false;
        currentTarget = null;
        ai.canMove = false;
    }

    private IEnumerator RegainPlayerControl(float delay)
    {
        yield return new WaitForSeconds(delay);
        EnableActions();
        UnlockPlayerInput("FreeFlowCharacterController");
    }

    public void onReachedDestinationFail()
    {
        UnlockPlayerInput("FreeFlowCharacterController");
        EnableActions();
        FreeFlowAttackMove move = actionQueue.Dequeue();
        if (move == null)
        {
            return;
        }
        if (move.victim.gameObject == null)
        {
            return;
        }
        currentTarget = null;
        ai.canMove = false;
        MovingToTargetPosition = false;

        move.victim.gameObject.GetComponent<EnemyLogic>().DisableForDuration(0);
    }



    #region == Victim Routines and Health Management ==
    private void EnableActions()
    {
        ActionsEnabled = true;
    }

    private void AllowJump()
    {
        invectorControllerInput.jumpInput.useInput = true;
    }
    #endregion

    public bool IsPlayerInputLocked()
    {
        return lockInputReasons.Count != 0;
    }
    public void LockPlayerInput(string reason)
    {
        lockInputReasons.Add(reason);
        invectorControllerInput.SetLockAllInput(true);
    }

    public void UnlockPlayerInput(string reason)
    {
        bool wasLocked = lockInputReasons.Count != 0;
        lockInputReasons.Remove(reason);
        if (wasLocked && lockInputReasons.Count == 0)
        {
            invectorControllerInput.SetLockAllInput(false);
        }
    }

    #region === What Attacks can Take ===
    public bool isTargetSexable()
    {
    	// might be a bug here if i dont return the comment out line
    	// return !hentaiSexCoordinator.IsSexing()
        return true;
    }

    public bool isTargetAttackable()
    {
        return hentaiSexCoordinator.IsSexing() == false;
    }
    #endregion

    #region === Free Flow Auto Movement ===
    IAstarAI ai;
    [HideInInspector] public Vector3 MovementDirection;
    [HideInInspector] public bool MovingToTargetPosition = false;
    [HideInInspector] public bool MovingToSpecifiedLocation = false;

    private Queue<float> rollingVelocity = new Queue<float>();
    /// <summary>
    /// DETECTS ASTAR character stuck
    /// Takes a rolling average and will determine if AStar is stuck or not
    /// </summary>
    /// <param name="currentMagnitude">ai.velocity.magnitude</param>
    /// <returns>true if character is stuck</returns>
    private bool circuitBreak(float currentMagnitude)
    {
        float SAMPLE_SIZE_REQUIRED = 10;
        float REQUIRED_MAGNITUDE = 0.4f;

        rollingVelocity.Enqueue(currentMagnitude);
        if (rollingVelocity.Count == SAMPLE_SIZE_REQUIRED)
        {
            float rollingAverage = rollingVelocity.Average();
            Debug.Log("ROLLING: " +rollingAverage);
            rollingVelocity.Dequeue();
            if (rollingAverage < REQUIRED_MAGNITUDE)
            {
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Updates the path to the current pathfinding target, this runs every frame
    /// </summary>
    void UpdatePath()
    {
        if (Input.GetKey(KeyCode.H))
        {
            GetComponent<Rigidbody>().isKinematic = !GetComponent<Rigidbody>().isKinematic;
            string logstring = $@"

  currentTarget null? ""{currentTarget==null}"",
  ai.canMove ""{ai.canMove}"" 
  ai.pathPending ""{ai.pathPending}""
  ai.destination ""{ai.destination}""
";
            Debug.Log(logstring);
        }
        // only ai move if we have a target
        if (currentTarget == null)
        {
            UnlockPlayerInput("FreeFlowCharacterController");
            astarTravelTime = MAX_ASTAR_TRAVEL_TIME;
            ai.canMove = false;
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            astarTravelTime = 0;
        }
        // stuck detection
        //circuitBreak(ai.velocity.magnitude);
        astarTravelTime -= Time.deltaTime;
        // we have a target
        ai.canMove = true;
        ai.destination = currentTarget.transform.position;
        animancer.SetFloat("InputMagnitude", 1.0f);
        RotateTowardTarget(currentTarget.transform.position, 10.0f);

        if ((ai.reachedDestination || Vector3.Distance(transform.position, ai.destination) <= 1.5))
        {
            // start the attack
            onReachedDestination();

        } else if (astarTravelTime < 0)
        {
            // failed to reach target!
            onReachedDestinationFail();
        }
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

    void RotateTowardTarget(Vector3 TargetPosition, float speed)
    {
        Vector3 dir = TargetPosition - transform.position;
        dir.y = 0; // keep the direction strictly horizontal
        Quaternion rot = Quaternion.LookRotation(dir);
        // slerp to the desired rotation over time
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, speed * Time.deltaTime);
    }
    #endregion
}
