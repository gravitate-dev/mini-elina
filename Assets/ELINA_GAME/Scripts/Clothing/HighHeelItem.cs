using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class HighHeelItem : MonoBehaviour
{
    [PropertyRange(0, 1f)]
    public float colliderAdditionalHeight;

    [PropertyRange(0,100f)]
    public float heelBlendshape;

    public bool disableControls;

    private BlendShapeItemCharacterController highHeelCharacterController;
    private List<System.Guid> disposables = new List<System.Guid>();
    void Awake()
    {
        highHeelCharacterController = GetComponentInParent<BlendShapeItemCharacterController>();
        if (highHeelCharacterController == null)
        {
            throw new Exception("Missing BlendShapeItemCharacterController on the any parent of" + gameObject.name);
        }
        int GO_ID = highHeelCharacterController.gameObject.GetInstanceID();
        disposables.Add(WickedObserver.AddListener("OnInventoryClose", (obj)=> { ModifyHeel(); }));
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (obj) => { ModifyHeel(); }));
        disposables.Add(WickedObserver.AddListener("OnRequestHighHeelItemRefresh:" + GO_ID, (obj) => { ModifyHeel(); }));
    }

    void Start()
    {
        //called once
        ModifyHeel();
        Invoke("ModifyHeel", 0.1f); // cheap hack because previewing the item would override it
    }

    private void ModifyHeel()
    {
        if (disableControls)
        {
            return;
        }
        highHeelCharacterController.ChangeHeel(colliderAdditionalHeight, heelBlendshape);
    }

    private void OnEnable()
    {
        if (highHeelCharacterController != null)
        {
            ModifyHeel();
        }
    }
    private void OnDisable()
    {
        if (disableControls)
        {
            return;
        }
        if (highHeelCharacterController != null)
        {
            highHeelCharacterController.ClearHeel();
        }
    }
    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
        if (disableControls)
        {
            return;
        }
        // when the high heel is removed perhaps?
        if (highHeelCharacterController != null)
        {
            highHeelCharacterController.ClearHeel();
        }
    }
}
