using Invector.vCharacterController;
using Invector.vCharacterController.AI;
using System.Collections.Generic;
using UnityEngine;

public class FreeFlowCharacterController : MonoBehaviour, FreeFlowGapListener
{
    public GameObject SUCCUBUS_SEX_CLONE_PREFAB;

    private FreeFlowGapCloser freeFlowGapCloser;
    private FreeFlowMovePicker freeFlowMovePicker;
    private FreeFlowTargetChooser freeFlowEnemyPicker;
    private FreeFlowAnimatorController freeFlowAnimatorController;
    private vShooterMeleeInput shooterMeleeInput;
    private Animator animator;
    private int GO_ID;
    private Queue<FreeFlowAttackMove> actionQueue = new Queue<FreeFlowAttackMove>();
    private float timeTillNextFreeFlow;
    private List<System.Guid> disposables = new List<System.Guid>();
    private FreeFlowTarget mostRecentTarget;
    private void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        freeFlowAnimatorController = GetComponent<FreeFlowAnimatorController>();
        shooterMeleeInput = GetComponent<vShooterMeleeInput>();
        freeFlowMovePicker = FindObjectOfType<FreeFlowMovePicker>();
        animator = GetComponent<Animator>();
        freeFlowGapCloser = GetComponent<FreeFlowGapCloser>();
        freeFlowEnemyPicker = GetComponent<FreeFlowTargetChooser>();

        // handle character controller state changes i have a pending invoke that i need to cancel
        disposables.Add(WickedObserver.AddListener("onStateKnockedOut:" + GO_ID, (obj) =>
        {
            WickedObserver.SendMessage("EnableInventoryWindow");
            CancelInvoke();
        }));
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj) =>
        {
            WickedObserver.SendMessage("EnableInventoryWindow");
            CancelInvoke();
        }));

        //todo set freeflow mode when appropriateGetClipByName
        animator.SetBool("FreeFlowMode", true);
    }

    private void Start()
    {
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    private int nextAction = 0;
    private int ACTION_NULL = 0;
    private int ACTION_ATTACK = 1;
    private int ACTION_COUNTER = 2;
    private int ACTION_EVADE = 3;
    private float ACTION_TIMEOUT_TIME = 1f; // game will listen for 0.3 seconds to do the next action otherwise it will ignore

    void Update()
    {

        if (Input.GetKey(KeyCode.H))
        {
            if (getMostRecentTarget() != null && getMostRecentTarget().gameObject != null )
            {
                // todo when throwing you need to disable the collider
                transform.position = getMostRecentTarget().gameObject.transform.position;
                //GetComponent<Rigidbody>().isKinematic = true;
            }
            // Center them on each other
        }

        /*if (Input.GetKey(KeyCode.H))
        {
            if (currentTarget != null)
            {
                // todo when throwing you need to disable the collider
                GetComponent<CapsuleCollider>().enabled = false;
                transform.position = currentTarget.gameObject.transform.position;
                GetComponent<Rigidbody>().isKinematic = true;
            }
            // Center them on each other
        } else if (!Input.GetKey(KeyCode.H))
        {
            // after that you need to renable it
            GetComponent<CapsuleCollider>().enabled = true;
            GetComponent<Rigidbody>().isKinematic = false;
        }*/
        if (ACTION_TIMEOUT_TIME < Time.time)
        {
            nextAction = ACTION_NULL;
        }
        if (Input.GetMouseButtonDown(0))
        {
            nextAction = ACTION_ATTACK;
            ACTION_TIMEOUT_TIME = Time.time + ACTION_TIMEOUT_TIME;
        } else if (Input.GetMouseButtonDown(1))
        {
            nextAction = ACTION_COUNTER;
            ACTION_TIMEOUT_TIME = Time.time + ACTION_TIMEOUT_TIME;
        } else if (Input.GetKeyDown(KeyCode.Space))
        {
            nextAction = ACTION_EVADE;
            ACTION_TIMEOUT_TIME = Time.time + ACTION_TIMEOUT_TIME;
        }
        if (actionQueue.Count==0 && timeTillNextFreeFlow < Time.time)
        {
            if (nextAction == ACTION_ATTACK)
            {
                Attack();
                nextAction = 0;
            } else if (nextAction == ACTION_COUNTER)
            {
                Counter();
                nextAction = 0;
            } else if (nextAction == ACTION_EVADE)
            {
                Evade();
                nextAction = 0;
            }
        }
    }
    
    private void Attack()
    {
        
        FreeFlowTarget target = freeFlowEnemyPicker.getTarget(FreeFlowTargetChooser.TARGET_REASON_ATTACK);
        if (target == null)
        {
            return;
        }
        mostRecentTarget = target;

        FreeFlowAttackMove attackMove = freeFlowMovePicker.PickMoveRandomly(target);
        if (attackMove == null)
        {
            return;
        }
        shooterMeleeInput.SetLockAllInput(true);
        attackMove.victimGO_ID = target.gameObject.GetInstanceID();
        attackMove.attacker = gameObject;
        attackMove.victim = target;

        actionQueue.Enqueue(attackMove);

        freeFlowGapCloser.MoveToTargetForAttack(attackMove, this);
    }

    private void Counter()
    {
        FreeFlowTarget target = freeFlowEnemyPicker.getTarget(FreeFlowTargetChooser.TARGET_REASON_COUNTER);
        if (target == null)
        {
            return;
        }
        mostRecentTarget = target;

        EnemyTicketHolder ticketHolder =  target.gameObject.GetComponent<EnemyTicketHolder>();
        if (ticketHolder == null)
        {
            return;
        }
        shooterMeleeInput.SetLockAllInput(true);
        ticketHolder.CancelAttack();

        FreeFlowAttackMove attackMove = freeFlowMovePicker.PickMoveRandomly(target);
        if (attackMove == null)
        {
            return;
        }
        attackMove.victimGO_ID = target.gameObject.GetInstanceID();
        attackMove.attacker = gameObject;
        attackMove.victim = target;

        actionQueue.Enqueue(attackMove);

        freeFlowGapCloser.MoveToTargetForAttack(attackMove, this);
    }

    

    private void Evade()
    {
        //todo jump
    }

    private void regainPlayerControl()
    {
        // TODO check if the inventory is open!
        WickedObserver.SendMessage("EnableInventoryWindow");
        shooterMeleeInput.SetLockAllInput(false);
    }

    private float calculateDelayTillNextAttack(FreeFlowAttackMove currentAttack)
    {
        return (currentAttack.victimAnimationDelay + currentAttack.attackerLockTimeAfterHit) / currentAttack.attackerAnimationSpeed;
    }

    private void PlaceIdeally(FreeFlowTarget target, FreeFlowAttackMove attack)
    {
        /*Vector3 startPos = target.gameObject.transform.position;
        Vector3 offset = transform.position - startPos;
        offset = offset.normalized * attack.idealDistance;

        // SPIDERMAN TODO MAYBE REMOVE IF HIGH HEEL?
        var newPos = offset + startPos;
        newPos.y = transform.position.y;
        transform.position = newPos;*/
    }

    private FreeFlowTarget getMostRecentTarget()
    {
        return mostRecentTarget;
    }

    public void onReachedDestination()
    {
        if (actionQueue.Count == 0)
        {
            return;
        }
        FreeFlowAttackMove action = actionQueue.Dequeue();

        FreeFlowAnimatorController victimFreeFlowAnimatorController = action.victim.gameObject.GetComponent<FreeFlowAnimatorController>();
        FreeFlowTargetable victimTargetable = action.victim.gameObject.GetComponent<FreeFlowTargetable>();
        int result = victimTargetable.hit();
        action.victimReactionId = result;

        // victim
        victimFreeFlowAnimatorController.startFreeFlowAttack(action);
        victimTargetable.startFreeFlowAttack(action);

        // attacker
        freeFlowAnimatorController.startFreeFlowAttack(action);

        float actionTotalTime = calculateDelayTillNextAttack(action);
        timeTillNextFreeFlow = Time.time + actionTotalTime;
        WickedObserver.SendMessage("DisableInventoryWindow");
        // we disable the character controller during this time!
        CancelInvoke();
        Invoke("regainPlayerControl", actionTotalTime);
    }

    public void onReachedDestinationFail()
    {
        shooterMeleeInput.SetLockAllInput(false);
        FreeFlowAttackMove move = actionQueue.Dequeue();
        RegainAiControl(move);
        timeTillNextFreeFlow = Time.time;
    }

    /// <summary>
    /// Only used when Player failed to attack AI
    /// </summary>
    /// <param name="move"></param>
    private void RegainAiControl(FreeFlowAttackMove move)
    {
        if (move == null)
        {
            return;
        }
        if (move.victim.gameObject == null) {
            return;
        }
        vControlAI controlAI = move.victim.gameObject.GetComponent<vControlAI>();
        if (controlAI==null)
        {
            return;
        }
        Debug.Log("WAKE UP 2");
        controlAI.EnableAIController();
    }
}
