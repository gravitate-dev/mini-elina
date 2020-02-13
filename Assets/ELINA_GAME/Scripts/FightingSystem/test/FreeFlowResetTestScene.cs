using Invector.vCharacterController;
using Invector.vCharacterController.AI;
using Invector.vCharacterController.AI.FSMBehaviour;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// To be used with FIGHT_TEST scene
/// Right click resets positions
/// </summary>
public class FreeFlowResetTestScene : MonoBehaviour
{
    public GameObject player;
    public GameObject[] targets;


    private Vector3 originalPlayerPosition;
    private List<Vector3> originalTargetPosition = new List<Vector3>();
    // Start is called before the first frame update
    void Start()
    {
        originalPlayerPosition = player.transform.position;
        foreach (GameObject go in targets)
        {
            if (go==null || !go.activeSelf)
            {
                continue;
            }
            originalTargetPosition.Add(go.transform.position);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetFighters();
        }
    }
    private void ResetAiFighter(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }
        vControlAI ai = enemy.GetComponent<vControlAI>();
        ai.ResetHealth();
        enemy.GetComponent<Animator>().SetBool("isDead", false);
        enemy.GetComponent<Rigidbody>().isKinematic = false;
        enemy.GetComponent<CapsuleCollider>().enabled = true;
        vFSMBehaviourController fsmBehaviourController = enemy.GetComponent<vFSMBehaviourController>();
        if (fsmBehaviourController != null)
        {
            fsmBehaviourController.ResetFSM();
        }
    }
    private void ResetFighters()
    {
        player.GetComponent<vThirdPersonController>().ChangeHealth(100);
        player.transform.position = originalPlayerPosition + Vector3.up;


        for (int i = 0; i < originalTargetPosition.Count; i++)
        {
            if (!targets[i].activeSelf)
            {
                continue;
            }
            ResetAiFighter(targets[i]);
            if (targets[i] == null)
            {
                return;
            }
            targets[i].transform.position = originalTargetPosition[i] + Vector3.up;
            targets[i].transform.LookAt(player.transform);
            //WickedObserver.SendMessage("onStateRegainControl:" + targets[i].gameObject.GetInstanceID());
        }
        Time.timeScale = 1.0f;
        WickedObserver.SendMessage("OnFreeFlowDebugAnimationStop");
    }
}
