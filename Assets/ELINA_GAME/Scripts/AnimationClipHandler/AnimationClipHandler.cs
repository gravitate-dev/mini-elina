using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationClipHandler : MonoBehaviour
{
    public static AnimationClipHandler INSTANCE;

    private List<AnimationClip> clips;
    private Dictionary<string, AnimationClip> clipMap = new Dictionary<string, AnimationClip>();

    private List<string> idleAnimations = new List<string>()
    {
        "Idle 1",
        "Idle 2",
    };

    void Awake()
    {
        INSTANCE = this;
        clips = new List<AnimationClip>();
        AnimationClip[] list = Resources.LoadAll<AnimationClip>("");
        clips.AddRange(list);
        foreach (AnimationClip clip in clips)
        {
            if (clipMap.ContainsKey(clip.name))
            {
                Debug.LogError("Animation Clip Handler: animation already exists (duplicate!): " + clip.name);
                continue;
            }
            clipMap.Add(clip.name, clip);
        }
    }

    public AnimationClip ClipByName(string name)
    {
        if (clipMap.ContainsKey(name) == false)
        {
            Debug.Log("WE DONT GOT : " + name + " animation");
        }
        return clipMap[name];
    }

    
    public AnimationClip getIdleAnimation()
    {
        int randIdx = UnityEngine.Random.Range(0, idleAnimations.Count);
        return clipMap[idleAnimations[randIdx]];
    }
}
