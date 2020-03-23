using Invector.vCharacterController;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// swaps rigs also updates clothes when swapping
/// </summary>
public class HentaiRigSwapper : MonoBehaviour
{
    public const int VIEW_MODE_PLAYER = 1;
    public const int VIEW_MODE_SEXDUMMY = 2;
    private GameObject elinaArmature;
    private GameObject sexElinaArmature;
    private int VISIBLE_GO_ID;
    private GameObject sexElinaGameObject;

    // implementation
    private int viewMode;
    private int GO_ID;
    private List<System.Guid> disposables = new List<System.Guid>();

    void Awake()
    {

        elinaArmature = findArmatureFromRoot(transform);
        foreach (Transform trans in transform)
        {
            if (trans.gameObject.CompareTag("SexDummy"))
            {
                sexElinaGameObject = trans.gameObject;
                sexElinaArmature = findArmatureFromRoot(trans);
                break;
            }
        }
    }

    private GameObject findArmatureFromRoot(Transform rootTrans)
    {
        foreach (Transform trans in rootTrans)
        {
            if (trans.gameObject.name.Equals("root"))
            {
                return trans.gameObject;
            }
        }
        return null;
    }

    void Start()
    {
        GO_ID = gameObject.GetInstanceID();
        switchToPlayer();
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (u) => { switchToSexDummy(); }));
        disposables.Add(WickedObserver.AddListener("onStateRegainControl:" + GO_ID, (u) => { switchToPlayer(); }));
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (u) => { switchToPlayer(); }));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    // show the sex elina
    private void switchToSexDummy()
    {
        if (viewMode == VIEW_MODE_SEXDUMMY)
        {
            return;
        }
        VISIBLE_GO_ID = sexElinaGameObject.GetInstanceID();
        //clothingDummyMirror.Mirror();
        viewMode = VIEW_MODE_SEXDUMMY;
        setGameElinaVisibility(false);
        setSexElinaVisibility(true);
    }

    private void switchToPlayer()
    {
        if (viewMode == VIEW_MODE_PLAYER)
        {
            return;
        }
        VISIBLE_GO_ID = gameObject.GetInstanceID();
        viewMode = VIEW_MODE_PLAYER;
        setGameElinaVisibility(true);
        setSexElinaVisibility(false);
    }

    private void setGameElinaVisibility(bool visible)
    {
        if (this == null)
        {
            return;
        }
        if (elinaArmature.activeSelf != visible)
        {
            elinaArmature.SetActive(visible);
        }

        if (visible)
        {
            WickedObserver.SendMessage("OnShowRig:" + gameObject.GetInstanceID());
        }
        else
        {
            WickedObserver.SendMessage("OnHideRig:" + gameObject.GetInstanceID());
        }
        GameObject parent = elinaArmature.transform.parent.gameObject;
        foreach (Transform child in parent.transform)
        {
            if (child.GetComponent<RigSwapTarget>())
            {
                child.GetComponent<SkinnedMeshRenderer>().enabled = visible;
            }
        }
    }

    private void setSexElinaVisibility(bool visible)
    {
        WickedObserver.SendMessage("OnMirrorSetVisibility:" + GO_ID, visible);
        if (sexElinaArmature == null)
        {
            return;
        }
        sexElinaArmature.SetActive(visible);
        GameObject sexElinaParent = sexElinaArmature.transform.parent.gameObject;
        //sexElinaParent.SetActive(visible);
        // skim the top level for skinned meshes
        if (visible)
        {
            WickedObserver.SendMessage("OnShowRig:" + sexElinaParent.gameObject.GetInstanceID());
        }
        else
        {
            WickedObserver.SendMessage("OnHideRig:" + sexElinaParent.gameObject.GetInstanceID());
        }
        foreach (Transform child in sexElinaParent.transform)
        {
            if (child.GetComponent<RigSwapTarget>())
            {
                child.GetComponent<SkinnedMeshRenderer>().enabled = visible;
            }
        }
    }

    public bool IsRigVisible(GameObject rig)
    {
        if (rig == null)
        {
            return false;
        }
        return VISIBLE_GO_ID.Equals(rig.GetInstanceID());
    }

}
