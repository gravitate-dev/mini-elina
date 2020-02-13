using System.Collections.Generic;
using UnityEngine;

public class EnemyTicketDispenser : MonoBehaviour
{
    public List<EnemyTicketHolder> enemiesAvailable = new List<EnemyTicketHolder>();
    private BoxCollider boxCollider;

    public int totalTicketLimit = 2;
    public int activeTicketCount;
    public float DISPENSE_DELAY = 4.0f;
    public float nextTimeDispenseTime;
    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        nextTimeDispenseTime = Time.time + DISPENSE_DELAY;
    }

    // Update is called once per frame
    void Update()
    {
        if (nextTimeDispenseTime < Time.time)
        {
            nextTimeDispenseTime = Time.time + DISPENSE_DELAY;
            DispenseTickets(2);
        }
    }

    private void DispenseTickets(int number)
    {
        if (enemiesAvailable.Count == 0)
        {
            return;
        }
        List<EnemyTicketHolder> chosenEnemies = new List<EnemyTicketHolder>();

        while (chosenEnemies.Count < number && enemiesAvailable.Count > chosenEnemies.Count)
        {
            int randIdx = Random.Range(0, enemiesAvailable.Count);
            EnemyTicketHolder enemyTicketHolder = enemiesAvailable[randIdx];

            if (enemyTicketHolder != null
                && enemyTicketHolder.CanGrabTicket())
            {
                chosenEnemies.Add(enemyTicketHolder);
            }
            enemiesAvailable.RemoveAt(randIdx);
        }
        foreach (EnemyTicketHolder chosen in chosenEnemies)
        {
            chosen.GiveTicket();
        }
        
    }
    private void RemoveHolderById(EnemyTicketHolder newHolder)
    {
        int removeIndex = -1;
        for (int i = 0; i < enemiesAvailable.Count; i++)
        {
            EnemyTicketHolder holder = enemiesAvailable[i];
            if (holder.GetInstanceID().Equals(holder.GetInstanceID()))
            {
                removeIndex = i;
                break;
            }
        }
        if (removeIndex == -1)
        {
            return;
        }
        enemiesAvailable.RemoveAt(removeIndex);
    }
    private void OnTriggerEnter(Collider other)
    {
        EnemyTicketHolder holder = other.GetComponent<EnemyTicketHolder>();
        if (holder != null)
        {
            enemiesAvailable.Add(holder);
        }
    }

    //When the Primitive exits the collision, it will change Color
    private void OnTriggerExit(Collider other)
    {
        EnemyTicketHolder holder = other.GetComponent<EnemyTicketHolder>();
        if (holder != null)
        {
            enemiesAvailable.Remove(holder);
        }
    }
}