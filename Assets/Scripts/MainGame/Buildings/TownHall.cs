using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownHall : Building
{
    [Header("TownHall Variables")]
    [SerializeField] public float TownRangeRadius;

    private void OnDrawGizmos()
    {

        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, TownRangeRadius);
    }
}
