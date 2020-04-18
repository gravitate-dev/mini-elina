using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI {
    [TaskCategory("AI")]
    [TaskDescription("Attack with flamethrower.")]
    public class AttackLaser : Action {
        public SharedGameObject TargetGameobject;
        public SharedVector3 AimOffsetFrom;
        public SharedVector3 AimOffsetTo;
        public SharedFloat AimTime;
        public SharedFloat AimAngle;
        public SharedBool IsAiming;

        float _aimTimer;
        float _aimTickTimer;
        Vector3? _aimPos;

        public SharedGameObject LaserGameobjectPrefab;
        RangedAttackLaserGuide _laserGuideInstance;
        public SharedGameObject ProjectileGameobjectPrefab;
        RangedAttackLaserGuidedProjectile _projectileInstance;

        Vector3 _offSetAimPos => transform.position + AimOffsetFrom.Value;

        public override TaskStatus OnUpdate() {
            if (_aimPos == null && !IsAiming.Value) {//start aiming
                //angle check
                Vector3 angleDir = TargetGameobject.Value.transform.position - transform.position;
                angleDir.y = transform.forward.y;//ignore y axis
                if (Vector3.Angle(angleDir, transform.forward) < AimAngle.Value) {
                    _aimTimer = Time.time;
                    _aimPos = TargetGameobject.Value.transform.position + AimOffsetTo.Value;

                    if (!_laserGuideInstance) {
                        _laserGuideInstance = GameObject.Instantiate(LaserGameobjectPrefab.Value).GetComponent<RangedAttackLaserGuide>();
                    }

                    _laserGuideInstance.gameObject.SetActive(true);

                    Vector3 dir = _aimPos.Value - _offSetAimPos;
                    dir.Normalize();

                    _laserGuideInstance.SetLaser(_offSetAimPos, _aimPos.Value + dir * 10f);
                    IsAiming.Value = true;
                }
            }


            if (IsAiming.Value && Time.time - _aimTimer > AimTime.Value) {//fire
                if (!_projectileInstance) {
                    _projectileInstance = GameObject.Instantiate(ProjectileGameobjectPrefab.Value).GetComponent<RangedAttackLaserGuidedProjectile>();
                    _projectileInstance.transform.position = _offSetAimPos;
                    _projectileInstance.transform.LookAt(_aimPos.Value);
                }
                _laserGuideInstance.gameObject.SetActive(false);

                _aimPos = null;
                IsAiming.Value = false;
                return TaskStatus.Success;
            }
            else {//keep aiming
                return TaskStatus.Failure;
            }
        }
    }
}
