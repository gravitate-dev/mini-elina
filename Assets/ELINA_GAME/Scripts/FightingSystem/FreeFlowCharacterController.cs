﻿using Invector.vCharacterController;
using Invector.vCharacterController.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerSpellLibrary;

/// <summary>
/// Controls invector too!
/// </summary>
public class FreeFlowCharacterController : MonoBehaviour, FreeFlowGapListener
{
    public GameObject SUCCUBUS_SEX_CLONE_PREFAB;

    private PlayerSpellLibrary playerSpellLibrary;
    private FreeFlowGapCloser freeFlowGapCloser;
    private FreeFlowMovePicker freeFlowMovePicker;
    private FreeFlowTargetChooser freeFlowEnemyPicker;
    private FreeFlowAnimatorController freeFlowAnimatorController;
    private vShooterMeleeInput shooterMeleeInput;
    private Animator animator;
    private int GO_ID;
    private Queue<FreeFlowAttackMove> actionQueue = new Queue<FreeFlowAttackMove>();
    private List<System.Guid> disposables = new List<System.Guid>();
    private HashSet<string> lockInputReasons = new HashSet<string>();

    private int nextAction = 0;
    private int ACTION_NULL = 0;
    private int ACTION_ATTACK = 1;
    private int ACTION_COUNTER = 2;
    private int ACTION_EVADE = 3;
    private int ACTION_MAGIC_SPELL = 4;
    private float DisableActionsTime;
    private float DisableActionsVictimHitAnimationTime;
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
    private GlideController glideController;
    #endregion
    private void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        playerSpellLibrary = GetComponent<PlayerSpellLibrary>();
        freeFlowAnimatorController = GetComponent<FreeFlowAnimatorController>();
        shooterMeleeInput = GetComponent<vShooterMeleeInput>();
        freeFlowMovePicker = FindObjectOfType<FreeFlowMovePicker>();
        animator = GetComponent<Animator>();
        glideController = GetComponent<GlideController>();
        freeFlowGapCloser = GetComponent<FreeFlowGapCloser>();
        freeFlowEnemyPicker = GetComponent<FreeFlowTargetChooser>();

        canBeAttacked = true;
        canBeSexed = true;
        canBeCarried = false;

        // handle character controller state changes i have a pending invoke that i need to cancel
        disposables.Add(WickedObserver.AddListener("onStateKnockedOut:" + GO_ID, (obj) =>
        {
            WickedObserver.SendMessage("EnableInventoryWindow");
            CancelInvoke();
        }));
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj) =>
        {
            WickedObserver.SendMessage("EnableInventoryWindow");
            HMove currentMove = new HMove((HMove)obj); // update loop for hentai moves
            canBeSexed = false;
            canBeAttacked = false;
            shooterMeleeInput.jumpInput.useInput = false;
            glideController.enabled = false;
            CancelInvoke();
        }));

        WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (unused) =>
        {
            canBeSexed = true;
            canBeAttacked = true;
            Invoke("AllowJump", 0.1f);
            glideController.enabled = true;
        });

        disposables.Add(WickedObserver.AddListener("OnFreeFlowAnimationFinish:" + GO_ID, (unused) =>
        {
            DisableActionsVictimHitAnimation(0);
        }));

        //todo set freeflow mode when appropriateGetClipByName
        animator.SetBool("FreeFlowMode", true);
    }

    private void Start()
    {
        //spells = playerSpellLibrary.GetSpells();
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    void Update()
    {

        CapturePlayersInput();
        
        if (ACTION_TIMEOUT_TIME < Time.time)
        {
            nextAction = ACTION_NULL;
        }
        if (DisableActionsTime > 0 || DisableActionsVictimHitAnimationTime > 0)
            {
            DisableActionsVictimHitAnimationTime -= Time.deltaTime;
            DisableActionsTime -= Time.deltaTime;
            return;
            }
        if (nextAction == ACTION_NULL || actionQueue.Count > 0)
        {
            // if i am busy or doing nothing return
            return;
        }
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
        
        GameObject target = freeFlowEnemyPicker.getTarget(FreeFlowTargetChooser.TARGET_REASON_ATTACK);
        if (target == null)
        {
            return;
        }

        FreeFlowAttackMove attackMove = freeFlowMovePicker.PickMoveRandomly(transform,target);
        if (attackMove == null)
        {
            return;
        }
        LockPlayerInput("FreeFlowCharacterController");
        attackMove.victimGO_ID = target.gameObject.GetInstanceID();
        attackMove.attacker = gameObject;
        attackMove.victim = target;

        actionQueue.Enqueue(attackMove);

        freeFlowGapCloser.MoveToTargetForAttack(attackMove, this);
    }

    private void Counter()
    {
        GameObject target = freeFlowEnemyPicker.getTarget(FreeFlowTargetChooser.TARGET_REASON_COUNTER);
        if (target == null)
        {
            return;
        }

        EnemyMeleeActionManager ticketHolder =  target.gameObject.GetComponent<EnemyMeleeActionManager>();
        if (ticketHolder == null)
        {
            return;
        }
        LockPlayerInput("FreeFlowCharacterController");
        ticketHolder.CancelAttack();

        FreeFlowAttackMove attackMove = freeFlowMovePicker.PickMoveRandomly(transform,target);
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

    private void MagicSpell(Spell spell)
    {
        if (spell.SPELL_NUMBER == SPELL_SUMMON_SEX_SUCCUBUS)
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

        GameObject newSuccubus = Instantiate(SUCCUBUS_SEX_CLONE_PREFAB);
        Vector3 pos = transform.position + transform.forward; // spawn demon in front
        newSuccubus.transform.position = pos;
        pos.y = transform.position.y + 0.5f;
        newSuccubus.transform.rotation = transform.rotation;
        /*SexShadowClone sexShadowClone = newSuccubus.GetComponent<SexShadowClone>();
        sexShadowClone.SetTarget(target);
        sexShadowClone.MoveThenSex();*/
    }

    private void Evade()
    {
        //todo jump
    }

    private void regainPlayerControl()
    {
        // TODO check if the inventory is open!
        WickedObserver.SendMessage("EnableInventoryWindow");
        UnlockPlayerInput("FreeFlowCharacterController");
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
        int result = victimTargetable.hit();
        action.victimReactionId = result;

        // victim
        victimFreeFlowAnimatorController.startFreeFlowAttack(action);
        victimTargetable.VictimHitRoutines(action);

        // attacker
        freeFlowAnimatorController.startFreeFlowAttack(action);

        float actionTotalTime = calculateDelayTillNextAttack(action);
        WickedObserver.SendMessage("DisableInventoryWindow");
        // we disable the character controller during this time!
        CancelInvoke();
        Invoke("regainPlayerControl", actionTotalTime);
        DisableActions(actionTotalTime);
    }

    public void onReachedDestinationFail()
    {
        UnlockPlayerInput("FreeFlowCharacterController");
        FreeFlowAttackMove move = actionQueue.Dequeue();
        if (move == null)
        {
            return;
        }
        if (move.victim.gameObject == null)
        {
            return;
        }
        move.victim.gameObject.GetComponent<EnemyLogic>().DisableForDuration(0);
    }



    #region == Victim Routines and Health Management ==
    public void VictimHitRoutines(FreeFlowAttackMove freeFlowAttackMove)
    {
        FreeFlowAttackMove attack = new FreeFlowAttackMove(freeFlowAttackMove);
        if (attack.victim.gameObject.GetInstanceID() == GO_ID)
        {
            // i am victim
            StartCoroutine(onHitRoutines(attack));
        }
    }
    private IEnumerator onHitRoutines(FreeFlowAttackMove move)
    {
        //Wait for attack anim to play
        // disable their attack
        float delay = move.victimAnimationDelay / move.attackerAnimationSpeed;
        DisableActionsVictimHitAnimation(10f);
        yield return new WaitForSeconds(delay);
        // todo do special on hits here this is after the character gets punched
    }

    private void DisableActionsVictimHitAnimation(float time)
    {
        DisableActionsVictimHitAnimationTime = time;
    }
    private void DisableActions(float time)
    {
        DisableActionsTime = time;
        nextAction = ACTION_NULL;
    }

    private void AllowJump()
    {
        shooterMeleeInput.jumpInput.useInput = true;
    }
    public int TakePhysicalHit()
    {
        currentHits++;
        return HIT_RESULT_NORMAL;
    }

    #endregion

    public void LockPlayerInput(string reason)
        {
        lockInputReasons.Add(reason);
        shooterMeleeInput.SetLockAllInput(true);
        }

    public void UnlockPlayerInput(string reason)
    {
        lockInputReasons.Remove(reason);
        if (lockInputReasons.Count == 0)
        {
            shooterMeleeInput.SetLockAllInput(false);
        }
    }
}
