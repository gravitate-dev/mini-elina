﻿using Animancer;
using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the hentai scenes by playing the animation IDs sequentially
/// </summary>
public class HentaiAnimatorController : MonoBehaviour
{

    //private AnimationEffectsInterface animationEffectsInterface;
    public AnimationClip idleAnimationClip;

    [HideInInspector]
    public HMove currentHMove;
    private HMove.AnimationItem playingScene;
    private HybridAnimancerComponent animancer;

    private HashSet<int> playedClips = new HashSet<int>();
    private int lastSceneIndex;
    private int sceneIndex;
    private int GO_ID;
    private AnimationClipHandler animationClipHandler;
    private HentaiSexCoordinator hentaiSexCoordinator;
    private Rigidbody rigidBody;
    // for ai
    private AIPath ai;
    private float PositionStickyTime = 0;
    private List<System.Guid> disposables = new List<System.Guid>();

    private void Start()
    {
        animationClipHandler = AnimationClipHandler.INSTANCE;
        GO_ID = gameObject.GetInstanceID();
        ai = GetComponent<AIPath>();
        rigidBody = GetComponent<Rigidbody>();
        hentaiSexCoordinator = GetComponent<HentaiSexCoordinator>();

        GameObject sexableElina = bfsForSexableElina(2);
        
        if (sexableElina == null)
        {
            animancer = GetComponent<HybridAnimancerComponent>();
            //animationEffectsInterface = GetComponent<AnimationEffectsInterface>();
        }
        else
        {
            //animationEffectsInterface = sexableElina.GetComponent<AnimationEffectsInterface>();
            animancer = sexableElina.GetComponent<HybridAnimancerComponent>();
        }
        if (animancer == null)
        {
            #if UNITY_EDITOR
            Debug.LogAssertion("CRITICAL: Missing animancer on" + gameObject.name);
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        if (idleAnimationClip!=null)
        {
            animancer.Play(idleAnimationClip);
        }
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, onStartHentaiMove));
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (unused)=> {
            ReturnToPlayableStates();
            }));
    }

    private void FixedUpdate()
    {
        if (currentHMove == null)
        {
            return;
        }
        PositionStickyTime -= Time.fixedDeltaTime;
        if (PositionStickyTime > 0)
        {
            transform.position = currentHMove.sexLocationPosition;
            transform.rotation = currentHMove.sexLocationRotation;
        }
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    

    private GameObject bfsForSexableElina(int maxDepths)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(transform);
        while (maxDepths>=0 && queue.Count > 0)
        {
            int elements = queue.Count;
            while (elements!=0)
            {
                elements--;
                Transform node = queue.Dequeue();
                if (node.gameObject.CompareTag("SexDummy"))
                {
                    return node.gameObject;
                }
                foreach(Transform children in node)
                {
                    queue.Enqueue(children);
                }
            }
            maxDepths--;
        }
        return null;
    }

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
        GameObject victim = hMove.victim.gameObject;//.GetComponent<HentaiHeatController>();
        if (victim == null)
        {
            Debug.LogError("ESCAPE THIS LOL");
            return false;
        }
        int i = 0;

        if (hMove.playClimax)
        {
            for (int j = 0; j < hMove.scenes.Length; j++) {
                HMove.AnimationItem scene = hMove.scenes[j];
                if (scene.isOrgasm)
                {
                    sceneIndex = j;
                    hMove.playClimax = false;
                    return true;
                }
            }
        }



        
        HentaiHeatController victimHeatController = victim.GetComponent<HentaiHeatController>();
        // Loop around the scenes to find a scene that i can play
        while (i++ < hMove.scenes.Length) {
            HMove.AnimationItem currentAction = hMove.scenes[sceneIndex];
            if (lastSceneIndex == sceneIndex && currentAction.oneShot)
            {
                // we dont play the same animation twice
                sceneIndex = (sceneIndex + 1) % hMove.scenes.Length;
                continue;
            }
            
            
            if (victimHeatController != null)
            {
                float victimHeat = victimHeatController.getOrgasmPercentage() * 100.0f;
                if (victimHeat < currentAction.minHeatLimit || victimHeat > currentAction.maxHeatLimit)
                {
                    sceneIndex = (sceneIndex + 1) % hMove.scenes.Length;
                    continue;
                }
            }

            // skip orgasm scenes
            if (currentAction.isOrgasm)
            {
                sceneIndex = (sceneIndex + 1) % hMove.scenes.Length;
                continue;
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
    private void playSexAnimations()
    {
        HMove hMove = currentHMove;
        if (hMove == null || hMove.scenes == null || !updateCurrentScene(hMove))
        {
            hentaiSexCoordinator.StopAllSexIfAny();
            return;
        }

        playingScene = hMove.scenes[sceneIndex];
        if (AmISexVictim())
        {
            if (playingScene.heatRate > 0)
            {
                WickedObserver.SendMessage("OnAnimationHeatRateChange:" + GO_ID, playingScene.heatRate);
            } else
            {
                WickedObserver.SendMessage("OnAnimationHeatRateChange:" + GO_ID, 0.0f);
            }
        }

        SetActorsPositionsForDuration(2.0f);

        string animationId = null;
        if (hMove.victim.gameObject.GetInstanceID() == GO_ID)
        {

            animationId = playingScene.victimAnimationId;
        }
        else
        {
            if (hMove.attackers != null)
            {
                for (int i = 0; i < hMove.attackers.Length; i++)
                {
                    if (hMove.attackers[i].gameObject == null) {
                        continue;
                    }
                    if (hMove.attackers[i].gameObject.GetInstanceID() == GO_ID)
                    {
                        animationId = playingScene.attackerAnimationIds[i];
                    }
                }
            }
        }
        if (animationId == null)
        {
            throw new Exception("Could not find an H Move for this attacker in this scene for move" + hMove.moveName);
        }
        


        // play animation if not already
        AnimationClip clip = animationClipHandler.ClipByName(animationId);
        // animation layer is always 0 for baselayer
        if (animancer.IsPlayingClip(clip))
        {
            return;
        }
        AnimancerState currentState = animancer.Play(clip, 0.25f);
        currentState.Time = 0;
        currentState.Events.OnEnd = () =>
        {
            currentState.Events.OnEnd = null;
            if (!hentaiSexCoordinator.IsSexing())
            {
                ReturnToPlayableStates();
                return;
            }
            playSexAnimations();
        };
        if (hMove.playClimax)
        {
            currentState.Speed = 2.0f;
            hMove.playClimax = false;
            if (AmISexVictim())
            {
                /*if (hMove.victim.reqParts[0].Equals("penis"))
                {
                    animationEffectsInterface.cumPenisAnimationEffect();
                }
                else
                {
                    animationEffectsInterface.cumPussyAnimationEffect();
                }*/
            }
            Debug.Log("Swap in a climax scene");
        }
    }

    private bool AmISexVictim()
    {
        if (currentHMove == null || 
            currentHMove.victim.gameObject == null ||
            currentHMove.victim.gameObject.GetInstanceID() != GO_ID)
        {
            return false;
        }
        return true;
    }

    private void SetActorsPositionsForDuration(float stickyTime)
    {
        PositionStickyTime = stickyTime;
    }

    private void onStartHentaiMove(object message)
    {
        if (currentHMove == null)
        {
            // first time trying sex!
            lastSceneIndex = -1;
        }
        currentHMove = new HMove((HMove)message);   
        if (ai != null)
        {
            // to prevent enemies from gravity affecting them
            ai.enabled = false;
        }
        if (rigidBody != null)
        {
            rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        }
        sceneIndex = currentHMove.sceneIndexSync;
        
        playSexAnimations();
    }

    private void ReturnToPlayableStates()
    {
        if (this == null)
        {
            return;
        }
        playedClips.Clear();
        currentHMove = null;
        PositionStickyTime = 0;
        WickedObserver.SendMessage("OnAnimationHeatRateChange:" + GO_ID, 0.0f);
        if (ai != null)
        {
            ai.enabled = true;
            //ai.gravity = Vector3.zero;
        }
        if (rigidBody != null)
        {
            rigidBody.isKinematic = false;
            rigidBody.detectCollisions = true;
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
        }
        if (animancer.IsPlaying())
        {
            animancer.Stop();
        }
        if (idleAnimationClip!=null)
        {
            animancer.Play(idleAnimationClip);
        }
        else
        {
            animancer.PlayController();
        }
    }

    public bool CanSkipCurrentSceneWithHeartBeat()
    {
        if (currentHMove == null || playingScene == null)
        {
            return true;
        }
        // orgasm scenes will not jump based on heat level
        if (playingScene.isOrgasm)
        {
            return false;
        }
        return true;
    }
}
