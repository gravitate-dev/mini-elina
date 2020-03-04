﻿using Animancer;
using JacobGames.SuperInvoke;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles both animation and sounds
/// </summary>
public class FreeFlowAnimatorController : MonoBehaviour
{
    private HybridAnimancerComponent animancer;
    private int GO_ID;
    private FreeFlowTargetable freeFlowTargetable;

    private GameObject stunEffectInstance;

    private List<System.Guid> disposables = new List<System.Guid>();
    
    void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        animancer = GetComponent<HybridAnimancerComponent>();
        if (animancer == null)
        {
            animancer = GetComponentInChildren<HybridAnimancerComponent>();
        }
        freeFlowTargetable = GetComponent<FreeFlowTargetable>();
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj) =>
        {
            RemoveDizzyStun();
        }));
        disposables.Add(WickedObserver.AddListener("onStunEndFreePunches:" + GO_ID, (obj) =>
        {
            RemoveDizzyStun();
        }));
    }

    public void startFreeFlowAttack(FreeFlowAttackMove freeFlowAttackMove)
    {
        FreeFlowAttackMove move = new FreeFlowAttackMove(freeFlowAttackMove);
        if (move.victim.gameObject.GetInstanceID() == GO_ID)
        {
            // victim
            var lookPos = move.attacker.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
            float delay = move.victimAnimationDelay / move.attackerAnimationSpeed;
            SuperInvoke.Run(delay, () =>
            {
                if (move.victimReactionId == FreeFlowTargetable.HIT_RESULT_STUN)
                {
                    StartStun();
                }
                else
                {
                PlayAnimation(move);
                }
            });
        }
        else
        {
            //attacker
            PlayAnimation(move);
            handleAttackerRotation(move);
        }
    }

    private void PlayAnimation(FreeFlowAttackMove move)
    {
        WickedObserver.SendMessage("OnFreeFlowAnimationStart:" + GO_ID);
        if (move.victim.gameObject != null && move.victim.gameObject.GetInstanceID() == GO_ID)
        {
            
            if (move.moveType == 1 || move.moveType == 2)
            {
                // these are punch and kicks
                //SoundSystem.INSTANCE.PlaySound("melee_impact", transform);

                SpecialFxRequestBuilder.newBuilder("AttackSparkle")
                .setOwner(transform, true)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.HALF_PLAYER_HEIGHT, 0))
                .setOffsetRotation(new Vector3(-90, 0, 0))
                .build().Play();
            }
            // i am victim
            string animationToPlay = move.DEBUG_VICTIM != null ? move.DEBUG_VICTIM.name : move.victimAnimation;
            // neat hook to change the animation style here
            
            /*if (move.victimReactionId == FreeFlowTargetable.HIT_RESULT_KNOCKDOWN)
            {
                animationToPlay = "KB_UpperKO_Flip";
            }*/
            AnimationClip clip = AnimationClipHandler.INSTANCE.ClipByName(animationToPlay);
                AnimancerState state = animancer.Play(clip, 0.1f, FadeMode.FixedDuration);
                state.Time = 0;
                state.Events.OnEnd = ReturnToNormal;
            }
            else
            {
            if (move.moveType == 3)
            {
                // throws require two to be together
                transform.position = move.victim.gameObject.transform.position;
            }
            string animationToPlay = move.DEBUG_ATTACK != null ? move.DEBUG_ATTACK.name : move.attackerAnimation;
            AnimationClip clip = AnimationClipHandler.INSTANCE.ClipByName(animationToPlay);
            AnimancerState state = animancer.Play(clip, 0.1f, FadeMode.FixedDuration);
            state.Time = 0;
            state.Speed = move.attackerAnimationSpeed;
            state.Events.OnEnd = ReturnToNormal;
            
        }
    }

    private void handleAttackerRotation(FreeFlowAttackMove move)
    {
        if (move.backwardsAttack)
        {
            var lookPos = move.victim.gameObject.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
            transform.Rotate(Vector3.up, 180);
        }
        else
        {
            var lookPos = move.victim.gameObject.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        }
    }

    private void ReturnToNormal()
    {
        WickedObserver.SendMessage("OnFreeFlowAnimationFinish:"+GO_ID);
        animancer.PlayController();
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
        ReturnToNormal();
    }
    
    private void StartStun()
    {
        if (stunEffectInstance != null)
        {
            Destroy(stunEffectInstance);
        }
        float stunTime = freeFlowTargetable.enemyStunTime;
        Invoke("RemoveDizzyStun", stunTime);
        stunEffectInstance = SpecialFxRequestBuilder.newBuilder("Stunned")
            .setOwner(transform, true)
            .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.PLAYER_HEIGHT, 0))
            .setOffsetRotation(new Vector3(-90, 0, 0))
            .setLifespan(stunTime)
            .build().Play();
        animancer.Play(AnimationClipHandler.INSTANCE.ClipByName("Stunned"));
    }
}
