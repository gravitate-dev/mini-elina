using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI {
    [TaskCategory("AI/Movement")]
    [TaskDescription("Get a position near target position.")]
    public class GetPosNearTarget : Action {
        public SharedVector3 TargetPos;
        public SharedVector2 RadiusRange;
        public SharedVector2 AngleRange;
        public SharedVector3 ReturnPos;

        public override TaskStatus OnUpdate() {
            if (TargetPos == null || RadiusRange == null || AngleRange == null) {
                return TaskStatus.Failure;
            }
            else {
                Vector3 dir = transform.position - TargetPos.Value;
                dir.Normalize();
                float randomAngle = Mathf.Clamp(Random.Range(AngleRange.Value.x, AngleRange.Value.y), -180f, 180f);
                float randomRadius = Mathf.Abs(Random.Range(RadiusRange.Value.x, RadiusRange.Value.y));
                ReturnPos.Value = TargetPos.Value + Quaternion.Euler(0, randomAngle, 0) * dir * randomRadius;
                return TaskStatus.Success;
            }
        }
    }
}