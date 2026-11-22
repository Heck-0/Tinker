using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace Tinker.Card;

/// <summary>
/// Co-op: one Workshop purchase, sent by the buyer's game to everyone else so their games apply the same
/// upgrade (see <see cref="WorkshopFlow"/>). Found by BaseLib automatically (it registers every
/// <see cref="ICustomMessage"/> in loaded mods), so every player needs the same Tinker version.
/// </summary>
public sealed class WorkshopMessage : ICustomMessage, IPacketSerializable
{
    private const int Version = 2;

    public ulong PlayerId;

    public int Op;

    public int Index;

    public string RelicId = "";

    public int Attribute;

    public int Units;

    public int Floor;

    private bool _valid = true;

    public bool ShouldBroadcast => true;

    internal static WorkshopMessage For(Player player, WorkshopPurchase purchase)
    {
        return new WorkshopMessage
        {
            PlayerId = player.NetId,
            Op = purchase.Op,
            Index = purchase.Index,
            RelicId = purchase.RelicId ?? "",
            Attribute = purchase.Attribute,
            Units = purchase.Units,
            Floor = purchase.Floor,
        };
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(Version);
        writer.WriteULong(PlayerId);
        writer.WriteInt(Op);
        writer.WriteInt(Index);
        writer.WriteString(RelicId ?? "");
        writer.WriteInt(Attribute);
        writer.WriteInt(Units);
        writer.WriteInt(Floor);
    }

    public void Deserialize(PacketReader reader)
    {
        _valid = reader.ReadInt() == Version;
        if (!_valid)
        {
            return;
        }
        PlayerId = reader.ReadULong();
        Op = reader.ReadInt();
        Index = reader.ReadInt();
        RelicId = reader.ReadString();
        Attribute = reader.ReadInt();
        Units = reader.ReadInt();
        Floor = reader.ReadInt();
    }

    public void HandleMessage(ulong senderId)
    {
        if (!_valid)
        {
            ModEntry.Error("Compra del taller de otra versión del mod; se ignora. Todos deben tener la misma versión.");
            return;
        }
        WorkshopFlow.ApplyRemote(PlayerId, new WorkshopPurchase(Op, Index, RelicId, Attribute, Units, Floor));
    }
}
