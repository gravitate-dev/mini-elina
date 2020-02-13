using Invector.vCharacterController;
using Invector.vCharacterController.AI;
using JacobGames.SuperInvoke;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows this target to get hit and also handles stuntime
/// </summary>
public class FreeFlowTargetable : MonoBehaviour
{

    public const float DIZZY_STUN_TIME = 5;
    public const float KNOCKDOWN_TIME = 2.7f;
    public const float SEX_TIME = 5.0f;
    
    public int stuff;


    public int hitsForKnockDown = 3;
    public int knockDownsForStun = 2;
    public int stunsForDefeat = 2;
    public int sexTimesToDefeat = 1;

    private int currentHits;
    [SerializeField]
    private int currentKnockDowns;
    [SerializeField]
    private int totalStuns;
    [SerializeField]
    private int currentSexedTimes;

    private bool defeated = false;


    public bool targetableAttack;
    public bool isStunned; // when stunned dont get hit
    public bool isSexing;
    public bool targetableCounter;
    public bool isChargingAttack;
    [SerializeField]
    private bool targetableSex;

    private bool isSuccubusSexTargetLocked;
    private int GO_ID;
    private Rigidbody rigidBody;
    private vControlAI ai;
    private List<System.Guid> disposables = new List<System.Guid>();

    // Start is called before the first frame update
    void Start()
    {
        GO_ID = gameObject.GetInstanceID();
        ai = GetComponent<vControlAI>();
        rigidBody = GetComponent<Rigidbody>();
        disposables.Add(WickedObserver.AddListener("OnSuccubusSexTargetLocked:" + GO_ID, (obj)=> { isSuccubusSexTargetLocked = true; }));
        disposables.Add(WickedObserver.AddListener("OnSuccubusSexTargetLockRemoved:" + GO_ID, (obj)=> { isSuccubusSexTargetLocked = false; }));
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            knockBack(debugLastForce);
        }
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    
    public bool isTargetSexable()
    {
        // if i am targeted by a succubus you cant summon any more!
        if (isSuccubusSexTargetLocked)
        {
            return false;
        }
        return targetableSex;
    }

    public void startFreeFlowAttack(FreeFlowAttackMove freeFlowAttackMove)
    {
        FreeFlowAttackMove attack = new FreeFlowAttackMove(freeFlowAttackMove);
        if (attack.victim.gameObject.GetInstanceID() == GO_ID)
        {
            // i am victim
            StartCoroutine(onHitRoutines(attack));
        }
    }
    Vector3 debugLastForce;
    private IEnumerator onHitRoutines(FreeFlowAttackMove move)
    {
        //Get force info prior to delay
        Vector3 force = transform.position - move.attacker.transform.position;
        //Get only x, z dir with mag of 1k
        force.y = 0;
        force = force.normalized * 1;
        //Wait for attack anim to play
        yield return new WaitForSeconds(move.victimAnimationDelay);
        ai.EnableAIController();
        //Knockback victim
        /*if (move.knockback)
        {
            knockBack(force);
        }*/
    }


    //Deprecated its jenky
    private void knockBack(Vector3 force)
    {
        rigidBody.AddForce(force, ForceMode.Impulse);
    }

    public const int HIT_RESULT_NORMAL = 0;
    public const int HIT_RESULT_KNOCKDOWN = 1;
    public const int HIT_RESULT_STUN = 2;
    

    
    public int hit()
    {
        currentHits++;
        if (currentHits >= hitsForKnockDown)
        {
            if (currentKnockDowns >= knockDownsForStun && totalStuns + 1 >= stunsForDefeat)
            {
                // defeat by stun
                return HIT_RESULT_NORMAL;
            } else if (currentKnockDowns >= knockDownsForStun)
            {
                stun();
                return HIT_RESULT_STUN;
            }
            else
            {
                knockdown();
            }
            return HIT_RESULT_KNOCKDOWN;
        }
        return HIT_RESULT_NORMAL;
    }

    public bool isTargetableForAttack()
    {
        return targetableAttack && !isStunned && !isSexing;
    }

    public bool isTargetableForCounter()
    {
        return targetableCounter;
    }

    private void knockdown()
    {
        currentHits = 0;
        currentKnockDowns++;
        disableTargetable(KNOCKDOWN_TIME);
    }

    private void stun()
    {
        totalStuns++;
        currentHits = 0;
        enableSexable(DIZZY_STUN_TIME);
        disableTargetable(DIZZY_STUN_TIME);
        // do stun
    }

    public int getCurrentHp()
    {
        int damageTaken = Mathf.Max(currentSexedTimes, knockDownsForStun) * hitsForKnockDown;
        return getMaxHp() - damageTaken;
    }

    public int getMaxHp()
    {
        return knockDownsForStun * hitsForKnockDown;
    }

    public int getSexHp()
    {
        return Mathf.Max(0,sexTimesToDefeat - currentSexedTimes);
    }

    private void enableTargetable()
    {
        SuperInvoke.Kill("PhysicalTargetable:" + GO_ID);
        targetableAttack = true;
    }
    private void disableTargetable(float duration)
    {
        SuperInvoke.Kill("PhysicalTargetable:"+GO_ID);
        SuperInvoke.Run(() =>
        {
            targetableAttack = true;
        }, duration, "PhysicalTargetable:" + GO_ID);
        targetableAttack = false;
    }
    private void enableSexable(float duration)
    {
        SuperInvoke.Kill("SexualTargetable:"+ GO_ID);
        SuperInvoke.Run(() => {
            targetableSex = false;
        }, duration, "SexualTargetable:" + GO_ID);
        Invoke("disableSexable", duration);
        targetableSex = true;
    }

    public void disableSexable()
    {
        SuperInvoke.Kill("SexualTargetable:" + GO_ID);
        targetableSex = false;
    }
}
