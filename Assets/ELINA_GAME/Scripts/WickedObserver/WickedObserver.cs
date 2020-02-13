using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WickedObserver
{
    private Dictionary<string, List<WickedAction>> listeners = new Dictionary<string, List<WickedAction>>();
    private Dictionary<Guid,string> trackingMap = new Dictionary<Guid,string>();
    private static WickedObserver INSTANCE;

    class WickedAction
    {
        public Guid guid;
        public Action<object> action;
        public string message;

        public WickedAction(Action<object> action, string message)
        {
            this.action = action;
            this.message = message;
            this.guid = System.Guid.NewGuid();
        }
    }
    private WickedObserver() {}

    public static void SendMessage(string message)
    {
        SendMessage(message, null);
    }

    public static void SendMessage(string message, object data)
    {
        if (INSTANCE == null)
        {
            INSTANCE = new WickedObserver();
        }

        if (INSTANCE.listeners.ContainsKey(message))
        {
            foreach (WickedAction wickedAction in INSTANCE.listeners[message]){
                wickedAction.action.Invoke(data);
            }
        }
    }

    /// <summary>
    /// Register a listener
    /// </summary>
    /// <param name="action">Callback to invoke</param>
    /// <param name="objectID">To prevent duplicate listeners this is from GetInstanceID()</param>
    public static Guid AddListener(string message, Action<object> action)
    {
        if (INSTANCE == null)
        {
            INSTANCE = new WickedObserver();
        }
        if (!INSTANCE.listeners.ContainsKey(message))
        {
            INSTANCE.listeners[message] = new List<WickedAction>();
        }
        WickedAction wickedAction = new WickedAction(action,message);
        INSTANCE.listeners[message].Add(wickedAction);
        INSTANCE.trackingMap.Add(wickedAction.guid, message);
        return wickedAction.guid;
    }

    /// <summary>
    /// Register a listener
    /// </summary>
    /// <param name="action">Callback to invoke</param>
    /// <param name="objectID">To prevent duplicate listeners this is from GetInstanceID()</param>
    public static void RemoveListener(Guid wickedActionGuid)
    {
        if (INSTANCE == null)
        {
            INSTANCE = new WickedObserver();
        }
        if (!INSTANCE.trackingMap.ContainsKey(wickedActionGuid))
        {
            // NOT FOUND
            Debug.LogWarning("Wicked action GUID not found");
            return;
        }
        string message = INSTANCE.trackingMap[wickedActionGuid];
        if (!INSTANCE.listeners.ContainsKey(message))
        {
            Debug.LogWarning("Not listening for message: " + message);
            return;
        }
        //Debug.Log("BEFORE REMOVAL" + INSTANCE.listeners[message].Count);
        int removeIdx = -1;
        for (int i = 0; i < INSTANCE.listeners[message].Count; i++)
        {
            if (INSTANCE.listeners[message][i].guid.Equals(wickedActionGuid))
            {
                removeIdx = i;
            }
        }

        if (removeIdx == -1)
        {
            return;
        }
        INSTANCE.listeners[message].RemoveAt(removeIdx);
        //Debug.Log("AFTER REMOVAL" + INSTANCE.listeners[message].Count);
    }

    public static void RemoveListener(List<Guid> guids)
    {
        foreach (Guid guid in guids)
        {
            RemoveListener(guid);
        }
    }

}
