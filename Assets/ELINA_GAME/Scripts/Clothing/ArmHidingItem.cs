using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmHidingItem : MonoBehaviour
{
    private ArmHider armhider;
    void Start()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.GetComponent<ArmHider>())
            {
                armhider = current.GetComponent<ArmHider>();
                break;
            }
            current = current.parent;
        }
        if (armhider == null)
        {
            throw new Exception("Missing armhider on the any parent of" + gameObject.name);
        }
        armhider.hidingArms = true;
        InvokeRepeating("HideArms", 0, 1.0f);
    }

    private void HideArms()
    {
        if (armhider != null) {
            armhider.hidingArms = true;
        }
    }

    private void OnDisable()
    {
        if (armhider != null)
        {
            armhider.hidingArms = false;
        }
    }
    private void OnDestroy()
    {
        if (armhider != null)
        {
            armhider.hidingArms = false;
        }
    }
}
