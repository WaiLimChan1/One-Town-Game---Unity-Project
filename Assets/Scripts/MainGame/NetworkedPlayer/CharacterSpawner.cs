using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.ComponentModel;

public class CharacterSpawner : NetworkBehaviour
{
    public static int WarriorCost = 6;
    public static int ArcherCost = 6;
    public static int VillagerCost = 4;

    public enum CharacterType { WARRIOR, ARCHER, VILLAGER, TNT, TORCH };
    static Vector2 enemySpawnDistanceRange = new Vector2(30, 40);

    [SerializeField] private NetworkedPlayer NetworkedPlayer;
    [SerializeField] private EnemySpawner EnemySpawner;
    [SerializeField] private NetworkPrefabRef[] NetworkedCharacterPrefabs;

    public override void Spawned()
    {
        EnemySpawner = GetComponent<EnemySpawner>();
    }

    public void SpawnEnemyGroup(int amount, float TNTSpawnChance)
    {
        if (!Runner.IsServer) return;
        if (EnemySpawner.Enemies.Count >= EnemySpawner.maxEnemyCount) return;

        Vector3 townHallPosition = NetworkedPlayer.TownHall.gameObject.transform.position;
        float randDegree = Random.Range(0f, 360f);
        float randDistance = Random.Range(enemySpawnDistanceRange.x, enemySpawnDistanceRange.y);

        float angleRadians = randDegree * Mathf.Deg2Rad;
        Vector3 changeVector = (new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0)).normalized * randDistance;
        Vector3 spawnPoint = townHallPosition + changeVector;

        for (int i = 0; i < amount; i++)
        {
            NetworkObject playerObject;
            if (Random.Range(0.0f, 1.0f) <= TNTSpawnChance)
                playerObject = Runner.Spawn(NetworkedCharacterPrefabs[(int)CharacterType.TNT], spawnPoint, Quaternion.identity, Object.InputAuthority);
            else
                playerObject = Runner.Spawn(NetworkedCharacterPrefabs[(int)CharacterType.TORCH], spawnPoint, Quaternion.identity, Object.InputAuthority);
            EnemySpawner.Enemies.Add(playerObject);
            if (EnemySpawner.Enemies.Count >= EnemySpawner.maxEnemyCount) return;
        }
    }

    public void Local_SpawnCharacter(PlayerRef playerRef, CharacterType CharacterType)
    {
        if (NetworkedPlayer.AssociatedCharactersHostRecord.Count >= NetworkedPlayer.MAX_TROOP_COUNT) return;
        //Cost
        if (CharacterType == CharacterType.WARRIOR)
        {
            if (NetworkedPlayer.GoldAmount < WarriorCost) return;
            else NetworkedPlayer.GoldAmount -= WarriorCost;
        }
        if (CharacterType == CharacterType.ARCHER)
        {
            if (NetworkedPlayer.GoldAmount < ArcherCost) return;
            else NetworkedPlayer.GoldAmount -= ArcherCost;
        }
        if (CharacterType == CharacterType.VILLAGER)
        {
            if (NetworkedPlayer.GoldAmount < VillagerCost) return;
            else NetworkedPlayer.GoldAmount -= VillagerCost;
        }

        //Check if playerRef has more troop space. Return if there is no more troop space
        if (CharacterType == CharacterType.WARRIOR || CharacterType == CharacterType.ARCHER || CharacterType == CharacterType.VILLAGER)
        {
            if (Runner.TryGetPlayerObject(playerRef, out var playerNetworkObject))
            {
                NetworkedPlayer NetworkedPlayer = playerNetworkObject.GetComponent<NetworkedPlayer>();
                if (NetworkedPlayer.AssociatedCharactersHostRecord.Count >= NetworkedPlayer.MAX_TROOP_COUNT)
                    return;
            }
        }

        //Determine Spawn Point.
        Vector3 spawnPoint;
        if (CharacterType == CharacterType.WARRIOR || CharacterType == CharacterType.ARCHER || CharacterType == CharacterType.VILLAGER)
            spawnPoint = new Vector3(0, 0, 0);
        else
        {
            if (EnemySpawner.Enemies.Count >= EnemySpawner.maxEnemyCount) return;

            Vector3 townHallPosition = NetworkedPlayer.TownHall.gameObject.transform.position;
            float randDegree = Random.Range(0f, 360f);
            float randDistance = Random.Range(enemySpawnDistanceRange.x, enemySpawnDistanceRange.y);

            float angleRadians = randDegree * Mathf.Deg2Rad;
            Vector3 changeVector = (new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0)).normalized * randDistance;
            spawnPoint = townHallPosition + changeVector;
        }


        NetworkObject playerObject = Runner.Spawn(NetworkedCharacterPrefabs[(int)CharacterType], spawnPoint, Quaternion.identity, playerRef);

        if (CharacterType == CharacterType.WARRIOR || CharacterType == CharacterType.ARCHER || CharacterType == CharacterType.VILLAGER)
        {
            playerObject.GetComponent<AllyCharacter>().NetworkedPlayer = NetworkedPlayer;
            NetworkedPlayer.AddAssociatedCharacter(playerObject);
        }

        if (CharacterType == CharacterType.TNT || CharacterType == CharacterType.TORCH)
        {
            EnemySpawner.Enemies.Add(playerObject);
        }
    }

    [Rpc(sources: RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_SpawnCharacter(PlayerRef playerRef, CharacterType CharacterType)
    {
       Local_SpawnCharacter(playerRef, CharacterType);
    }

    public void SpawnCharacter(PlayerRef playerRef, CharacterType CharacterType)
    {
        if (Runner.IsServer) Local_SpawnCharacter(playerRef, CharacterType);
        else Rpc_SpawnCharacter(playerRef, CharacterType);
    }
}
