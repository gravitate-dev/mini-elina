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

    public const int HIT_RESULT_NORMAL = 0;
    public const int HIT_RESULT_RAGDOLL = 1;
    public const int HIT_RESULT_STUN = 2;
    public const int HIT_RESULT_DEFEAT = 5;


    public const float SEX_TIME = 7.0f;
    public const float KNOCKDOWN_TIME = 2.7f;
    private const int FREE_STUN_HITS_ALLOWED = 3;
    
    [BoxGroup("Attack Stats")]
    public int stuff;

    [BoxGroup("Defense Stats")]
    public int hitsForDefeat = 10;
    [BoxGroup("Defense Stats")]
    public int sexToHitsFactor = 3;
    [BoxGroup("Defense Stats")]
    public int hitsToStun = 8;
    
    [BoxGroup("Stun Settings")]
    public int stunTimeFreeHitsAllowed = 2;
    [BoxGroup("Stun Settings")]
    public float enemyStunTime = 3.0f;

    private int stunFreeHitsLeft = 3;
    
    [SerializeField]
    [HideInEditorMode]
    private int currentHits;
    [SerializeField]
    [HideInEditorMode]
    private int currentHitsToStunCountdown;

    public bool defeated = false;
    [HideInEditorMode]
    public bool isSexing;

    private int GO_ID;
    private RagdollEnabler ragdollEnabler;
    private HentaiSexCoordinator hentaiSexCoordinator;
    private EnemyLogic enemyLogic;
    private HybridAnimancerComponent animancer;
    private List<System.Guid> disposables = new List<System.Guid>();
    private float DisableAttackbleTime;
    private float TargetSexableTime;



    // Start is called before the first frame update
    void Start()
    {
        GO_ID = gameObject.GetInstanceID();
        ragdollEnabler = GetComponent<RagdollEnabler>();
        animancer = GetComponent<HybridAnimancerComponent>();
        enemyLogic = GetComponent<EnemyLogic>();
        hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();
        currentHitsToStunCountdown = hitsToStun;
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (move)=>
        {
            HMove temp = (HMove)move;
            isSexing = false;
            if (temp.victim.gameObject.GetInstanceID() == GO_ID)
            {
                // take damage if i am being sexed
                sexHit();
            }
        }));
        // to know while i am being sexed
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj)=>
        {
            if (isSexing) { return; }
            isSexing = true;
            CancelInvoke();
            if (!HentaiSexCoordinator.isPlayerInvolved((HMove)obj))
            {
                Invoke("endSex", SEX_TIME);
            }
            
            
            
            
        }));

        disposables.Add(WickedObserver.AddListener("OnFreeFlowAnimationFinish:" + GO_ID, (unused) =>
        {
            // reboot an enemy after an attack
            enemyLogic.DisableForDuration(0f);
        }));
    }

    private void Update()
    {
        if (DisableAttackbleTime >= 0)
        {
            DisableAttackbleTime -= Time.deltaTime;
        }
        if (TargetSexableTime >= 0)
        {
            TargetSexableTime -= Time.deltaTime;
        }
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
        EnableSexable(0);
        DisableAttackble(0);
        hentaiSexCoordinator.StopAllSexIfAny();
    }
    
    public bool isTargetSexable()
    {
        return TargetSexableTime > 0 && !isSexing;
    }

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
        enemyLogic.RestartMovement();
        enemyLogic.DisableForDuration(move.victimAnimationDelay + 0.2f);
        yield return new WaitForSeconds(move.victimAnimationDelay);
        // sad removed it
        //ragdollEnabler.DebugShowOff();

        // normal stun hit time
        if (move.victimReactionId == HIT_RESULT_STUN)
        {
            stun();
        }
        else if (move.victimReactionId == HIT_RESULT_DEFEAT)
        {
            SetToDefeated();
        } else 
        {
            enemyLogic.DisableForDuration(move.victimStunTime);
        }
    }
    
    public void sexHit()
    {
        currentHits += sexToHitsFactor;
        if (currentHits + 1 >= hitsForDefeat)
        {
            SetToDefeated();
        }
    }

    private void SetToDefeated()
    {
        enemyLogic.Defeat();
        defeated = true;

        animancer.Play(AnimationClipHandler.INSTANCE.ClipByName("AnimeDeath_FallForward"));
        Destroy(gameObject, 1f);

    }
    public int hit()
    {
        if (enemyLogic.isStunned())
        {
            stunFreeHitsLeft--;
            if (stunFreeHitsLeft <= 0)
            {
                enemyLogic.DisableForDurationByStun(0);
                WickedObserver.SendMessage("onStunEndFreePunches:" + GO_ID);
            }
        }
        currentHits++;
        if (currentHits + 1 >= hitsForDefeat)
        {
            SetToDefeated();
            return HIT_RESULT_DEFEAT;
        }
        currentHitsToStunCountdown--;
        if (currentHitsToStunCountdown <= 0)
        {
            currentHitsToStunCountdown = hitsToStun;
            return HIT_RESULT_STUN;
        }
        return HIT_RESULT_NORMAL;
    }

    public bool isTargetableForAttack()
    {   //  no ragdolled enemies          no defeated    no sexing 
        return (ragdollEnabler==null || ragdollEnabler.targetable ) && !defeated && !isSexing && DisableAttackbleTime < 0;
    }

    private void stun()
    {
        stunFreeHitsLeft = FREE_STUN_HITS_ALLOWED;
        EnableSexable(enemyStunTime);
        enemyLogic.DisableForDurationByStun(enemyStunTime);
    }

    public int getCurrentHp()
    {
        return Mathf.Max(0,getMaxHp() + currentHits);
    }

    public int getMaxHp()
    {
        return hitsForDefeat;
    }

    private void DisableAttackble(float duration)
    {
        DisableAttackbleTime = duration;
    }
    private void EnableSexable(float duration)
    {
        TargetSexableTime = duration;
    }
}
