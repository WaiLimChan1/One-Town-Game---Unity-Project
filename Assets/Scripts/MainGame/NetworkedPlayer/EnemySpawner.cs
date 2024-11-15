using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] public List<NetworkObject> Enemies;
    [SerializeField] public int maxEnemyCount = 150;

    [Header("EnemySpawner Components")]
    [SerializeField] private NetworkedPlayer networkedPlayer;
    [SerializeField] private CharacterSpawner characterSpawner;

    [Header("EnemySpawner Values")]
    private Vector2 enemySpawnTimeRange = new Vector2(10, 1);
    private Vector2 TNTSpawnChanceRange = new Vector2(0f, 0.7f);
    private Vector2 groupSpawnTimeRange = new Vector2(30, 20);
    private Vector2 groupSizeRange = new Vector2(5, 30);

    [SerializeField] private float enemySpawnTime;
    [Range(0.0f,1.0f)] [SerializeField] private float TNTSpawnChance;
    TickTimer enemySpawnTimer;

    [SerializeField] private float groupSpawnTime;
    [SerializeField] private int groupSize;
    TickTimer groupSpawnTimer;

    [SerializeField] private float maxDifficultyTime;
    [Networked] public float gameTimer { get; set; }

    public override void Spawned()
    {
        networkedPlayer = GetComponent<NetworkedPlayer>();
        characterSpawner = GetComponent<CharacterSpawner>();

        enemySpawnTimer = TickTimer.CreateFromSeconds(Runner, enemySpawnTime);
        groupSpawnTimer = TickTimer.CreateFromSeconds(Runner, groupSpawnTime);
    }

    public void SpawnEnemy()
    {
        if (Enemies.Count >= maxEnemyCount) return;

        if (enemySpawnTimer.ExpiredOrNotRunning(Runner))
        {
            if (Random.Range(0.0f, 1.0f) <= TNTSpawnChance) characterSpawner.SpawnCharacter(Object.InputAuthority, CharacterSpawner.CharacterType.TNT);
            else characterSpawner.SpawnCharacter(Object.InputAuthority, CharacterSpawner.CharacterType.TORCH);
            enemySpawnTimer = TickTimer.CreateFromSeconds(Runner, enemySpawnTime);
        }

        if (groupSpawnTimer.ExpiredOrNotRunning(Runner))
        {
            characterSpawner.SpawnEnemyGroup(groupSize, TNTSpawnChance);
            groupSpawnTimer = TickTimer.CreateFromSeconds(Runner, enemySpawnTime);
        }
    }

    public void IncreaseDifficulty()
    {
        float ratio = gameTimer / maxDifficultyTime;
        enemySpawnTime = Mathf.Lerp(enemySpawnTimeRange.x, enemySpawnTimeRange.y, ratio);
        TNTSpawnChance = Mathf.Lerp(TNTSpawnChanceRange.x, TNTSpawnChanceRange.y, ratio);
        groupSpawnTime = Mathf.Lerp(groupSpawnTimeRange.x, groupSpawnTimeRange.y, ratio);
        groupSize = (int) Mathf.Lerp(groupSizeRange.x, groupSizeRange.y, ratio);

        gameTimer += Runner.DeltaTime;
    }

    public void CleanEnemiesList()
    {
        //Remove null characters, and update target index properly
        for (int i = 0; i < Enemies.Count; i++)
        {
            NetworkObject currentCharacter = Enemies[i];
            if (currentCharacter == null)
            {
                Enemies.RemoveAt(i);
                i--;
            }
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;
        if (Runner.LocalPlayer != Object.InputAuthority) return;

        if (networkedPlayer.TownHall.healthNetworked > 0)
        {
            SpawnEnemy();
            IncreaseDifficulty();
        }
        CleanEnemiesList();
    }
}