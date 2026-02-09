using Nioh3AffixEditor.Models;

namespace Nioh3AffixEditor.Engine;

public sealed class AffixEngineNotImplemented : IAffixEngine
{
    public string Name => "NotImplemented";

    public bool IsAttached => false;

    public ProcessInfo? AttachedProcess => null;

    public bool IsCaptureEnabled => false;

    public ulong? EquipmentBaseAddress => null;

    public Task AttachAsync(ProcessInfo process, CancellationToken cancellationToken)
        => throw new NotImplementedException("Affix engine not implemented.");

    public Task DetachAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task EnableCaptureAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("Affix engine not implemented.");

    public Task DisableCaptureAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<IReadOnlyList<AffixSlotData>> ReadAffixesAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("Affix engine not implemented.");

    public Task WriteAffixesAsync(IReadOnlyList<AffixSlotData> slots, CancellationToken cancellationToken)
        => throw new NotImplementedException("Affix engine not implemented.");

    public Task<EquipmentData> ReadEquipmentAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException("Affix engine not implemented.");

    public Task WriteEquipmentAsync(EquipmentData data, CancellationToken cancellationToken)
        => throw new NotImplementedException("Affix engine not implemented.");
}
