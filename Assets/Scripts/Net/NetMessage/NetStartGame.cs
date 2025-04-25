using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetStartGame : NetMessage
{
    public int gameMode;

    public NetStartGame(){
        Code = OpCode.START_GAME;
    }

    public NetStartGame(DataStreamReader reader){
        Code = OpCode.START_GAME;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer){
        writer.WriteByte((byte)Code);
        writer.WriteInt(gameMode); // Assuming gameMode is an enum or byte value
    }

    public override void Deserialize(DataStreamReader reader){
        gameMode = reader.ReadInt(); // Read the string but ignore it
    }

    public override void ReceivedOnClient(){
        NetUtility.C_START_GAME?.Invoke(this);
    }

    public override void ReceivedOnServer(NetworkConnection cnn){
        NetUtility.S_START_GAME?.Invoke(this, cnn);
    }
}
