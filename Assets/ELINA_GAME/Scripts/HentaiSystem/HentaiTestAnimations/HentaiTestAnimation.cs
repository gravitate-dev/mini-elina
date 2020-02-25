using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HentaiTestAnimation : MonoBehaviour
{
    public AnimationClip clip;
    private string lastValue;
    private Animator animator;
    private AnimatorOverrideController overrideController;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;
        overrideController["SexyTimeHolder"] = clip;
        if (clip!= null)
        {
            lastValue = clip.name;
            animator.SetBool("SexyTime", true);
        }
        InvokeRepeating("ObserveClip", 0, 1.0f);
    }

    private void ObserveClip()
    {
        if (clip == null) {
            return;
        }
        if (lastValue != null && lastValue.Equals(clip.name))
        {
            return;
        }
        overrideController["SexyTimeHolder"] = clip;
        lastValue = clip.name;
    }
}
