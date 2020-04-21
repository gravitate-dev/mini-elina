using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DamageDealer;

public class MeleeProjectile : Projectile
{
    public float StunTime;
    public float KnockBack = 0;
    public float TimeUntilRemoval = 0;
    public float AttackWidth;
    public float AttackHeight;
    public float AttackDepth;
    public LayerMask HitboxLayer;
    List<DamagedObject> DamagedObjects = new List<DamagedObject>();

    private void Start()
    {
        float damage = CastedBy.GetComponent<EnemyLogic>().SelectedAttack.Damage;
        
        Collider[] hitColliders = Physics.OverlapBox(transform.position, new Vector3(AttackWidth / 2, AttackHeight / 2, AttackDepth / 2), Quaternion.identity, HitboxLayer);

        foreach (Collider hit in hitColliders)
        {
            if (hit.transform.root != CastedBy.transform.root && !DamagedObjects.Any(x => x.Obj == hit.transform.root.gameObject))
            {
                hit.transform.GetComponent<HealthSystem>().TakeDamage(damage, StunTime, - hit.transform.root.forward, KnockBack, hit.transform.root.position - transform.root.position);
                DamagedObjects.Add(new DamagedObject { Obj = hit.transform.root.gameObject, TimeUntilRemoval = 999 });
            }
        }
    }

    private void Update()
    {
        TimeUntilRemoval -= Time.deltaTime;

        if (TimeUntilRemoval <= 0)
        {
            // Preserve any particle effects so that they can finish
            foreach (ParticleSystem emit in gameObject.transform.root.GetComponentsInChildren<ParticleSystem>())
            {
                emit.transform.parent = null;
                emit.Stop();
            }
            // Destroy the attack object
            Destroy(transform.root.gameObject);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        //Check that it is being run in Play Mode, so it doesn't try to draw this in Editor mode
            //Draw a cube where the OverlapBox is (positioned where your GameObject is as well as a size)
        Gizmos.DrawWireCube(transform.position, new Vector3(AttackWidth / 2, AttackHeight / 2, AttackDepth / 2));
    }

}