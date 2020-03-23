using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HentaiHeatController : MonoBehaviour
{
    public float currentHeat = 0.0f;
    public float currentLust = 0.0f;
    /// <summary>
    /// This is the heat from the sex animations
    /// </summary>
    private float animationHeatRate = 0f;
    private float restHeatRate = -0.5f;
    private float fuckHeatRate = 5.0f;
    private float heatToOrgasm = 100.0f;
    private float floorHeat = 0.0f; // the minimum orgasm state the player can return to
    
    private bool isPlayer;

    //private Progressor progressor;
    private List<System.Guid> disposables = new List<System.Guid>();
    public void Awake()
    {
        int GO_ID = gameObject.GetInstanceID();
        StartCoroutine(updateHeat());
        isPlayer = GO_ID.Equals(IAmElina.ELINA.GetInstanceID());
        if (isPlayer)
        {
            /*if (progressor == null)
            {
                // cheap tech debt
                progressor = GameObject.Find("Image - Cum").GetComponent<Progressor>();
            }*/
        }
        disposables.Add(WickedObserver.AddListener("OnAnimationHeatRateChange:" + GO_ID, (obj)=>
        {
            this.animationHeatRate = (float)obj;
        }));
        disposables.Add(WickedObserver.AddListener(HentaiSexCoordinator.EVENT_STOP_H_MOVE_LOCAL + GO_ID, (move) =>
        {
            HMove temp = (HMove)move;
            ZeroAnimHeatRate();
            if (temp!=null && temp.playground)
            {
                ZeroHeat();
                ZeroLust();
            }
        }));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (gameObject.GetInstanceID().Equals(IAmElina.ELINA.GetInstanceID()))
            {
                currentHeat = 99;
            }
        }
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    public void ZeroHeat()
    {
        currentHeat = 0;
    }

    public void ZeroLust()
    {
        currentLust = 0;
    }

    public void ZeroAnimHeatRate()
    {
        animationHeatRate = 0;
    }

    private IEnumerator updateHeat()
    {
        while (true)
        {
            if (animationHeatRate > 0)
            {
                currentHeat = currentHeat + (fuckHeatRate* animationHeatRate);
            } else
            {
                // lowers heat
                currentHeat = (Mathf.Max(currentHeat + restHeatRate, floorHeat));
            }
            /*if (isPlayer)
            {
                progressor.SetProgress(getOrgasmPercentage());
            }*/
            yield return new WaitForSeconds(1.0f);
        }
    }
   
    public float getOrgasmPercentage()
    {
        return currentHeat / heatToOrgasm;
    }

}