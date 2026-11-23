using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace Tinker;

/// <summary>
/// Co-op: the host's settings, for both the card and the relic side. Replaces the two halves' separate messages.
///
/// The two halves still send at the same moments as before, each filling only its own part:
/// - Relic part: sent in the lobby just before the run starts (Relic Upgrader then freezes it into the run).
/// - Card part: sent when a run starts or loads, and again whenever a client asks.
///
/// Delivery flags match what each half used before: the card part was broadcast and buffered (BaseLib's
/// default); the relic part was neither.
/// </summary>
public sealed class HostSettingsMessage : ICustomMessage, IPacketSerializable
{
    /// <summary>Card Upgrader settings JSON, or null.</summary>
    public string CardJson;

    /// <summary>Relic Upgrader settings JSON, or null.</summary>
    public string RelicJson;

    public bool ShouldBroadcast => RelicJson == null;

    public bool ShouldBuffer => RelicJson == null;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(CardJson ?? "");
        writer.WriteString(RelicJson ?? "");
    }

    public void Deserialize(PacketReader reader)
    {
        CardJson = NullIfEmpty(reader.ReadString());
        RelicJson = NullIfEmpty(reader.ReadString());
    }

    public void HandleMessage(ulong senderId)
    {
        if (CardJson != null)
        {
            Tinker.Card.HostSettingsSync.Receive(CardJson);
        }
        if (RelicJson != null)
        {
            Tinker.Relic.Settings.Receive(RelicJson, senderId);
        }
    }

    private static string NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
}

/// <summary>Co-op: a client asking the host to resend its (card) settings, e.g. after loading a run.</summary>
public sealed class RequestHostSettingsMessage : ICustomMessage, IPacketSerializable
{
    public bool ShouldBroadcast => true;

    public void Serialize(PacketWriter writer)
    {
    }

    public void Deserialize(PacketReader reader)
    {
    }

    public void HandleMessage(ulong senderId)
    {
        Tinker.Card.HostSettingsSync.OnRequest();
    }
}
