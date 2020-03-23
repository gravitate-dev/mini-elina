using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyPositionManager : MonoBehaviour
{
    //PUBLIC VARIABLES
    [Header("References")]
    [Tooltip("Player/priority target")]
    public Transform targetTransform;
    [Header("Variables")]
    [Tooltip("Enemies will move to this distance if all 6 spots are occupied")]
    public float unavailableSpotHoverDistance = 6f;
    [Tooltip("Hexagon is rotated along with player transform")]
    public bool orientToPlayer = false;
    [Tooltip("Move points are returned on the Y position of the actor requesting them; otherwise the players's y position is returned")]
    public bool returnPlanar = true;
    [Tooltip("Radius of hexagon")]
    public float hexagonRadius = 3f;
    [Tooltip("How often the target positions are updated - not done every frame due to optimization")]
    public float positionUpdateRate = 0.25f;

    //PRIVATE VARIABLES
    private float positionTimer = 0f;
    private Vector3[] hexagonPoints;
    private List<DummyEnemy> enemyList = new List<DummyEnemy>(); //list should populated in enemy script by active enemies

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        positionTimer -= Time.deltaTime;
        if (positionTimer <= 0f)
        {
            positionTimer = positionUpdateRate;
            if (targetTransform != null)
                UpdateEnemyMoveTargets();
            else
                Debug.LogError("target is null");
        }
    }

    private void UpdateEnemyMoveTargets()
    {
        UpdateHexagon();

        List<DummyEnemy> tempList = enemyList;

        List<DummyEnemy> tempLowPriorityEnemies = new List<DummyEnemy>();
        /*
        for (int k = 0; k < tempList.Count; k++)
        {
            if (tempList[k].onCooldown)
                tempLowPriorityEnemies.Add(tempList[k]);
        }
        for (int l = 0; l < tempLowPriorityEnemies.Count; l++)
            tempList.Remove(tempLowPriorityEnemies[l]);
*/
    //    tempList = tempList.OrderBy(x => Vector3.Distance(targetTransform.position, x.transform.position)).ToList();
        tempList = tempList.OrderBy(x => x.onCooldown).ToList();

      //  for (int d = 0; d < tempList.Count; d++)
     //       Debug.Log(tempList[d].gameObject.name + " " + tempList[d].onCooldown);

        for (int c = tempList.Count - 1; c >= hexagonPoints.Length; c--)
        {
            var v = tempList[c];
            if (v.onCooldown)
            {
                tempList.RemoveAt(c);
                tempLowPriorityEnemies.Add(v);
            }
        }


        //assign available positions
        for (int i = 0; i < hexagonPoints.Length; i++)
        {
            tempList = tempList.OrderBy(x => Vector3.Distance(hexagonPoints[i], x.transform.position)).ToList();
            tempList[0].movePos = hexagonPoints[i];
            tempList.Remove(tempList[0]);
        }
        tempLowPriorityEnemies.AddRange(tempList);

        //make the rest move to distance
        for (int j = 0; j < tempLowPriorityEnemies.Count; j++)
        {
            tempLowPriorityEnemies[j].movePos = (targetTransform.position - tempLowPriorityEnemies[j].transform.position).normalized * -unavailableSpotHoverDistance + targetTransform.position;
         //   Debug.DrawRay(tempLowPriorityEnemies[j].movePos, Vector3.up, Color.yellow, 0.2f);
        }
    }

    private void UpdateHexagon()
    {
        float ang = orientToPlayer ? Vector3.SignedAngle(targetTransform.forward, Vector3.forward, Vector3.up) * Mathf.Deg2Rad : 0f;

        //Get the middle of the panel
        var pX = targetTransform.position.x;
        var pY = targetTransform.position.y;
        var pZ = targetTransform.position.z;

        hexagonPoints = new Vector3[6];
        //Create 6 points
        for (int a = 0; a < 6; a++)
        {
            hexagonPoints[a] = new Vector3(pX + hexagonRadius * (float)Mathf.Cos((ang + a) * 60 * Mathf.PI / 180f), pY, pZ + hexagonRadius * (float)Mathf.Sin((ang + a) * 60 * Mathf.PI / 180f));
            Debug.DrawRay(hexagonPoints[a], Vector3.up, Color.red, positionUpdateRate);
        }
    }

    public void RegisterEnemy(DummyEnemy enemy)
    {
        enemyList.Add(enemy);
    }

    public void RemoveEnemy(DummyEnemy enemy)
    {
        enemyList.Remove(enemy);
    }
}