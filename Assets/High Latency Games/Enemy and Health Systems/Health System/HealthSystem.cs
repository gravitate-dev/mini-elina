using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/* Copyright High Latency Games - All Rights Reserved
 * This License grants to the END-USER a non-exclusive, worldwide, and perpetual license to this file and its contents to integrate only as 
 * incorporated and embedded components of electronic games and interactive media and distribute such electronic game and interactive media. 
 * END-USER may otherwise not reproduce, distribute, sublicense, rent, lease or lend this file or its contents.
 * Written by Lee Griffiths <leegriffithsdesigns@gmail.com>, April 9, 2019
 */

[RequireComponent(typeof(AudioSource))]
public class HealthSystem : MonoBehaviour
{
    public bool IsEnemy;

    public float MaxHealth;
    public float CurrentHealth;

    [Header("Audio")]
    public AudioClip[] PainSounds;
    public List<AudioClip> ArmorImpactSounds;
    public List<AudioClip> FleshImpactSounds;
    public List<AudioClip> FleshDestructionSounds;
    public List<AudioClip> ArmorDestructionSounds;
    [HideInInspector] public AudioSource audio;

    [Header("Gore FX")]
    public GameObject LimbDestruction;
    public GameObject[] LeakingBlood;
    public GameObject[] CircularBlood;
    public GameObject[] BurstBlood;



    void Start()
    {
        CurrentHealth = MaxHealth;
        audio = GetComponent<AudioSource>();
        if (audio == null)
        {
            audio = gameObject.AddComponent<AudioSource>();
        }
        if (!IsEnemy)
        {
            WickedObserver.SendMessage("OnHealthChange", CurrentHealth / MaxHealth);
        }
    }

    public void TakeDamage(float Damage, float StunDuration, Vector3 KnockbackDir, float Force, Vector3 DamageDirection)
    {
        if (CurrentHealth <= 0)
            return;

        PlayPainSound();

        CurrentHealth -= Damage;
        if (!IsEnemy)
        {
            WickedObserver.SendMessage("OnHealthChange", CurrentHealth / MaxHealth);
        }
        if (IsEnemy)
        {
            EnemyLogic enemy = GetComponent<EnemyLogic>();
            if (!enemy.Attacking || enemy.SelectedAttack.CanBeInterupted)
            {
                if (Force != 0)
                {
                    GetComponent<EnemyLogic>().Knockback(KnockbackDir, Force, StunDuration);
                }
                else if (StunDuration > 0)
                {
                    enemy.DisableForDuration(StunDuration);
                    enemy.Stunned = true;
                }
                GetComponent<EnemyLogic>().ReactToHit(DamageDirection, true);
            }
        }
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
    }

    public void Die()
    {
        CurrentHealth = 0;
        /*GetComponent<RagdollEnabler>().BeginRagdoll();
        Destroy(GetComponent<RagdollEnabler>());
        Component[] components = GetComponents<Component>();
        foreach (Component c in components)
        {
            if(!(c is Transform) && !(c is Pathfinding.Seeker) && !(c is HealthSystem) && !(c is AudioSource))
                Destroy(c);
        }
        */
        DamageDealer[] componentsInChildren = GetComponentsInChildren<DamageDealer>();
        foreach (DamageDealer c in componentsInChildren)
        {
            if (c is DamageDealer)
                Destroy(c);
        }
    }

    public void PlayPainSound()
    {
        if (PainSounds.Length <= 0) return;

        int n = Random.Range(1, PainSounds.Length);
        if (n >= PainSounds.Length) n = 0;
        audio.clip = PainSounds[n];
        audio.PlayOneShot(audio.clip);
        // move picked sound to index 0 so it's not picked next time
        PainSounds[n] = PainSounds[0];
        PainSounds[0] = audio.clip;
    }
}
