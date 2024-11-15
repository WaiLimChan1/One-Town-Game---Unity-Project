using Fusion;
using UnityEngine;

public class TNT : EnemyCharacter
{
    [Header("TNT Variables")]
    [SerializeField] private NetworkPrefabRef DynamitePrefab = NetworkPrefabRef.Empty;
    [SerializeField] private float throwForceMagnitude;

    protected override void EndAnimation()
    {
        if (statusNetworked == CharacterStatus.ATTACK_SIDE_1)
            if (AnimationController.AnimationFinished())
                statusNetworked = CharacterStatus.IDLE;
    }

    protected override void DetermineStatus()
    {
        EndAnimation();
        if (statusNetworked == CharacterStatus.ATTACK_SIDE_1) return;

        if (target != null)
        {
            //float targetDistance = Vector3.Distance(transform.position, target.transform.position);
            //Calculate target distance differently for AllyCharacter and building
            float targetDistance = 0;

            //Calculate target distance for AllyCharacter
            AllyCharacter ally = target.GetComponent<AllyCharacter>();
            if (ally != null)
                targetDistance = Vector3.Distance(transform.position, target.transform.position);

            //Calculate target distance for building
            Building building = target.GetComponent<Building>();
            if (building != null)
            {
                Vector2 origin = transform.position;
                Vector2 direction = movementDirectionNetworked;
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, SearchRadius, LayerMask.GetMask("Building"));
                if (hit.collider != null && hit.collider.gameObject == target) targetDistance = hit.distance;
                else targetDistance = SearchRadius;
            }

            if (targetDistance > AttackRange)
                statusNetworked = CharacterStatus.RUN;
            else if (targetDistance <= AttackRange && AttackCoolDownTimer.ExpiredOrNotRunning(Runner))
            {
                statusNetworked = CharacterStatus.ATTACK_SIDE_1;
                AttackCoolDownTimer = TickTimer.CreateFromSeconds(Runner, AttackCoolDown);
            }
            else if (targetDistance <= RetreatRange)
            {
                movementDirectionNetworked = -1 * (target.transform.position - transform.position).normalized;
                facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
                statusNetworked = CharacterStatus.RUN;
            }
            else statusNetworked = CharacterStatus.IDLE;
        }
        else //Move towards TownHall
        {
            movementDirectionNetworked = (EnemyCharacter.TownHallPosition - transform.position).normalized;
            facingDirectionDegreeNetworked = Mathf.Atan2(movementDirectionNetworked.y, movementDirectionNetworked.x) * Mathf.Rad2Deg;
            statusNetworked = CharacterStatus.RUN;
        }
    }

    public override void Render()
    {
        //Flip Character
        if (IsFacingRight()) spriteRenderer.flipX = false;
        if (IsFacingLeft()) spriteRenderer.flipX = true;

        AnimationController.ChangeAnimation((int)statusNetworked);
    }

    public override void AnimationTriggerAttack()
    {
        if (Runner.IsServer)
        {
            var rotation = Quaternion.Euler(0f, 0f, facingDirectionDegreeNetworked);
            var dynamite = Runner.Spawn(DynamitePrefab, transform.position, rotation, Object.InputAuthority);
            dynamite.GetComponent<Dynamite>().SetUp(damage, facingDirectionDegreeNetworked);
        }
    }
}
