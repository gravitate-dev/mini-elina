using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI {
    [TaskCategory("AI")]
    [TaskDescription("Attack with flamethrower.")]
    public class AttackLaser : Action {
        public SharedFloat AimTime;
        public SharedFloat AimForgetTime;
        float _aimTimer;
        float _aimTickTimer;

        //dev
        public SharedGameObject devTargetGameobject;
        float _devAimDrawTimer;

        public override TaskStatus OnUpdate() {
            if(Time.time - _aimTickTimer > AimForgetTime.Value) {//rest aim timer
                _aimTimer = Time.time;
                _devAimDrawTimer = Time.time;
            }
            _aimTickTimer = Time.time;

            if(Time.time - _aimTimer > AimTime.Value) {//fire
                Debug.DrawLine(transform.position + Vector3.up, devTargetGameobject.Value.transform.position + Vector3.up, Color.green, 1f);
                //Debug.LogWarning("Fire");
                _aimTimer = Time.time;
                _devAimDrawTimer = Time.time;
                return TaskStatus.Success;
            }
            else {//keep aiming
                if(Time.time - _devAimDrawTimer > 1f) {
                    _devAimDrawTimer = Time.time;
                    Debug.DrawLine(transform.position + Vector3.up, devTargetGameobject.Value.transform.position + Vector3.up, Color.red, 0.5f);
                    //Debug.LogWarning("Aim");
                }
                return TaskStatus.Failure;
            }
        }
    }
}
