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
        public float lifeSpanSeconds;
    }
    public static SpecialFxWizard INSTANCE;
    public SpecialFx[] effects;
    private Dictionary<string, SpecialFx> effectsDict = new Dictionary<string, SpecialFx>();

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

        GameObject newParticleEffect = GameObject.Instantiate(effect.prefab, spawnPosition, effect.prefab.transform.rotation) as GameObject;
        if (specialFxRequest.hasOffsetRotation)
        {
            newParticleEffect.transform.rotation = Quaternion.Euler(specialFxRequest.offsetRotation);
        }
        if (specialFxRequest.parentToOwner)
        {
            newParticleEffect.transform.parent = specialFxRequest.owner;
        }
        
        newParticleEffect.name = "PE_" + effect.name;
        // Store Looping Particle Effects Systems

        float destroyIn = (specialFxRequest.lifespan == 0) ? effect.lifeSpanSeconds : specialFxRequest.lifespan;
        if (destroyIn < 0)
        {
            // its forever dont destroy
        } else
        {
            Destroy(newParticleEffect, destroyIn);
        }
        return newParticleEffect;
    }

}
