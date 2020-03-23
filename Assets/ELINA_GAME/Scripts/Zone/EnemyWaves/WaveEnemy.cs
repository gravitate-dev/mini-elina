using UnityEngine;

/// <summary>
/// When the enemy dies it needs to tell the wave about it
/// </summary>
public class WaveEnemy : MonoBehaviour
{
    private int waveId;

    public void setWaveId(int waveId)
    {
        this.waveId = waveId;
    }

    private void OnDestroy()
    {
        WickedObserver.SendMessage("OnWaveEnemyDefeated:" + waveId);
    }
}
