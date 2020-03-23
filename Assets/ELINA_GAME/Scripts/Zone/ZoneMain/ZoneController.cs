using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ZoneController : MonoBehaviour
{
    public List<GameObject> trapGates = new List<GameObject>();

    public int currentZoneEventId;
    public int eventStageIndex;

    public int ZONE_STATE;
    public const int ZONE_NOT_STARTED = 0;
    public const int ZONE_ACTIVE = 1;
    public const int ZONE_COMPLETE = 2;

    /// <summary>
    /// Events occur in sequence and must all be completed for the zone to advance.
    /// </summary>
    public List<UnityEvent> events = new List<UnityEvent>();
    public UnityEvent OnCompleteZoneEvent;

    public List<int> activeEvents = new List<int>();
    void Start()
    {
        OpenDoors();
        InvokeRepeating("AdvanceEvents", 0, 3.0f);
    }

    // Update is called once per frame
    void AdvanceEvents()
    {
        if (ZONE_STATE == ZONE_COMPLETE)
        {
            CancelInvoke();
        }
        if (ZONE_STATE != ZONE_ACTIVE)
        {
            return;
        }
        if (activeEvents.Count == 0)
        {
            // StartNextEvents
            events[eventStageIndex].Invoke();
            eventStageIndex++;
        }
    }

    public void StartZone()
    {
        if (ZONE_STATE == ZONE_NOT_STARTED)
        {
            LockDoors();
            ZONE_STATE = ZONE_ACTIVE;
            return;
        }
    }

    #region === Door management ===
    private void LockDoors()
    {
        foreach (GameObject gate in trapGates)
        {
            gate.SetActive(true);
        }
    }
    private void OpenDoors()
    {
        foreach (GameObject gate in trapGates)
        {
            gate.SetActive(false);
        }
    }
    #endregion

    #region === Event Management ===
    public void OnEventStart(int eventId)
    {
        activeEvents.Add(eventId);
    }

    public void OnEventComplete(int eventId)
    {
        activeEvents.Remove(eventId);
        if (activeEvents.Count == 0 && eventStageIndex == events.Count)
        {
            // done
            ZONE_STATE = ZONE_COMPLETE;
            if (OnCompleteZoneEvent != null)
            {
                OnCompleteZoneEvent.Invoke();
                OpenDoors();
            }
        }
    }
    #endregion
}
