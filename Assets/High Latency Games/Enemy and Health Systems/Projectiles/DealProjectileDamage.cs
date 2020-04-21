using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DamageDealer;

/* Copyright High Latency Games - All Rights Reserved
 * This License grants to the END-USER a non-exclusive, worldwide, and perpetual license to this file and its contents to integrate only as 
 * incorporated and embedded components of electronic games and interactive media and distribute such electronic game and interactive media. 
 * END-USER may otherwise not reproduce, distribute, sublicense, rent, lease or lend this file or its contents.
 * Written by Lee Griffiths <leegriffithsdesigns@gmail.com>, April 9, 2019
 */

public class DealProjectileDamage : MonoBehaviour
{
    public float StunTime;
    public float MinDamage;
    public float MaxDamage;
    public float MinGuardBreak;
    public float MaxGuardBreak;
    public float KnockBack;
    public LayerMask HitboxLayer;

    float OverlapCheckRadius;

    Vector3 PositionLastFrame;
    List<DamagedObject> DamagedObjects = new List<DamagedObject>();
    bool firstLoop = true;
    GameObject Caster;

    public void Start()
    {
        Caster = transform.root.GetComponent<Projectile>().CastedBy;
    }

    public void Update()
    {
        if (firstLoop) PositionLastFrame = transform.position;

        float dmg = Random.Range(MinDamage, MaxDamage);
        float guardBreak = Random.Range(MinGuardBreak, MaxGuardBreak);

        // Check for overlap
        OverlapCheckRadius = transform.GetComponent<SphereCollider>().radius * transform.lossyScale.x;
        Collider[] hitColliders = Physics.OverlapSphere((PositionLastFrame + transform.position) / 2, OverlapCheckRadius, HitboxLayer);

        foreach (Collider hit in hitColliders)
        {
            if (hit.transform.root.tag == "Enemy" && hit.transform.root != Caster.transform.root && !DamagedObjects.Any(x => x.Obj == hit.transform.root.gameObject))
            {
                hit.transform.GetComponent<DamageReceiver>().TakeDamage(dmg, StunTime, guardBreak, transform.root.forward, KnockBack, transform.position - PositionLastFrame);
                DamagedObjects.Add(new DamagedObject { Obj = hit.transform.root.gameObject, TimeUntilRemoval = 999 });
            }
            else if (hit.transform.root.tag == "Player" && hit.transform.root != Caster.transform.root && /*hit.transform != hit.transform.root && */!DamagedObjects.Any(x => x.Obj == hit.transform.root.gameObject))
            {
                hit.transform.GetComponent<DamageReceiver>().TakeDamage(dmg, StunTime, guardBreak, transform.root.forward, KnockBack, transform.position - PositionLastFrame);
                DamagedObjects.Add(new DamagedObject { Obj = hit.transform.root.gameObject, TimeUntilRemoval = 999 });
            }
        }

        // Reset the positions last frame
        PositionLastFrame = transform.position;
    }
}
