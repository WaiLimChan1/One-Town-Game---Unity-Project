using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalManagers : MonoBehaviour
{
    [SerializeField] private GameObject parentObj;
    public static GlobalManagers Instance { get; private set; }

    [field: SerializeField] public NetworkRunnerController NetworkRunnerController { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(parentObj);
        }
    }
}
