using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

public class NetworkedPlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef playerNetworkPrefab;

    public override void Spawned()
    {
        if (Runner.IsServer) 
            PlayerJoined(Runner.LocalPlayer);
    }

    public void PlayerJoined(PlayerRef playerRef)
    {
        if (Runner.IsServer)
        {
            var playerObject = Runner.Spawn(playerNetworkPrefab, new Vector3(0, 0, 0), Quaternion.identity, playerRef);
            Runner.SetPlayerObject(playerRef, playerObject);
        }
    }

    public void PlayerLeft(PlayerRef playerRef)
    {
        if (Runner.IsServer)
        {
            if (Runner.TryGetPlayerObject(playerRef, out var playerNetworkObject))
            {
                NetworkedPlayer NetworkedPlayer = playerNetworkObject.GetComponent<NetworkedPlayer>();
                if (NetworkedPlayer != null) NetworkedPlayer.DespawnAllAssociatedCharacter();
                Runner.Despawn(playerNetworkObject);
            }
            Runner.SetPlayerObject(playerRef, null);
        }
    }
}
