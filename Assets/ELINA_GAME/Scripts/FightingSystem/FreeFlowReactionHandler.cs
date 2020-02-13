using Animancer;
using Invector.vCharacterController.AI;
using System.Collections.Generic;
using UnityEngine;

public class FreeFlowReactionHandler : MonoBehaviour
{
    private HybridAnimancerComponent animancer;
    private vControlAI ai;

    
    private bool isDizzyStunned;

    private double stunLockTime;
    private bool enterStunLock;
    private GameObject stunEffectInstance;

    private AnimationClip stunnedClip;
    private List<System.Guid> disposables = new List<System.Guid>();
    void Awake()
    {
        
        int GO_ID = gameObject.GetInstanceID();
        gameObject.name = GO_ID + "_GUY";
        animancer = GetComponent<HybridAnimancerComponent>();
        stunnedClip = AnimationClipHandler.INSTANCE.ClipByName("Stunned");
        ai = GetComponent<vControlAI>();
        disposables.Add(WickedObserver.AddListener("OnFreeFlowVictimAnimationCommence:" + GO_ID, OnFreeFlowVictimAnimationCommence));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    private void OnEnable()
    {
        // todo
    }
    private void OnFreeFlowVictimAnimationCommence(object message)
    {
        FreeFlowAttackMove move = (FreeFlowAttackMove)message;
        if (move.victimReactionId == FreeFlowTargetable.HIT_RESULT_NORMAL)
        {
            if (stunLockTime < move.victimStunTime)
            {
                stunLockTime = move.victimStunTime;
            }
        } else if (move.victimReactionId == FreeFlowTargetable.HIT_RESULT_KNOCKDOWN)
        {
            stunLockTime = FreeFlowTargetable.KNOCKDOWN_TIME;
        } else if (move.victimReactionId == FreeFlowTargetable.HIT_RESULT_STUN)
        {
            CancelInvoke();
            isDizzyStunned = true;
            stunLockTime = FreeFlowTargetable.DIZZY_STUN_TIME;
            Invoke("RemoveDizzyStun", FreeFlowTargetable.DIZZY_STUN_TIME);
            if (stunEffectInstance != null)
            {
                Destroy(stunEffectInstance);
            }
            /*stunEffectInstance = SpecialFxRequestBuilder.newBuilder("Stunned")
                .setOwner(transform, true)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.PLAYER_HEIGHT, 0))
                .setOffsetRotation(new Vector3(-90, 0, 0))
                .build().Play();*/
            animancer.Play(stunnedClip);
        }
    }

    /// <summary>
    /// Can be from being sexed
    /// </summary>
    private void RemoveDizzyStun()
    {
        Destroy(stunEffectInstance);
        ReturnToNormalFromStun();
        isDizzyStunned = false;
    }

    private void Update()
    {
        if (stunLockTime > 0)
        {
            ai.Stop();
            if (!enterStunLock)
            {
                enterStunLock = true;
            }

            stunLockTime -= Time.deltaTime;
            if (stunLockTime < 0)
            {
                stunLockTime = 0;
            }
        }
        else
        {
            if (enterStunLock)
            {
                //DEBUG_ON_STUN_STOP_SHOW();
                enterStunLock = false;
            }
        }
    }

    private void ReturnToNormalFromStun()
    {
        animancer.CrossFadeInFixedTime(Animator.StringToHash("Free Locomotion"), 0);
    }
}
