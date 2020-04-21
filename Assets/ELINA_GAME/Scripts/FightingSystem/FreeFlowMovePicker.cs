using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class FreeFlowMovePicker : MonoBehaviour
{

    private string basePath = "Assets\\Resources\\CombatMoves\\";

    private List<FreeFlowAttackMove> moveList = new List<FreeFlowAttackMove>();
    private AnimationClipHandler animationClipHandler;

    public static FreeFlowMovePicker INSTANCE;
    public bool useDebugMove;
    public FreeFlowAttackMove debugMove;

    [Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    private void GenerateJSON()
    {
        string finisherClause = "";
        if (debugMove.finisher)
        {
            finisherClause = $@"
    ""finisher"":""true"",
";
        }
        string backWardsClause = "";
        if (debugMove.backwardsAttack)
        {
            backWardsClause = $@"
    ""backwardsAttack"":""true"",
";
        }
        string s = $@"
{{
    ""moveName"":""{debugMove.moveName}"",{finisherClause+backWardsClause}
	""idealDistance"":{debugMove.idealDistance},
	""attackerAnimation"":""{debugMove.DEBUG_ATTACK.name}"",
	""victimAnimation"":""{debugMove.DEBUG_VICTIM.name}"",
	""victimAnimationDelay"":{debugMove.victimAnimationDelay},
    ""attackerLockTimeAfterHit"":{debugMove.attackerLockTimeAfterHit}
}},";
        Debug.Log(s);      
    }

    // Start is called before the first frame update
    void Awake()
    {
        INSTANCE = this;
        animationClipHandler = AnimationClipHandler.INSTANCE;

#if UNITY_EDITOR
        InitCombatMoves();
#elif UNITY_WEBGL
        InitCombatMovesWebGL();
#else
        InitCombatMoves();
#endif

        moveList.Sort(new SortDistance());
    }

    #region === WebGL Init ===
    [Button(ButtonSizes.Large), GUIColor(0, 1, 0)]
    public void PrintAllJsonFilesForWebGL()
    {
        Debug.Log(String.Join(",", Directory.GetFiles(basePath, "*.json", SearchOption.AllDirectories)));
    }
    private void InitCombatMovesWebGL()
    {
        string[] fileNames = GetJsonFiles();
        foreach (string fname in fileNames)
        {
            StartCoroutine(LoadCombatMoveWebGL(fname));
        }
    }
    private string[] GetJsonFiles()
    {
        string[] named = new string[] {
        "Assets\\Resources\\CombatMoves\\_base_counters.json",
        "Assets\\Resources\\CombatMoves\\_base_kicks.json",
        "Assets\\Resources\\CombatMoves\\_base_punches.json",
    };
        return named;
    }

    private IEnumerator LoadCombatMoveWebGL(string filePath)
    {
        UnityWebRequest www = UnityWebRequest.Get(filePath);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            AddCombatMove(www.downloadHandler.text);
        }
    }
    #endregion

    #region === Init ===
    private void InitCombatMoves()
    {
        string[] fileNames = Directory.GetFiles(basePath, "*.json", SearchOption.AllDirectories);
        foreach (string fname in fileNames)
        {
            string json = File.ReadAllText(fname);
            AddCombatMove(json);
        }
    }
    private void AddCombatMove(string json)
    {
        try
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.DefaultValueHandling = DefaultValueHandling.Populate;
            List<FreeFlowAttackMove> moves = JsonConvert.DeserializeObject<List<FreeFlowAttackMove>>(json, settings);
            foreach (FreeFlowAttackMove move in moves)
            {
                if (move.disabled)
                {
                    continue;
                }
                // debug this later
                moveList.Add(move);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("ERROR IN JSON FOR " + json);
        }
    }
    #endregion

    private class SortDistance : IComparer<FreeFlowAttackMove>
    {
        public int Compare(FreeFlowAttackMove x, FreeFlowAttackMove y)
        {
            if (x == null && y == null) return 0;
            if (x == null && y != null) return 1; // Equal
            if (x != null && y == null) return -1; // Equal

            //todo sort on ideal or nonIdealDistance?
            float epsilon = Mathf.Abs(x.idealDistance - y.idealDistance);
            if (epsilon < 0.01)
            {
                return x.moveName.CompareTo(y.moveName);
            }
            if (x.idealDistance > y.idealDistance)
            {
                return 1;
            } else
            {
                return -1;
            }
        }
    }

    public FreeFlowAttackMove PickMoveRandomly(Transform player, GameObject target, bool counter)
    {
        if (target == null)
        {
            return null;
        }
        if (useDebugMove)
        {
            return debugMove;
        }

        FreeFlowTargetable ffTargetable = target.GetComponent<FreeFlowTargetable>();

        Vector3 planarTargetPos = Vector3.ProjectOnPlane(target.transform.position, Vector3.up);
        float targetDistance = Vector3.Distance(player.position, planarTargetPos);

        List<FreeFlowAttackMove> possibilities = new List<FreeFlowAttackMove>();
        for (int i = 0; i < moveList.Count; i++)
        {
            FreeFlowAttackMove move = moveList[i];
            if (move.isCounter != counter)
            {
                continue; // pick counters
            }
            possibilities.Add(move);
        }
        if (possibilities.Count == 0)
        {
            Debug.Log("Too close to attack: " + targetDistance);
            return null;
        }
        int randIdx = UnityEngine.Random.Range(0, possibilities.Count);

        return possibilities[randIdx];
    }

    public AnimationClip GetClipByName(string name)
    {
        return animationClipHandler.ClipByName(name);
    }
}
