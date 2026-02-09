using Nioh3AffixEditor.Models;

namespace Nioh3AffixEditor.Engine;

public interface IAffixEngine
{
    string Name { get; }

    bool IsAttached { get; }
    ProcessInfo? AttachedProcess { get; }

    bool IsCaptureEnabled { get; }
    ulong? EquipmentBaseAddress { get; }

    Task AttachAsync(ProcessInfo process, CancellationToken cancellationToken);
    Task DetachAsync(CancellationToken cancellationToken);

    Task EnableCaptureAsync(CancellationToken cancellationToken);
    Task DisableCaptureAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AffixSlotData>> ReadAffixesAsync(CancellationToken cancellationToken);
    Task WriteAffixesAsync(IReadOnlyList<AffixSlotData> slots, CancellationToken cancellationToken);

    Task<EquipmentData> ReadEquipmentAsync(CancellationToken cancellationToken);
    Task WriteEquipmentAsync(EquipmentData data, CancellationToken cancellationToken);
}
