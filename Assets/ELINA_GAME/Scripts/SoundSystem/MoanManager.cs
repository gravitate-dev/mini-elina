
using System.Collections.Generic;
using UnityEngine;

public class MoanManager : MonoBehaviour
{
    public string style;
    //private SoundyController playingSoundyController;
    List<System.Guid> disposables = new List<System.Guid>();
    private void Awake()
    {
        /*int GO_ID = GetComponentInParent<HentaiAnimatorController>().gameObject.GetInstanceID();
        disposables.Add(WickedObserver.AddListener("onStartHentaiMove:" + GO_ID, (obj) =>
        {
            HMove move = (HMove)obj;
            // todo get moan style
            style = "sex-slow";
        }));
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (obj) =>
        {
            HeavyBreath();
        }));*/
    }

    public void HeavyBreath()
    {
        // todo
    }
    public void Moan()
    {
        if (!CanPlayNewSound())
        {
            return;
        }
        //playingSoundyController = SoundSystem.INSTANCE.PlaySound(style, transform);
    }

    private bool CanPlayNewSound()
    {
        /*if (playingSoundyController == null)
        {
            return true;
        }
        return !playingSoundyController.AudioSource.isPlaying;*/
        return false;
    }
}
