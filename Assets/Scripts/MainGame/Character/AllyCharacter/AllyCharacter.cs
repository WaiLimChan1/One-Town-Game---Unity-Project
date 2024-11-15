using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class AllyCharacter : Character
{
    public enum CommandStatus { NONE, PATROL, FOLLOW, HOLD_POSITION }
    public static float StopFollowDistance = 3;
    public static float MoveOutOfWayDistance = 1;
    public static float StopMoveHoldDistance = 0.2f;

    [Header("Ally Character Components")]
    public NetworkedPlayer NetworkedPlayer;
    [SerializeField] public Image ControlTargetIcon;
    [SerializeField] protected Image CommandIconBackground;
    [SerializeField] protected Image CommandIcon;

    [SerializeField] protected GameObject enemyTarget;
    [SerializeField] protected GameObject followTarget;

    [SerializeField] protected float patrolKeepTime;
    [SerializeField] protected TickTimer patrolTimer;
    [SerializeField] protected Vector3 patrolPosition;

    [SerializeField] protected Vector3 holdPosition;
    public void RecordHoldPosition() { holdPosition = transform.position; }

    [Networked] public CommandStatus commandStatusNetworked { get; set; }


    public override void Spawned()
    {
        base.Spawned();
        commandStatusNetworked = CommandStatus.PATROL;
    }



    //---------------------------------------------------------------------------------------------
    //Target Character Take Player Input
    public abstract void EndAnimation();
    protected abstract void DetermineStatusNetworked(PlayerData input, NetworkButtons buttonsPrev);

    public void InfluencedByInput(PlayerData input, NetworkButtons buttonsPrev)
    {
        DetermineFacingDirectionDegreeNetwored(input);
        DetermineStatusNetworked(input, buttonsPrev);
        movementDirectionNetworked = input.movementDirection;
    }
    //---------------------------------------------------------------------------------------------


    //---------------------------------------------------------------------------------------------
    //Command Input Helper Functions 
    protected void FindEnemyTarget()
    {
        List<GameObject> foundCharacters = new List<GameObject>();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, SearchRadius, LayerMask.GetMask("EnemyCharacter"));
        foreach (Collider2D collider in colliders)
        {
            EnemyCharacter enemy = collider.GetComponent<EnemyCharacter>();
            if (enemy != null) foundCharacters.Add(collider.gameObject);
        }

        if (foundCharacters.Count == 0)
        {
            enemyTarget = null;
            return;
        }

        int minIndex = 0;
        float minDistance = Vector3.Distance(this.transform.position, foundCharacters[0].transform.position);

        for (int i = 1; i < foundCharacters.Count; i++)
        {
            float currentDistance = Vector3.Distance(this.transform.position, foundCharacters[i].transform.position);
            if (currentDistance < minDistance)
            {
                minIndex = i;
                minDistance = currentDistance;
            }
        }
        enemyTarget = foundCharacters[minIndex];
    }

    protected void FindFollowTarget()
    {
        if (NetworkedPlayer.ControlTargetCharacterNetworked != null)
            followTarget = NetworkedPlayer.ControlTargetCharacterNetworked.gameObject;
        else 
            followTarget = null;
    }

    protected void Patrol()
    {
        if (patrolTimer.ExpiredOrNotRunning(Runner))
        {
            Vector3 townHallPosition = NetworkedPlayer.TownHall.gameObject.transform.position;
            float randDegree = Random.Range(0f, 360f);
            float randDistance = Random.Range(1, NetworkedPlayer.TownHall.TownRangeRadius);

            float angleRadians = randDegree * Mathf.Deg2Rad;
            Vector3 changeVector = (new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0)).normalized * randDistance;
            patrolPosition = townHallPosition + changeVector;
            patrolTimer = TickTimer.CreateFromSeconds(Runner, patrolKeepTime);
        }

        //Move to patrol position
        movementDirectionNetworked = (patrolPosition - transform.position).normalized;
        facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

        if (statusNetworked == CharacterStatus.IDLE || statusNetworked == CharacterStatus.RUN)
        {
            statusNetworked = CharacterStatus.RUN;

            float patrolPositionDistance = Vector3.Distance(transform.position, patrolPosition);
            if (patrolPositionDistance < StopMoveHoldDistance) statusNetworked = CharacterStatus.IDLE;
        }
    }
    //---------------------------------------------------------------------------------------------


    //---------------------------------------------------------------------------------------------
    //Character Take Command Input
    public void InfluencedByNoneCommand()
    {
        if (statusNetworked == CharacterStatus.RUN) 
            statusNetworked = CharacterStatus.IDLE;
    }

    public virtual void InfluencedByPatrolCommand()
    {
        FindEnemyTarget();
        if (enemyTarget != null)
        {
            movementDirectionNetworked = (enemyTarget.transform.position - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
        }
    }

    public virtual void InfluencedByFollowCommand()
    {
        FindEnemyTarget();
        FindFollowTarget();
    }

    public virtual void InfluencedByHoldPositionCommand()
    {
        FindEnemyTarget();
    }

    public void InfluencedByCommand()
    {
        if (!Runner.IsServer) return;
        EndAnimation();

        if (commandStatusNetworked == CommandStatus.NONE) InfluencedByNoneCommand();
        if (commandStatusNetworked == CommandStatus.PATROL) InfluencedByPatrolCommand();
        if (commandStatusNetworked == CommandStatus.FOLLOW) InfluencedByFollowCommand();
        if (commandStatusNetworked == CommandStatus.HOLD_POSITION) InfluencedByHoldPositionCommand();
    }
    //---------------------------------------------------------------------------------------------

    public override bool ObstacleIsInTheWay(float currentDegree, float range, float startingPosOffSet)
    {
        float angleRadians = currentDegree * Mathf.Deg2Rad;
        Vector3 direction = (new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0)).normalized;
        Vector3 changeVector = direction * range;
        Vector3 startingPosition = transform.position + direction * startingPosOffSet;


        RaycastHit2D hitBuilding = Physics2D.Raycast(startingPosition, direction, range, LayerMask.GetMask("Building"));
        RaycastHit2D hitAllyCharacter = Physics2D.Raycast(startingPosition, direction, range, LayerMask.GetMask("AllyCharacter"));
        return hitBuilding.collider != null || hitAllyCharacter.collider != null;
    }

    public virtual void Logic()
    {
        UpdatePosition();
    }

    //---------------------------------------------------------------------------------------------
    //Character UI. Target and Command ICON Character
    public void InactivateControlTargetIcon() 
    {
        ControlTargetIcon.gameObject.SetActive(false); 
    }
    public void ActivateControlTargetIcon() 
    {
        ControlTargetIcon.gameObject.SetActive(true); 
    }
    public void InactivateCommandIcon() {
        CommandIconBackground.gameObject.SetActive(false);
        CommandIcon.gameObject.SetActive(false); 
    }
    public void ActivateCommandIcon() {
        CommandIconBackground.gameObject.SetActive(true); 
        CommandIcon.gameObject.SetActive(true); 
    }

    public void SetCommandIconColor()
    {
        if (commandStatusNetworked == CommandStatus.NONE) CommandIcon.color = Color.red;
        if (commandStatusNetworked == CommandStatus.PATROL) CommandIcon.color = Color.yellow;
        if (commandStatusNetworked == CommandStatus.FOLLOW) CommandIcon.color = Color.green;
        if (commandStatusNetworked == CommandStatus.HOLD_POSITION) CommandIcon.color = Color.blue;

        CommandIcon.fillAmount = healthNetworked / maxHealth;
    }
    //---------------------------------------------------------------------------------------------
}

