using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    private Animator animator;
    private CharacterController cc;
    private EnemyLogic enemy;
    private Vector3 velocity = Vector3.zero;
    private float FallCounter = 0;
    private float XAxisVel;
    private float YAxisVel;
    private float SpeedMultiplier;

    // Use this for initialization
    void Start()
    {
        animator = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        enemy = GetComponent<EnemyLogic>();
    }

    void Update()
    {
        GetVelocityValues();
    }

    float idleTimer = 0;
    float runTimer = 0;
    public void GetVelocityValues()
    {
        bool running = false;

        // Get the velocity from the rigid body
        Vector3 relativeVel = velocity = transform.InverseTransformDirection(enemy.MovementDirection);
        XAxisVel = relativeVel.x;
        YAxisVel = relativeVel.z;

        if ((enemy.MovementDirection.x != 0 || enemy.MovementDirection.z != 0)) { idleTimer = 0; }
        else idleTimer += Time.deltaTime;

        if (YAxisVel > 0.95f || YAxisVel < -0.95 || XAxisVel > 0.95f || XAxisVel < -0.95) runTimer += Time.deltaTime;
        else runTimer -= Time.deltaTime * 10;
        runTimer = Mathf.Clamp(runTimer, 0, 1f);
        if (runTimer > 0.8) { running = true; }

        //running = enemy.CurrentSpeed > 2;

        animator.SetBool("Moving", idleTimer < 0.15f);

        // Normalize it to 1
        if (XAxisVel > 1)
            XAxisVel = 1;
        if (XAxisVel < -1)
            XAxisVel = -1;
        if (YAxisVel > 1)
            YAxisVel = 1;
        if (YAxisVel < -1)
            YAxisVel = -1;

        // Get the speed multiplier
        SpeedMultiplier = cc.velocity.magnitude / 4f;
        if (SpeedMultiplier < 1) SpeedMultiplier = 1;

        if (running) { YAxisVel *= 2; XAxisVel *= 2; }

        if (!cc.isGrounded) FallCounter += Time.deltaTime;
        else FallCounter = 0;

        animator.SetFloat("ForwardMovement", YAxisVel, 0.2f, Time.deltaTime);
        animator.SetFloat("HorizontalMovement", XAxisVel, 0.2f, Time.deltaTime);
        animator.SetFloat("SpeedMultiplier", SpeedMultiplier);
        animator.SetBool("Jumping", FallCounter >= 0.5f);
        animator.SetBool("Sliding", false);
        animator.SetBool("Dodging", false);
        animator.SetBool("Rolling", false);
    }
}
