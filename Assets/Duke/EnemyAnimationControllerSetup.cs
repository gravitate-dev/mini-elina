using Pathfinding;
using UnityEngine;

//This script should be merged in EnemyAnimationController.cs
//Separated to avoid changing original scripts
public class EnemyAnimationControllerSetup : MonoBehaviour {
    [SerializeField] AIPath _ai = default;
    [SerializeField] EnemyLogic _enemyLogic = default;

    // Update is called once per frame
    void Update() {
        if (_ai == null || _enemyLogic == null)
            return;
        Vector3 dir = Vector3.zero;
        if (!_ai.reachedDestination && !_ai.isStopped) {
            dir = _ai.steeringTarget - transform.position;
        }
        _enemyLogic.MovementDirection = dir;

    }
}
