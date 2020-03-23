using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyActionManager : MonoBehaviour
{
    public const string ACTION_MELEE = "ACTION_MELEE";
    public const string ACTION_RANGE = "ACTION_RANGE";
    public const string ACTION_SEX = "ACTION_SEX";

    [HideInEditorMode]
    public bool startActionNow;

    [InfoBox("Set to below 0.01 to prevent an ability")]
    [BoxGroup("Probability")]
    [Range(0,1)]
    public float meleeChance;

    [BoxGroup("Probability")]
    [Range(0, 1)]
    public float sexChance;

    [HideInInspector]
    public bool defeated;

    public float warningTimeMelee = 1.0f;
    public float warningTimeRange = 5.0f;

    public string wishAction;
    private GameObject currentAlertEffect;
    private FreeFlowTargetable freeFlowTargetable;
    private List<Tuple<string,float, float>> actionProbabilities = new List<Tuple<string,float,float>>();
    private AiHentai aiHentai;
    private FreeFlowAnimatorController freeFlowAnimatorController;

    // player
    private FreeFlowCharacterController playerFreeFlowCharacterController;
    void Awake()
    {
        freeFlowAnimatorController = GetComponent<FreeFlowAnimatorController>();
        playerFreeFlowCharacterController = IAmElina.ELINA.gameObject.GetComponent<FreeFlowCharacterController>();
        aiHentai = GetComponent<AiHentai>();
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
    public float StartRandomAttack()
    {
        wishAction = PickActionRandomly();
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
            aiHentai.FuckTarget(IAmElina.ELINA);
        } else if (wishAction == ACTION_MELEE)
        {
            doMelee();
        }
        wishAction = null;
    }

    private void doMelee()
    {
        FreeFlowAttackMove attackMove = FreeFlowMovePicker.INSTANCE.PickMoveRandomly(transform, IAmElina.ELINA);
        if (attackMove == null)
        {
            return;
        }
        attackMove.victimGO_ID = IAmElina.ELINA.GetInstanceID();
        attackMove.attacker = gameObject;
        attackMove.victim = IAmElina.ELINA;

        FreeFlowAnimatorController victimFreeFlowAnimatorController = attackMove.victim.GetComponent<FreeFlowAnimatorController>();
        int result = FreeFlowTargetable.HIT_RESULT_NORMAL;
        attackMove.victimReactionId = result;

        // victim
        victimFreeFlowAnimatorController.startFreeFlowAttack(attackMove);
        playerFreeFlowCharacterController.VictimHitRoutines(attackMove);
        
        // attacker
        freeFlowAnimatorController.startFreeFlowAttack(attackMove);
    }

    public void CancelAttack()
    {
        wishAction = null;
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
        }
        return SpecialFxRequestBuilder.newBuilder(effectName)
                .setOwner(transform, true)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.HALF_PLAYER_HEIGHT, 0))
                .setOffsetRotation(new Vector3(-90, 0, 0))
                .build().Play();
    }

    public bool isWishingMeleeAttack()
    {
        return wishAction == ACTION_MELEE;
    }
}
