using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Villager : AllyCharacter
{
    [Header("Villager Variables")]
    [SerializeField] protected string[] AttackNames = { "Right", "Left"};
    [SerializeField] protected BoxCollider2D[] AttackBoxes;

    [SerializeField] protected float buildHealAmount;
    [SerializeField] protected float pickUpResourceRadius;
    [SerializeField] protected float dropResourceRadius;

    [Header("Villager Holding Resource")]
    [SerializeField] private GameObject GoldSprite;
    [SerializeField] private GameObject WoodSprite;
    [SerializeField] private GameObject MeatSprite;
    [Networked] public bool holdingResource { get; set; }
    [Networked] public ResourceGenerator.ResourceType resourceType { get; set; }

    [SerializeField] protected GameObject repairBuildingTarget;
    [SerializeField] protected GameObject resourceGeneratorTarget;

    public override void Spawned()
    {
        base.Spawned();

        holdingResource = false;
    }

    public override void EndAnimation()
    {
        if (statusNetworked != CharacterStatus.IDLE && statusNetworked != CharacterStatus.RUN)
            if (AnimationController.AnimationFinished())
                statusNetworked = CharacterStatus.IDLE;
    }

    //For villager IDLE, RUN, BUILD, CHOP, HOLD_IDLE, HOLD_RUN
    protected override void DetermineStatusNetworked(PlayerData input, NetworkButtons buttonsPrev)
    {
        EndAnimation();

        if (statusNetworked != CharacterStatus.BUILD && statusNetworked != CharacterStatus.CHOP)
        {
            statusNetworked = CharacterStatus.IDLE;
            if (input.movementDirection.magnitude > 0) statusNetworked = CharacterStatus.RUN;

            var pressed = input.NetworkButtons.GetPressed(buttonsPrev);
            if (pressed.WasPressed(buttonsPrev, NetworkedPlayer.PlayerInputButtons.LeftMouseButton))
            {
                if (AttackCoolDownTimer.ExpiredOrNotRunning(Runner) && !holdingResource)
                {
                    statusNetworked = CharacterStatus.BUILD; //Build
                    AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
                }
            }
        }
    }

    //---------------------------------------------------------------------------------------------
    //Command Input Helper Functions 
    protected void FindRepairBuildingTarget()
    {
        List<GameObject> foundBuilding = new List<GameObject>();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, SearchRadius, LayerMask.GetMask("Building"));
        foreach (Collider2D collider in colliders)
        {
            Building building = collider.GetComponent<Building>();
            if (building != null && building.healthNetworked > 0 && building.healthNetworked < building.maxHealth) foundBuilding.Add(collider.gameObject);
        }

        if (foundBuilding.Count == 0)
        {
            repairBuildingTarget = null;
            return;
        }

        int minIndex = 0;
        float minDistance = Vector3.Distance(this.transform.position, foundBuilding[0].transform.position);

        for (int i = 1; i < foundBuilding.Count; i++)
        {
            float currentDistance = Vector3.Distance(this.transform.position, foundBuilding[i].transform.position);
            if (currentDistance < minDistance)
            {
                minIndex = i;
                minDistance = currentDistance;
            }
        }
        repairBuildingTarget = foundBuilding[minIndex];
    }

    protected void FindResourceGeneratorTarget()
    {
        List<GameObject> foundResourceGenerator = new List<GameObject>();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, SearchRadius, LayerMask.GetMask("ResourceGenerator"));
        foreach (Collider2D collider in colliders)
        {
            ResourceGenerator resourceGenerator = collider.GetComponent<ResourceGenerator>();
            if (resourceGenerator != null && resourceGenerator.resourceCount > 0) foundResourceGenerator.Add(collider.gameObject);
        }

        if (foundResourceGenerator.Count == 0)
        {
            resourceGeneratorTarget = null;
            return;
        }

        int minIndex = 0;
        float minDistance = Vector3.Distance(this.transform.position, foundResourceGenerator[0].transform.position);

        for (int i = 1; i < foundResourceGenerator.Count; i++)
        {
            float currentDistance = Vector3.Distance(this.transform.position, foundResourceGenerator[i].transform.position);
            if (currentDistance < minDistance)
            {
                minIndex = i;
                minDistance = currentDistance;
            }
        }
        resourceGeneratorTarget = foundResourceGenerator[minIndex];
    }
    //---------------------------------------------------------------------------------------------

    //---------------------------------------------------------------------------------------------
    //Character Take Command Input
    public override void InfluencedByPatrolCommand()
    {
        //Bring resource to town hall
        if (holdingResource)
        {
            movementDirectionNetworked = (NetworkedPlayer.TownHall.transform.position - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
            statusNetworked = CharacterStatus.RUN;
            return;
        }

        if (statusNetworked == CharacterStatus.IDLE || statusNetworked == CharacterStatus.RUN)
        {
            FindEnemyTarget();
            if (enemyTarget != null) //Running from Enemy
            {
                movementDirectionNetworked = (enemyTarget.transform.position - transform.position).normalized;
                facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

                float targetDistance = Vector3.Distance(transform.position, enemyTarget.transform.position);
                if (targetDistance <= RetreatRange)
                {
                    movementDirectionNetworked = -1 * (enemyTarget.transform.position - transform.position).normalized;
                    facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
                    statusNetworked = CharacterStatus.RUN;
                    return;
                }
            }

            FindRepairBuildingTarget();
            if (repairBuildingTarget != null) //Repair building
            {
                movementDirectionNetworked = (repairBuildingTarget.transform.position - transform.position).normalized;
                facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

                float targetDistance = 0;
                Vector2 origin = transform.position;
                Vector2 direction = movementDirectionNetworked;
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, SearchRadius, LayerMask.GetMask("Building"));
                if (hit.collider != null && hit.collider.gameObject == repairBuildingTarget) targetDistance = hit.distance + AttackRange / 2;
                else targetDistance = SearchRadius;

                //Determine status based on target distance
                if (targetDistance > AttackRange)
                {
                    statusNetworked = CharacterStatus.RUN;
                    return;
                }
                else if (targetDistance <= AttackRange)
                {
                    if (AttackCoolDownTimer.ExpiredOrNotRunning(Runner)) statusNetworked = CharacterStatus.BUILD;
                    else statusNetworked = CharacterStatus.IDLE;
                    return;
                }
            }

            FindResourceGeneratorTarget();
            if (resourceGeneratorTarget != null) //Repair building
            {
                if (!holdingResource)
                {
                    movementDirectionNetworked = (resourceGeneratorTarget.transform.position - transform.position).normalized;
                    facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
                    statusNetworked = CharacterStatus.RUN;
                    return;
                }
            }

            //No target, then just patrol the town
            Patrol();
            
        }

    }

    public override void InfluencedByFollowCommand()
    {
        if (statusNetworked == CharacterStatus.BUILD || statusNetworked == CharacterStatus.CHOP) return;

        //Follow Target
        FindFollowTarget();
        if (followTarget != null)
        {
            movementDirectionNetworked = (followTarget.transform.position - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

            statusNetworked = CharacterStatus.RUN;

            float followTargetDistance = Vector3.Distance(transform.position, followTarget.transform.position);
            if (followTargetDistance < StopFollowDistance) statusNetworked = CharacterStatus.IDLE;
            if (followTargetDistance < MoveOutOfWayDistance)
            {
                statusNetworked = CharacterStatus.RUN;
                movementDirectionNetworked *= -1;
            }
        }
        else
            statusNetworked = CharacterStatus.IDLE;
    }

    public override void InfluencedByHoldPositionCommand()
    {
        if (statusNetworked == CharacterStatus.BUILD || statusNetworked == CharacterStatus.CHOP) return;

        //Bring resource to town hall
        if (holdingResource)
        {
            movementDirectionNetworked = (NetworkedPlayer.TownHall.transform.position - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
            statusNetworked = CharacterStatus.RUN;
            return;
        }

        FindResourceGeneratorTarget();
        if (resourceGeneratorTarget != null) //Repair building
        {
            if (!holdingResource)
            {
                movementDirectionNetworked = (resourceGeneratorTarget.transform.position - transform.position).normalized;
                facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
                statusNetworked = CharacterStatus.RUN;
                return;
            }
        }
        else
            Patrol();

        ////Move To Hold Position
        //movementDirectionNetworked = (holdPosition - transform.position).normalized;
        //facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

        //if (statusNetworked == CharacterStatus.IDLE || statusNetworked == CharacterStatus.RUN)
        //{
        //    statusNetworked = CharacterStatus.RUN;

        //    float holdPositionDistance = Vector3.Distance(transform.position, holdPosition);
        //    if (holdPositionDistance < StopMoveHoldDistance) statusNetworked = CharacterStatus.IDLE;
        //}
    }
    //---------------------------------------------------------------------------------------------

    protected void PickUpResource()
    {
        if (holdingResource) return;
        if (statusNetworked == CharacterStatus.IDLE || statusNetworked == CharacterStatus.RUN)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickUpResourceRadius, LayerMask.GetMask("ResourceGenerator"));
            foreach (Collider2D collider in colliders)
            {
                ResourceGenerator ResourceGenerator = collider.GetComponent<ResourceGenerator>();
                if (ResourceGenerator != null && ResourceGenerator.resourceCount > 0)
                {
                    ResourceGenerator.LoseResource();
                    holdingResource = true;
                    resourceType = ResourceGenerator.resourceType;
                    return;
                }
            }
        }
    }

    protected void GiveUpResources()
    {
        if (!holdingResource) return;
        Vector3 townHallPosition = NetworkedPlayer.TownHall.transform.position;
        if (Vector3.Distance(transform.position, townHallPosition) <= dropResourceRadius)
        {
            NetworkedPlayer.GainGold();
            holdingResource = false;
        }
    }

    public override void Logic()
    {
        UpdatePosition();
        PickUpResource();
        GiveUpResources();
    }

    public override void Render()
    {
        //Flip Character
        if (statusNetworked == CharacterStatus.IDLE || statusNetworked == CharacterStatus.RUN)
        {
            if (IsFacingRight()) spriteRenderer.flipX = false;
            if (IsFacingLeft()) spriteRenderer.flipX = true;
        }

        //Change status to HOlD Status
        if (holdingResource)
        {
            if (statusNetworked == CharacterStatus.IDLE) statusNetworked = CharacterStatus.HOLD_IDLE;
            if (statusNetworked == CharacterStatus.RUN) statusNetworked = CharacterStatus.HOLD_RUN;
        }

        //Render Holding Resources
        GoldSprite.SetActive(false);
        WoodSprite.SetActive(false);
        MeatSprite.SetActive(false);
        if (holdingResource)
        {
            if (resourceType == ResourceGenerator.ResourceType.GOLD) GoldSprite.SetActive(true);
            else if (resourceType == ResourceGenerator.ResourceType.WOOD) WoodSprite.SetActive(true);
            else if (resourceType == ResourceGenerator.ResourceType.MEAT) MeatSprite.SetActive(true);
        }

        AnimationController.ChangeAnimation((int)statusNetworked);
    }

    public override void AnimationTriggerAttack()
    {
        int index = 0;
        if (statusNetworked == CharacterStatus.BUILD)
        {
            if (!spriteRenderer.flipX) index = 0;
            if (spriteRenderer.flipX) index = 1;
        }

        BoxCollider2D attackBox = AttackBoxes[index];

        //Hitting Enemy Characters
        Collider2D[] colliders = Physics2D.OverlapBoxAll(attackBox.bounds.center, attackBox.bounds.size, 0, LayerMask.GetMask("EnemyCharacter"));
        foreach (Collider2D collider in colliders)
        {
            Character enemy = collider.GetComponent<Character>();
            if (enemy != null && enemy.healthNetworked > 0) enemy.TakeDamageNetworked(damage);
        }

        //Healing Buildings
        colliders = Physics2D.OverlapBoxAll(attackBox.bounds.center, attackBox.bounds.size, 0, LayerMask.GetMask("Building"));
        foreach (Collider2D collider in colliders)
        {
            Building building = collider.GetComponent<Building>();
            if (building != null && building.healthNetworked > 0) building.HealNetworked(buildHealAmount);
        }
    }
}