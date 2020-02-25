using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HentaiChibiGui : MonoBehaviour
{
    public static int CHIBI_NORMAL = 0;
    public static int CHIBI_FIGHTING = 1;
    public static int CHIBI_BONDAGE = 2;
    public static int CHIBI_ENJOYING = 3;
    public static int CHIBI_CLIMAX = 4;
    public static int CHIBI_SILLY = 5;

    public Sprite emptySprite;
    /// <summary>
    /// Order MATTERS
    /// </summary>
    public Sprite[] chibis;

    private Image image;
    private int GO_ID;
    private List<System.Guid> disposables = new List<System.Guid>();
    void Start()
    {
        GO_ID = IAmElina.ELINA.GetInstanceID();
        disposables.Add(WickedObserver.AddListener("OnDisplayChibi", OnDisplayChibi));
        disposables.Add(WickedObserver.AddListener("onOrgasm:" + GO_ID, onOrgasm));
        image = GetComponent<Image>();
        image.sprite = emptySprite;
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    private void onOrgasm(object unused)
    {
        image.sprite = chibis[CHIBI_CLIMAX];
        CancelInvoke();
        Invoke("HideChibi", 10.0f);
    }

    public void OnDisplayChibi(object message)
    {
        int idx = (int)message;
        image.sprite = chibis[idx];
        CancelInvoke();
        Invoke("HideChibi",10.0f);
    }

    private void HideChibi()
    {
        image.sprite = emptySprite;
    }

}
