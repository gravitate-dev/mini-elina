using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NOTE only for player
/// </summary>
public class HentaiLustManager : MonoBehaviour
{

    // 0 to 100f lust levels
    // at 50.0f lust level character is not controllable and starts masterbating until lust is at 25.0f, during masterbation lust cant go up

    public float lustLevel = 0;
    public float lustMax = 100.0f;
    public float decreaseLustRate = 10.0f;
    public bool isMasterbating;
    private float timeLustWentUpLast;
    private const float safetyTime = 4000;
    private bool sentMasterbationRequest;
    private int GO_ID;
    private List<System.Guid> disposables = new List<System.Guid>();
    // once lust goes above 50.0F character should start jacking off until it drops down to 0
    void Awake()
    {
        GO_ID = gameObject.GetInstanceID();
        InvokeRepeating("CheckMasterbate", 0.1f, 2.0f);
        disposables.Add(WickedObserver.AddListener("OnStartMasterbationCallback", OnStartMasterbationCallback));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    private void CheckMasterbate()
    {
        lustLevel = Mathf.Min(lustLevel, lustMax);
        if (lustLevel > 80.0f)
        {
            // at 80 its forced masterbation
            if (!isMasterbating && !sentMasterbationRequest)
            {
                StartMasterbation(5.0f);
            }
        }
        else if (lustLevel > 50.0f)
        {
            // at 50 its masterbate only when safe
            if (!isMasterbating && isSafeForMasterbation() && !sentMasterbationRequest)
            {
                StartMasterbation(5.0f);
            }
        }
        if (isMasterbating && lustLevel < 0)
        {
            StopMasterbation();
        }
        if (!isMasterbating)
        {
            lustLevel = Mathf.Max(0, lustLevel - decreaseLustRate);
        }

        /*if (progressor != null)
        {
            progressor.SetProgress(lustLevel/100.0f);
        }*/
    }

    private bool isSafeForMasterbation()
    {
        if (timeLustWentUpLast + safetyTime < Time.time*1000)
        {
            return true;
        }
        return false;
    }

    private void StartMasterbation(float duration)
    {
        sentMasterbationRequest = true;
        WickedObserver.SendMessage("StartMasterbating:" + GO_ID, duration);
    }

    private void OnStartMasterbationCallback(object message)
    {
        bool success = (bool)message;
        if (success)
        {
            WickedObserver.SendMessage("OnDisplayChibi", HentaiChibiGui.CHIBI_SILLY);
            isMasterbating = true;
            clearLust();
        }
        sentMasterbationRequest = false;
    }

    private void StopMasterbation()
    {
        isMasterbating = false;
    }

    public void addLust(float amount)
    {
        if (isMasterbating)
        {
            return;
        }
        timeLustWentUpLast = Time.time * 1000;
        lustLevel = Mathf.Min(lustMax, lustLevel + amount);
    }

    public void clearLust()
    {
        lustLevel = 0;
    }
}
