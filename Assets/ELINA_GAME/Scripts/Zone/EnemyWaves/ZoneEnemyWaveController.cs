using System;
using System.Collections.Generic;
using UnityEngine;

public class ZoneEnemyWaveController : MonoBehaviour
{
    [Serializable]
    public class EnemyWave
    {
        public List<string> playerMessages;
        public List<EnemySquad> enemySquads;
        public int maxEnemiesAllowed;
    }

    [Serializable]
    public class EnemySquad
    {
        public GameObject enemyPrefab;
        public int totalCount;
        public int deployedCount;
        public NPCDressUpItem uniform;
        public NPCDressUpItem uniformSex;
        public List<NPCDressUpItem> hairs = new List<NPCDressUpItem>();
    }

    public EnemyWave enemyWave;


    private ZoneController zoneController;

    public int defeatedEnemyCount;
    public int totalEnemyCount;

    private int GO_ID;
    public int currentlySpawnedEnemies;

    private Collider fightArea;
    private List<Guid> disposables = new List<Guid>();

    public void StartWave()
    {
        fightArea = GetComponent<Collider>();
        GO_ID = gameObject.GetInstanceID();
        zoneController = GetComponentInParent<ZoneController>();
        zoneController.OnEventStart(GetInstanceID());

        // calculate total enemies
        foreach (EnemySquad squad in enemyWave.enemySquads)
        {
            totalEnemyCount += squad.totalCount;
        }
        disposables.Add(WickedObserver.AddListener("OnWaveEnemyDefeated:" + GO_ID, (unused) =>
        {
            defeatedEnemyCount++;
            if (defeatedEnemyCount == totalEnemyCount)
            {
                // victory
                zoneController.OnEventComplete(GetInstanceID());
                return;
            }
            SpawnInEnemy(enemyWave);
        }));

        int initialSpawnCount = 0;
        while (initialSpawnCount++< enemyWave.maxEnemiesAllowed)
        {
            SpawnInEnemy(enemyWave);
        }
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    private void SpawnInEnemy(EnemyWave wave)
    {
        List<EnemySquad> possibleWaveGroups = new List<EnemySquad>();
        foreach (EnemySquad group in wave.enemySquads) {
            if (group.deployedCount < group.totalCount)
            {
                possibleWaveGroups.Add(group);
            }
        }
        if (possibleWaveGroups.Count == 0)
        {
            // dont spawn in anymore enemies!
            return;
        }
        int rndIdx = UnityEngine.Random.Range(0, possibleWaveGroups.Count);

        DeployEnemy(possibleWaveGroups[rndIdx]);

        
    }

    private void DeployEnemy(EnemySquad enemyWaveGroup)
    {
        enemyWaveGroup.deployedCount++;
        GameObject newEnemy = Instantiate(enemyWaveGroup.enemyPrefab);
        CharacterDressUp characterDressUp = newEnemy.GetComponent<CharacterDressUp>();
        NPCDressUpItem chosenHair = null;
        if (enemyWaveGroup.hairs.Count != 0)
        {
            int rdx = UnityEngine.Random.Range(0, enemyWaveGroup.hairs.Count);
            chosenHair = enemyWaveGroup.hairs[rdx];
        }
        List<GameObject> clothes = new List<GameObject>();
        List<GameObject> sexClothes = new List<GameObject>();
        if (enemyWaveGroup.uniform != null)
        {
            clothes.AddRange(enemyWaveGroup.uniform.items);
        }
        if (enemyWaveGroup.uniformSex != null)
        {
            sexClothes.AddRange(enemyWaveGroup.uniformSex.items);
        }

        characterDressUp.FullDressUp(clothes, sexClothes, (chosenHair!=null) ? chosenHair.items : null);
        
        newEnemy.transform.position = RandomPointInBounds(fightArea.bounds);
        newEnemy.GetComponent<WaveEnemy>().setWaveId(GO_ID);
    }

    public static Vector3 RandomPointInBounds(Bounds bounds)
    {
        return new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            bounds.min.y,
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );
    }     
}
