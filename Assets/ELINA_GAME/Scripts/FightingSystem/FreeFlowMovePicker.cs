using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FreeFlowMovePicker : MonoBehaviour
{

    private string basePath = "Assets\\Resources\\CombatMoves\\";

    private List<FreeFlowAttackMove> moveList = new List<FreeFlowAttackMove>();
    private AnimationClipHandler animationClipHandler;


    public bool useDebugMove;
    public FreeFlowAttackMove debugMove;

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
        animationClipHandler = AnimationClipHandler.INSTANCE;
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.DefaultValueHandling = DefaultValueHandling.Populate;
        string[] fileNames = Directory.GetFiles(basePath, "*.json", SearchOption.AllDirectories);
        foreach (string fname in fileNames)
        {
            string json = File.ReadAllText(fname);
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

        moveList.Sort(new SortDistance());
    }

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

    public FreeFlowAttackMove PickMoveRandomly(FreeFlowTarget target)
    {
        if (target == null)
        {
            return null;
        }
        if (useDebugMove)
        {
            return debugMove;
        }
        List<FreeFlowAttackMove> possibilities = new List<FreeFlowAttackMove>();
        for (int i = 0; i < moveList.Count; i++)
        {
            FreeFlowAttackMove move = moveList[i];
            float minDistance = move.idealDistance - 0.5f;
            if (minDistance > target.distance)
            {
                // we are too close to do the attack at minimum we need this little space
                // so we exit
                break;
            }
            possibilities.Add(move);
        }
        if (possibilities.Count == 0)
        {
            Debug.Log("Too close to attack: " + target.distance);
            return null;
        }
        int randIdx = Random.Range(0, possibilities.Count);

        return possibilities[randIdx];
    }

    public AnimationClip GetClipByName(string name)
    {
        return animationClipHandler.ClipByName(name);
    }
}
