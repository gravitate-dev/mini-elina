﻿using Newtonsoft.Json;
using System.ComponentModel;
using UnityEngine;

[System.Serializable]
public class HMove
{
    /// <summary>
    /// sceneIndexSync and playClimaxSync and loopCountSync are used to synchronize the sex happening
    /// </summary>
    [JsonIgnore]
    public int sceneIndexSync;
    [JsonIgnore]
    public int loopCountSync;

    /// <summary>
    /// true - if player is fucking around with NPCs
    /// effects ____
    /// *heat builds from zero when move starts
    /// *cumming doesnt have any effect of player health
    /// *hp does not recover while having sex
    /// after exiting the orgasm bar is set to 0% and lewd bar set to 0%
    /// </summary>
    public bool playground;
    public bool dynamicBoneDisable;
    public bool codeControlFaces;
    [JsonIgnore]
    public bool playClimax;

    [DefaultValue(false)]
    public bool disabled;

    public string moveName;
    /// <summary>
    /// only set true for solo/masterbate ones
    /// </summary>
    [DefaultValue(false)]
    public bool interruptable;

    /// <summary>
    /// This is the victims location everytime.
    /// </summary>
    public Vector3 sexLocationPosition;
    public Quaternion sexLocationRotation;
    public Victim victim;
    public Attacker[] attackers;
    /// <summary>
    /// lewd,humil
    /// </summary>
    [DefaultValue("lewd")]
    public string category = "lewd";

    /// <summary>
    /// victim, attacker (always the lead attacker)
    /// </summary>
    [DefaultValue("victim")]
    public string location;

    public AnimationItem[] scenes;

    [System.Serializable]
    public class Victim
    {
        /// <summary>
        /// Parts that must be open for sex, use to limit move to certain genders
        /// "ass|anus|breast|hand|mouth|penis|pussy|stomach"
        /// </summary>
        public string[] reqParts;

        [JsonIgnore]
        public GameObject gameObject;

    }
    [System.Serializable]
    public class Attacker
    {
        /// <summary>
        /// What sex parts needed
        /// penis - only male/futa
        /// pussy - only female/futa
        /// flat - only male
        /// ass - all
        /// device for a device move
        /// "ass|breast|hand|mouth|penis|pussy|stomach|device"
        /// </summary>
        public string usingPart;

        /// <summary>
        /// what to fuck
        /// "ass|anus|breast|hand|mouth|penis|pussy|stomach"
        /// </summary>
        public string targetPart;

        [JsonIgnore]
        public GameObject gameObject;
    }

    [System.Serializable]
    public class AnimationItem
    {
        [DefaultValue(1.0f)]
        public float heatRate;

        /// <summary>
        /// If there is an orgasm scene
        /// </summary>
        public bool isOrgasm;

        /// <summary>
        /// If this is a clip that should only be played once, and continued
        /// </summary>
        public bool oneShot;
        /// <summary>
        /// If the victims heat is too low this scene will not play
        /// </summary>
        public float minHeatLimit = -1;

        /// <summary>
        /// If the victims heat is too high this scene will not play
        /// </summary>
        [DefaultValue(float.MaxValue)]
        public float maxHeatLimit;

        /// <summary>
        /// Decides if this animation sequence should be played again, useful for excluding start animations like unzip pants then fuck, don't repeat unzip pants animation and fuck again
        /// true - When the animationset is played through, this animation wont be played a 2nd time around
        /// false - this will be included the 2nd time the animationset is played
        /// </summary>
        [DefaultValue(true)]
        public bool replay = true;

        /// <summary>
        /// Rate to which to play the moans, if zero then no moans are played
        /// </summary>
        public int bpm;
        /// <summary>
        /// These moods correspond to the moan types that are played while sexing
        /// reluctant
        /// enjoying
        /// fucked_silly
        /// </summary>
        public string victimMoodOverride;
        public string victimAnimationId;
        public string[] attackerAnimationIds;

        [DefaultValue(1)]
        public int loopCount = 1;

        [DefaultValue(1.0f)]
        public float animationSpeed = 1.0f;

        public Sound[] sounds;

        public AnimationItem(AnimationItem copy)
        {
            if (copy == null || copy.attackerAnimationIds == null || copy.attackerAnimationIds.Length == 0)
            {
                return;
            }
            this.isOrgasm = copy.isOrgasm;
            this.oneShot = copy.oneShot;
            this.minHeatLimit = copy.minHeatLimit;
            this.maxHeatLimit = copy.maxHeatLimit;
            this.heatRate = copy.heatRate;
            this.replay = copy.replay;
            this.bpm = copy.bpm;
            this.victimMoodOverride = copy.victimMoodOverride;
            this.victimAnimationId = copy.victimAnimationId;
            try
            {
                this.attackerAnimationIds = new string[copy.attackerAnimationIds.Length];
            } catch (System.Exception)
            {
                Debug.LogError("SCOOBY");
            }
            for (int j = 0; j < copy.attackerAnimationIds.Length; j++)
            {
                this.attackerAnimationIds[j] = copy.attackerAnimationIds[j];
            }


            this.loopCount = copy.loopCount = 1;
            this.animationSpeed = copy.animationSpeed = 1.0f;

            if (copy.sounds != null && copy.sounds.Length != 0)
            {
                this.sounds = new Sound[copy.sounds.Length];

                for (int j = 0; j < copy.sounds.Length; j++)
                {
                    Sound sound = new Sound();
                    sound.soundId = copy.sounds[j].soundId;
                    sound.time = copy.sounds[j].time;
                    sound.probability = copy.sounds[j].probability;
                    this.sounds[j] = sound;
                }
            }

        }
    }

    [System.Serializable]
    public class Sound
    {
        public string soundId;
        /// <summary>
        /// When to play the sound in the animation
        /// 0 = start
        /// 0.5 = half way through the animation clip
        /// 1 = end of the animation
        /// </summary>
        public float time = 0.0f;

        /// <summary>
        /// Probability of playing from 0 to 1.0
        /// </summary>
        [DefaultValue(1.0f)]
        public float probability = 1.0f;
    }

    public HMove(HMove copy)
    {
        if (copy == null)
        {
            return;
        }
        this.sceneIndexSync = copy.sceneIndexSync;
        this.playground = copy.playground;
        this.dynamicBoneDisable = copy.dynamicBoneDisable;
        this.codeControlFaces = copy.codeControlFaces;
        this.playClimax = copy.playClimax;
        this.loopCountSync = copy.loopCountSync;
        this.disabled = copy.disabled;
        this.interruptable = copy.interruptable;
        this.moveName = copy.moveName;

        this.sexLocationPosition = new Vector3(copy.sexLocationPosition.x, copy.sexLocationPosition.y, copy.sexLocationPosition.z);
        this.sexLocationRotation = new Quaternion(copy.sexLocationRotation.x, copy.sexLocationRotation.y, copy.sexLocationRotation.z, copy.sexLocationRotation.w);

        this.victim = new Victim();

        this.victim.reqParts = new string[copy.victim.reqParts.Length];
        for (int i = 0; i < copy.victim.reqParts.Length; i++)
        {
            this.victim.reqParts[i] = copy.victim.reqParts[i];
        }
        this.victim.gameObject = copy.victim.gameObject;

        this.attackers = new Attacker[copy.attackers.Length];
        for (int i = 0; i < copy.attackers.Length; i++)
        {
            Attacker attacker = new Attacker();
            attacker.targetPart = copy.attackers[i].targetPart;
            attacker.usingPart = copy.attackers[i].usingPart;
            attacker.gameObject = copy.attackers[i].gameObject;
            this.attackers[i] = attacker;
        }

        this.category = copy.category;

        this.scenes = new AnimationItem[copy.scenes.Length];
        for (int i = 0; i < copy.scenes.Length; i++)
        {
            this.scenes[i] = new AnimationItem(copy.scenes[i]);
        }
    }

    /// <summary>
    /// While this target is being sexed, this will check if another enemy can join in
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="avaibleParts"></param>
    /// <returns>Returns -1 if they can not participate otherwise it will return  the id of the attacker</returns>
    public int GetOpenAttackerIndex(GameObject attacker, string[] avaibleParts)
    {
        int GO_ID = attacker.GetInstanceID();
        for (int i = 0; i < attackers.Length; i++)
        {
            Attacker possibility = attackers[i];
            if (possibility.gameObject == null || possibility.gameObject.GetInstanceID() != GO_ID)
            {
                // possible match compare parts
                foreach (string part in avaibleParts)
                {
                    if (part.Equals(possibility.usingPart))
                    {
                        return i;
                    }
                }
            }
        }
        return -1;
    }
}
