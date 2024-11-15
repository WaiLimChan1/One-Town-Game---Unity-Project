using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyCharacter : Character
{
    protected static Vector3 TownHallPosition = new Vector3(0, 0, 0);
    [SerializeField] protected GameObject target;

    protected void FindTarget()
    {
        List<GameObject> foundTargets = new List<GameObject>();

        //Find all ally targets
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, SearchRadius, LayerMask.GetMask("AllyCharacter"));
        foreach (Collider2D collider in colliders)
        {
            AllyCharacter ally = collider.GetComponent<AllyCharacter>();
            if (ally != null) foundTargets.Add(collider.gameObject);
        }

        //Find all building targets
        colliders = Physics2D.OverlapCircleAll(transform.position, SearchRadius, LayerMask.GetMask("Building"));
        foreach (Collider2D collider in colliders)
        {
            Building building = collider.GetComponent<Building>();
            if (building != null && building.healthNetworked > 0) foundTargets.Add(collider.gameObject);
        }

        //No targets found
        if (foundTargets.Count == 0)
        {
            target = null;
            return;
        }

        //Find the closest target
        int minIndex = 0;
        float minDistance = Vector3.Distance(this.transform.position, foundTargets[0].transform.position);

        for (int i = 1; i < foundTargets.Count; i++)
        {
            float currentDistance = Vector3.Distance(this.transform.position, foundTargets[i].transform.position);
            if (currentDistance < minDistance)
            {
                minIndex = i;
                minDistance = currentDistance;
            }
        }
        target = foundTargets[minIndex];
    }

    protected abstract void EndAnimation();
    protected abstract void DetermineStatus();

    public override bool ObstacleIsInTheWay(float currentDegree, float range, float startingPosOffSet)
    {
        float angleRadians = currentDegree * Mathf.Deg2Rad;
        Vector3 direction = (new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0)).normalized;
        Vector3 changeVector = direction * range;
        Vector3 startingPosition = transform.position + direction * startingPosOffSet;


        RaycastHit2D hitBuilding = Physics2D.Raycast(startingPosition, direction, range, LayerMask.GetMask("Building"));
        RaycastHit2D hitAllyCharacter = Physics2D.Raycast(startingPosition, direction, range, LayerMask.GetMask("EnemyCharacter"));
        return hitBuilding.collider != null || hitAllyCharacter.collider != null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;
        FindTarget();

        if (target != null)
        {
            movementDirectionNetworked = (target.transform.position - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
        }

        DetermineStatus();
        UpdatePosition();
    }
}