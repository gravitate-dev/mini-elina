using Animancer;
using JacobGames.SuperInvoke;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows this target to get hit and also handles stuntime
/// </summary>
public class FreeFlowTargetable : MonoBehaviour
{

    public const float SEX_TIME = 7.0f;
    public const float KNOCKDOWN_TIME = 2.7f;
    private const int FREE_STUN_HITS_ALLOWED = 3;
    
    [BoxGroup("Attack Stats")]
    public int stuff;

    [BoxGroup("Defense Stats")]
    public int hitsForKnockDown = 8;
    [BoxGroup("Defense Stats")]
    public int knockDownsToDefeat = 2;


    [BoxGroup("Stun Settings")]
    public int stunTimeFreeHitsAllowed = 2;
    [BoxGroup("Stun Settings")]
    public float enemyStunTime = 5.0f;

    private int stunFreeHitsLeft = 3;
    
    [SerializeField]
    [HideInEditorMode]
    private int currentHits;
    [SerializeField]
    [HideInEditorMode]
    private int currentKnockDowns;

    private bool defeated = false;


    
    public bool targetCanBeAttacked;
    [HideInEditorMode]
    public bool isSexing;
    [HideInEditorMode]
    public bool targetableCounter;
    [HideInEditorMode]
    public bool isChargingAttack;
    [SerializeField]
    [HideInEditorMode]
    private bool targetableSex;

    private int GO_ID;
    private HentaiSexCoordinator hentaiSexCoordinator;
    private EnemyLogic enemyLogic;
    private HybridAnimancerComponent animancer;
    private List<System.Guid> disposables = new List<System.Guid>();
    
    

    // Start is called before the first frame update
    void Start()
    {
        GO_ID = gameObject.GetInstanceID();
        animancer = GetComponent<HybridAnimancerComponent>();
        enemyLogic = GetComponent<EnemyLogic>();
        hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (u)=>
        {
            isSexing = false;
            sexHit();
        }));
        // to know while i am being sexed
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj)=>
        {
            isSexing = true;
            if (defeated)
            {
                return; // ignore when defeated
            }
            
            CancelInvoke();
            if (HentaiSexCoordinator.isPlayerInvolved((HMove)obj))
            {
                // when the player sexes we let it happen forever.
                return;
            }
            Invoke("endSex", SEX_TIME);
            
        }));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    private void endSex()
    {
        if (this == null)
        {
            return;
        }
        isSexing = false;
        targetCanBeAttacked = true;
        targetableSex = false;
        hentaiSexCoordinator.stopAllSex();
    }
    
    public bool isTargetSexable()
    {
        return targetableSex && !isSexing;
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
    private IEnumerator onHitRoutines(FreeFlowAttackMove move)
    {
        //Get force info prior to delay
        Vector3 force = transform.position - move.attacker.transform.position;
        //Get only x, z dir with mag of 1k
        force.y = 0;
        force = force.normalized * 1;
        //Wait for attack anim to play
        // disable their attack
        enemyLogic.RestartMovement();
        enemyLogic.DisableForDuration(move.victimAnimationDelay + 0.2f);
        yield return new WaitForSeconds(move.victimAnimationDelay);

        // knock back
        enemyLogic.Knockback(new Vector3(0, 0, -3), 0.5f, true);

        // normal stun hit time
        if (move.victimReactionId == HIT_RESULT_STUN)
        {
            stun();
        }
        else if (move.victimReactionId == HIT_RESULT_KNOCKDOWN)
        {
            knockdown();
        }
        else if (move.victimReactionId == HIT_RESULT_DEFEAT)
        {
            SetToDefeated();
        } else 
        {
            enemyLogic.DisableForDuration(move.victimStunTime);
        }
    }

    public const int HIT_RESULT_NORMAL = 0;
    public const int HIT_RESULT_KNOCKDOWN = 1;
    public const int HIT_RESULT_STUN = 2;
    public const int HIT_RESULT_DEFEAT = 3;
    
    public void sexHit()
    {
        currentKnockDowns++;
        if (currentKnockDowns >= knockDownsToDefeat)
        {
            SetToDefeated();
        }
    }

    private void SetToDefeated()
    {
        targetCanBeAttacked = false;
        targetableSex = false;
        enemyLogic.Defeat();
        /*DecisionProvider decisionProvider = GetComponent<DecisionProvider>();
        if (decisionProvider != null)
        {
            decisionProvider.defeated = true;
        }
        defeated = true;

        AnimancerState state = animancer.Play(AnimationClipHandler.INSTANCE.ClipByName("AnimeDeath_FallForward"));
        state.Events.OnEnd = () =>
        {
            Destroy(gameObject, 0.4f);
        };*/
        
    }
    public int hit()
    {
        /*if (enemyLogic.isStunned())
        {
            stunFreeHitsLeft--;
            if (stunFreeHitsLeft <= 0)
            {
                enemyLogic.DisableForDurationByStun(0);
                WickedObserver.SendMessage("onStunEndFreePunches:" + GO_ID);
            }
        }*/
        currentHits++;
        if (currentHits >= hitsForKnockDown)
        {
            if (currentKnockDowns + 1 >= knockDownsToDefeat)
            {
                // defeat by stun
                return HIT_RESULT_DEFEAT;
            }
            return HIT_RESULT_KNOCKDOWN;
        }
        return HIT_RESULT_NORMAL;
    }

    public bool isTargetableForAttack()
    {
        return targetCanBeAttacked && !isSexing;
    }

    public bool isTargetableForCounter()
    {
        return enemyLogic.Attacking;
    }

    private void knockdown()
    {
        currentHits = 0;
        currentKnockDowns++;
        disableTargetable(KNOCKDOWN_TIME);
        enemyLogic.DisableForDuration(KNOCKDOWN_TIME);
    }

    private void stun()
    {
        stunFreeHitsLeft = FREE_STUN_HITS_ALLOWED;
        enableSexable(FreeFlowReactionHandler.DIZZY_STUN_TIME);
        //enemyLogic.DisableForDurationByStun(FreeFlowReactionHandler.DIZZY_STUN_TIME);
    }

    public int getCurrentHp()
    {
        int damageTaken = (currentKnockDowns * hitsForKnockDown) + currentHits;
        return Mathf.Max(0,getMaxHp() - damageTaken);
    }

    public int getMaxHp()
    {
        return knockDownsToDefeat * hitsForKnockDown;
    }

    private void disableTargetable(float duration)
    {
        SuperInvoke.Kill("PhysicalTargetable:"+GO_ID);
        SuperInvoke.Run(() =>
        {
            targetCanBeAttacked = true;
        }, duration, "PhysicalTargetable:" + GO_ID);
        targetCanBeAttacked = false;
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
