using Animancer;
using Invector.vCharacterController.AI;
using System.Collections.Generic;
using UnityEngine;

public class FreeFlowReactionHandler : MonoBehaviour
{
    private HybridAnimancerComponent animancer;

    private GameObject stunEffectInstance;
    public const float DIZZY_STUN_TIME = 5;

    private AnimationClip stunnedClip;
    private List<System.Guid> disposables = new List<System.Guid>();
    void Awake()
    {
        
        int GO_ID = gameObject.GetInstanceID();
        gameObject.name = GO_ID + "_GUY";
        animancer = GetComponent<HybridAnimancerComponent>();
        stunnedClip = AnimationClipHandler.INSTANCE.ClipByName("Stunned");
        disposables.Add(WickedObserver.AddListener("OnFreeFlowVictimAnimationCommence:" + GO_ID, OnFreeFlowVictimAnimationCommence));
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj) =>
        {
            RemoveDizzyStun();
        }));
        disposables.Add(WickedObserver.AddListener("onStunEndFreePunches:" + GO_ID, (obj) =>
        {
            RemoveDizzyStun();
        }));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    private void OnFreeFlowVictimAnimationCommence(object message)
    {
        FreeFlowAttackMove move = (FreeFlowAttackMove)message;
        if (move.victimReactionId == FreeFlowTargetable.HIT_RESULT_STUN)
        {
            if (stunEffectInstance != null)
            {
                Destroy(stunEffectInstance);
            }
            float stunTime = move.victim.GetComponent<FreeFlowTargetable>().enemyStunTime;
            stunEffectInstance = SpecialFxRequestBuilder.newBuilder("Stunned")
                .setOwner(transform, true)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.PLAYER_HEIGHT, 0))
                .setOffsetRotation(new Vector3(-90, 0, 0))
                .setLifespan(stunTime)
                .build().Play();
            animancer.Play(stunnedClip);
        }
    }

    /// <summary>
    /// Can be from being sexed
    /// </summary>
    private void RemoveDizzyStun()
    {
        if (stunEffectInstance != null)
        {
            Destroy(stunEffectInstance);
        }
        ReturnToNormalFromStun();
    }

    private void ReturnToNormalFromStun()
    {
        animancer.PlayController();
        //animancer.CrossFadeInFixedTime(Animator.StringToHash("Free Locomotion"), 0);
    }
}
