using UnityEngine;
using UnityEngine.Assertions;

public class RangedAttackLaserGuide : MonoBehaviour {
    [SerializeField] LineRenderer _lRLaserGuide;

    private void Awake() {
        Assert.IsNotNull(_lRLaserGuide);
        _lRLaserGuide.positionCount = 2;
    }

    public void SetLaser(Vector3 fromPos, Vector3 toPos) {
        _lRLaserGuide.SetPosition(0, fromPos);
        _lRLaserGuide.SetPosition(1, toPos);
    }
}
