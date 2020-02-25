using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighHeelItemListener : MonoBehaviour
{
    public string highHeelBlendshapeName;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private int GO_ID;
    private List<System.Guid> disposables = new List<System.Guid>();
    void Awake()
    {
        
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        if (GetComponentInParent<Animator>() == null)
        {
            return;
        }
        BlendShapeItemCharacterController bsicc = GetComponentInParent<BlendShapeItemCharacterController>();
        if (bsicc == null)
        {
            Debug.LogError(gameObject.name + "<-- we have a clothing item that is being worn by someone without a blendshapeitemcharactercontroller, ruh-roh");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            return;
        }
        GO_ID = bsicc.gameObject.GetInstanceID();
        disposables.Add(WickedObserver.AddListener("OnHighHeelEquipped:" + GO_ID, OnHighHeelEquipped));
        disposables.Add(WickedObserver.AddListener("OnHighHeelRemoved:" + GO_ID, OnHighHeelRemoved));
        // a late refresh to make sure the value is update
        Invoke("DoubleCheckShoes", 0.2f);
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    private void DoubleCheckShoes()
    {
        WickedObserver.SendMessage("OnRequestHighHeelItemRefresh:" + GO_ID);
    }
    private void OnHighHeelEquipped(object message)
    {
        if (this == null)
        {
            return;
        }
        float heelValue = (float)message;
        int idx = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(highHeelBlendshapeName);
        skinnedMeshRenderer.SetBlendShapeWeight(idx, heelValue);
    }

    private void OnHighHeelRemoved(object unused)
    {
        if (this == null)
        {
            return;
        }
        if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
        {
            Debug.Log("What am i : " + gameObject.name);
        }
        int idx = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(highHeelBlendshapeName);
        skinnedMeshRenderer.SetBlendShapeWeight(idx, 0);
    }
}
