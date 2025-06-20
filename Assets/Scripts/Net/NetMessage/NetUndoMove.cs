using Unity.Collections;
using Unity.Networking.Transport;

public class NetUndoMove : NetMessage
{
    public int originalX;
    public int originalY;
    public int destinationX;
    public int destinationY;

    public NetUndoMove(){
        Code = OpCode.UNDO_MOVE;
    }

    public NetUndoMove(DataStreamReader reader){
        Code = OpCode.UNDO_MOVE;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(originalX);
        writer.WriteInt(originalY);
        writer.WriteInt(destinationX);
        writer.WriteInt(destinationY);
    }

    public override void Deserialize(DataStreamReader reader)
    {
        originalX = reader.ReadInt();
        originalY = reader.ReadInt();
        destinationX = reader.ReadInt();
        destinationY = reader.ReadInt();
    }

    public override void ReceivedOnClient(){
        NetUtility.C_UNDO_MOVE?.Invoke(this);
    }

    public override void ReceivedOnServer(NetworkConnection cnn){
        NetUtility.S_UNDO_MOVE?.Invoke(this, cnn);
    }
}