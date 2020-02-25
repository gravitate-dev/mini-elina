using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmHider : MonoBehaviour
{
    [HideInInspector]
    public bool hidingArms;
    private List<GameObject> arms = new List<GameObject>();
    private bool shownArmsYet;
    private List<System.Guid> disposables = new List<System.Guid>();

    void Start()
    {
        int GO_ID = gameObject.GetInstanceID();
        // TODO assign gameobjects instead of use this wasteful method
        findArms(gameObject);
        disposables.Add(WickedObserver.AddListener("OnHideArms:"+GO_ID, (obj) =>
        {
            hidingArms = (bool)obj;
        }));
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }
    private void findArms(GameObject seed)
    {
        Queue<GameObject> queue = new Queue<GameObject>();
        queue.Enqueue(seed);
        while (queue.Count != 0)
        {
            GameObject curr = queue.Dequeue();
            foreach (Transform t in curr.transform)
            {
                if (isAnArm(t.gameObject))
                {
                    arms.Add(t.gameObject);
                }
                else
                {
                    queue.Enqueue(t.gameObject);
                }
            }
        }
    }

    private bool isAnArm(GameObject t)
    {
        return t.name.Equals("shoulder.l") || t.name.Equals("shoulder.r");
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (hidingArms)
        {
            hideArms();
        } else
        {
            if (!shownArmsYet)
            {
                shownArmsYet = true;
                showArms();
            }
        }
    }

    private void hideArms()
    {
        shownArmsYet = false;
        foreach (var go in arms)
        {
            go.transform.localScale = Vector3.zero;
        }
    }
    private void showArms()
    {
        foreach (var go in arms)
        {
            go.transform.localScale = Vector3.one;
        }
    }
}
