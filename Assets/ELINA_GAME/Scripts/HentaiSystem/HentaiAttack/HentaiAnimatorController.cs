using Animancer;
using Invector.vCharacterController;
using Invector.vCharacterController.AI;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the hentai scenes by playing the animation IDs sequentially
/// </summary>
public class HentaiAnimatorController : MonoBehaviour
{

    public bool hasNpcIdle;

    [HideInInspector]
    public HMove currentHMove;
    private HybridAnimancerComponent animancer;

    private HashSet<int> playedClips = new HashSet<int>();
    private int lastSceneIndex;
    private int GO_ID;
    private AnimationClipHandler animationClipHandler;
    private HentaiSexCoordinator hentaiSexCoordinator;
    private Rigidbody rigidBody;
    private List<System.Guid> disposables = new List<System.Guid>();

    private void Start()
    {
        GO_ID = gameObject.GetInstanceID();
        rigidBody = GetComponent<Rigidbody>();
        hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();
        if (hentaiSexCoordinator == null)
        {
            throw new Exception("GameObject: " + gameObject.name + " must have a HentaiSexCoordinator");
        }
        
        GameObject sexableElina = null;
        // find sexableElina
        if (transform.CompareTag("SexDummy"))
        {
            sexableElina = transform.gameObject;
        }
        else
        {
            foreach (Transform t in transform)
            {
                if (t.CompareTag("SexDummy"))
                {
                    sexableElina = t.gameObject;
                }
            }

        }
        if (sexableElina == null)
        {
            animancer = GetComponent<HybridAnimancerComponent>();
        }
        else
        {
            animancer = sexableElina.GetComponent<HybridAnimancerComponent>();
        }
        if (animancer == null)
        {
            #if UNITY_EDITOR
            Debug.LogAssertion("CRITICAL: Missing animancer on" + gameObject.name);
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }


        animationClipHandler = AnimationClipHandler.INSTANCE;

        /// <summary>
        /// our sexytime animator needs to communicate with this class so we share a varaible
        /// <see cref="HentaiSexyTimeStateAnimatorWatcher"/>
        /// </summary>
        animancer.SetInteger("GO_ID", GO_ID);

        if (hasNpcIdle)
        {
            AnimationClip idleClip = animationClipHandler.getIdleAnimation();
            if (idleClip != null)
            {
                animancer.Play(idleClip);
            }
        }

        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, onStartHentaiMove));
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, onCoordinatorStopMove));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    int sceneIndex;


    /// <summary>
    /// Sets the next scene to play out
    /// 
    /// If no scene can be played return false
    /// </summary>
    /// <param name="hMove"></param>
    /// <returns></returns>
    private bool updateCurrentScene(HMove hMove)
    {
        if (hMove == null)
            return false;
        HMove.AnimationItem currentAction;
        int i = 0;

        // between 0 and 100

        HentaiHeatController victimHeatController = hMove.victim.gameObject.GetComponent<HentaiHeatController>();
        
        // Loop around the scenes to find a scene that i can play
        while (i++ < hMove.scenes.Length) {
            if (lastSceneIndex == sceneIndex && hMove.scenes[sceneIndex].oneShot)
            {
                // we dont play the same animation twice
                sceneIndex = (sceneIndex + 1) % hMove.scenes.Length;
                continue;
            }
            currentAction = hMove.scenes[sceneIndex];

            if (victimHeatController != null)
            {
                HMove.AnimationItem scene = hMove.scenes[sceneIndex];
                float victimHeat = victimHeatController.getOrgasmPercentage() * 100.0f;
                if (victimHeat < scene.minHeatLimit || victimHeat > scene.maxHeatLimit)
                {
                    sceneIndex = (sceneIndex + 1) % hMove.scenes.Length;
                    continue;
                }
            }

            // skip non-replayable
            if (playedClips.Contains(sceneIndex) && !currentAction.replay)
            {
                sceneIndex = (sceneIndex + 1) % hMove.scenes.Length;
                continue;
            }
            playedClips.Add(sceneIndex);
            lastSceneIndex  = sceneIndex;
            return true;
        }
        lastSceneIndex = -1;
        return false;
    }

    /// <summary>
    /// Plays sex animations, idempotent - safe to call multiple times to update the actors
    /// </summary>
    /// <param name="hMove"></param>
    /// <returns>
    /// True if can act
    /// False if there are no moves to do i.e. not replayable after a first time play through
    /// </returns>
    public bool playSexAnimations()
    {
        HMove hMove = currentHMove;
        if (hMove == null || hMove.scenes == null)
            return false;
        if (!updateCurrentScene(hMove))
            return false;

        HMove.AnimationItem scene = hMove.scenes[sceneIndex];
        if (scene.heatRate > 0 && currentHMove.victim.GO_ID == GO_ID)
        {
            // only if the current sex move provides heat and the person getting fucked is me
            WickedObserver.SendMessage("OnAnimationHeatRateChange:" + GO_ID, scene.heatRate);
        }
        else
        {
            WickedObserver.SendMessage("OnAnimationHeatRateChange:" + GO_ID, 0.0f);
        }

        transform.position = currentHMove.sexLocationPosition;
        transform.rotation = currentHMove.sexLocationRotation;

        string animationId = null;
        if (hMove.victim.GO_ID == GO_ID)
        {
            animationId = scene.victimAnimationId;
        }
        else
        {
            if (hMove.attackers != null)
            {
                for (int i = 0; i < hMove.attackers.Length; i++)
                {
                    if (hMove.attackers[i].GO_ID == GO_ID)
                    {
                        animationId = scene.attackerAnimationIds[i];
                    }
                }
            }
        }
        if (animationId == null)
        {
            throw new Exception("Could not find an H Move for this attacker in this scene for move" + hMove.moveName);
        }
        AnimationClip clip = animationClipHandler.ClipByName(animationId);
        // animation layer is always 0 for baselayer
        if (animancer.IsPlayingClip(clip))
        {
            return true;
        }
        AnimancerState state = animancer.Play(clip, 0.25f);
        state.Time = 0;
        state.Events.OnEnd = () =>
        {
            playSexAnimations();
        };
        return true;

    }
 
    private void onStartHentaiMove(object message)
    {
        
        lastSceneIndex = -1;
        currentHMove = new HMove((HMove)message);
        
        if (currentHMove == null)
        {
            throw new Exception("H MOVE IS NULL");
        }
        if (rigidBody != null)
        {
            rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        }
        sceneIndex = currentHMove.sceneIndexSync;
        if (!playSexAnimations())
        {
            Debug.Log("Stopped because no more H MOVES");
            hentaiSexCoordinator.stopAllSex();
            return;
        }

        
    }

    private void onCoordinatorStopMove(object message)
    {
        playedClips.Clear();
        if (this == null)
        {
            return;
        }
        currentHMove = null;
        WickedObserver.SendMessage("OnAnimationHeatRateChange:" + GO_ID, 0.0f);

        if (rigidBody != null)
        {
            rigidBody.isKinematic = false;
            rigidBody.detectCollisions = true;
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
        }
        if (hasNpcIdle)
        {
            AnimationClip idleClip = animationClipHandler.getIdleAnimation();
            if (idleClip != null)
            {
                animancer.Play(idleClip);
                return;
            }
        } 
        animancer.Stop();
    }
}
