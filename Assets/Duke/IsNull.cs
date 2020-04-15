namespace BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject {
    [TaskCategory("Unity/GameObject")]
    [TaskDescription("Returns Success if the GameObject is null, otherwise Failure.")]
    public class IsNull : Conditional {
        [Tooltip("The GameObject that the task operates on. If null the task GameObject is used.")]
        public SharedGameObject targetGameObject;

        public override TaskStatus OnUpdate() {
            return GetDefaultGameObject(targetGameObject.Value) == null ? TaskStatus.Success : TaskStatus.Failure;
        }

        public override void OnReset() {
            targetGameObject = null;
        }
    }
}