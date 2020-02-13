using UnityEngine;

public class FreeFlowTarget
{
    public enum AttackState
    {
        none,
        charing,
        will_parry
    }
    public enum FightState
    {
        none,
        stunned
    }
    public float distance;
    public bool isBehindMe;
    public bool hasShield;

    public AttackState attackState;
    public FightState fightState;
    
    public GameObject gameObject;

    public FreeFlowTarget() { }
    public FreeFlowTarget(FreeFlowTarget copy)
    {
        distance = copy.distance;
        isBehindMe = copy.isBehindMe;
        hasShield = copy.hasShield;
        attackState = copy.attackState;
        fightState = copy.fightState;
        gameObject = copy.gameObject;
    }
}
