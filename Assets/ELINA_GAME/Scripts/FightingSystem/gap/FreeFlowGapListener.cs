using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface FreeFlowGapListener
{
    void onReachedDestination();
    void onReachedDestinationFail();
}
