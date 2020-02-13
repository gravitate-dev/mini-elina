using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PussyCam : MonoBehaviour
{
    public GameObject pussy;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(pussy.transform);
    }
}
