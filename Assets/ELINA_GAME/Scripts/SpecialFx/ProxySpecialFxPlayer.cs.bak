using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows special fx to be called from an Animation Clip
/// </summary>
public class ProxySpecialFxPlayer : MonoBehaviour
{

    public Transform animationHips;
    public void PlaySpecialEffectFromAnimation(string arguments)
    {
        string[] parsed = arguments.Split(':');
        if (parsed.Length == 1)
        {
            SpecialFxRequestBuilder.newBuilder(parsed[0])
                .setOwner(animationHips, false)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.HALF_PLAYER_HEIGHT, 0))
                .build().Play();
        } else if (parsed.Length == 2)
        {
            SpecialFxRequestBuilder.newBuilder(parsed[0])
                .setOwner(animationHips, false)
                .setOffsetPosition(new Vector3(0, SpecialFxRequestBuilder.HALF_PLAYER_HEIGHT, 0))
                .setLifespan(float.Parse(parsed[1]))
                .build().Play();
        }
        
        
    }
}
