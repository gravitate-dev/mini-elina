using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Copyright High Latency Games - All Rights Reserved
 * This License grants to the END-USER a non-exclusive, worldwide, and perpetual license to this file and its contents to integrate only as 
 * incorporated and embedded components of electronic games and interactive media and distribute such electronic game and interactive media. 
 * END-USER may otherwise not reproduce, distribute, sublicense, rent, lease or lend this file or its contents.
 * Written by Lee Griffiths <leegriffithsdesigns@gmail.com>, April 9, 2019
 */

[RequireComponent(typeof(AudioSource))]
public class DamageReceiver : MonoBehaviour
{
    public float Health;
    public float DamageModifier = 1;
    public bool IsArmor;
    public bool TriggersBlock;
    public bool KillsIfDestroyed;

    public HealthSystem HealthSystem;

    public bool LeftLowerArm;
    public bool RightLowerArm;

    public bool LeftUpperArm;
    public bool RightUpperArm;

    public bool LeftLowerLeg;
    public bool RightLowerLeg;

    public bool LeftUpperLeg;
    public bool RightUpperLeg;

    public bool LeftHand;
    public bool RightHand;

    public bool Head;
    public bool Chest;
    public bool Hips;

    void Start()
    {
        if (HealthSystem == null) HealthSystem = transform.root.GetComponent<HealthSystem>();
        if (Health > HealthSystem.MaxHealth) Health = HealthSystem.MaxHealth;
        if (DamageModifier <= 0) DamageModifier = 1;
    }

    public void TakeDamage(float Damage, float StunDuration, float GuardBreakAmount, Vector3 KnockbackDir, float Force, Vector3 DamageDirection)
    {
        Debug.Log("DAMAGED AT: " + this.transform.name);
        Health -= Damage * DamageModifier;
        if (Health <= 0) { Disable(DamageDirection); }
        if (!IsArmor)
        {
            try
            {
                PlayGore(false);
                this.HealthSystem.TakeDamage(Damage * DamageModifier, StunDuration, KnockbackDir, Force, DamageDirection);
            }
            catch { };
        }
        /*if (TriggersBlock && transform.root.tag == "Player")
        {
            transform.root.GetComponent<MeleeHandler>().AbsorbBlock(GuardBreakAmount);
        }*/
        PlayImpactSound();
    }

    public void Disable(Vector3 DamageDirection)
    {
        // Remove any impaled weapons first so they dont get lost forever
        // TODO SPIDERMAN maybe add back in?
        
        /*WeaponInfo[] impaledWeapons = GetComponentsInChildren<WeaponInfo>();
        if(impaledWeapons.Length > 0)
        {
            foreach(WeaponInfo weapon in impaledWeapons)
            {
                Debug.Log("FOUND ONE TO REMOVE");
                weapon.transform.parent = null;
                weapon.gameObject.GetComponent<Collider>().isTrigger = false;
                weapon.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            }
        }*/

        // If the receiver was a piece of armor, make it fall off the character. Otherwise delimb the character
        if (IsArmor)
        {
            transform.parent = null;
            transform.GetComponent<Collider>().isTrigger = false;
            transform.GetComponent<Rigidbody>().useGravity = true;
            transform.GetComponent<Rigidbody>().isKinematic = false;
            //transform.GetComponent<Rigidbody>().AddForce(-DamageDirection.normalized, ForceMode.Impulse);
        }
        else
        {
            PlayGore(true);
            transform.localScale = Vector3.zero;
        }
        PlayDestructionSound();
        if (KillsIfDestroyed)
        {
            try
            {
                this.HealthSystem.Die();
            }
            catch { }
        }
        Destroy(this);
    }

    public void PlayImpactSound()
    {
        if (IsArmor)
        {
            if (HealthSystem.ArmorImpactSounds.Count <= 0) return;

            int n = Random.Range(1, HealthSystem.ArmorImpactSounds.Count);
            if (n >= HealthSystem.ArmorImpactSounds.Count) n = 0;
            HealthSystem.audio.clip = HealthSystem.ArmorImpactSounds[n];
            HealthSystem.audio.PlayOneShot(HealthSystem.audio.clip);
            // move picked sound to index 0 so it's not picked next time

            HealthSystem.ArmorImpactSounds[n] = HealthSystem.ArmorImpactSounds[0];
            HealthSystem.ArmorImpactSounds[0] = HealthSystem.audio.clip;
        }
        else
        {
            if (HealthSystem.FleshImpactSounds.Count <= 0) return;
            int n = Random.Range(1, HealthSystem.FleshImpactSounds.Count);
            if (n >= HealthSystem.FleshImpactSounds.Count) n = 0;
            HealthSystem.audio.clip = HealthSystem.FleshImpactSounds[n];
            HealthSystem.audio.PlayOneShot(HealthSystem.audio.clip);
            // move picked sound to index 0 so it's not picked next time
            HealthSystem.FleshImpactSounds[n] = HealthSystem.FleshImpactSounds[0];
            HealthSystem.FleshImpactSounds[0] = HealthSystem.audio.clip;
        }
    }

    public void PlayDestructionSound()
    {
        if (IsArmor)
        {
            if (HealthSystem.ArmorDestructionSounds.Count <= 0) return;

            int n = Random.Range(1, HealthSystem.ArmorDestructionSounds.Count);
            if (n >= HealthSystem.ArmorDestructionSounds.Count) n = 0;
            HealthSystem.audio.clip = HealthSystem.ArmorDestructionSounds[n];
            HealthSystem.audio.PlayOneShot(HealthSystem.audio.clip);
            // move picked sound to index 0 so it's not picked next time
            HealthSystem.ArmorDestructionSounds[n] = HealthSystem.ArmorDestructionSounds[0];
            HealthSystem.ArmorDestructionSounds[0] = HealthSystem.audio.clip;
        }
        else
        {
            if (HealthSystem.FleshDestructionSounds.Count <= 0) return;

            int n = Random.Range(1, HealthSystem.FleshDestructionSounds.Count);
            if (n >= HealthSystem.FleshDestructionSounds.Count) n = 0;
            HealthSystem.audio.clip = HealthSystem.FleshDestructionSounds[n];
            HealthSystem.audio.PlayOneShot(HealthSystem.audio.clip);
            // move picked sound to index 0 so it's not picked next time
            HealthSystem.FleshDestructionSounds[n] = HealthSystem.FleshDestructionSounds[0];
            HealthSystem.FleshDestructionSounds[0] = HealthSystem.audio.clip;
        }
    }

    public void PlayGore(bool Destroyed)
    {
        try {
            Joint joint = GetComponent<Joint>();
            if (LeftHand)
            {
                if (Destroyed)
                {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                    if (HealthSystem.CurrentHealth > 0)
                        transform.root.GetComponent<RagdollEnabler>().BeginRagdoll();
                }
                else Instantiate(HealthSystem.BurstBlood[0], transform.position, Quaternion.identity);
            }
            if (RightHand) {
                if (Destroyed) {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                    if (HealthSystem.CurrentHealth > 0)
                        transform.root.GetComponent<RagdollEnabler>().BeginRagdoll();
                }
                else Instantiate(HealthSystem.BurstBlood[0], transform.position, Quaternion.identity);
            }

            if (LeftLowerArm)
            {
                if (Destroyed)
                {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                    if (HealthSystem.CurrentHealth > 0)
                        transform.root.GetComponent<RagdollEnabler>().BeginRagdoll();
                }
                else Instantiate(HealthSystem.BurstBlood[0], transform.position, Quaternion.identity);
            }
            if (RightLowerArm)
            {
                if (Destroyed)
                {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                    if (HealthSystem.CurrentHealth > 0)
                        transform.root.GetComponent<RagdollEnabler>().BeginRagdoll();
                }
                else Instantiate(HealthSystem.BurstBlood[0], transform.position, Quaternion.identity);
            }

            if (LeftUpperArm) {
                if (Destroyed) {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                    if (HealthSystem.CurrentHealth > 0)
                        transform.root.GetComponent<RagdollEnabler>().BeginRagdoll();
                }
                else Instantiate(HealthSystem.BurstBlood[0], transform.position, Quaternion.identity);
            }
            if (RightUpperArm) {
                if (Destroyed)
                {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                    if (HealthSystem.CurrentHealth > 0)
                        transform.root.GetComponent<RagdollEnabler>().BeginRagdoll();
                }
                else Instantiate(HealthSystem.BurstBlood[0], transform.position, Quaternion.identity);
            }

            if (LeftLowerLeg) {
                if (Destroyed) {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                }
                else Instantiate(HealthSystem.BurstBlood[1], transform.position, Quaternion.identity);
            }
            if (RightLowerLeg) {
                if (Destroyed)
                {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                }
                else Instantiate(HealthSystem.BurstBlood[1], transform.position, Quaternion.identity);
            }

            if (LeftUpperLeg) {
                if (Destroyed) {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                }
                else Instantiate(HealthSystem.BurstBlood[2], transform.position, Quaternion.identity);
            }
            if (RightUpperLeg) {
                if (Destroyed) {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                }
                else Instantiate(HealthSystem.BurstBlood[2], transform.position, Quaternion.identity);
            }

            if (Head) {
                if (Destroyed) {
                    Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                    GameObject go = Instantiate(HealthSystem.LeakingBlood[2], Vector3.zero, Quaternion.identity);
                    go.transform.parent = transform;
                }
                else Instantiate(HealthSystem.CircularBlood[0], transform.position, Quaternion.identity);
            }
            if (Chest) {
                if (Destroyed) Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                else Instantiate(HealthSystem.CircularBlood[2], transform.position, Quaternion.identity);
            }
            if (Hips) {
                if (Destroyed) Instantiate(HealthSystem.LimbDestruction, transform.position, Quaternion.identity);
                else Instantiate(HealthSystem.CircularBlood[2], transform.position, Quaternion.identity);
            }
        }
        catch { }
    }
}
