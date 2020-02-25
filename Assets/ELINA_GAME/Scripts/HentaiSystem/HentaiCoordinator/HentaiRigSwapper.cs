using Invector.vCharacterController;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// swaps rigs also updates clothes when swapping
/// </summary>
public class HentaiRigSwapper : MonoBehaviour
{

    private GameObject elinaArmature;
    private GameObject sexElinaArmature;
    private int GO_ID;
    private List<System.Guid> disposables = new List<System.Guid>();

    void Awake()
    {
        if (elinaArmature == null)
        {
            elinaArmature = findArmatureFromRoot(transform);
        }
        if (sexElinaArmature == null)
        {
            Transform sexElinaTransform = null;
            foreach (Transform trans in transform)
            {
                if (trans.gameObject.CompareTag("SexDummy"))
                {
                    sexElinaTransform = trans;
                    break;
                }
            }
            if (sexElinaTransform != null)
            {
                sexElinaArmature = findArmatureFromRoot(sexElinaTransform);
            }
        }
    }

    private GameObject findArmatureFromRoot(Transform rootTrans)
    {
        GameObject armature = null;
        foreach (Transform trans in rootTrans)
        {
            if (trans.gameObject.name.Equals("root"))
            {
                armature = trans.gameObject;
                break;
            }
        }
        return armature;
    }
    
    void Start()
    {
        GO_ID = gameObject.GetInstanceID();
        switchToPlayer();
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (u)=> { switchToSexDummy(); }));
        disposables.Add(WickedObserver.AddListener("onStateRegainControl:" + GO_ID, (u)=> { switchToPlayer(); }));
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (u)=> { switchToPlayer(); }));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    // show the sex elina
    private void switchToSexDummy()
    {
        setGameElinaVisibility(false);
        setSexElinaVisibility(true);
    }

    private void switchToPlayer()
    {
        setGameElinaVisibility(true);
        setSexElinaVisibility(false);
    }

    private void setGameElinaVisibility(bool visible)
    {
        if (this == null)
        {
            return;
        }
        try
        {
            if (elinaArmature.activeSelf != visible)
            {
                elinaArmature.SetActive(visible);
            }
        } catch (System.Exception)
        {
            Debug.Log("ELINA ARMATURE NOT THERE ON " + gameObject.name);
        }

        GameObject parent = elinaArmature.transform.parent.gameObject;
        foreach (Transform t in parent.transform)
        {
            if (t.GetComponent<SkinnedMeshRenderer>() != null)
            {
                //t.GetComponent<SkinnedMeshRenderer>().enabled = visible;
                t.gameObject.SetActive(visible);
            }
            if (t.name.Equals("MountPoints"))
            {
                t.gameObject.SetActive(visible);
            }
        }
    }
    
    private void setSexElinaVisibility(bool visible)
    {
        if (visible)
        {
            WickedObserver.SendMessage("OnUpdateClothing:" + GO_ID, true);
        }
        if (sexElinaArmature == null)
        {
            return;
        }
        sexElinaArmature.SetActive(visible);
        GameObject sexElinaParent = sexElinaArmature.transform.parent.gameObject;
        foreach (Transform t in sexElinaParent.transform)
        {
            if (t.GetComponent<SkinnedMeshRenderer>() != null)
            {
                t.gameObject.SetActive(visible);
                //t.GetComponent<SkinnedMeshRenderer>().enabled = visible;
            }
        }
    }
}
