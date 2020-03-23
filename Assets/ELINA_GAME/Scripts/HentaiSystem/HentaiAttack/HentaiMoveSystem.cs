using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HentaiMoveSystem : MonoBehaviour
{

    public static HentaiMoveSystem INSTANCE;
    private string basePath = "Assets\\Resources\\Animations\\";
    public List<HMove> hMoves = new List<HMove>();
    void Awake()
    {
        INSTANCE = this;
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
        string[] fileNames = Directory.GetFiles(basePath, "*.json", SearchOption.AllDirectories);
        foreach (string fname in fileNames)
        {
            string json = File.ReadAllText(fname);
            List<HMove> moves = JsonConvert.DeserializeObject<List<HMove>>(json, settings);
            foreach (HMove hmove in moves)
            {
                if (hmove.disabled)
                {
                    continue;
                }
                // debug this later
                hMoves.Add(hmove);
            }
        }
    }

    public HMove GetRandomHMoveForParts(string[] attackerPartsAvail, string[] victimPartsAvail)
    {
        List<HMove> moves = getHMoveForParts(attackerPartsAvail, victimPartsAvail);
        if (moves.Count == 0)
        {
            return null;
        }
        int randIdx = Random.Range(0, moves.Count);
        return moves[randIdx];
    }
    /// <summary>
    /// Gets a list of moves
    /// </summary>
    /// <param name="attackerPartsAvail"></param>
    /// <param name="victimPartsAvail"></param>
    /// <param name="attackerRequiredTags"></param>
    /// <returns></returns>
    public List<HMove> getHMoveForParts(string[] attackerPartsAvail, string[] victimPartsAvail)
    {
        if (attackerPartsAvail==null || attackerPartsAvail.Length ==0 || victimPartsAvail==null || victimPartsAvail.Length == 0)
        {
            Debug.LogError("Attack or victim parts can not be null/empty");
        }
        HashSet<string> attackerParts = new HashSet<string>(attackerPartsAvail);
        HashSet<string> victimParts = new HashSet<string>(victimPartsAvail);
        List<HMove> possibilities = new List<HMove>();
        foreach (HMove m in hMoves){
            // if its a solo move
            if (m.attackers == null || m.attackers.Length < 0)
            {
                continue;
            }
            if (!attackerParts.Contains(m.attackers[0].usingPart))
            {
                continue;
            }
            bool okay = false;
            foreach (string victimReq in m.victim.reqParts)
            {
                if (victimParts.Contains(victimReq))
                {
                    okay = true;
                }
            }

            if (!okay)
            {
                continue;
            }
            possibilities.Add(m);
        }
        return possibilities;
    }
    
    public HMove getFemaleMasterbation()
    {
        HashSet<string> set = new HashSet<string>();
        set.Add("pussy");
        set.Add("boobs");
        return getSoloHMove(set);
    }
    public HMove getMaleMasterbation()
    {
        HashSet<string> set = new HashSet<string>();
        set.Add("penis");
        return getSoloHMove(set);
    }

    private HMove getSoloHMove(HashSet<string> victimParts)
    {
        List<HMove> possibilities = new List<HMove>();
        foreach (HMove m in hMoves)
        {
            
            // if its a solo move
            if (m.attackers != null && m.attackers.Length > 0)
            {
                continue;
            }
            bool okay = false;
            foreach (string victimReq in m.victim.reqParts)
            {
                if (victimParts.Contains(victimReq))
                {
                    okay = true;
                }
            }

            if (!okay)
            {
                continue;
            }
            possibilities.Add(m);
        }
        if (possibilities.Count > 0)
        {
            return possibilities[0];
        }
        return null;
    }

    public HMove getHMoveByName( string name ) {
        foreach (HMove move in hMoves)
        {
            if (move.moveName.Equals(name))
            {
                return move;
            }
        }
        return null;
    }

}
