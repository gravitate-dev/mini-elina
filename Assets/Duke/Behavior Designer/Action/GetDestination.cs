using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Pathfinding;

namespace AI {
    [TaskCategory("AI/Movement")]
    [TaskDescription("Tells AI to move to a position.")]
    public class GetDestination : Action {
        IAstarAI _ai;
        public SharedVector3 ReturnPos;

        public override void OnAwake() {
            _ai = gameObject.GetComponent<IAstarAI>();
        }

        public override TaskStatus OnUpdate() {
            if (_ai == null) {
                ReturnPos.Value = transform.position;
                return TaskStatus.Failure;
            }
            else {
                ReturnPos.Value = _ai.destination;
                return TaskStatus.Success;
            }
        }
    }
}