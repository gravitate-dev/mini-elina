using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Only used by AI
/// This is so that the sex shadow clone can reach the destination in time
/// </summary>
public class HentaiReservationManager : MonoBehaviour
{
    public GameObject reserver;

    public void ReserveForMe(GameObject reserver)
    {
        this.reserver = reserver;
    }

    public void UnReserve(GameObject reserver)
    {
        if (this.reserver == null)
        {
            this.reserver = null;
        } else if (this.reserver !=null && this.reserver == reserver)
        {
            this.reserver = null;
        }
        
    }
}
