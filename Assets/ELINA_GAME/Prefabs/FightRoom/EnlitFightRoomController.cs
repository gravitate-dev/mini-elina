using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnlitFightRoomController : MonoBehaviour
{
    public EnlitFightRoomMetaData enlitFightRoomMetaData;
    public bool Fighting;
    public bool IsFightStarted;
    private BoxCollider boxCollider;
    [SerializeField]
    private List<GameObject> enemiesAlive = new List<GameObject>();

    [System.Serializable]
    public class EnemyGroup
    {
        public List<EnemyUnit> enemySquads;
    }

    [System.Serializable]
    public class EnemyUnit
    {
        public GameObject enemyPrefab;
        public int totalCount;
    }


    /// <summary>
    /// Use this for when the fight room is staticaly placed
    /// </summary>
    public bool dontExpand = true;
    public List<EnemyGroup> EnemyWaves = new List<EnemyGroup>();
    private List<System.Guid> disposables = new List<System.Guid>();
    private int currentEnemyWaveIndex;
    public int currentAliveEnemies = 0;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (!dontExpand)
        {
            StartCoroutine(DelaySetBoundary());
        }

    }

    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartFightOnce();
        }
    }

    private void StartFightOnce()
    {
        if (IsFightStarted)
        {
            return;
        }
        Debug.Log("START FIGHT");
        IsFightStarted = true;
        if (SpawnNextWave())
        {
            /*foreach (EnlitDungeonDoorController doorController in EnlitDoors)
            {
                doorController.Open();
            }*/
        }
    }

    #region === Enemy Spawn / Defeat Management ===
    /// <summary>
    /// 
    /// </summary>
    /// <returns>True if fight room beat</returns>
    private bool SpawnNextWave()
    {
        if (currentEnemyWaveIndex == EnemyWaves.Count)
        {
            return true;
        }
        EnemyGroup group = EnemyWaves[currentEnemyWaveIndex];
        foreach ( EnemyUnit unit in group.enemySquads)
        {
            for (int i = 0; i < unit.totalCount; i++)
            {
                GameObject newEnemy = Instantiate(unit.enemyPrefab);
                newEnemy.transform.position = RandomPointInBounds(boxCollider.bounds);
                newEnemy.GetComponent<FreeFlowTargetable>().AddOnDefeatListener(OnEnemyDefeated);
                /*newEnemy.GetComponent<CharacterDressUp>().enemyStyle = CharacterDressUp.EnemyStyle.basic;*/
                enemiesAlive.Add(newEnemy);
                currentAliveEnemies++;
            }
        }
        currentEnemyWaveIndex++;
        return false;
    }

    private static Vector3 RandomPointInBounds(Bounds bounds)
    {
        return new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            bounds.min.y,
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );
    }


    private void OnEnemyDefeated()
    {
        currentAliveEnemies--;
        if (currentAliveEnemies == 0)
        {
            if (SpawnNextWave())
            {
                /*foreach (EnlitDungeonDoorController doorController in EnlitDoors)
                {
                    doorController.Open();
                }*/
            }
        }
    }
    #endregion


    #region === Set Room Size ===
    /// <summary>
    /// Raycasts to find the width and height of the battle area
    /// </summary>
    private IEnumerator DelaySetBoundary()
    {
        yield return new WaitForEndOfFrame();
        Vector3 dimensions = GetRoomSize(transform);
        boxCollider.size = dimensions;
    }

    /// <summary>
    /// Returns a vector2(width, length) of a rooms size based on the center object provided
    /// </summary>
    Vector3 GetRoomSize(Transform center)
    {
        float distRight = 0;
        float distLeft = 0;
        float distForward = 0;
        float distBackward = 0;
        RaycastHit[] hitsDown = Physics.RaycastAll(center.position, -center.up, 1000).OrderBy(x => Vector3.Distance(center.position, x.point)).ToArray<RaycastHit>();
        RaycastHit[] hitsRight = Physics.RaycastAll(center.position, center.right, 1000).OrderBy(x => Vector3.Distance(center.position, x.point)).ToArray<RaycastHit>();
        RaycastHit[] hitsLeft = Physics.RaycastAll(center.position, -center.right, 1000).OrderBy(x => Vector3.Distance(center.position, x.point)).ToArray<RaycastHit>();
        RaycastHit[] hitsForward = Physics.RaycastAll(center.position, center.forward, 1000).OrderBy(x => Vector3.Distance(center.position, x.point)).ToArray<RaycastHit>();
        RaycastHit[] hitsBackward = Physics.RaycastAll(center.position, -center.forward, 1000).OrderBy(x => Vector3.Distance(center.position, x.point)).ToArray<RaycastHit>();

        foreach (RaycastHit hit in hitsRight)
        {
            if (hit.transform.tag == "WALL")
            {
                distRight = Vector3.Distance(center.position, hit.point);
                break;
            }
        }
        foreach (RaycastHit hit in hitsLeft)
        {
            if (hit.transform.tag == "WALL")
            {
                distLeft = Vector3.Distance(center.position, hit.point);
                break;
            }
        }
        foreach (RaycastHit hit in hitsForward)
        {
            if (hit.transform.tag == "WALL")
            {
                distForward = Vector3.Distance(center.position, hit.point);
                break;
            }
        }
        foreach (RaycastHit hit in hitsBackward)
        {
            if (hit.transform.tag == "WALL")
            {
                distBackward = Vector3.Distance(center.position, hit.point);
                break;
            }
        }
        float height = 1.0f;
        foreach (RaycastHit hit in hitsDown)
        {
            if (hit.transform.tag == "FLOOR")
            {
                height = Vector3.Distance(center.position, hit.point);
                break;
            }
        }
        float width = Mathf.Min(distRight,distLeft);
        float depth = Mathf.Min(distForward, distBackward);
        return new Vector3(width * 2, height, depth * 2);
    }
    #endregion
}
