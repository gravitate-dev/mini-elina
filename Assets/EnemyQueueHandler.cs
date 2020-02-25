using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static EnemyLogic;

public class EnemyQueueHandler : MonoBehaviour
{
    public float UpdateTimer;
    public float TimeBetweenAttacks;
    public int PermittedAttackerType;
    public int QueueSize;
    float AttackTimer = 0;
    List<GameObject> Enemies = new List<GameObject>();
    List<GameObject> Queue = new List<GameObject>();
    List<int> AttackerGroupSizes = new List<int>();

    void Start()
    {
        int[] tmpSizes = { 1, 2, 1, 1, 1, 2, 1, 1, 1, 2, 2, 1, 3 };
        AttackerGroupSizes.AddRange(tmpSizes);
        QueueSize = AttackerGroupSizes[0];
        AttackerGroupSizes = AttackerGroupSizes.OrderBy(x => Random.value).ToList();
    }

    void Update()
    {
        UpdateEnemyList();
        if (!CleanQueue()) return;
        StartAttack();
    }

    /// <summary>
    /// Updates the list of enemies in the scene
    /// </summary>
    void UpdateEnemyList()
    {
        UpdateTimer -= Time.deltaTime;
        if (UpdateTimer <= 0)
        {
            UpdateTimer = 0.5f;
            Enemies = GameObject.FindGameObjectsWithTag("Enemy").ToList<GameObject>();
        }
    }

    /// <summary>
    /// Removes any enemies that have already attacked and replace them if possible
    /// </summary>
    bool CleanQueue()
    {
        // Remove enemies in queue that have already attacked
        for (int i = Queue.Count - 1; i >= 0; i--)
        {
            GameObject enemy = Queue[i];
            if(enemy == null)
            {
                UpdateTimer = 0;
                Queue.RemoveAt(i);
                return false;
            }

            EnemyLogic el = Queue[i].GetComponent<EnemyLogic>();
            if ((PermittedAttackerType != 3 && PermittedAttackerType != el.EnemyType)
                ||el.CurrentBehavior == Behavior.MovingToAttack
                || el.CurrentBehavior == Behavior.Disabled
                || el.Attacking
                || el.AttackCooldownTimer > 0)
            {
                Queue.RemoveAt(i);
            }
        }

        if (Queue.Count > QueueSize) {
            Queue.Clear();
            return false;
        }

        // Repopulate the queue if needed
        if (Queue.Count < QueueSize && Queue.Count < Enemies.Count)
        {
            List<GameObject> SortedList = Enemies.OrderBy(o => o.GetComponent<EnemyLogic>().DistanceFromPlayer).ToList();
            foreach (GameObject enemy in SortedList)
            {
                if (enemy.GetComponent<EnemyLogic>().Aggro 
                    && !enemy.GetComponent<EnemyLogic>().Attacking 
                    && enemy.GetComponent<EnemyLogic>().CurrentBehavior != Behavior.MovingToAttack
                    && enemy.GetComponent<EnemyLogic>().CurrentBehavior != Behavior.Disabled
                    && enemy.GetComponent<EnemyLogic>().AttackCooldownTimer <= 0
                    && (PermittedAttackerType == 3 || PermittedAttackerType == enemy.GetComponent<EnemyLogic>().EnemyType))
                    Queue.Add(enemy);
            }
        }

        return true;
    }

    /// <summary>
    /// Attempts to start an attack if the attack timeout is 0
    /// </summary>
    void StartAttack()
    {
        AttackTimer -= Time.deltaTime;

        if (AttackTimer <= 0)
        {
            if (Queue.Count == 0) return;

            // First we need to check that all enemies in the queue are valid
            for (int i = 0; i < Queue.Count; i++)
            {
                GameObject enemy = Queue[i];
                if (enemy == null)
                {
                    UpdateTimer = 0;
                    return;
                }
            }

            float HOOK_YOUR_DISTANCE_HERE = 1;

            // All enemies in the queue should be valid, and the queue size is equal to the
            // attacker group size. So we make all enemies in the queue attack at once
            for (int i = 0; i < Queue.Count; i++)
            {
                Queue[i].GetComponent<EnemyLogic>().SetAttackDistance(HOOK_YOUR_DISTANCE_HERE);
            }

            // Shuffle the number of attackers for the next attack
            AttackTimer = TimeBetweenAttacks;
            AttackerGroupSizes = AttackerGroupSizes.OrderBy(x => Random.value).ToList();
            QueueSize = AttackerGroupSizes[0];
        }
    }
}
