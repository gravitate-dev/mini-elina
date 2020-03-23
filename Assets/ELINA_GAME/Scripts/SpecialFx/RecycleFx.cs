using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RecycleFx : MonoBehaviour
{
    public UnityEvent recycleEvent;
    [HideInInspector]
    public string recycleTag;

    // Update is called once per frame
    public void Recycle()
    {
        if (recycleEvent != null)
        {
            recycleEvent.Invoke();
        }
    }
}
