using Invector.vCharacterController;
using Invector.vShooter;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BlendShapeItem;

/// <summary>
/// Place me on Elina/Sex Elina/ The root obj, not armature
/// Also controls dynamic bones
/// </summary>
public class BlendShapeItemCharacterController : MonoBehaviour
{

    [Required]
    public string highHeelBlendshapeName;
    private vThirdPersonController thirdPersonController;
    private CharacterController characterController;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private int GO_ID;
    public float currentHeelHeight; ///<see cref="HentaiSexCoordinator.cs/>
    private DynamicBone[] dynamicBones;

    private float standingHeight;

    private List<Guid> disposables = new List<Guid>();
    void Awake()
    {
        dynamicBones = GetComponents<DynamicBone>();

        skinnedMeshRenderer = findElinaSkin();
        characterController = GetComponent<CharacterController>();
        thirdPersonController = GetComponent<vThirdPersonController>();
        CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            // for player
            standingHeight = capsuleCollider.height;
        } else if (characterController != null)
        {
            // for enemies
            standingHeight = characterController.height;
        }
        HentaiSexCoordinator hentaiSexCoodinator = GetComponentInParent<HentaiSexCoordinator>();
        if (hentaiSexCoodinator != null)
        {
            GO_ID = hentaiSexCoodinator.gameObject.GetInstanceID();
        }
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, onStartHentaiMove));
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (obj) =>
        {
            if (dynamicBones == null)
            {
                return;
            }
            foreach (DynamicBone dynamicBone in dynamicBones)
            {
                if (dynamicBone == null)
                {
                    continue;
                }
                dynamicBone.enabled = true;
            }

        }));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    private SkinnedMeshRenderer findElinaSkin()
    {
        foreach (Transform t in transform)
        {
            if (t.GetComponent<SkinnedMeshRenderer>() && t.GetComponent<BindClothingTarget>())
            {
                return t.GetComponent<SkinnedMeshRenderer>();
            }
        }
        return null;
    }

    public void ChangeHeel(float newHeight, float heelBlendshape)
    {
        currentHeelHeight = newHeight;
        if (thirdPersonController != null)
        {
            // for player
            thirdPersonController.colliderHeight = standingHeight + currentHeelHeight;
        } else if (characterController != null)
        {
            // for enemy
            characterController.height = standingHeight + currentHeelHeight;
        }
        WickedObserver.SendMessage("OnHighHeelEquipped:" + GO_ID, heelBlendshape); 
        SetBlendshape(highHeelBlendshapeName, heelBlendshape);
    }

    public void ClearHeel()
    {
        currentHeelHeight = 0;
        if (thirdPersonController != null)
        {
            // for player
            thirdPersonController.colliderHeight = standingHeight;
        } else if (characterController != null)
        {
            // for enemy
            characterController.height = standingHeight;
        }
        WickedObserver.SendMessage("OnHighHeelRemoved:" + GO_ID);
        SetBlendshape(highHeelBlendshapeName, 0);
    }

    public void ApplyBlendShapeItem(BlendShapeItem blendShapeItem)
    {
        foreach(BlendAttribute attr in blendShapeItem.blendAttributes)
        {
            SetBlendshape(attr.blendName, attr.blendValue);
        }
    }

    public void RemoveBlendShapeItem(BlendShapeItem blendShapeItem)
    {
        foreach (BlendAttribute attr in blendShapeItem.blendAttributes)
        {
            SetBlendshape(attr.blendName, 0);
        }
    }

    private void SetBlendshape(string name, float value)
    {
        if (skinnedMeshRenderer == null)
        {
            return;
        }
        int idx = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(name);
        if (idx == -1)
        {
            Debug.LogError("Missing blendshape `" + name + "` on mesh" + gameObject.name);
            return;
        }
        skinnedMeshRenderer.SetBlendShapeWeight(idx, value);
    }

    private void onStartHentaiMove(object message)
    {
        HMove move = (HMove)message;
        if (move.dynamicBoneDisable)
        {
            foreach (DynamicBone dynamicBone in dynamicBones)
            {
                dynamicBone.enabled = false;
            }
        }
    }
}
