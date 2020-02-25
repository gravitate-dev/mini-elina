using UnityEngine;

/// <summary>
/// List of soundGroups
/// moan_shots - 2~3 second long moans, 18 of them DB: Sex Examples: Female Moan 01,02,03...
/// </summary>
public class SoundSystem : MonoBehaviour
{
/*
    public static SoundSystem INSTANCE;
    [System.Serializable]
    public class SoundGroup
    {
        public string label;
        public string dbName;
        public string[] soundNames;
    }

    public AudioMixer masterMixer;

    private string basePath = "Assets\\Resources\\Sounds\\";
    private GameOptionsController gameOptionsController;

    private List<SoundGroup> soundGroups = new List<SoundGroup>();
    private List<System.Guid> disposables = new List<System.Guid>();
    void Awake()
    {
        INSTANCE = this;
        SoundyManager.Play("General", "soundy_silent_warmup", transform);
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.DefaultValueHandling = DefaultValueHandling.Populate;
        DirectoryInfo directoryInfo = new DirectoryInfo(basePath);
        FileInfo[] fileInfo = directoryInfo.GetFiles();
        string[] fileNames = Directory.GetFiles(basePath, "*.json", SearchOption.AllDirectories);

        foreach (string fname in fileNames)
        {
            string json = File.ReadAllText(fname);
            SoundGroup soundGroup = JsonConvert.DeserializeObject<SoundGroup>(json, settings);
            soundGroups.Add(soundGroup);
        }

        disposables.Add(WickedObserver.AddListener("GameOptionsInitialized", (unused) =>
        {
            gameOptionsController = GameOptionsController.INSTANCE;
            SetVolumeLevelsFromGameOptions();
        }));
        disposables.Add(WickedObserver.AddListener("OnGameOptionsChanged:" + GameOptionsController.AUDIO_MASTER_VOLUME, (message) =>
         {
             float value = (float)message;
             masterMixer.SetFloat("masterVolume", LinearToDecibel(value));
         }));

        disposables.Add(WickedObserver.AddListener("OnGameOptionsChanged:" + GameOptionsController.AUDIO_MUSIC_VOLUME, (message) =>
        {
            float value = (float)message;
            masterMixer.SetFloat("musicVolume", LinearToDecibel(value));
        }));

        disposables.Add(WickedObserver.AddListener("OnGameOptionsChanged:" + GameOptionsController.AUDIO_FIGHT_EFFECTS_VOLUME, (message) =>
        {
            float value = (float)message;
            masterMixer.SetFloat("fightVolume", LinearToDecibel(value));
        }));

        disposables.Add(WickedObserver.AddListener("OnGameOptionsChanged:" + GameOptionsController.AUDIO_SEX_EFFECTS_VOLUME, (message) =>
        {
            float value = (float)message;
            masterMixer.SetFloat("musicVolume", LinearToDecibel(value));
        }));

        //SoundyManager.Play("Music", "testMusic", IAmElina.ELINA.transform.position);
        
    }

    private void OnDestroy()
    {
        WickedObserver.RemoveListener(disposables);
    }

    private void SetVolumeLevelsFromGameOptions()
    {
        masterMixer.SetFloat("masterVolume", LinearToDecibel(gameOptionsController.GetAudioMasterVolume()));
        masterMixer.SetFloat("musicVolume", LinearToDecibel(gameOptionsController.GetAudioMusicVolume()));
        masterMixer.SetFloat("fightVolume", LinearToDecibel(gameOptionsController.GetAudioFightEffectsVolume()));
        masterMixer.SetFloat("sexVolume", LinearToDecibel(gameOptionsController.GetAudioSexEffectsVolume()));
    }

    private float LinearToDecibel(float linear)
    {
        float dB;

        if (linear != 0)
            dB = 20.0f * Mathf.Log10(linear);
        else
            dB = -144.0f;

        return dB;
    }*/

    /*public SoundyController PlaySound(string groupName, Transform position)
    {
        for (int i = 0; i < soundGroups.Count; i++)
        {
            if (soundGroups[i].label.Equals(groupName))
            {
                return playRandomSoundFromGroup(soundGroups[i], position);
            }
        }
        Debug.LogError("Sound group not found: " + groupName);
        return null;
    }*/

    /*private SoundyController playRandomSoundFromGroup(SoundGroup soundGroup, Transform position)
    {
        int soundIndex = Random.Range(0, soundGroup.soundNames.Length);
        return SoundyManager.Play(soundGroup.dbName, soundGroup.soundNames[soundIndex], position);
    }*/
}
