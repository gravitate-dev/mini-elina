using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ButtonCooldown : MonoBehaviour
{
    //Public
    [Header("Invoke 'BeginCooldown' to... begin cooldown")]
    private float duration = 1;
    public Image overlay;
    public Button button;
    public UnityEvent onFinish;

    //Private
    private bool isCooldown;
    private float startTime;

    void Update()
    {
        if (isCooldown)
        {
            //Handle fill amount
            overlay.fillAmount = 1 - Mathf.InverseLerp(0, duration, Time.time - startTime);

            //Check if finished
            if (Time.time - startTime >= duration)
            {
                isCooldown = false;
                button.enabled = true;
                onFinish.Invoke();
            }
        }
    }

    public void BeginCooldown(float duration)
    {
        this.duration = duration;
        isCooldown = true;
        overlay.fillAmount = 1;
        startTime = Time.time;
        button.enabled = false;
    }

    public bool IsOnCooldown()
    {
        return isCooldown;
    }
}
