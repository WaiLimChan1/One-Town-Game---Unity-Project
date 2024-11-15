using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using NanoSockets;

public struct PlayerData : INetworkInput
{
    public Vector2 mouseWorldPosition;
    public Vector2 movementDirection;
    public NetworkButtons NetworkButtons;
}
