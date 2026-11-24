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
    private const int Version = 3;

    public ulong PlayerId;

    public int Op;

    public int Index;

    public string Id = "";

    public int Attribute;

    public int Units;

    private bool _valid = true;

    public bool ShouldBroadcast => true;

    internal static WorkshopMessage For(Player player, WorkshopPurchase purchase)
    {
        return new WorkshopMessage
        {
            PlayerId = player.NetId,
            Op = purchase.Op,
            Index = purchase.Index,
            Id = purchase.Id ?? "",
            Attribute = purchase.Attribute,
            Units = purchase.Units,
        };
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(Version);
        writer.WriteULong(PlayerId);
        writer.WriteInt(Op);
        writer.WriteInt(Index);
        writer.WriteString(Id ?? "");
        writer.WriteInt(Attribute);
        writer.WriteInt(Units);
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
        Id = reader.ReadString();
        Attribute = reader.ReadInt();
        Units = reader.ReadInt();
    }

    public void HandleMessage(ulong senderId)
    {
        if (!_valid)
        {
            ModEntry.Error("Workshop purchase from another mod version; ignored. Everyone needs the same version.");
            return;
        }
        WorkshopFlow.ApplyRemote(PlayerId, new WorkshopPurchase(Op, Index, Id, Attribute, Units));
    }
}
