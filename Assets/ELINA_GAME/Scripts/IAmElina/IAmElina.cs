using UnityEngine;

/// <summary>
/// This script allows all objects that require elina to get a handle to her
/// </summary>
public class IAmElina : MonoBehaviour
{
    public static GameObject ELINA;
    // Start is called before the first frame update
    void Awake()
    {
        ELINA = gameObject;
    }
}
