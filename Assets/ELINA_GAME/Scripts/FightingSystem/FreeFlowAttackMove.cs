using Newtonsoft.Json;
using System.ComponentModel;
using UnityEngine;

/// <summary>
/// Class that represents an attack between one or more parties
/// </summary>
[System.Serializable]
public class FreeFlowAttackMove
{
    [JsonIgnore]
    public AnimationClip DEBUG_ATTACK;
    [JsonIgnore]
    public AnimationClip DEBUG_VICTIM;
    //todo disable time when punching recovery time???
    public string moveName;
    // 1 = kick
    // 2 = punch
    // 3 = throw
    public int moveType;

    // TODO SPIDERMAN set minimum distance
    public float idealDistance;
    public string attackerAnimation;

    [DefaultValue(1)]
    public float attackerAnimationSpeed;
    // Time that the player cant attack (to prevent animation spam)
    public float attackerLockTimeAfterHit;

    // should the player do the attack backwards
    public bool backwardsAttack;
    [DefaultValue(false)]
    public bool finisher;

    [DefaultValue(false)]
    public bool disabled;

    [DefaultValue(false)]
    public bool noCrossfade;

    [DefaultValue(true)]
    public bool knockback;

    // for hits that did not KO
    public float victimAnimationDelay;
    public string victimAnimation;
    [DefaultValue(1)]
    public float victimStunTime;

    /// <summary>
    /// While the whole attack is playing plus the stun time, target is ignored for attacks when TRUE
    /// </summary>
    [DefaultValue(false)]
    public bool victimImmuneDuringAttack;

    [JsonIgnore]
    public GameObject attacker;
    [JsonIgnore]
    public GameObject victim;

    [JsonIgnore]
    public string note; // debug note for dev work

    [JsonIgnore]
    public int victimGO_ID;
    
    [JsonIgnore]
    public int victimReactionId; // the reaction stun,normal,knockdown

    public FreeFlowAttackMove() { }
    public FreeFlowAttackMove(FreeFlowAttackMove copy)
    {
        this.knockback = copy.knockback;
        this.victimReactionId = copy.victimReactionId;
        this.noCrossfade = copy.noCrossfade;
        this.attackerAnimationSpeed = copy.attackerAnimationSpeed;
        this.DEBUG_ATTACK = copy.DEBUG_ATTACK;
        this.DEBUG_VICTIM = copy.DEBUG_VICTIM;
        this.moveName = copy.moveName;
        this.moveType = copy.moveType;
        this.idealDistance = copy.idealDistance;
        this.attackerAnimation = copy.attackerAnimation;
        this.attackerLockTimeAfterHit = copy.attackerLockTimeAfterHit;
        this.backwardsAttack = copy.backwardsAttack;
        this.finisher = copy.finisher;
        this.disabled = copy.disabled;
        this.victimAnimationDelay = copy.victimAnimationDelay;
        this.victimAnimation = copy.victimAnimation;
        this.victimStunTime = copy.victimStunTime;
        this.victimImmuneDuringAttack = copy.victimImmuneDuringAttack;
        this.attacker = copy.attacker;
        this.victim = copy.victim;
        this.note = copy.note;
        this.victimGO_ID = copy.victimGO_ID;
    }
}
