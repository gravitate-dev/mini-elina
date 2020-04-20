using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Pathfinding;

namespace AI {
    [TaskCategory("AI/Movement")]
    [TaskDescription("Tells AI to stop at the current position.")]
    public class SetIsStopped : Action {
        public SharedBool TargetValue;
        IAstarAI _ai;

        public override void OnAwake() {
            _ai = gameObject.GetComponent<IAstarAI>();
        }

        public override TaskStatus OnUpdate() {
            if (_ai == null) {
                return TaskStatus.Failure;
            }
            else {
                _ai.isStopped = TargetValue.Value;
                return TaskStatus.Success;
            }
        }
    }
}