using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class ClothingDummyMirror : MonoBehaviour
{
    //parent must be the humanoid object
    private GameObject parent;

    private GameObject parentHead;
    [InfoBox("Sexable head that holds hairs,accessories")]
    public GameObject childHead;

    private List<System.Guid> disposables = new List<System.Guid>();
    void Start()
    {
        if (transform.parent != null)
        {
            parent = transform.parent.GetComponentInParent<Animator>().gameObject;
            Animator parentAnimator = parent.GetComponent<Animator>();
            if (parentAnimator != null && parentAnimator.isHuman)
            {
                parentHead = parentAnimator.GetBoneTransform(HumanBodyBones.Head).gameObject;
            }
            else
            {
                parentHead = parent.transform.Find("head").gameObject;
            }
        }
        disposables.Add(WickedObserver.AddListener("OnUpdateClothing:" + parent.GetInstanceID(), (obj)=>
        {
            bool forceActivate = (bool)obj;
            RefreshHeadAccessories(forceActivate);
            RefreshClothing(forceActivate);
        }));
        RefreshHeadAccessories(false);
        RefreshClothing(false);
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    private void RefreshHeadAccessories(bool shouldShow)
    {
        // clean child
        foreach (GameObject go in gatherClothing(childHead, true))
        {
            Destroy(go);
        }

        // fetch parent
        List<GameObject> parentGoHeadAccessories = gatherClothing(parentHead, true);
        var amIActive = isSexElinaActive() || shouldShow;
        foreach (var go in parentGoHeadAccessories)
        {
            var newGo = Instantiate(go, childHead.transform);
            newGo.SetActive(amIActive);
        }
    }
    private void RefreshClothing(bool shouldShow)
    {
        List<GameObject> myClothes = getMyClothes();
        foreach (GameObject go in myClothes)
        {
            Destroy(go);
        }
        List<GameObject> parentGo =  getParentClothes();
        var amIActive = isSexElinaActive() || shouldShow;
        foreach (var go in parentGo)
        {
            var newGo = Instantiate(go, transform);
            newGo.SetActive(amIActive);
            newGo.GetComponent<SkinnedMeshRenderer>().enabled = amIActive;
            newGo.GetComponent<SkinnedMeshRenderer>().updateWhenOffscreen = true;
            if (newGo.GetComponent<BindClothingToWearer>() != null)
            {
                newGo.GetComponent<BindClothingToWearer>().rebind();
            }
        }
    
    }


    private List<GameObject> getParentClothes()
    {
        List<GameObject> newClothes = new List<GameObject>();
        // updates the currently worn clothing
        if (parent != null)
        {
            newClothes.AddRange(gatherClothing(parent, true));
            foreach (Transform t in parent.transform)
            {
                if (t.name.Equals("MountPoints"))
                {
                    newClothes.AddRange(gatherClothing(t.gameObject, false));
                    break;
                }
            }
        }
        return newClothes;
    }

    private List<GameObject> getMyClothes()
    {
        return gatherClothing(gameObject, true);
    }
    /// <summary>
    /// Only called for players
    /// Uses BFS
    /// </summary>
    /// <param name="mountPoint"></param>
    /// <returns></returns>
    public List<GameObject> gatherClothing(GameObject seed, bool firstLevelOnly)
    {
        /* BFS implementation */
        List<GameObject> ans = new List<GameObject>();
        Queue<GameObject> paths = new Queue<GameObject>();
        paths.Enqueue(seed);
        while (paths.Count != 0)
        {
            GameObject curr = paths.Dequeue();
            foreach (Transform t in curr.transform)
            {
                if (t.GetComponent<SkinnedMeshRenderer>())
                {
                    // grab all transferrable clothes.
                    // BindClothingTarget = human wearer
                    // BindClothingForever is stuff like dick and wings
                    bool isClothing = (t.GetComponent<BindClothingTarget>() == null);
                    bool canMirror = (t.GetComponent<DoNotMirrorClothing>() == null); 
                    if (isClothing && canMirror)
                    {
                        ans.Add(t.gameObject);
                    }
                } else if (t.GetComponent<BindAccessory>())
                {
                    ans.Add(t.gameObject);
                }
                paths.Enqueue(t.gameObject);
            }
            if (firstLevelOnly)
            {
                break;
            }
        }
        return ans;
    }
    private bool isSexElinaActive()
    {
        return transform.Find("root").gameObject.activeSelf;
    }
}
