using UnityEngine;

/// <summary>
/// Used for the SexyTime animator state
/// Notifies when one sexy time has completed
/// </summary>
public class HentaiSexyTimeStateAnimatorWatcher : StateMachineBehaviour
{
    double DEBOUNCER_DELAY_MS = 1000;
    double DEBOUNCER_LAST_TIME_EXIT;
    private int GO_ID;
    System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    public HentaiSexyTimeStateAnimatorWatcher()
    {
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {

    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        double cur_time = (System.DateTime.UtcNow - epochStart).TotalMilliseconds;
        if (DEBOUNCER_LAST_TIME_EXIT + DEBOUNCER_DELAY_MS > cur_time)
        {
            return;
        }
        DEBOUNCER_LAST_TIME_EXIT = cur_time;
        if (GO_ID == 0)
        {
            GO_ID = animator.GetInteger("GO_ID");
        }
        WickedObserver.SendMessage("onSexyTimeLoop:" + GO_ID);
    }
}
