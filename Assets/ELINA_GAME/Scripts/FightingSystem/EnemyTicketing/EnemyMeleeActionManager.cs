using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyMeleeActionManager : MonoBehaviour
{
    public const string ACTION_MELEE = "ACTION_MELEE";
    public const string ACTION_RANGE = "ACTION_RANGE";
    public const string ACTION_SEX = "ACTION_SEX";
    public const string ACTION_BONDAGE = "ACTION_BONDAGE";

    [HideInEditorMode]
    public bool startActionNow;

    [InfoBox("Set to below 0.01 to prevent an ability")]
    [BoxGroup("Probability")]
    [Range(0,1)]
    public float meleeChance;

    [BoxGroup("Probability")]
    [Range(0, 1)]
    public float sexChance;
    [BoxGroup("Probability")]
    [Range(0, 1)]
    public float bondageChance;

    [HideInInspector]
    public bool defeated;

    public bool hasTicket;
    public float warningTimeMelee = 1.0f;
    public float warningTimeRange = 5.0f;

    public string wishAction;
    private GameObject currentAlertEffect;
    private FreeFlowTargetable freeFlowTargetable;
    private List<Tuple<string,float, float>> actionProbabilities = new List<Tuple<string,float,float>>();
    /*private AiHentai aiHentai;*/

    // player
    private PlayerTargetable playerTargetable;
    void Awake()
    {
        /*playerTargetable = IAmElina.ELINA.gameObject.GetComponent<PlayerTargetable>();
        aiHentai = GetComponent<AiHentai>();*/
        freeFlowTargetable = GetComponent<FreeFlowTargetable>();
        GenerateBuckets();
    }

    private void GenerateBuckets()
    {
        float total = 0;
        if (meleeChance > 0.01f)
        {
            total += meleeChance;
        }
        if (sexChance > 0.01f)
        {
            total += sexChance;
        }
        if (bondageChance > 0.01f)
        {
            total += bondageChance;
        }

        float baseValue = 0;
        if (meleeChance > 0.01f)
        {
            float rangeSize = meleeChance / total;
            actionProbabilities.Add(new Tuple<string,float, float>(ACTION_MELEE, baseValue, baseValue + rangeSize));
            baseValue += rangeSize;
        }
        if (sexChance > 0.01f)
        {
            float rangeSize = sexChance / total;
            actionProbabilities.Add(new Tuple<string, float, float>(ACTION_SEX, baseValue, baseValue + rangeSize));
            baseValue += rangeSize;
        }
        if (bondageChance > 0.01f)
        {
            float rangeSize = bondageChance / total;
            actionProbabilities.Add(new Tuple<string, float, float>(ACTION_BONDAGE, baseValue, baseValue + rangeSize));
            baseValue += rangeSize;
        }

    }

    private string PickActionRandomly()
    {
        float rand = UnityEngine.Random.Range(0, 1.0f);
        foreach ( Tuple<string,float,float> actionRange in actionProbabilities)
        {
            float min = actionRange.Item2;
            float max = actionRange.Item3;
            if (min<= rand && rand <= max)
            {
                return actionRange.Item1;
            }
        }
        Debug.LogError("SEVERE OUT OF BOUNDS!");
        return ACTION_MELEE;
    }

    /// <summary>
    /// Enemy Grabs a ticket and returns the action they will take
    /// </summary>
    /// <returns>Delay until action executed</returns>
    public float StartRandomAction()
    {
        wishAction = ACTION_SEX;// = PickActionRandomly();
        SetTargetableState(wishAction);
        currentAlertEffect = DisplayFXWarning(wishAction);
        float actionDelay = GetActionDelay(wishAction);
        Invoke("DoTicketAction", actionDelay);
        return actionDelay;
    }

    private void DoTicketAction()
    {
        //For now return
        if (wishAction == ACTION_SEX)
        {
            //aiHentai.FuckTarget(IAmElina.ELINA.gameObject.GetInstanceID());
        }
        wishAction = null;
        freeFlowTargetable.targetableCounter = false;
    }

    public void CancelAttack()
    {
        wishAction = null;
        freeFlowTargetable.targetableCounter = false;
        CancelInvoke();
        if (currentAlertEffect != null)
        {
            Destroy(currentAlertEffect);
        }
    }

    public bool isFree()
    {
        return wishAction == null || wishAction.Length==0;
    }
    private void SetTargetableState(string type)
    {
        switch (type)
        {
            case ACTION_MELEE:
                freeFlowTargetable.targetableCounter = true;
                break;
            case ACTION_RANGE:
                break;
            case ACTION_SEX:
                freeFlowTargetable.targetableCounter = true;
                break;
            case ACTION_BONDAGE:
                freeFlowTargetable.targetableCounter = true;
                break;
        }
    }

    private float GetActionDelay(string type)
    {
        switch (type)
        {
            case ACTION_MELEE:
                return warningTimeMelee;
            case ACTION_RANGE:
                return warningTimeRange;
            case ACTION_SEX:
                return warningTimeMelee;                
            case ACTION_BONDAGE:
                return warningTimeMelee;
            default:
                return warningTimeMelee;
        }
    }

        private GameObject DisplayFXWarning(string type)
    {
        string effectName = "AlertMelee";
        float warningTime = warningTimeMelee;
        switch (type)
        {
            case ACTION_MELEE:
                warningTime = warningTimeMelee;
                effectName = "AlertMelee";
                break;
            case ACTION_RANGE:
                warningTime = warningTimeRange;
                effectName = "AlertRange";
                break;
            case ACTION_SEX:
                warningTime = warningTimeMelee;
                effectName = "AlertSex";
                break;
            case ACTION_BONDAGE:
                warningTime = warningTimeMelee;
                effectName = "AlertBondage";
                break;
        }
        return SpecialFxRequestBuilder.newBuilder(effectName)
                .setOwner(transform, true)
                .setLifespan(warningTime)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.HALF_PLAYER_HEIGHT, 0))
                .setOffsetRotation(new Vector3(-90, 0, 0))
                .build().Play();
    }
}
