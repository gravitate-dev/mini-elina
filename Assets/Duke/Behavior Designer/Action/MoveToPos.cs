using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Pathfinding;

namespace AI {
    [TaskCategory("AI/Movement")]
    [TaskDescription("Tells AI to move to a position.")]
    public class MoveToPos : Action {
        IAstarAI _ai;
        public SharedVector3 Destination;

        public override void OnAwake() {
            _ai = gameObject.GetComponent<IAstarAI>();
        }

        public override TaskStatus OnUpdate() {
            if (_ai == null || Destination == null) {
                return TaskStatus.Failure;
            }
            else {
                _ai.destination = Destination.Value;
                return TaskStatus.Success;
            }
        }
    }
}