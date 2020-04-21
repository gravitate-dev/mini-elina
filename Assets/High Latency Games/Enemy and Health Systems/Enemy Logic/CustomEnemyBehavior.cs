using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Copyright High Latency Games - All Rights Reserved
 * This License grants to the END-USER a non-exclusive, worldwide, and perpetual license to this file and its contents to integrate only as 
 * incorporated and embedded components of electronic games and interactive media and distribute such electronic game and interactive media. 
 * END-USER may otherwise not reproduce, distribute, sublicense, rent, lease or lend this file or its contents.
 * Written by Lee Griffiths <leegriffithsdesigns@gmail.com>, April 9, 2019
 */

public abstract class CustomEnemyBehavior : MonoBehaviour
{
    public string BehaviorName;
    public bool OverridesOtherBehaviors;
    public float ChanceToPerformBehavior;
    public float DurationToPerformBehavior;
    [HideInInspector] public EnemyLogic Enemy;

    public abstract bool CheckOverride();
    public abstract void CustomUpdate();
}
