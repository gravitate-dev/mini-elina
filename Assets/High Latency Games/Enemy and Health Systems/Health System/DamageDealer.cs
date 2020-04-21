using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/* Copyright High Latency Games - All Rights Reserved
 * This License grants to the END-USER a non-exclusive, worldwide, and perpetual license to this file and its contents to integrate only as 
 * incorporated and embedded components of electronic games and interactive media and distribute such electronic game and interactive media. 
 * END-USER may otherwise not reproduce, distribute, sublicense, rent, lease or lend this file or its contents.
 * Written by Lee Griffiths <leegriffithsdesigns@gmail.com>, April 9, 2019
 */

public class DamageDealer : MonoBehaviour
{
    public LayerMask HitboxLayer;
    public float DamageInterval;
    public float OverlapCheckRadius;

    [HideInInspector] public List<Transform> DamagePoints = new List<Transform>();
    [HideInInspector] public List<Vector3> PositionsLastFrame = new List<Vector3>();
    public List<DamagedObject> DamagedObjects = new List<DamagedObject>();
    EnemyLogic enemy;
    bool enemyAttackingLastFrame = false;

    void Awake()
    {
        DamagePoints.Clear();
        PositionsLastFrame.Clear();
        DamagedObjects.Clear();
        foreach (Transform child in transform)
            DamagePoints.Add(child);

        // Store enemy script if this is part of an enemy
        enemy = transform.root.GetComponent<EnemyLogic>();
    }

    void LateUpdate()
    {
        // No sense checking if enemy is not attacking
        if (enemy != null && !enemy.Attacking)
        {
            enemyAttackingLastFrame = false;
            return;
        }
        else if (enemy != null && enemy.Attacking)
        {
            if (!enemyAttackingLastFrame)
            {
                enemyAttackingLastFrame = true;
                DamagePoints.Clear();
                ClearDamagedObjects();
                foreach (Transform child in transform)
                    DamagePoints.Add(child);
            }
        }

        // Calculate the damage to deal
        float dmg = 0;
        float knockbackForce = 1.5f;
        float guardBreak = 0;

        if (transform.root.tag == "Enemy")
        {
            EnemyLogic.Attack attk = enemy.SelectedAttack;
            if (attk != null)
            {
                dmg = attk.Damage;
                guardBreak = Random.Range(attk.MinGuardBreak, attk.MaxGuardBreak);
            }
        }
        else
        {
            dmg = 10;
            guardBreak = 10;
        }

        // Spherecast from the positions last frame to the positions this frame and check for hits
        for (int i = 0; i < PositionsLastFrame.Count; i++)
        {
            RaycastHit[] hits;
            float distance = Vector3.Distance(PositionsLastFrame[i], DamagePoints[i].transform.position);
            OverlapCheckRadius = DamagePoints[i].GetComponent<SphereCollider>().radius * DamagePoints[i].lossyScale.x;
            hits = Physics.SphereCastAll(PositionsLastFrame[i], OverlapCheckRadius, DamagePoints[i].transform.position - PositionsLastFrame[i], distance, HitboxLayer);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.root.tag == "Enemy" && hit.transform.root != transform.root && !DamagedObjects.Any(x => x.Obj == hit.transform.root.gameObject))
                {
                    hit.transform.GetComponent<DamageReceiver>().TakeDamage(dmg, 0, guardBreak, transform.root.forward, knockbackForce, DamagePoints[i].transform.position - PositionsLastFrame[i]);
                    DamagedObjects.Add(new DamagedObject { Obj = hit.transform.root.gameObject, TimeUntilRemoval = DamageInterval });
                }
                else if (hit.transform.root.tag == "Player" && hit.transform.root != transform.root && hit.transform != hit.transform.root && !DamagedObjects.Any(x => x.Obj == hit.transform.root.gameObject))
                {
                    hit.transform.GetComponent<DamageReceiver>().TakeDamage(dmg, 0, guardBreak, transform.root.forward, knockbackForce, DamagePoints[i].transform.position - PositionsLastFrame[i]);
                    DamagedObjects.Add(new DamagedObject { Obj = hit.transform.root.gameObject, TimeUntilRemoval = DamageInterval });
                }
            }
            // Check for overlap
            Collider[] hitColliders = Physics.OverlapSphere((PositionsLastFrame[i] + DamagePoints[i].transform.position) / 2, OverlapCheckRadius, HitboxLayer);
            foreach (Collider hit in hitColliders)
            {
                if (hit.transform.root.tag == "Enemy" && hit.transform.root != transform.root && !DamagedObjects.Any(x => x.Obj == hit.transform.root.gameObject))
                {
                    hit.transform.GetComponent<DamageReceiver>().TakeDamage(dmg, 0, guardBreak, transform.root.forward, knockbackForce, DamagePoints[i].transform.position - PositionsLastFrame[i]);
                    DamagedObjects.Add(new DamagedObject { Obj = hit.transform.root.gameObject, TimeUntilRemoval = DamageInterval });
                }
                else if (hit.transform.root.tag == "Player" && hit.transform.root != transform.root && hit.transform != hit.transform.root && !DamagedObjects.Any(x => x.Obj == hit.transform.root.gameObject))
                {
                    hit.transform.GetComponent<DamageReceiver>().TakeDamage(dmg, 0, guardBreak, transform.root.forward, knockbackForce, DamagePoints[i].transform.position - PositionsLastFrame[i]);
                    DamagedObjects.Add(new DamagedObject { Obj = hit.transform.root.gameObject, TimeUntilRemoval = DamageInterval });
                }
            }
        }

        // Reset the positions last frame
        PositionsLastFrame.Clear();
        foreach (Transform point in DamagePoints)
        {
            PositionsLastFrame.Add(point.position);
        }

        // Reduce the timer between an object taking hits again
        for(int i = DamagedObjects.Count; i-- > 0;)
        {
            DamagedObjects[i].TimeUntilRemoval -= Time.deltaTime;
            if(DamagedObjects[i].TimeUntilRemoval <= 0) { DamagedObjects.RemoveAt(i); }
        }
    }

    public void ClearDamagedObjects()
    {
        if (PositionsLastFrame.Count > 0)
            PositionsLastFrame.Clear();

        if (DamagedObjects.Count > 0)
            DamagedObjects.Clear();
    }

    [System.Serializable]
    public class DamagedObject
    {
        public GameObject Obj;
        public float TimeUntilRemoval;
    }

    
}
