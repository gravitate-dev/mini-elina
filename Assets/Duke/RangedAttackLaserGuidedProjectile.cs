using UnityEngine;

public class RangedAttackLaserGuidedProjectile : MonoBehaviour {
    [SerializeField] float _speed = 5f;
    [SerializeField] float _lifeTime = 5f;
    float _deathTimer;

    private void Awake() {
        _deathTimer = Time.time + _lifeTime;
    }

    private void Update() {
        transform.position += transform.forward * _speed * Time.deltaTime;

        //out of time, destroy self
        if(Time.time > _deathTimer) {
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject == IAmElina.ELINA) {
            //Debug.LogWarning("Hit Elina");
            Destroy(this.gameObject);
        }
    }
}
