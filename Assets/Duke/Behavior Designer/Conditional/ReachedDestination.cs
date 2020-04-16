using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Pathfinding;

namespace AI {
    [TaskCategory("AI/Movement")]
    [TaskDescription("Has this AI reached its destination?")]
    public class ReachedDestination : Conditional {
        IAstarAI _ai;

        public override void OnAwake() {
            _ai = gameObject.GetComponent<IAstarAI>();
        }

        public override TaskStatus OnUpdate() {
            return (_ai != null && _ai.reachedDestination) ? 
                TaskStatus.Success : TaskStatus.Failure;
        }
    }
}