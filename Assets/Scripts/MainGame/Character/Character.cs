using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public abstract class Character : NetworkBehaviour
{
    public enum CharacterStatus { 
        IDLE, RUN, 
        ATTACK_SIDE_1, ATTACK_SIDE_2, ATTACK_DOWN_1, ATTACK_DOWN_2, ATTACK_UP_1, ATTACK_UP_2, 
        BUILD, CHOP, HOLD_IDLE, HOLD_RUN
    };
    protected static float pathingRaycastAmount = 30;
    protected float pathingRaycastRange = 0.5f;
    protected float pathingRaycastOffSet = 0.4f;

    [Header("Character Components")]
    protected Rigidbody2D Rigid;
    protected SpriteRenderer spriteRenderer;
    protected AnimationController AnimationController;

    [Header("Character Variables")]
    [SerializeField] protected float maxHealth = 100;
    [SerializeField] protected float speed = 5;
    [SerializeField] protected float damage;
    [SerializeField] protected float SearchRadius = 7;
    [SerializeField] protected float AttackRange;
    [SerializeField] protected float RetreatRange;

    [Networked] public float healthNetworked { get; set; }

    [Networked] protected TickTimer AttackCoolDownTimer { get; set; }
    [SerializeField] protected float AttackCoolDown;

    [Networked] public float facingDirectionDegreeNetworked { get; set; }
    protected bool IsFacingRight() { return facingDirectionDegreeNetworked < 90 && facingDirectionDegreeNetworked > -90; }
    protected bool IsFacingLeft() { return facingDirectionDegreeNetworked > 90 || facingDirectionDegreeNetworked < -90; }

    [Networked] public Vector2 movementDirectionNetworked { get; set; }

    [Networked] public CharacterStatus statusNetworked { get; set; }

    public override void Spawned()
    {
        Rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        AnimationController = GetComponentInChildren<AnimationController>();

        healthNetworked = maxHealth;
        facingDirectionDegreeNetworked = 0;
        movementDirectionNetworked = Vector2.zero;
        statusNetworked = CharacterStatus.IDLE;
    }

    protected void DetermineFacingDirectionDegreeNetwored(PlayerData input)
    {
        Vector2 vectorToMouse = input.mouseWorldPosition - new Vector2(transform.position.x, transform.position.y);
        facingDirectionDegreeNetworked = Mathf.Atan2(vectorToMouse.y, vectorToMouse.x) * Mathf.Rad2Deg;
    }

    public virtual void UpdatePosition()
    {
        AvoidObstaclePathing();

        if (statusNetworked == CharacterStatus.RUN)
            Rigid.position += movementDirectionNetworked * speed * Runner.DeltaTime;
    }

    public override void FixedUpdateNetwork()
    {

    }

    public override void Render()
    {
        //Flip Character
        if (statusNetworked == CharacterStatus.IDLE || statusNetworked == CharacterStatus.RUN)
        {
            if (IsFacingRight()) spriteRenderer.flipX = false;
            if (IsFacingLeft()) spriteRenderer.flipX = true;
        }

        AnimationController.ChangeAnimation((int)statusNetworked);
    }

    public virtual void TakeDamageNetworked(float damage)
    {
        AnimationController.SetTrigger("Hurt");
        healthNetworked -= damage;
        if (healthNetworked <= 0) Runner.Despawn(Object);
    }

    public virtual void AnimationTriggerAttack()
    {

    }

    //---------------------------------------------------------------------------------------------
    //Avoid Obstacle Pathing
    public abstract bool ObstacleIsInTheWay(float currentDegree, float range, float startingPosOffSet);

    public void AvoidObstaclePathing()
    {
        float movementDirectionAngle = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;

        if (!ObstacleIsInTheWay(movementDirectionAngle, pathingRaycastRange, pathingRaycastOffSet))
            return;

        float changeAmount = (float)(360.0 / pathingRaycastAmount);
        float currentUpDegree = movementDirectionAngle + changeAmount;
        float currentDownDegree = movementDirectionAngle - changeAmount;
        for (int i = 0; i < pathingRaycastAmount; i++)
        {
            if (!ObstacleIsInTheWay(currentUpDegree, pathingRaycastRange, pathingRaycastOffSet))
            {
                float angleRadians = currentUpDegree * Mathf.Deg2Rad;
                movementDirectionNetworked = (new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0)).normalized;
                return;

            }
            if (!ObstacleIsInTheWay(currentDownDegree, pathingRaycastRange, pathingRaycastOffSet))
            {
                float angleRadians = currentDownDegree * Mathf.Deg2Rad;
                movementDirectionNetworked = (new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0)).normalized;
                return;
            }

            currentUpDegree += changeAmount;
            currentDownDegree -= changeAmount;
        }
    }

    //Pathing Gizmos
    //private void OnDrawGizmos()
    //{
    //    float changeAmount = (float)(360.0 / pathingRaycastAmount);
    //    float currentDegree = 0;
    //    for (int i = 0; i < pathingRaycastAmount; i++)
    //    {
    //        if (ObstacleIsInTheWay(currentDegree, pathingRaycastRange, pathingRaycastOffSet)) Gizmos.color = Color.red;
    //        else Gizmos.color = Color.green;

    //        float angleRadians = currentDegree * Mathf.Deg2Rad;
    //        Vector3 direction = (new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0)).normalized;
    //        Vector3 changeVector = direction * pathingRaycastRange;
    //        Vector3 startingPosition = transform.position + direction * pathingRaycastOffSet;
    //        Gizmos.DrawLine(startingPosition, startingPosition + changeVector);

    //        currentDegree += changeAmount;
    //    }
    //}
    //---------------------------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        float angleRadians = facingDirectionDegreeNetworked * Mathf.Deg2Rad;
        Vector2 vector = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(vector.x, vector.y, 0) * 10);

        Gizmos.color = Color.grey;
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(movementDirectionNetworked.x, movementDirectionNetworked.y, 0) * 10);

        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, SearchRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, RetreatRange);
    }
}
