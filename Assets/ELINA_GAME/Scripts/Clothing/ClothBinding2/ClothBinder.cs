using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothBinder : MonoBehaviour
{
    public bool showOnlyDuringSex;
    public bool hideDuringSex;
    private bool rigVisibility;
    private bool sexVisibility;
    private bool isPreviewItem;

    public bool isMirroredInstance;
    /// <summary>
    /// Change this via the setGenderId
    /// </summary>
    [ValueDropdown("BindType")]
    public int bindType;

    public bool bindToHead;

    // independent is used for hairs
    public const int BIND_TYPE_INDEPENDENT = 1;
    public const int BIND_TYPE_COPY = 2;
    public const int BIND_TYPE_MAGICA = 3;

    public int SHOW_STATE = -1;
    private const int STATE_VISIBLE = 0;
    private const int STATE_HIDDEN = 1;


    // magic variables
    [InfoBox("Do not mix this script and this prefab together")]
    public GameObject magicaClothingPrefab;
    private int magicaBindId = -1;
    //private MagicaCloth.MagicaAvatar magicaAvatar;

    // item is created the binding is done by the cloth binder
    // once bound it should tell the ClothDirector i was bound
    // by finding the parents tag is SexDummy
    // controls the show / hide states
    private ClothMirror clothMirror;

    private static IEnumerable BindType = new ValueDropdownList<int>()
{
    { "Independent", 1 },
    { "CopyBones", 2 },
    { "MagicaAvatar", 3 },
};

    private List<System.Guid> disposables = new List<System.Guid>();
    private GameObject wearer;
    private SkinnedMeshRenderer myRenderer;
    private bool forceVisiblity;

    public void SetToPreviewItem()
    {
        if (isPreviewItem)
        {
            return;
        }
        isPreviewItem = true;
        disposables.Add(WickedObserver.AddListener("OnInventoryClose", (obj) =>
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }));
    }

    private void Awake()
    {
        StartCoroutine(Setup());
    }

    private IEnumerator Setup()
    {
        WaitForEndOfFrame delay = new WaitForEndOfFrame();
        //yield return delay;
        /// safe delay <see cref="HoverPreviewitemSlot"/>
        int attempts = 10;
        while (attempts-- > 0)
        {

            if (FindParent())
            {
                break;
            }
            yield return delay;
        }
        if (attempts < 0)
        {
            Debug.LogError("UNABLE TO SETUP FOR " + gameObject.name);
        }
        FindMyRenderer();
        if (bindType == BIND_TYPE_COPY)
        {
            MapAndBind();
        }
        else if (bindType == BIND_TYPE_MAGICA)
        {
            //magicaAvatar = GetComponentInParent<MagicaCloth.MagicaAvatar>();
        }
        BindToRigVisibility();
        BindToSexVisibility();
        if (!isPreviewItem)
        {
            LinkToClothDirector();
        }
        UpdateVisibility();
    }

    private void OnDestroy()
    {
        if (bindType == BIND_TYPE_MAGICA)
        {
            // extra procesing
            Hide();
        }
        WickedObserver.SendMessage("OnDestroyClothBinder:" + gameObject.GetInstanceID());
        WickedObserver.RemoveListener(disposables);
    }


    private void BindToSexVisibility()
    {
        HentaiSexCoordinator hentaiSexCoordinator = GetComponentInParent<HentaiSexCoordinator>();
        if (hentaiSexCoordinator == null)
        {
            return;
        }
        sexVisibility = hentaiSexCoordinator.IsSexing();
        int id = hentaiSexCoordinator.gameObject.GetInstanceID();
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + id, (hmove) =>
        {
            if (!sexVisibility)
            {
                return;
            }
            sexVisibility = false;
            UpdateVisibility();
        }));
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + id, (hmove) =>
        {
            if (sexVisibility)
            {
                return;
            }
            sexVisibility = true;
            UpdateVisibility();
        }));
    }
    private void BindToRigVisibility()
    {
        HentaiRigSwapper hentaiRigSwapper = GetComponentInParent<HentaiRigSwapper>();
        if (hentaiRigSwapper == null)
        {
            // clothing always shown
            rigVisibility = true;
            return;
        }
        // only components that have a rig swapper will use this method
        GameObject rig = GetComponentInParent<Animancer.HybridAnimancerComponent>().gameObject;
        rigVisibility = hentaiRigSwapper.IsRigVisible(rig);
        disposables.Add(WickedObserver.AddListener("OnShowRig:" + rig.GetInstanceID(), (unused) =>
        {
            rigVisibility = true;
            UpdateVisibility();
        }));
        disposables.Add(WickedObserver.AddListener("OnHideRig:" + rig.GetInstanceID(), (unused) =>
        {
            rigVisibility = false;
            UpdateVisibility();
        }));
    }

    public void SetClothingPair(int goId)
    {
        disposables.Add(WickedObserver.AddListener("OnDestroyClothBinder:" + goId, (unused) =>
        {
            Destroy(gameObject);
        }));
    }
    private void LinkToClothDirector()
    {
        // only link if it is swappable
        HentaiRigSwapper hentaiRigSwapper = GetComponentInParent<HentaiRigSwapper>();
        if (hentaiRigSwapper == null)
        {
            // no linking
            return;
        }
        // if the animancer parent has tag sex dummy i am a mirror
        Animancer.HybridAnimancerComponent owner = GetComponentInParent<Animancer.HybridAnimancerComponent>();
        if (owner.CompareTag("SexDummy"))
        {
            isMirroredInstance = true;
            return;
        } else
        {
            isMirroredInstance = false;
        }
        ClothMirror clothMirror = GetComponentInParent<ClothMirror>();
        clothMirror.Link(this);
    }

    public void ForceHide()
    {
        forceVisiblity = true;
        Hide();
    }
    public void ForceUpdateVisibility()
    {
        forceVisiblity = true;
        UpdateVisibility();
    }
    private void UpdateVisibility()
    {
        if (!rigVisibility)
        {
            Hide();
            return;
        }
        if (hideDuringSex && sexVisibility)
        {
            Hide();
            return;
        }
        if (showOnlyDuringSex && !sexVisibility)
        {
            Hide();
            return;
        }
        Show();
    }
    public void Show()
    {
        if (SHOW_STATE == STATE_VISIBLE && !forceVisiblity)
        {
            return;
        }
        
        if (bindType == BIND_TYPE_MAGICA)
        {
            if (magicaBindId != -1)
            {
                //magicaAvatar.DetachAvatarParts(magicaBindId);
                magicaBindId = -1;
            }
        }
        else
        {
            gameObject.SetActive(true);
        }
        forceVisiblity = false;
        SHOW_STATE = STATE_VISIBLE;
    }

    public void Hide()
    {
        if (SHOW_STATE == STATE_HIDDEN && !forceVisiblity)
        {
            return;
        }
        forceVisiblity = false;
        SHOW_STATE = STATE_HIDDEN;
        if (bindType == BIND_TYPE_MAGICA)
        {
            if (magicaBindId == -1)
            {
                return;
            }
            //magicaAvatar.DetachAvatarParts(magicaBindId);
            magicaBindId = -1;
            ColorableItemProxy proxy = GetComponent<ColorableItemProxy>();
            if (proxy != null)
            {
                proxy.colorableItem = null;
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void FindMyRenderer()
    {
        SkinnedMeshRenderer temp = GetComponent<SkinnedMeshRenderer>();
        if (temp != null)
        {
            myRenderer = temp;
            return;
        }
        foreach (Transform child in transform)
        {
            temp = child.GetComponent<SkinnedMeshRenderer>();
            if (temp != null)
            {
                myRenderer = temp;
                return;
            }
        }
    }
    private bool FindParent()
    {
        Animancer.HybridAnimancerComponent path = GetComponentInParent<Animancer.HybridAnimancerComponent>();
        if (path != null)
        {
            wearer = path.gameObject;
            //wearerGO_ID = path.gameObject.GetInstanceID();
            return true;
        }
        return false;
    }

    private void MapAndBind()
    {
        SkinnedMeshRenderer TargetMeshRenderer = null;
        foreach (Transform trans in wearer.transform)
        {
            if (trans.GetComponent<BindClothingTarget>())
            {
                TargetMeshRenderer = trans.GetComponent<SkinnedMeshRenderer>();
                break;
            }
        }

        if (TargetMeshRenderer == null)
        {
            Debug.LogError("Unable to find target mesh renderer");
            return;
        }
        Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
        foreach (Transform bone in TargetMeshRenderer.bones)
            boneMap[bone.gameObject.name] = bone;

        if (myRenderer == null)
        {
            Debug.LogError("Unable to map with method BONE_COPY");
            return;
        }
        if (myRenderer.rootBone == null)
        {
            if (myRenderer.bones.Length != 0)
            {
                myRenderer.rootBone = myRenderer.bones[0];
            }
            else
            {
                myRenderer.rootBone = TargetMeshRenderer.bones[0];
            }
        }
        Transform[] newBones = new Transform[myRenderer.bones.Length];
        if (myRenderer.bones.Length > 0 && myRenderer.bones[0] == null)
        {
            return;
        }
        for (int i = 0; i < myRenderer.bones.Length; ++i)
        {
            GameObject bone = myRenderer.bones[i].gameObject;
            //Debug.Log("bone: " + bone.name);
            if (!boneMap.TryGetValue(bone.name, out newBones[i]))
            {
                //Debug.Log("Unable to map bone \"" + bone.name + "\" to target skeleton.");
                continue;
            }
        }
        myRenderer.bones = newBones;
    }

    public void SelfDestructIn(float seconds)
    {
        CancelInvoke();
        Invoke("DestroySelf", seconds);
    }
    
    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}
