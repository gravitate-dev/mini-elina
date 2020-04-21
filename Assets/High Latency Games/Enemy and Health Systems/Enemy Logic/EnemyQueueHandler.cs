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
    public float AttackTimer = 0;
    List<GameObject> Enemies = new List<GameObject>();
    public List<GameObject> Queue = new List<GameObject>();
    List<int> AttackerGroupSizes = new List<int>();
    List<GameObject> CurrentAttackingEnemies = new List<GameObject>();

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
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var current = Enemies[i];
                if (current.GetComponent<EnemyLogic>() == null)
                    Enemies.RemoveAt(i);
            }
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
            if (enemy == null)
            {
                UpdateTimer = 0;
                Queue.RemoveAt(i);
                return false;
            }

            EnemyLogic el = Queue[i].GetComponent<EnemyLogic>();
            if (el == null || (PermittedAttackerType != 3 && PermittedAttackerType != el.EnemyType)
                || el.CurrentBehavior == Behavior.MovingToAttack
                || el.CurrentBehavior == Behavior.Disabled
                || el.Attacking
                || el.AttackCooldownTimer > 0
                || el.AllAttacksOnCooldown())
            {
                Queue.RemoveAt(i);
            }
        }

        if (Queue.Count > QueueSize)
        {
            Queue.Clear();
            return false;
        }

        // Repopulate the queue if needed
        if (AttackTimer <= 0)
        {
            List<GameObject> SortedList;
            try
            {
                SortedList = Enemies.OrderBy(o => o.GetComponent<EnemyLogic>().DistanceFromPlayer).ToList();
            }
            catch { return false; }
            foreach (GameObject enemy in SortedList)
            {
                if (Queue.Count < QueueSize && Queue.Count < Enemies.Count)
                {
                    if (enemy.GetComponent<EnemyLogic>().Aggro
                        && !enemy.GetComponent<EnemyLogic>().Attacking
                        && enemy.GetComponent<EnemyLogic>().CurrentBehavior != Behavior.MovingToAttack
                        && enemy.GetComponent<EnemyLogic>().CurrentBehavior != Behavior.Disabled
                        && enemy.GetComponent<EnemyLogic>().AttackCooldownTimer <= 0
                        && !enemy.GetComponent<EnemyLogic>().AllAttacksOnCooldown()
                        && (PermittedAttackerType == 3 || PermittedAttackerType == enemy.GetComponent<EnemyLogic>().EnemyType))
                        Queue.Add(enemy);
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Attempts to start an attack if the attack timeout is 0
    /// </summary>
    void StartAttack()
    {
        CleanAttackingEnemies();
        if (CurrentAttackingEnemies.Count > 0) return;

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

            // All enemies in the queue should be valid, and the queue size is equal to the
            // attacker group size. So we make all enemies in the queue attack at once
            for (int i = 0; i < Queue.Count; i++)
            {
                Queue[i].GetComponent<EnemyLogic>().ChooseAttack();
                Queue[i].GetComponent<EnemyLogic>().SetAttackDistance(Queue[i].GetComponent<EnemyLogic>().SelectedAttack.MinimumDistance);
                CurrentAttackingEnemies.Add(Queue[i]);
            }

            // Shuffle the number of attackers for the next attack
            AttackTimer = TimeBetweenAttacks;
            AttackerGroupSizes = AttackerGroupSizes.OrderBy(x => Random.value).ToList();
            QueueSize = AttackerGroupSizes[0];
        }
    }

    void CleanAttackingEnemies()
    {
        for (int i = CurrentAttackingEnemies.Count; i-- > 0;)
        {
            if (CurrentAttackingEnemies[i] == null || CurrentAttackingEnemies[i].GetComponent<EnemyLogic>() == null ||
                CurrentAttackingEnemies[i].GetComponent<EnemyLogic>().CurrentBehavior != Behavior.MovingToAttack)
                CurrentAttackingEnemies.RemoveAt(i);
        }

    }
}
