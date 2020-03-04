using Animancer;
using Invector.vCharacterController;
using Invector.vCharacterController.AI;
using JacobGames.SuperInvoke;
using UnityEngine;

/// <summary>
/// Handles both animation and sounds
/// </summary>
public class FreeFlowAnimatorController : MonoBehaviour
{
    private HybridAnimancerComponent animancer;
    private int GO_ID;
    private AnimationClipHandler animationClipHandler;
    private SoundSystem soundSystem;
    void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        animancer = GetComponent<HybridAnimancerComponent>();
        soundSystem = FindObjectOfType<SoundSystem>();
        animationClipHandler = AnimationClipHandler.INSTANCE;
    }

    public void startFreeFlowAttack(FreeFlowAttackMove freeFlowAttackMove)
    {
        FreeFlowAttackMove move = new FreeFlowAttackMove(freeFlowAttackMove);
        if (move.victim.gameObject.GetInstanceID() == GO_ID)
        {
            var lookPos = move.attacker.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
            float delay = move.victimAnimationDelay / move.attackerAnimationSpeed;
            SuperInvoke.Run(delay, () =>
            {
                PlayAnimation(move);
            });
        }
        else
        {
            //attacker
            PlayAnimation(move);
        }
        if (move.attacker.GetInstanceID() == GO_ID)
        {
            handleAttackerRotation(move);
        }
    }

    private void PlayAnimation(FreeFlowAttackMove move)
    {
        
        if (move.victim.gameObject != null && move.victim.gameObject.GetInstanceID() == GO_ID)
        {
            
            if (move.moveType == 1 || move.moveType == 2)
            {
                // these are punch and kicks
                //soundSystem.PlaySound("melee_impact", transform);

                SpecialFxRequestBuilder.newBuilder("AttackSparkle")
                .setOwner(transform, true)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.HALF_PLAYER_HEIGHT, 0))
                .setOffsetRotation(new Vector3(-90, 0, 0))
                .build().Play();
            }
            // i am victim
            string animationToPlay = move.DEBUG_VICTIM != null ? move.DEBUG_VICTIM.name : move.victimAnimation;
            if (move.victimReactionId == FreeFlowTargetable.HIT_RESULT_KNOCKDOWN)
            {
                animationToPlay = "KB_UpperKO_Flip";
            }
            AnimationClip clip = animationClipHandler.ClipByName(animationToPlay);
            if (move.moveType == 3)
            {
                // its a throw
                AnimancerState state = animancer.Play(clip, 0.1f, FadeMode.FixedDuration);
                state.Time = 0;
                state.Events.OnEnd = ReturnToNormal;
            }
            else
            {
                AnimancerState state = animancer.Play(clip, 0.1f, FadeMode.FixedDuration);
                state.Time = 0;
                state.Events.OnEnd = ReturnToNormal;
            }
            WickedObserver.SendMessage("OnFreeFlowVictimAnimationCommence:" + move.victim.gameObject.GetInstanceID(), move);
        }
        else
        {
            if (move.moveType == 3)
            {
                // throws require two to be together
                transform.position = move.victim.gameObject.transform.position;
            }
            string animationToPlay = move.DEBUG_ATTACK != null ? move.DEBUG_ATTACK.name : move.attackerAnimation;
            AnimationClip clip = animationClipHandler.ClipByName(animationToPlay);
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
        animancer.PlayController();
    }

    
}
