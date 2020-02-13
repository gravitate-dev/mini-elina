using Invector.vCharacterController.AI.FSMBehaviour;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyTicketHolder : MonoBehaviour
{

    public const string ACTION_MELEE = "ACTION_MELEE";
    public const string ACTION_RANGE = "ACTION_RANGE";
    public const string ACTION_SEX = "ACTION_SEX";
    public const string ACTION_BONDAGE = "ACTION_BONDAGE";

    [Range(0, 1)]
    public float meleeChance;
    [Range(0, 1)]
    public float rangeChance;
    [Range(0, 1)]
    public float sexChance;
    [Range(0, 1)]
    public float bondageChance;

    [HideInInspector]
    public bool defeated;

    public bool hasTicket;
    public float warningTimeMelee = 3.0f;
    public float warningTimeRange = 5.0f;

    private string currentAction;
    private GameObject currentAlertEffect;
    private FreeFlowTargetable freeFlowTargetable;
    private vFSMBehaviourController fsmBehaviorController;
    private vFSMState stateAttack;
    private List<Tuple<string,float, float>> actionProbabilities = new List<Tuple<string,float,float>>();

    private float ticketLockOutTime;
    private const float DEBOUNCE_NEXT_TICKET_TIMEOUT = 2.0f;
    void Awake()
    {
        freeFlowTargetable = GetComponent<FreeFlowTargetable>();
        GenerateBuckets();
        fsmBehaviorController = GetComponent<vFSMBehaviourController>();

        // setup attack, rape, bondage states
        foreach (vFSMState state in fsmBehaviorController.fsmBehaviour.states)
        {
            if (state.Name.Equals("ENEMY_MELEE_ATTACK"))
            {
                stateAttack = state;
            }
        }
    }

    /// <summary>
    /// If target
    /// NOT SEXING
    /// NOT BEING SEXED
    /// NOT STUNNED
    /// </summary>
    /// <returns></returns>
    public bool CanGrabTicket()
    {
        if (defeated)
        {
            return false;
        }
        if (ticketLockOutTime > Time.time)
        {
            return false;
        }
        bool isBusy = freeFlowTargetable.isStunned || freeFlowTargetable.isSexing || freeFlowTargetable.isChargingAttack;
        return !isBusy && !hasTicket;
    }

    private void GenerateBuckets()
    {
        float total = 0;
        if (meleeChance > 0.01f)
        {
            total += meleeChance;
        }
        if (rangeChance > 0.01f)
        {
            total += rangeChance;
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
        if (rangeChance > 0.01f)
        {
            float rangeSize = rangeChance / total;
            actionProbabilities.Add(new Tuple<string, float, float>(ACTION_RANGE, baseValue, baseValue + rangeSize));
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
    /// <returns></returns>
    public string GiveTicket()
    {
        hasTicket = true;
        currentAction = PickActionRandomly();
        SetTargetableState(currentAction);
        //currentAlertEffect = DisplayFXWarning(currentAction);
        float actionDelay = GetActionDelay(currentAction);
        Invoke("DoTicketAction", actionDelay);
        return currentAction;
    }

    private void DoTicketAction()
    {
        //For now return
        hasTicket = false;
        freeFlowTargetable.targetableCounter = false;

        // todo findout how to make enemy attack player


    }

    public void CancelAttack()
    {
        ticketLockOutTime = Time.time + DEBOUNCE_NEXT_TICKET_TIMEOUT;
        freeFlowTargetable.targetableCounter = false;
        CancelInvoke();
        if (currentAlertEffect != null)
        {
            Destroy(currentAlertEffect);
        }
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
        /*return SpecialFxRequestBuilder.newBuilder(effectName)
                .setOwner(transform, true)
                .setLifespan(warningTime)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.HALF_PLAYER_HEIGHT, 0))
                .setOffsetRotation(new Vector3(-90, 0, 0))
                .build().Play();*/
        return null;
    }
}
