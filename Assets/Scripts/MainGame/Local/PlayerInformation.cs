using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInformation : MonoBehaviour
{
    public static PlayerInformation instance;
    [SerializeField] public NetworkedPlayer NetworkedPlayer;

    [SerializeField] private TMP_Text WarriorCost;
    [SerializeField] private TMP_Text ArcherCost;
    [SerializeField] private TMP_Text VillagerCost;

    [SerializeField] private TMP_Text WarriorCount;
    [SerializeField] private TMP_Text ArcherCount;
    [SerializeField] private TMP_Text VillagerCount;
    [SerializeField] private TMP_Text GoldCount;

    [SerializeField] private TMP_Text SurvivalTime;

    [SerializeField] private Button WarriorBuyButton;
    [SerializeField] private Button ArcherrBuyButton;
    [SerializeField] private Button VillagerBuyButton;

    public void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void SpawnCharacter(CharacterSpawner.CharacterType characterType)
    {
        if (NetworkedPlayer == null) return;
        if (Input.GetKeyDown(KeyCode.Space)) return;
        NetworkedPlayer.SpawnCharacter(characterType);
    }
    
    private void InitializeTroopCost()
    {
        WarriorCost.text = "X " + CharacterSpawner.WarriorCost;
        ArcherCost.text = "X " + CharacterSpawner.ArcherCost;
        VillagerCost.text = "X " + CharacterSpawner.VillagerCost;
    }

    private void Start()
    {
        InitializeTroopCost();
        WarriorBuyButton.onClick.AddListener(() => SpawnCharacter(CharacterSpawner.CharacterType.WARRIOR));
        ArcherrBuyButton.onClick.AddListener(() => SpawnCharacter(CharacterSpawner.CharacterType.ARCHER));
        VillagerBuyButton.onClick.AddListener(() => SpawnCharacter(CharacterSpawner.CharacterType.VILLAGER));
    }

    public void UpdateTroopAndResourceCounts()
    {
        int warriorCount = 0;
        int archerCount = 0;
        int villagerCount = 0;

        if (NetworkedPlayer != null)
        {
            for (int i = 0; i < NetworkedPlayer.AssociatedCharactersNetworked.Length; i++)
            {
                if (NetworkedPlayer.AssociatedCharactersNetworked[i] != null)
                {
                    Warrior Warrior = NetworkedPlayer.AssociatedCharactersNetworked[i].GetComponent<Warrior>();
                    if (Warrior != null)
                    {
                        warriorCount++;
                        continue;
                    }

                    Archer Archer = NetworkedPlayer.AssociatedCharactersNetworked[i].GetComponent<Archer>();
                    if (Archer != null)
                    {
                        archerCount++;
                        continue;
                    }

                    Villager Villager = NetworkedPlayer.AssociatedCharactersNetworked[i].GetComponent<Villager>();
                    if (Villager != null)
                    {
                        villagerCount++;
                        continue;
                    }
                }
            }

            WarriorCount.text = "X " + warriorCount;
            ArcherCount.text = "X " + archerCount;
            VillagerCount.text = "X " + villagerCount;
            GoldCount.text = "X " + NetworkedPlayer.GoldAmount;
        }
    }

    public void Update()
    {
        UpdateTroopAndResourceCounts();
        if (NetworkedPlayer != null) SurvivalTime.text = "Survival Time: " + (int) NetworkedPlayer.GetSurvivalTime() + " Seconds";
    }
}
