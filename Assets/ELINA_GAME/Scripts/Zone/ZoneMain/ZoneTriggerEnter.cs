using System.Collections.Generic;
using UnityEngine;

public class ZoneTriggerEnter : MonoBehaviour
{
    public bool destroySelfOnEnter = true;
    private bool started;

    private void OnTriggerEnter(Collider other)
    {
        if (started)
        {
            return;
        }
        ZoneController zoneController = GetComponentInParent<ZoneController>();
        if (other.gameObject.CompareTag("Player"))
        {
            started = true;
        }
        zoneController.StartZone();
        if (destroySelfOnEnter)
        {
            Destroy(gameObject);
        }
    }
}
