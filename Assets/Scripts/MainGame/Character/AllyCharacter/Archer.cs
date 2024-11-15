using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

public class Archer : AllyCharacter
{
    public enum BowStatus { IDLE, FIRE };

    [Header("Archer Variables")]
    [SerializeField] private NetworkPrefabRef arrowPrefab = NetworkPrefabRef.Empty;
    [SerializeField] private float fireMoveSpeed;

    [SerializeField] private SpriteRenderer BowRenderer;
    [SerializeField] private Transform BowTransform;
    [SerializeField] private AnimationController BowAnimationController;

    [Networked] public BowStatus bowStatus { get; set; }

    public override void Spawned()
    {
        base.Spawned();

        bowStatus = BowStatus.IDLE;
    }

    public override void EndAnimation()
    {
        if (bowStatus == BowStatus.FIRE)
            if (BowAnimationController.AnimationFinished())
                bowStatus = BowStatus.IDLE;
    }

    private void DetermineBowStatusNetworked(PlayerData input, NetworkButtons buttonsPrev)
    {
        EndAnimation();

        var pressed = input.NetworkButtons.GetPressed(buttonsPrev);
        if (pressed.WasPressed(buttonsPrev, NetworkedPlayer.PlayerInputButtons.LeftMouseButton))
        {
            bowStatus = BowStatus.FIRE;
        }
    }

    protected override void DetermineStatusNetworked(PlayerData input, NetworkButtons buttonsPrev)
    {
        statusNetworked = CharacterStatus.IDLE;
        if (input.movementDirection.magnitude > 0) statusNetworked = CharacterStatus.RUN;

        DetermineBowStatusNetworked(input, buttonsPrev);
    }

    //---------------------------------------------------------------------------------------------
    //Character Take Command Input
    public override void InfluencedByPatrolCommand()
    {
        base.InfluencedByPatrolCommand();

        if (enemyTarget != null)
        {
            float targetDistance = Vector3.Distance(transform.position, enemyTarget.transform.position);
            if (targetDistance > AttackRange)
                statusNetworked = CharacterStatus.RUN;
            else if (targetDistance <= AttackRange && AttackCoolDownTimer.ExpiredOrNotRunning(Runner))
            {
                bowStatus = BowStatus.FIRE;
                AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
            }
            else statusNetworked = CharacterStatus.IDLE;

            if (targetDistance <= RetreatRange)
            {
                movementDirectionNetworked = -1 * (enemyTarget.transform.position - transform.position).normalized;
                statusNetworked = CharacterStatus.RUN;
            }
        }
        else
            Patrol();
    }

    public override void InfluencedByFollowCommand()
    {
        base.InfluencedByFollowCommand();

        //Handle Nearby Enemies
        if (enemyTarget != null)
        {
            movementDirectionNetworked = (enemyTarget.transform.position - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

            float targetDistance = Vector3.Distance(transform.position, enemyTarget.transform.position);
            if (targetDistance <= AttackRange && AttackCoolDownTimer.ExpiredOrNotRunning(Runner))
            {
                bowStatus = BowStatus.FIRE;
                AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
            }
            else if (targetDistance <= RetreatRange)
            {
                movementDirectionNetworked *= -1;
                statusNetworked = CharacterStatus.RUN;
                return;
            }
        }

        //Follow Target
        if (followTarget != null)
        {
            movementDirectionNetworked = (followTarget.transform.position - transform.position).normalized;
            if (enemyTarget == null) facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
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
        base.InfluencedByHoldPositionCommand();

        //Handle Nearby Enemies
        if (enemyTarget != null)
        {
            movementDirectionNetworked = (enemyTarget.transform.position - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

            float targetDistance = Vector3.Distance(transform.position, enemyTarget.transform.position);
            if (targetDistance <= AttackRange && AttackCoolDownTimer.ExpiredOrNotRunning(Runner))
            {
                bowStatus = BowStatus.FIRE;
                AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
            }
            else if (targetDistance <= RetreatRange)
            {
                movementDirectionNetworked *= -1;
                statusNetworked = CharacterStatus.RUN;
                return;
            }
        }

        //Move To Hold Position
        movementDirectionNetworked = (holdPosition - transform.position).normalized;
        statusNetworked = CharacterStatus.RUN;

        float holdPositionDistance = Vector3.Distance(transform.position, holdPosition);
        if (holdPositionDistance < StopMoveHoldDistance) statusNetworked = CharacterStatus.IDLE;

    }
    //---------------------------------------------------------------------------------------------

    public override void UpdatePosition()
    {
        AvoidObstaclePathing();
        if (statusNetworked == CharacterStatus.RUN)
        {
            if (bowStatus == BowStatus.FIRE) Rigid.position += movementDirectionNetworked * fireMoveSpeed * Runner.DeltaTime;
            else Rigid.position += movementDirectionNetworked * speed * Runner.DeltaTime;
        }    
    }

    private void HandleBowVisuals()
    {
        //Flip And Rotation
        if (bowStatus == BowStatus.FIRE)
        {
            BowRenderer.flipX = false;
            if (IsFacingRight()) BowRenderer.flipY = false;
            if (IsFacingLeft()) BowRenderer.flipY = true;
            BowTransform.rotation = Quaternion.Euler(0f, 0f, facingDirectionDegreeNetworked);
        }
        else
        {
            BowRenderer.flipY = false;
            if (IsFacingRight()) BowRenderer.flipX = false;
            if (IsFacingLeft()) BowRenderer.flipX = true;
            BowTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        //Update Animation
        BowAnimationController.ChangeAnimation((int)bowStatus);
    }

    public override void Render()
    {
        base.Render();
        HandleBowVisuals();
    }

    public override void AnimationTriggerAttack()
    {
        if (Runner.IsServer)
        {
            var arrow = Runner.Spawn(arrowPrefab, transform.position, BowTransform.rotation, Object.InputAuthority);
            arrow.GetComponent<Arrow>().SetUp(damage);
        }
    }
}