
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using TMPro;
using Unity.VisualScripting;

public class PlayerController : NetworkBehaviour
{
    /*
    //Player Name
    //---------------------------------------------------------------------------------------------------------------------------------------------
    [Networked(OnChanged = nameof(OnNicknameChanged))] public NetworkString<_8> playerName { get; set; }
    private static void OnNicknameChanged(Changed<PlayerController> changed) { changed.Behaviour.SetPlayerNickName(changed.Behaviour.playerName); }
    private void SetPlayerNickName(NetworkString<_8> nickName) { PlayerNameText.text = nickName + " " + Object.InputAuthority.PlayerId; }

    [Rpc(sources: RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcSetNickName(NetworkString<_8> nickName) { playerName = nickName; }
    //---------------------------------------------------------------------------------------------------------------------------------------------

    //Player Health Display
    //---------------------------------------------------------------------------------------------------------------------------------------------
    [SerializeField] private Image HealthBar;
    [SerializeField] private TextMeshProUGUI HealthAmountText;
    //---------------------------------------------------------------------------------------------------------------------------------------------

    [Header("Components")]
    [SerializeField] private GameObject Cam;
    [SerializeField] private TextMeshProUGUI PlayerNameText;
    [SerializeField] private ChampionAnimationController ChampionAnimationController;
    private Rigidbody2D Rigid;

    [Header("Champion Variables")]
    [SerializeField] private float maxHealth = 100;
    [Networked] public float healthNetworked { get; set; }

    [SerializeField] private float maxMana = 500;
    [Networked] private float manaNetworked { get; set; }

    [SerializeField] protected bool dead;

    [SerializeField] private float moveSpeed = 10;
    [SerializeField] private float airMoveSpeed = 5;
    [SerializeField] private float rollMoveSpeed = 15;

    [SerializeField] private float blockPercentage = 0.5f;

    [Header("Champion Jump Variables")]
    [SerializeField] private float jumpForce = 10;
    [SerializeField] private float groundCheckDistance = 1.3f;
    [SerializeField] private LayerMask WhatIsGround;
    [Networked] private float inAirHorizontalMovementNetworked { get; set; }
    [SerializeField] private float inAirHorizontalMovement;

    [SerializeField] private bool inAir { get { return !Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, WhatIsGround); } }

    [Header("Champion Attack Variables")]
    [SerializeField] protected Transform AttackBoxesParent;
    [SerializeField] protected string[] ListNames = { "Air Attack", "Attack1", "Attack2", "Attack3", "Special Attack" };
    [SerializeField] protected BoxCollider2D[] AttackBoxes;
    [SerializeField] protected float[] AttackDamages;

    [Header("Champion Crowd Control Variables")]
    [SerializeField] protected float[] CrowdControlStrength;

    [Networked] public bool tookHitNetworked { get; private set; }

    [Networked] public bool isFacingLeftNetworked { get; set; }
    public bool isFacingLeft;

    [Networked] private NetworkButtons buttonsPrev { get; set; }

    public enum Status
    {
        IDLE, RUN,
        JUMP_UP, JUMP_DOWN, AIR_ATTACK,
        ATTACK1, ATTACK2, ATTACK3, SPECIAL_ATTACK,
        ROLL,
        BEGIN_MEDITATE, MEDITATE,
        BEGIN_DEFEND, DEFEND,
        TAKE_HIT, BEGIN_DEATH, FINISHED_DEATH
    }

    [Networked] public Status statusNetworked { get; set; }
    public Status status;

    private enum PlayerInputButtons
    {
        None,
        Left,
        Right,
        Jump
    }

    private void SetLocalObjects()
    {
        if (Runner.LocalPlayer == Object.InputAuthority)
        {
            Cam.SetActive(true);
            RpcSetNickName(GlobalManagers.Instance.NetworkRunnerController.LocalPlayerName);
        }
        else Cam.SetActive(false);
    }

    public override void Spawned()
    {
        Rigid = GetComponent<Rigidbody2D>();

        healthNetworked = maxHealth;
        tookHitNetworked = false;
        isFacingLeftNetworked = isFacingLeft = false;
        statusNetworked = status = Status.IDLE;
        inAirHorizontalMovementNetworked = inAirHorizontalMovement = 0;

        SetLocalObjects();
    }

    public void TakeDamageNetworked(float damage, bool attackerIsFacingLeft)
    {
        if (dead) return;
        if (healthNetworked <= 0) return;

        if (statusNetworked == Status.DEFEND && isFacingLeftNetworked != attackerIsFacingLeft)
            damage -= damage * blockPercentage;
        else
            tookHitNetworked = true;

        healthNetworked -= damage;
        if (healthNetworked < 0) healthNetworked = 0;
    }

    public void AddVelocity(Vector2 velocity)
    {
        Rigid.velocity += velocity;
    }

    private void EndStatus()
    {
        //End single animation status
        if (status == Status.AIR_ATTACK || status == Status.ATTACK1 || status == Status.ATTACK2 || status == Status.ATTACK3 || status == Status.SPECIAL_ATTACK ||
            status == Status.ROLL || status == Status.TAKE_HIT)
            if (ChampionAnimationController.AnimationFinished())
                status = Status.IDLE;

        //End Air Status
        if (status == Status.JUMP_UP) if (Rigid.velocity.y < 0) status = Status.JUMP_DOWN;
        if (status == Status.JUMP_DOWN) if (!inAir) status = Status.IDLE;
    }

    protected virtual void TakeInput()
    {
        if (dead)
        {
            return;
        }


        //Determine direction
        if (status == Status.IDLE || status == Status.RUN || status == Status.JUMP_UP || status == Status.JUMP_DOWN)
        {
            if (Input.GetKey(KeyCode.A)) { isFacingLeft = true; }
            if (Input.GetKey(KeyCode.D)) { isFacingLeft = false; }
        }

        //If Character is in an interruptable status
        if (status == Status.IDLE || status == Status.RUN ||
            status == Status.BEGIN_MEDITATE || status == Status.MEDITATE ||
            status == Status.BEGIN_DEFEND || status == Status.DEFEND)
        {
            status = Status.IDLE;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                status = Status.RUN;
                if (Input.GetKeyDown(KeyCode.W)) status = Status.ROLL;
            }
            if (Input.GetKeyDown(KeyCode.Space)) status = Status.JUMP_UP;
            if (Input.GetKeyDown(KeyCode.G)) status = Status.ATTACK1;
            if (Input.GetKeyDown(KeyCode.H)) status = Status.ATTACK2;
            if (Input.GetKeyDown(KeyCode.J)) status = Status.ATTACK3;
            if (Input.GetKeyDown(KeyCode.K)) status = Status.SPECIAL_ATTACK;
            if (Input.GetKey(KeyCode.Q))
            {
                //Begin_Meditate, then Begin_Meditate into Meditate, then if already Meditating, continue meditating
                Status lastStatus = (Status)ChampionAnimationController.GetAnimatorStatus();
                if (lastStatus != Status.BEGIN_MEDITATE && lastStatus != Status.MEDITATE) status = Status.BEGIN_MEDITATE;
                else if (lastStatus == Status.BEGIN_MEDITATE)
                {
                    if (ChampionAnimationController.AnimationFinished()) status = Status.MEDITATE;
                    else status = Status.BEGIN_MEDITATE;
                }
                else if (lastStatus == Status.MEDITATE) status = Status.MEDITATE;
            }
            if (Input.GetKey(KeyCode.S))
            {
                //Begin_Defend, then Begin_defend into Defend, then if already Defending, continue Defending
                Status lastStatus = (Status)ChampionAnimationController.GetAnimatorStatus();
                if (lastStatus != Status.BEGIN_DEFEND && lastStatus != Status.DEFEND) status = Status.BEGIN_DEFEND;
                else if (lastStatus == Status.BEGIN_DEFEND)
                {
                    if (ChampionAnimationController.AnimationFinished()) status = Status.DEFEND;
                    else status = Status.BEGIN_DEFEND;
                }
                else if (lastStatus == Status.DEFEND) status = Status.DEFEND;

            }
        }

        //In Air Input
        if (inAir)
        {
            //In Air Movement
            inAirHorizontalMovement = 0;
            if (Input.GetKey(KeyCode.A)) inAirHorizontalMovement += -1;
            if (Input.GetKey(KeyCode.D)) inAirHorizontalMovement += 1;

            if (status != Status.AIR_ATTACK && status != Status.TAKE_HIT)
            {
                //If inAir, change animation to Jump depending on y velocity.
                if (Rigid.velocity.y > 0) status = Status.JUMP_UP;
                else if (Rigid.velocity.y < 0) status = Status.JUMP_DOWN;

                //In Air Attack
                if (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.J))
                    status = Status.AIR_ATTACK;
            }
        }
    }

    private void CheckDeath()
    {
        if (healthNetworked <= 0)
        {
            dead = true;

            Status lastStatus = (Status)ChampionAnimationController.GetAnimatorStatus();
            if (lastStatus != Status.BEGIN_DEATH && lastStatus != Status.FINISHED_DEATH) status = Status.BEGIN_DEATH;
            if (lastStatus == Status.BEGIN_DEATH)
            {
                if (ChampionAnimationController.AnimationFinished()) status = Status.FINISHED_DEATH;
                else status = Status.BEGIN_DEATH;
            }
            else if (lastStatus == Status.FINISHED_DEATH) status = Status.FINISHED_DEATH;
        }
        else
        {
            if (dead)
            {
                dead = false;
                status = Status.IDLE;
            }
        }
    }

    private void DetermineStatus()
    {
        //Stop if LocalPlayer does not have Input Authority
        if (!(Runner.LocalPlayer == Object.InputAuthority)) return;

        EndStatus();
        TakeInput();
        if (tookHitNetworked) status = Status.TAKE_HIT;
        CheckDeath();
    }

    public void BeforeUpdate()
    {
        DetermineStatus();
    }

    public void AnimationTriggerAttack()
    {
        if (Runner.IsServer)
        {
            int index = 0;
            if (statusNetworked == Status.AIR_ATTACK) index = 0;
            else if (statusNetworked == Status.ATTACK1) index = 1;
            else if (statusNetworked == Status.ATTACK2) index = 2;
            else if (statusNetworked == Status.ATTACK3) index = 3;
            else if (statusNetworked == Status.SPECIAL_ATTACK) index = 4;

            BoxCollider2D attackBox = AttackBoxes[index];
            float damage = AttackDamages[index];

            Collider2D[] colliders = Physics2D.OverlapBoxAll(attackBox.bounds.center, attackBox.bounds.size, 0, LayerMask.GetMask("Champion"));
            foreach (Collider2D collider in colliders)
            {
                PlayerController enemy = collider.GetComponent<PlayerController>();
                if (enemy != null && enemy != this && enemy.healthNetworked > 0)
                    enemy.TakeDamageNetworked(damage, isFacingLeftNetworked);
            }
        }
    }

    public virtual void ApplyCrowdControl(PlayerController enemy, float crowdControlStrength) { }
    public virtual void AnimationTriggerCrowdControl()
    {
        if (Runner.IsServer)
        {
            int index = 0;
            if (statusNetworked == Status.AIR_ATTACK) index = 0;
            else if (statusNetworked == Status.ATTACK1) index = 1;
            else if (statusNetworked == Status.ATTACK2) index = 2;
            else if (statusNetworked == Status.ATTACK3) index = 3;
            else if (statusNetworked == Status.SPECIAL_ATTACK) index = 4;

            BoxCollider2D crowdControlBox = AttackBoxes[index];
            float crowdControlStrength = CrowdControlStrength[index];

            Collider2D[] colliders = Physics2D.OverlapBoxAll(crowdControlBox.bounds.center, crowdControlBox.bounds.size, 0, LayerMask.GetMask("Champion"));
            foreach (Collider2D collider in colliders)
            {
                PlayerController enemy = collider.GetComponent<PlayerController>();
                if (enemy != null && enemy != this && enemy.healthNetworked > 0)
                    ApplyCrowdControl(enemy, crowdControlStrength);
            }
        }
    }
    private void UpdateHealthBarVisual()
    {
        HealthBar.fillAmount = healthNetworked / maxHealth;
        HealthAmountText.text = $"{(int)healthNetworked}/{(int)maxHealth}";
    }

    private void UpdateChampionVisual()
    {
        ChampionAnimationController.Flip(isFacingLeftNetworked);

        //Attack Boxes And Crowd Control Boxes Flip
        if (isFacingLeftNetworked) AttackBoxesParent.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        else AttackBoxesParent.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        ChampionAnimationController.ChangeAnimation(statusNetworked);
        if (statusNetworked == Status.TAKE_HIT)
        {
            if (tookHitNetworked) ChampionAnimationController.RestartAnimation(); //Restart take hit animation
            tookHitNetworked = false;
        }

        //Keep NormalizedTime under 1
        if (ChampionAnimationController.AnimationFinished())
            ChampionAnimationController.RestartAnimation();
    }

    private void UpdatePosition(PlayerData playerData)
    {
        if (playerData.status == Status.RUN)
        {
            float xChange = moveSpeed * Time.fixedDeltaTime;
            if (playerData.isFacingLeft) xChange *= -1;
            Rigid.position = new Vector2(Rigid.position.x + xChange, Rigid.position.y);
        }
        if (playerData.status == Status.JUMP_UP)
        {
            if (!inAir)
                Rigid.velocity = new Vector2(Rigid.velocity.x, Rigid.velocity.y + jumpForce);
        }
        if (playerData.status == Status.ROLL)
        {
            float xChange = rollMoveSpeed * Time.fixedDeltaTime;
            if (playerData.isFacingLeft) xChange *= -1;
            Rigid.position = new Vector2(Rigid.position.x + xChange, Rigid.position.y);
        }

        //Moving Left or Right In Air
        if (inAir &&
            playerData.status != Status.TAKE_HIT && playerData.status != Status.BEGIN_DEATH && playerData.status != Status.FINISHED_DEATH)
        {
            float xChange = playerData.inAirHorizontalMovement * airMoveSpeed * Time.fixedDeltaTime;
            Rigid.position = new Vector2(Rigid.position.x + xChange, Rigid.position.y);
        }
    }

    private void UpdatePosition2()
    {
        if (statusNetworked == Status.RUN)
        {
            float xChange = moveSpeed * Time.fixedDeltaTime;
            if (isFacingLeftNetworked) xChange *= -1;
            Rigid.position = new Vector2(Rigid.position.x + xChange, Rigid.position.y);
        }
        if (statusNetworked == Status.JUMP_UP)
        {
            if (!inAir)
                Rigid.velocity = new Vector2(Rigid.velocity.x, Rigid.velocity.y + jumpForce);
        }
        if (statusNetworked == Status.ROLL)
        {
            float xChange = rollMoveSpeed * Time.fixedDeltaTime;
            if (isFacingLeftNetworked) xChange *= -1;
            Rigid.position = new Vector2(Rigid.position.x + xChange, Rigid.position.y);
        }

        //Moving Left or Right In Air
        if (inAir &&
            statusNetworked != Status.TAKE_HIT && statusNetworked != Status.BEGIN_DEATH && statusNetworked != Status.FINISHED_DEATH)
        {
            float xChange = inAirHorizontalMovementNetworked * airMoveSpeed * Time.fixedDeltaTime;
            Rigid.position = new Vector2(Rigid.position.x + xChange, Rigid.position.y);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner.TryGetInputForPlayer<PlayerData>(Object.InputAuthority, out var playerData))
        {
            statusNetworked = playerData.status;
            isFacingLeftNetworked = playerData.isFacingLeft;
            inAirHorizontalMovementNetworked = playerData.inAirHorizontalMovement;
            //UpdatePosition(playerData);
        }

        UpdateHealthBarVisual();
        UpdateChampionVisual();
        UpdatePosition2();
        buttonsPrev = playerData.NetworkButtons;
    }

    public PlayerData GetPlayerNetworkInput()
    {
        PlayerData data = new PlayerData();
        data.status = status;
        data.isFacingLeft = isFacingLeft;
        data.inAirHorizontalMovement = inAirHorizontalMovement;
        return data;
    }

    private void OnDrawGizmos()
    {
        if (inAir) Gizmos.color = Color.red;
        else Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
    }

    */
}