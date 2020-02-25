using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlendShapeItem : MonoBehaviour
{
    [System.Serializable]
    public class BlendAttribute
    {
        public string blendName;
        [PropertyRange(0, 100f)]
        public float blendValue; // between 0 and 100
    };

    public BlendAttribute[] blendAttributes;
    private BlendShapeItemCharacterController blendShapeItemCharacterController;

    void Start()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.GetComponent<BlendShapeItemCharacterController>())
            {
                blendShapeItemCharacterController = current.GetComponent<BlendShapeItemCharacterController>();
                break;
            }
            current = current.parent;
        }
        if (blendShapeItemCharacterController == null)
        {
            throw new Exception("Missing BlendShapeItemCharacterController on the any parent of" + gameObject.name);
        }
        blendShapeItemCharacterController.ApplyBlendShapeItem(this);
    }

    private void OnEnable()
    {
        // MAYBE DISABLE ME AND ONLY RELY ON START? for apply
        if (blendShapeItemCharacterController != null)
        {
            blendShapeItemCharacterController.ApplyBlendShapeItem(this);
        }
    }

    private void OnDisable()
    {
        // when the high heel is removed perhaps?
        if (blendShapeItemCharacterController != null && this!=null)
        {
            blendShapeItemCharacterController.RemoveBlendShapeItem(this);
        }
    }

}
