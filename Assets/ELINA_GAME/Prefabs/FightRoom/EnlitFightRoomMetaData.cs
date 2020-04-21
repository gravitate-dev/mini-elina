using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class EnlitFightRoomMetaData
{
    public int roomId;
    public List<int> adjacentRoomIds = new List<int>();
    //public List<EnlitDungeonDoorController> doors = new List<EnlitDungeonDoorController>();
}
