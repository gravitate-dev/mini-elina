using UnityEngine;

public class DummyEnemy : MonoBehaviour
{
    public Vector3 movePos;
    public EnemyPositionManager enemyPositionManager;
   public bool onCooldown = false; //test thing that would make an enemy lower priority in the queue; such as being stunned, being low hp, and having attacked recently

    // Start is called before the first frame update
    private void Start()
    {
        enemyPositionManager.RegisterEnemy(this);
    }

    // Update is called once per frame
    private void Update()
    {
        TempMove();
    }

    private void TempMove()
    {
        this.transform.position = Vector3.MoveTowards(transform.position, movePos, 10f * Time.deltaTime);
        Debug.DrawLine(this.transform.position, movePos, Color.gray);
        Debug.DrawRay(movePos, Vector3.down, onCooldown ? Color.white : Color.yellow);
        this.transform.rotation =(movePos - transform.position).sqrMagnitude > 0f ? Quaternion.LookRotation(movePos - transform.position, Vector3.up) : Quaternion.identity;
    }

    private void EnemyDead()
    {
        enemyPositionManager.RemoveEnemy(this);
    }
}