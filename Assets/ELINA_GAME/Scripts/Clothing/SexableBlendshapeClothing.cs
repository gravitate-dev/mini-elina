using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class SexableBlendshapeClothing : MonoBehaviour
{
    public bool takeOffDuringSex;
    [InfoBox("ass,boobs,flat,foot,hand,mouth,penis,stomach")]
    public string clothingTarget;
    public string blendshapeName; // during sex this is turned to 100
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private HentaiSexCoordinator hentaiSexCoordinator;
    private int GO_ID;
    private List<System.Guid> disposables = new List<System.Guid>();
    void Start()
    {
        Rigidbody rigidbody = GetComponentInParent<Rigidbody>();
        if (rigidbody)
        {
            GO_ID = rigidbody.gameObject.GetInstanceID();
        } else
        {
            // no rigid body look for blendshape controller
            BlendShapeItemCharacterController bsicc = GetComponentInParent<BlendShapeItemCharacterController>();
            if (bsicc == null)
            {
                Debug.LogError("we have a clothing item that is being worn by someone without a blendshapeitemcharactercontroller or rigidbody, ruh-roh");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                return;
            }
            GO_ID = bsicc.gameObject.GetInstanceID();

        }
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

        hentaiSexCoordinator = GetComponentInParent<HentaiSexCoordinator>();
        if (hentaiSexCoordinator != null)
        {
            UpdateVisibility(hentaiSexCoordinator.AmIVictim());
        }
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, onStartHentaiMove));
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, onCoordinatorStopMove));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    private void onStartHentaiMove(object message)
    {
        if (this == null)
        {
            return;
        }
        HMove hmove = (HMove)message;
        if (hentaiSexCoordinator != null)
        {
            UpdateVisibility(hentaiSexCoordinator.AmIVictim());
        }
    }

    private void onCoordinatorStopMove(object unused)
    {
        if (this == null)
        {
            return;
        }
        int idx = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
        skinnedMeshRenderer.SetBlendShapeWeight(idx, 0);
    }

    private void UpdateVisibility(bool isSexVictim)
    {
        if (isSexVictim == false) {
            return;
        }

        if (takeOffDuringSex)
        {
            skinnedMeshRenderer.enabled = false;
        }
        else
        {
            int idx = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
            skinnedMeshRenderer.SetBlendShapeWeight(idx, 100);
        }
    }
}
