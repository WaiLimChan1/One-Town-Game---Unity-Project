using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CharacterSpawner;

public class NetworkedPlayer : NetworkBehaviour, IBeforeUpdate
{
    public override void Spawned()
    {
        ControlTargetIndex = 0;
        GoldAmount = 30;
        GoldAmount += CharacterSpawner.WarriorCost;
        passiveIncomeTimer = TickTimer.CreateFromSeconds(Runner, passiveIncomeTime);
        FindTownHall();

        if (Runner.LocalPlayer == Object.InputAuthority)
        {
            LocalCamera = Camera.main.gameObject.GetComponent<LocalCamera>();
            CharacterSpawner.SpawnCharacter(Runner.LocalPlayer, CharacterSpawner.CharacterType.WARRIOR);
            PlayerInformation.instance.NetworkedPlayer = this;

            GameOver.instance.NetworkedPlayer = this;
            GameOver.instance.gameObject.SetActive(false);
        }
    }

    public void GameIsOver()
    {
        if (Runner.LocalPlayer == Object.InputAuthority)
            if (TownHall.healthNetworked <= 0)
            {
                GameOver.instance.TurnOn();
                Time.timeScale = 0;
            }
    }

    public float GetSurvivalTime()
    {
        if (Runner.TryGetPlayerObject(3, out var playerNetworkObject))
        {
            NetworkedPlayer NetworkedPlayer = playerNetworkObject.GetComponent<NetworkedPlayer>();
            if (NetworkedPlayer != null)
                return NetworkedPlayer.GetComponent<EnemySpawner>().gameTimer;
        }
        return 0;
    }


    //----------------------------------------------------------------------------------------------------------------------------
    //Resources
    [Networked] public int GoldAmount { get; set; }
    public void GainGold()
    {
        GoldAmount++;
    }

    public float passiveIncomeTime = 10;
    public int passiveIncomeAmount = CharacterSpawner.VillagerCost;
    TickTimer passiveIncomeTimer;
    public void PassiveIncomeLogic()
    {
        if (TownHall == null) return;
        if (TownHall.healthNetworked <= 0) return;
        if (passiveIncomeTimer.ExpiredOrNotRunning(Runner))
        {
            passiveIncomeTimer = TickTimer.CreateFromSeconds(Runner, passiveIncomeTime);
            GoldAmount += passiveIncomeAmount;
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------


    //----------------------------------------------------------------------------------------------------------------------------
    //TownHall
    [SerializeField] public TownHall TownHall;
    public void FindTownHall()
    {
        //if (Runner.IsServer || Runner.LocalPlayer == Object.InputAuthority)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(new Vector3(0, 0, 0), 10, LayerMask.GetMask("Building"));
            foreach (Collider2D collider in colliders)
            {
                TownHall FoundTownHall = collider.GetComponent<TownHall>();
                if (FoundTownHall != null && FoundTownHall.healthNetworked > 0)
                {
                    TownHall = FoundTownHall;
                    return;
                }
            }
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------



    //----------------------------------------------------------------------------------------------------------------------------
    //Associated Characters
    [SerializeField] private CharacterSpawner CharacterSpawner;
    [SerializeField] public List<NetworkObject> AssociatedCharactersHostRecord;

    public void AddAssociatedCharacter(NetworkObject AssociatedCharacter) 
    { 
        AssociatedCharactersHostRecord.Add(AssociatedCharacter);
        UpdateAssociatedCharactersNetworked();
    }

    public void DespawnAllAssociatedCharacter()
    {
        if (Runner.IsServer)
            foreach (var character in AssociatedCharactersHostRecord)
                Runner.Despawn(character);
    }

    public const int MAX_TROOP_COUNT = 75;
    [Networked, Capacity(MAX_TROOP_COUNT)] public NetworkArray<NetworkObject> AssociatedCharactersNetworked => default;
    public void UpdateAssociatedCharactersNetworked()
    {
        if (!Runner.IsServer) return;
        List<NetworkObject> myList = Enumerable.Repeat<NetworkObject>(null, MAX_TROOP_COUNT).ToList();
        AssociatedCharactersNetworked.CopyFrom(myList, 0, myList.Count);

        AssociatedCharactersNetworked.CopyFrom(AssociatedCharactersHostRecord, 0, AssociatedCharactersHostRecord.Count);
    }

    //----------------------------------------------------------------------------------------------------------------------------



    //----------------------------------------------------------------------------------------------------------------------------
    //Local Camera
    [SerializeField] public LocalCamera LocalCamera;

    public void HandleLocalCameraLogic()
    {
        if (Runner.LocalPlayer != Object.InputAuthority) return;

        if (LocalCamera.locked && ControlTargetCharacterNetworked != null)
        {
            Transform focusTarget = ControlTargetCharacterNetworked.GetComponent<NetworkRigidbody2D>().InterpolationTarget.transform;
            LocalCamera.SetPosition(focusTarget.position);
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------

    private Vector2 mouseWorldPosition;
    private Vector2 movementDirection;

    public enum PlayerInputButtons
    {
        None,
        LeftMouseButton,
    }
    [Networked] public NetworkButtons buttonsPrev { get; set; }



    //----------------------------------------------------------------------------------------------------------------------------
    //Control Target Index. The Character player is controller
    [Networked] public int ControlTargetIndex { get; set; }
    [Networked] public NetworkObject ControlTargetCharacterNetworked { get; set; }

    public void Local_IncrementTargetIndex(bool reset)
    {
        ControlTargetIndex++;
        if (ControlTargetIndex >= AssociatedCharactersHostRecord.Count) ControlTargetIndex = 0;
        if (AssociatedCharactersHostRecord.Count == 0 || reset)
        {
            ControlTargetIndex = -1;
            ControlTargetCharacterNetworked = null;
        }
    }

    [Rpc(sources: RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_IncrementTargetIndex(bool reset) { Local_IncrementTargetIndex(reset); }

    public void IncrementTargetIndex(bool reset)
    {
        if (Runner.IsServer) Local_IncrementTargetIndex(reset);
        else Rpc_IncrementTargetIndex(reset);
    }
    //----------------------------------------------------------------------------------------------------------------------------



    //----------------------------------------------------------------------------------------------------------------------------
    //Command Target Index. The Characters player has selected for command.
    public List<int> CommandSelectedIndicesHostRecord = new List<int>();
    [Networked, Capacity(MAX_TROOP_COUNT)] public NetworkArray<int> CommandSelectedIndicesNetworked => default;
    public void UpdateCommandSelectedIndicesNetworked()
    {
        if (!Runner.IsServer) return;
        List<int> emptyList = Enumerable.Repeat(-1, MAX_TROOP_COUNT).ToList();
        CommandSelectedIndicesNetworked.CopyFrom(emptyList, 0, emptyList.Count);

        CommandSelectedIndicesNetworked.CopyFrom(CommandSelectedIndicesHostRecord, 0, CommandSelectedIndicesHostRecord.Count);
    }

    [SerializeField] public Image SelectionRectangle;
    public float minSize = 0.5f;
    public bool CommandSelecting = false;
    public Vector2 CommandSelectedStartPoint = new Vector2(0,0);

    public void Local_SelectCharactersForCommand(Vector2 CommandSelectedStartPoint, Vector2 CommandSelectedEndPoint)
    {
        List<NetworkObject> foundNetworkObjects = new List<NetworkObject>();

        //Calculate Selection Rectangle
        Vector3 center = (CommandSelectedStartPoint + CommandSelectedEndPoint) / 2;
        Vector3 size = CommandSelectedStartPoint - CommandSelectedEndPoint;
        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 1);

        //Find Ally Characters associated with this NetworkedPlayer inside of the rectangle.
        Collider2D[] colliders = Physics2D.OverlapBoxAll(center, size, 0, LayerMask.GetMask("AllyCharacter"));
        foreach (Collider2D collider in colliders)
        {
            AllyCharacter ally = collider.GetComponent<AllyCharacter>();
            if (ally != null && ally.NetworkedPlayer == this) 
                foundNetworkObjects.Add(collider.gameObject.GetComponent<NetworkObject>());
        }

        //Selecting Rectangle is less than min size, select for control target index instead
        if (size.x <= minSize || size.y <= minSize)
        {
            for (int i = 0; i < foundNetworkObjects.Count; i++)
            {
                for (int j = 0; j < AssociatedCharactersHostRecord.Count; j++)
                {
                    if (foundNetworkObjects[i] == AssociatedCharactersHostRecord[j])
                    {
                        ControlTargetIndex = j;
                        ControlTargetCharacterNetworked = AssociatedCharactersHostRecord[j];
                    }
                }
            }
            return;
        }

        //Find the index of the foundNetworkObjects
        CommandSelectedIndicesHostRecord.Clear(); //First old selected indexes for command
        for (int i = 0; i < foundNetworkObjects.Count; i++)
        {
            for (int j = 0; j < AssociatedCharactersHostRecord.Count; j++)
            {
                if (foundNetworkObjects[i] == AssociatedCharactersHostRecord[j])
                    CommandSelectedIndicesHostRecord.Add(j);
            }
        }
        UpdateCommandSelectedIndicesNetworked();
    }

    [Rpc(sources: RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_SelectCharactersForCommand(Vector2 CommandSelectedStartPoint, Vector2 CommandSelectedEndPoint) 
    { Local_SelectCharactersForCommand(CommandSelectedStartPoint, CommandSelectedEndPoint); }

    public void SelectCharactersForCommand(Vector2 CommandSelectedStartPoint, Vector2 CommandSelectedEndPoint)
    {
        if (Runner.IsServer) Local_SelectCharactersForCommand(CommandSelectedStartPoint, CommandSelectedEndPoint);
        else Rpc_SelectCharactersForCommand(CommandSelectedStartPoint, CommandSelectedEndPoint);
    }

    public void TakeInputForCommandSelectedIndexes()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            CommandSelectedStartPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CommandSelecting = true;
        }
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            Vector2 CommandSelectedEndPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            SelectCharactersForCommand(CommandSelectedStartPoint, CommandSelectedEndPoint);

            //Reset
            CommandSelecting = false;
            CommandSelectedStartPoint = new Vector2(0, 0);
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------




    //----------------------------------------------------------------------------------------------------------------------------
    //Command Target Index. Selecting all of one troop type or all troops

    public void Local_SelectAllTroopsForCommand(CharacterSpawner.CharacterType characterType)
    {
        CommandSelectedIndicesHostRecord.Clear(); //First old selected indexes for command
        for (int i = 0; i < AssociatedCharactersHostRecord.Count; i++)
        {
            if (characterType == CharacterType.WARRIOR) 
                if (AssociatedCharactersHostRecord[i].gameObject.GetComponent<Warrior>() != null)
                    CommandSelectedIndicesHostRecord.Add(i);

            if (characterType == CharacterType.ARCHER)
                if (AssociatedCharactersHostRecord[i].gameObject.GetComponent<Archer>() != null)
                    CommandSelectedIndicesHostRecord.Add(i);

            if (characterType == CharacterType.VILLAGER)
                if (AssociatedCharactersHostRecord[i].gameObject.GetComponent<Villager>() != null)
                    CommandSelectedIndicesHostRecord.Add(i);

            if (characterType == CharacterType.TORCH || characterType == CharacterType.TNT)
                CommandSelectedIndicesHostRecord.Add(i);
        }
        UpdateCommandSelectedIndicesNetworked();
    }

    [Rpc(sources: RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_SelectAllTroopsForCommand(CharacterSpawner.CharacterType characterType)
    { Local_SelectAllTroopsForCommand(characterType); }

    public void SelectAllTroopsForCommand(CharacterSpawner.CharacterType characterType)
    {
        if (Runner.IsServer) Local_SelectAllTroopsForCommand(characterType);
        else Rpc_SelectAllTroopsForCommand(characterType);
    }

    public void TakeInputForAllCommandSelectedIndexes()
    {
        if (!Input.GetKey(KeyCode.LeftShift)) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectAllTroopsForCommand(CharacterSpawner.CharacterType.WARRIOR);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectAllTroopsForCommand(CharacterSpawner.CharacterType.ARCHER);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectAllTroopsForCommand(CharacterSpawner.CharacterType.VILLAGER);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectAllTroopsForCommand(CharacterSpawner.CharacterType.TORCH); //Select All
    }
    //----------------------------------------------------------------------------------------------------------------------------






    //----------------------------------------------------------------------------------------------------------------------------
    //Change Command Status. 
    public void Local_TakeInputForCommandChange(AllyCharacter.CommandStatus CommandStatus)
    {
        try
        {
            RemoveNullCharacters();
            for (int i = 0; i < CommandSelectedIndicesHostRecord.Count; i++)
            {
                int index = CommandSelectedIndicesHostRecord[i];
                if (index < AssociatedCharactersHostRecord.Count)
                    if (AssociatedCharactersHostRecord[index] != null)
                    {
                        AssociatedCharactersHostRecord[index].GetComponent<AllyCharacter>().commandStatusNetworked = CommandStatus;
                        if (CommandStatus == AllyCharacter.CommandStatus.HOLD_POSITION)
                            AssociatedCharactersHostRecord[index].GetComponent<AllyCharacter>().RecordHoldPosition();
                    }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An exception occurred: {ex.Message}");
        }
    }

    [Rpc(sources: RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_TakeInputForCommandChange(AllyCharacter.CommandStatus CommandStatus)
    {
        try
        {
            Local_TakeInputForCommandChange(CommandStatus);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An exception occurred: {ex.Message}");
        }
    }

    public void TakeInputForCommandChange(AllyCharacter.CommandStatus CommandStatus)
    {
        if (Runner.IsServer) Local_TakeInputForCommandChange(CommandStatus);
        else Rpc_TakeInputForCommandChange(CommandStatus);
    }

    public void TakeInputForCommandChange()
    {
        if (Input.GetKey(KeyCode.LeftShift)) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) TakeInputForCommandChange(AllyCharacter.CommandStatus.NONE);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TakeInputForCommandChange(AllyCharacter.CommandStatus.PATROL);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TakeInputForCommandChange(AllyCharacter.CommandStatus.FOLLOW);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TakeInputForCommandChange(AllyCharacter.CommandStatus.HOLD_POSITION);
    }
    //----------------------------------------------------------------------------------------------------------------------------




    //----------------------------------------------------------------------------------------------------------------------------
    //Player Input
    public void SpawnCharacter(CharacterSpawner.CharacterType characterType)
    {
        CharacterSpawner.SpawnCharacter(Runner.LocalPlayer, characterType);
    }

    public void TakeInputSpawnTroops()
    {
        if (Input.GetKeyDown(KeyCode.T)) CharacterSpawner.SpawnCharacter(Runner.LocalPlayer, CharacterSpawner.CharacterType.WARRIOR);
        if (Input.GetKeyDown(KeyCode.Y)) CharacterSpawner.SpawnCharacter(Runner.LocalPlayer, CharacterSpawner.CharacterType.ARCHER);
        if (Input.GetKeyDown(KeyCode.U)) CharacterSpawner.SpawnCharacter(Runner.LocalPlayer, CharacterSpawner.CharacterType.VILLAGER);

        if (Input.GetKeyDown(KeyCode.I)) CharacterSpawner.SpawnCharacter(Runner.LocalPlayer, CharacterSpawner.CharacterType.TNT);
        if (Input.GetKeyDown(KeyCode.O)) CharacterSpawner.SpawnCharacter(Runner.LocalPlayer, CharacterSpawner.CharacterType.TORCH);
        if (Input.GetKeyDown(KeyCode.P))
        {
            for (int i = 0; i < 30; i++) CharacterSpawner.SpawnCharacter(Runner.LocalPlayer, CharacterSpawner.CharacterType.TORCH);
        }

    }

    public void TakeInput()
    {
        if (!(Runner.LocalPlayer == Object.InputAuthority)) return;

        //TakeInputSpawnTroops();

        //Calculating Mouse World Position
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //Calculating Movement Direction
        movementDirection = new Vector2(0, 0);
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKey(KeyCode.W)) movementDirection += new Vector2(0, 1);
            if (Input.GetKey(KeyCode.S)) movementDirection += new Vector2(0, -1);
            if (Input.GetKey(KeyCode.D)) movementDirection += new Vector2(1, 0);
            if (Input.GetKey(KeyCode.A)) movementDirection += new Vector2(-1, 0);
            movementDirection = movementDirection.normalized;
        }

        //Control Troop Selection
        if (Input.GetKeyDown(KeyCode.Q)) IncrementTargetIndex(false);
        if (Input.GetKeyDown(KeyCode.E)) IncrementTargetIndex(true);

        //Command Rectangle Troop Selection
        TakeInputForCommandSelectedIndexes();

        //Command Selection For Entire Troop Type Or All Troop
        TakeInputForAllCommandSelectedIndexes();

        //Change Command
        TakeInputForCommandChange();

        //Camera input
        LocalCamera.TakeInput();
    }

    void IBeforeUpdate.BeforeUpdate()
    {
        TakeInput();
    }

    public PlayerData GetNetworkInput()
    {
        PlayerData data = new PlayerData();
        data.mouseWorldPosition = mouseWorldPosition;
        data.movementDirection = movementDirection;
        data.NetworkButtons.Set(PlayerInputButtons.LeftMouseButton, Input.GetKey(KeyCode.Mouse0));
        return data;
    }
    //----------------------------------------------------------------------------------------------------------------------------




    //----------------------------------------------------------------------------------------------------------------------------
    //Network Update
    private void DetermineTargetCharacter()
    {
        if (Runner.IsServer)
        {
            if (ControlTargetIndex >= 0 && ControlTargetIndex < AssociatedCharactersHostRecord.Count)
            {
                NetworkObject targetCharacter = AssociatedCharactersHostRecord[ControlTargetIndex];
                if (targetCharacter != null)
                {
                    ControlTargetCharacterNetworked = targetCharacter;
                }
                else
                {
                    //Keey track of Associated Characters
                    AssociatedCharactersHostRecord.RemoveAt(ControlTargetIndex);
                    UpdateAssociatedCharactersNetworked();

                    //Keey track of control target index
                    ControlTargetIndex = -1;
                    ControlTargetCharacterNetworked = null;

                    //Keey track of selected command troop
                    for (int i = 0; i < CommandSelectedIndicesHostRecord.Count; i++)
                    {
                        if (CommandSelectedIndicesHostRecord[i] > ControlTargetIndex) CommandSelectedIndicesHostRecord[i]--;
                        else if (CommandSelectedIndicesHostRecord[i] == ControlTargetIndex)
                        {
                            CommandSelectedIndicesHostRecord.RemoveAt(CommandSelectedIndicesHostRecord.IndexOf(ControlTargetIndex));
                            i--;
                        }
                    }
                    UpdateCommandSelectedIndicesNetworked();
                }
            }
            else
            {
                ControlTargetIndex = -1;
                ControlTargetCharacterNetworked = null;
            }
        }
    }

    private void ProcessPlayerInputForTargetCharacter()
    {
        if (Runner.TryGetInputForPlayer<PlayerData>(Object.InputAuthority, out PlayerData input))
        {
            if (ControlTargetCharacterNetworked != null) ControlTargetCharacterNetworked.GetComponent<AllyCharacter>().InfluencedByInput(input, buttonsPrev);
        }
    }

    private void RemoveNullCharacters()
    {
        //Remove null characters, and update target index properly
        for (int i = 0; i < AssociatedCharactersHostRecord.Count; i++)
        {
            NetworkObject currentCharacter = AssociatedCharactersHostRecord[i];
            if (currentCharacter == null)
            {
                //Keey track of Associated Characters
                AssociatedCharactersHostRecord.RemoveAt(i);

                //Keey track of control target index
                if (ControlTargetIndex > i) ControlTargetIndex--;
                i--;

                //Keey track of selected command troop
                for (int j = 0; j < CommandSelectedIndicesHostRecord.Count; j++)
                {
                    if (CommandSelectedIndicesHostRecord[j] > i) CommandSelectedIndicesHostRecord[j]--;
                    else if (CommandSelectedIndicesHostRecord[j] == i)
                    {
                        CommandSelectedIndicesHostRecord.RemoveAt(CommandSelectedIndicesHostRecord.IndexOf(i));
                        j--;
                    }
                }
            }
        }
        UpdateAssociatedCharactersNetworked();
        UpdateCommandSelectedIndicesNetworked();
    }

    private void ProcessCommandInputForNonControlTargetCharacter()
    {
        if (Runner.TryGetInputForPlayer<PlayerData>(Object.InputAuthority, out PlayerData input))
        {
            for (int i = 0; i < AssociatedCharactersHostRecord.Count; i++)
            {
                if (i != ControlTargetIndex)
                {
                    AllyCharacter currentCharacter = AssociatedCharactersHostRecord[i].GetComponent<AllyCharacter>();

                    //Selected Characters With Non Command will take user input
                    if (CommandSelectedIndicesHostRecord.Contains(i) && currentCharacter.commandStatusNetworked == AllyCharacter.CommandStatus.NONE)
                    {
                        currentCharacter.InfluencedByInput(input, buttonsPrev);
                        continue;
                    }

                    //Command Input
                    currentCharacter.InfluencedByCommand();
                }
            }
            buttonsPrev = input.NetworkButtons;
        }
    }

    public override void FixedUpdateNetwork()
    {
        DetermineTargetCharacter();
        ProcessPlayerInputForTargetCharacter();
        
        RemoveNullCharacters();
        ProcessCommandInputForNonControlTargetCharacter();

        //Run Logic
        for (int i = 0; i < AssociatedCharactersHostRecord.Count; i++)
        {
            AssociatedCharactersHostRecord[i].GetComponent<AllyCharacter>().Logic();
        }

        PassiveIncomeLogic();
        GameIsOver();
    }
    //----------------------------------------------------------------------------------------------------------------------------


    //----------------------------------------------------------------------------------------------------------------------------
    //Late Update
    public void LateUpdate()
    {
        HandleLocalCameraLogic();
    }
    //----------------------------------------------------------------------------------------------------------------------------


    //----------------------------------------------------------------------------------------------------------------------------
    //Character Selection Rectangle, Control target UI, Command Icon UI
    private void RenderOtherPlayerControlTarget()
    {
        //Inactivate All Command Icon
        foreach (var AssociatedCharacter in AssociatedCharactersNetworked)
        {
            if (AssociatedCharacter != null)
            {
                AllyCharacter allyCharacter = AssociatedCharacter.GetComponent<AllyCharacter>();
                allyCharacter.InactivateControlTargetIcon();
                allyCharacter.InactivateCommandIcon();
            }
        }

        //Active Control Target Icon
        if (ControlTargetCharacterNetworked != null)
        {
            ControlTargetCharacterNetworked.GetComponent<AllyCharacter>().ActivateControlTargetIcon();
            ControlTargetCharacterNetworked.GetComponent<AllyCharacter>().ControlTargetIcon.color = Color.green;
        }
    }

    private void RenderSelectionRectangle()
    {
        if (CommandSelecting)
        {
            SelectionRectangle.gameObject.SetActive(true);
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 center = (CommandSelectedStartPoint + mouseWorldPos) / 2;
            Vector3 size = CommandSelectedStartPoint - mouseWorldPos;
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y));

            SelectionRectangle.rectTransform.position = center;
            SelectionRectangle.rectTransform.sizeDelta = size;
        }
        else
        {
            SelectionRectangle.rectTransform.position = new Vector3(0,0,0);
            SelectionRectangle.rectTransform.sizeDelta = new Vector3(0, 0, 0);
            SelectionRectangle.gameObject.SetActive(false);
        }
    }

    private void RenderCharacterUI()
    {
        //Inactivate All Command Icon
        foreach (var AssociatedCharacter in AssociatedCharactersNetworked)
        {
            if (AssociatedCharacter != null)
            {
                AllyCharacter allyCharacter = AssociatedCharacter.GetComponent<AllyCharacter>();
                allyCharacter.InactivateControlTargetIcon();
                allyCharacter.InactivateCommandIcon();
            }
        }

        //Active Control Target Icon
        if (ControlTargetCharacterNetworked != null)
        {
            ControlTargetCharacterNetworked.GetComponent<AllyCharacter>().ActivateControlTargetIcon();
        }

        //Active All Control Target Icon
        if (Input.GetKey(KeyCode.Mouse1))
        {
            foreach (var AssociatedCharacter in AssociatedCharactersNetworked)
            {
                if (AssociatedCharacter != null)
                {
                    AllyCharacter allyCharacter = AssociatedCharacter.GetComponent<AllyCharacter>();
                    allyCharacter.ActivateControlTargetIcon();
                }
            }
        }

        //Activate Command Icon and set Command Icon Color
        for (int i = 0; i < CommandSelectedIndicesNetworked.Length; i++)
        {
            int index = CommandSelectedIndicesNetworked[i];
            if (index == -1) break;

            if (AssociatedCharactersNetworked[index] != null)
            {
                AllyCharacter allyCharacter = AssociatedCharactersNetworked[index].GetComponent<AllyCharacter>();
                allyCharacter.ActivateCommandIcon();
                allyCharacter.SetCommandIconColor();
            }
        }
    }

    public override void Render()
    {
        if (Runner.LocalPlayer != Object.InputAuthority)
        {
            RenderOtherPlayerControlTarget();
            return;
        }
        RenderSelectionRectangle();
        RenderCharacterUI();
    }

    private void OnDrawGizmos()
    {
        if (Runner.LocalPlayer != Object.InputAuthority) return; 

        if (CommandSelecting)
        {
            Gizmos.color = Color.green;
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 center = (CommandSelectedStartPoint + mouseWorldPos) / 2;
            Vector3 size = CommandSelectedStartPoint - mouseWorldPos;
            Gizmos.DrawWireCube(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 1));
        }

        float indicatorOffset = 0.8f;
        float indicatorSize = 0.2f;

        for (int i = 0; i < CommandSelectedIndicesNetworked.Length; i++)
        {
            int index = CommandSelectedIndicesNetworked[i];
            if (index == -1) break;

            if (AssociatedCharactersNetworked[index] != null)
            {
                //Determine Color
                AllyCharacter allyCharacter = AssociatedCharactersNetworked[index].GetComponent<AllyCharacter>();
                if (allyCharacter.commandStatusNetworked == AllyCharacter.CommandStatus.NONE) Gizmos.color = Color.red;
                if (allyCharacter.commandStatusNetworked == AllyCharacter.CommandStatus.PATROL) Gizmos.color = Color.yellow;
                if (allyCharacter.commandStatusNetworked == AllyCharacter.CommandStatus.FOLLOW) Gizmos.color = Color.green;
                if (allyCharacter.commandStatusNetworked == AllyCharacter.CommandStatus.HOLD_POSITION) Gizmos.color = Color.blue;

                Vector3 pos = AssociatedCharactersNetworked[index].transform.position;
                pos += new Vector3(0, indicatorOffset);
                Gizmos.DrawSphere(pos, indicatorSize);
            }
        }
        
        if (ControlTargetCharacterNetworked != null)
        {
            Gizmos.color = Color.white;
            Vector3 pos = ControlTargetCharacterNetworked.transform.position;
            pos += new Vector3(0, indicatorOffset);
            Gizmos.DrawWireSphere(pos, indicatorSize + 0.1f);
        }
    }
    //----------------------------------------------------------------------------------------------------------------------------
}
