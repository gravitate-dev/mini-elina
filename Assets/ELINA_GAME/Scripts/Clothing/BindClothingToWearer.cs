using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Same as <see cref="BindClothing"/> but it targets the component BindTargetClothing
/// </summary>
public class BindClothingToWearer : MonoBehaviour
{
    public SkinnedMeshRenderer TargetMeshRenderer;
    private List<Guid> disposables = new List<Guid>();
    public void DestroyOnInventoryClose()
    {
        disposables.Add(WickedObserver.AddListener("OnInventoryClose", (obj) =>
         {
             if (gameObject != null)
             {
                 Destroy(gameObject);
             }
         }));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    private void Awake()
    {
        rebind();
    }
    public void rebind()
    {
        mapTarget();
        bind();
    }

    private void bind()
    {
        if (TargetMeshRenderer == null)
        {
            return;
        }
        Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
        foreach (Transform bone in TargetMeshRenderer.bones)
            boneMap[bone.gameObject.name] = bone;

        SkinnedMeshRenderer myRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
        if (myRenderer.rootBone==null)
        {
            myRenderer.rootBone = myRenderer.bones[0];
        }
        Transform[] newBones = new Transform[myRenderer.bones.Length];
        if (myRenderer.bones.Length>0 && myRenderer.bones[0] == null)
        {
            return;
        }
        for (int i = 0; i < myRenderer.bones.Length; ++i)
        {
            GameObject bone = myRenderer.bones[i].gameObject;
            //Debug.Log("bone: " + bone.name);
            if (!boneMap.TryGetValue(bone.name, out newBones[i]))
            {
                Debug.Log("Unable to map bone \"" + bone.name + "\" to target skeleton.");
                break;
            }
        }
        myRenderer.bones = newBones;
    }
    private void mapTarget()
    {
        var parent = gameObject.transform.parent;
        foreach (Transform trans in parent.transform)
        {
            if (trans.GetComponent<BindClothingTarget>())
            {
                TargetMeshRenderer = trans.GetComponent<SkinnedMeshRenderer>();
            }
        }
    }
}
