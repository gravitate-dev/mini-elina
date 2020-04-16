using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI {
    [TaskCategory("AI")]
    [TaskDescription("Attack with flamethrower.")]
    public class AttackFlameThrower : Action {
        //dev
        public SharedGameObject devGameobject;
        public override TaskStatus OnUpdate() {
            Debug.DrawLine(transform.position + Vector3.up, devGameobject.Value.transform.position + Vector3.up, Color.red, 1f);
            return TaskStatus.Success;
        }
    }
}