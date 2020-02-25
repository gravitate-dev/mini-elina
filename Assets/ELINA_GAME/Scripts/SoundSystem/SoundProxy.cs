using UnityEngine;

public class SoundProxy : MonoBehaviour
{
    int timesNeededToMoan = 2;
    int hitTimes = 0;

    private MoanManager moanManager;
    private void Awake()
    {
        moanManager = GetComponent<MoanManager>();
    }
    public void PlaySoundProxy(string soundGroup)
    {
        hitTimes++;
        //SoundSystem.INSTANCE.PlaySound(soundGroup, transform);
        if (hitTimes == timesNeededToMoan)
        {
            timesNeededToMoan = UnityEngine.Random.Range(2, 4);
            hitTimes = 0;
            if (moanManager != null)
            {
                moanManager.Moan();
            }
        }
    }
}
