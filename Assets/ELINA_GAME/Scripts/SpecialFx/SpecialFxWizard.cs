using System;
using System.Collections.Generic;
using UnityEngine;

public class SpecialFxWizard : MonoBehaviour
{
    [System.Serializable]
    public class SpecialFx
    {
        public string name;
        public GameObject prefab;
        public Vector3 spawnOffsets;
    }
    public static SpecialFxWizard INSTANCE;
    public SpecialFx[] effects;
    private Dictionary<string, SpecialFx> effectsDict = new Dictionary<string, SpecialFx>();

    private Dictionary<string, List<GameObject>> pool = new Dictionary<string, List<GameObject>>();

    public void Awake()
    {
        INSTANCE = this;
        foreach (SpecialFx fx in effects)
        {
            effectsDict.Add(fx.name, fx);
        }
    }

    public static GameObject PlayEffect(SpecialFxRequestBuilder.SpecialFxRequest specialFxRequest)
    {
        if (INSTANCE == null)
        {
            return null;
        }
        return INSTANCE._PlayEffect(specialFxRequest);
    }

    private GameObject _PlayEffect(SpecialFxRequestBuilder.SpecialFxRequest specialFxRequest)
    {
        if (!effectsDict.ContainsKey(specialFxRequest.effectName))
        {
            throw new Exception(specialFxRequest.effectName + " Key doesnt exist in special effects, check naming!");
        }
        SpecialFx effect = effectsDict[specialFxRequest.effectName];
        // Spawn Currently Selected Particle Effect
        Vector3 positionInWorldToSpawn = specialFxRequest.owner.position;
        if (specialFxRequest.offsetPosition != null)
        {
            positionInWorldToSpawn += specialFxRequest.offsetPosition;
        }
        Vector3 spawnPosition = positionInWorldToSpawn + effect.spawnOffsets;

        GameObject newParticleEffect = Recycle(specialFxRequest.effectName);
        if (newParticleEffect != null)
        {
            if (newParticleEffect.GetComponent<RecycleFx>())
            {
                newParticleEffect.GetComponent<RecycleFx>().Recycle();
            }
            newParticleEffect.transform.position = spawnPosition;
            newParticleEffect.transform.rotation = effect.prefab.transform.rotation;
        }
        else
        {
            newParticleEffect = GameObject.Instantiate(effect.prefab, spawnPosition, effect.prefab.transform.rotation) as GameObject;
        }
        if (newParticleEffect.GetComponent<RecycleFx>())
        {
            newParticleEffect.GetComponent<RecycleFx>().recycleTag = specialFxRequest.effectName;
        }
        if (specialFxRequest.hasOffsetRotation)
        {
            newParticleEffect.transform.rotation = Quaternion.Euler(specialFxRequest.offsetRotation);
        }
        if (specialFxRequest.parentToOwner)
        {
            newParticleEffect.transform.parent = specialFxRequest.owner;
        }
        
        newParticleEffect.name = "PE_" + effect.name;
        return newParticleEffect;
    }

    private GameObject Recycle(String key)
    {
        if (!pool.ContainsKey(key))
        {
            return null;
        }
        else
        {
            
            List<GameObject> candidates = pool[key];
            GameObject value = candidates[0];
            candidates.RemoveAt(0);
            if (candidates.Count == 0)
            {
                pool.Remove(key);
            }
            return value;
        }
    }

    // destorys and reuses it, checks the GetInstanceId to prevent duplications
    public static void StoreToPool(GameObject incomingGameObject)
    {
        if (incomingGameObject == null)
        {
            return;
        }
        if (incomingGameObject.GetComponent<RecycleFx>() == null)
        {
            Debug.LogError("Missing recycleFX tag, can not store to pool!");
            Destroy(incomingGameObject);
        }
        string key = incomingGameObject.GetComponent<RecycleFx>().recycleTag;
        List<GameObject> entries;
        if (!INSTANCE.pool.ContainsKey(key))
        {
            entries = new List<GameObject>();
        } else
        {
            entries = INSTANCE.pool[key];
        }
        bool alreadyAdded = false;
        foreach (GameObject entry in entries)
        {
            if (entry!=null && entry.GetInstanceID() == incomingGameObject.GetInstanceID())
            {
                alreadyAdded = true;
                break;
            }
        }
        if (alreadyAdded)
        {
            return;
        }
        entries.Add(incomingGameObject);
        incomingGameObject.transform.position = new Vector3(-9000, -9000, -9000);
        incomingGameObject.transform.parent = null;
        INSTANCE.pool[key] = entries;
    }
}
