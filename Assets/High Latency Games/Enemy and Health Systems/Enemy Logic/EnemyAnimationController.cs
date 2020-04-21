using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Copyright High Latency Games - All Rights Reserved
 * This License grants to the END-USER a non-exclusive, worldwide, and perpetual license to this file and its contents to integrate only as 
 * incorporated and embedded components of electronic games and interactive media and distribute such electronic game and interactive media. 
 * END-USER may otherwise not reproduce, distribute, sublicense, rent, lease or lend this file or its contents.
 * Written by Lee Griffiths <leegriffithsdesigns@gmail.com>, April 9, 2019
 */

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
    float idleTimer = 0;
    float runTimer = 0;

    // Use this for initialization
    void Start()
    {
        animator = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        enemy = GetComponent<EnemyLogic>();
        int mirrorAnimations = Random.Range(1, 3);
        if (mirrorAnimations == 1) {
        }
    }

    void Update()
    {
        GetVelocityValues();
    }

    public void GetVelocityValues()
    {
        bool running = false;

        // Get the velocity from the rigid body
        Vector3 relativeVel = velocity = transform.InverseTransformDirection(enemy.BlendedMovementDirection);
        relativeVel.y = 0;
        XAxisVel = relativeVel.x;
        YAxisVel = relativeVel.z;

        if ((Mathf.Max(Mathf.Abs(cc.velocity.x), Mathf.Abs(cc.velocity.z)) > 0.1f)) { idleTimer = 0; }
        else idleTimer += Time.deltaTime;

        if (YAxisVel > 0.95f || YAxisVel < -0.95 || XAxisVel > 0.95f || XAxisVel < -0.95) runTimer += Time.deltaTime;
        else runTimer -= Time.deltaTime * 10;
        runTimer = Mathf.Clamp(runTimer, 0, 1f);
        if (runTimer > 0.8) { running = true; }

        //running = enemy.CurrentSpeed > 2;

        animator.SetBool("Moving", idleTimer < 0.05f);
        animator.SetBool("Attacking", enemy.Attacking);

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
        SpeedMultiplier = Mathf.Max(Mathf.Abs(cc.velocity.x), Mathf.Abs(cc.velocity.z));
        //if (SpeedMultiplier < 1) SpeedMultiplier = 1;

        /*if (running) { */
        YAxisVel *= SpeedMultiplier;
        XAxisVel *= SpeedMultiplier; 

        if (!cc.isGrounded) FallCounter += Time.deltaTime;
        else FallCounter = 0;

        animator.SetFloat("ForwardMovement", YAxisVel, 0.25f, Time.deltaTime);
        animator.SetFloat("HorizontalMovement", XAxisVel, 0.25f, Time.deltaTime);
        animator.SetFloat("SpeedMultiplier", SpeedMultiplier);
        animator.SetBool("Jumping", FallCounter >= 0.5f);
        animator.SetBool("Sliding", false);
        animator.SetBool("Dodging", false);
        animator.SetBool("Rolling", false);
    }
}
