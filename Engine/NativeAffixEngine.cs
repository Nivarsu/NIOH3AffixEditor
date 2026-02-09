using Nioh3AffixEditor.Models;

namespace Nioh3AffixEditor.Engine;

/// <summary>
/// 基于 C++ DLL 的词条引擎实现
/// </summary>
public sealed class NativeAffixEngine : IAffixEngine, IDisposable
{
    private ProcessInfo? _attachedProcess;
    private bool _disposed;

    private ulong? _lastAffixSnapshotBase;
    private IReadOnlyList<AffixSlotData>? _lastAffixSnapshot;

    public string Name => "NativeEngine";

    public bool IsAttached => NativeBridge.IsAttached();

    public ProcessInfo? AttachedProcess => _attachedProcess;

    public bool IsCaptureEnabled => NativeBridge.IsCaptureEnabled();

    /// <summary>
    /// 当前是否是武器模式（否则是装备模式）
    /// </summary>
    public bool IsWeaponMode => NativeBridge.IsWeaponMode();

    /// <summary>
    /// 获取当前装备类型 (0=未知, 1=武器, 2=装备)
    /// </summary>
    public int CurrentEquipmentType => NativeBridge.GetCurrentEquipmentType();

    // 调试属性 - 分别获取武器和装备的Hook状态和基址
    /// <summary>
    /// 武器Hook是否已启用
    /// </summary>
    public bool IsWeaponHookEnabled => NativeBridge.IsWeaponHookEnabled();

    /// <summary>
    /// 装备Hook是否已启用
    /// </summary>
    public bool IsArmorHookEnabled => NativeBridge.IsArmorHookEnabled();

    /// <summary>
    /// 武器基址（直接从武器Hook获取）
    /// </summary>
    public ulong? WeaponBaseAddress
    {
        get
        {
            var addr = NativeBridge.GetWeaponBase();
            return addr == 0 ? null : addr;
        }
    }

    /// <summary>
    /// 装备基址（直接从装备Hook获取）
    /// </summary>
    public ulong? ArmorBaseAddress
    {
        get
        {
            var addr = NativeBridge.GetArmorBase();
            return addr == 0 ? null : addr;
        }
    }

    public ulong? EquipmentBaseAddress
    {
        get
        {
            var addr = NativeBridge.GetEquipmentBase();
            return addr == 0 ? null : addr;
        }
    }

    /// <summary>
    /// 技能学习条件绕过是否已启用
    /// </summary>
    public bool IsSkillBypassEnabled => NativeBridge.IsSkillBypassEnabled();

    /// <summary>
    /// 启用技能学习条件绕过
    /// </summary>
    public bool EnableSkillBypass()
    {
        ThrowIfDisposed();

        if (!IsAttached)
        {
            return false;
        }

        return NativeBridge.EnableSkillBypass();
    }

    /// <summary>
    /// 禁用技能学习条件绕过
    /// </summary>
    public bool DisableSkillBypass()
    {
        return NativeBridge.DisableSkillBypass();
    }

    public Task AttachAsync(ProcessInfo process, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (IsAttached)
        {
            throw new InvalidOperationException("Already attached to a process. Detach first.");
        }

        if (!NativeBridge.AttachProcess((uint)process.Id))
        {
            var error = NativeBridge.GetLastErrorString();
            throw new InvalidOperationException($"Failed to attach to process: {error}");
        }

        _attachedProcess = process;
        _lastAffixSnapshotBase = null;
        _lastAffixSnapshot = null;
        return Task.CompletedTask;
    }

    public Task DetachAsync(CancellationToken cancellationToken)
    {
        if (IsAttached)
        {
            NativeBridge.DetachProcess();
        }
        _attachedProcess = null;
        _lastAffixSnapshotBase = null;
        _lastAffixSnapshot = null;
        return Task.CompletedTask;
    }

    public Task EnableCaptureAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (!IsAttached)
        {
            throw new InvalidOperationException("Not attached to any process.");
        }

        if (!NativeBridge.EnableCapture())
        {
            var error = NativeBridge.GetLastErrorString();
            throw new InvalidOperationException($"Failed to enable capture: {error}");
        }

        return Task.CompletedTask;
    }

    public Task DisableCaptureAsync(CancellationToken cancellationToken)
    {
        if (IsCaptureEnabled)
        {
            NativeBridge.DisableCapture();
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AffixSlotData>> ReadAffixesAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (!IsAttached)
        {
            throw new InvalidOperationException("Not attached to any process.");
        }

        if (!IsCaptureEnabled)
        {
            throw new InvalidOperationException("Capture not enabled.");
        }

        var equipBase = EquipmentBaseAddress;
        if (equipBase is null)
        {
            throw new InvalidOperationException("尚未捕获到装备基址。请在游戏中移动一次装备选择光标。");
        }

        var slots = new List<AffixSlotData>(7);
        for (int i = 0; i < 7; i++)
        {
            if (NativeBridge.TryReadAffixEx(i, out int id, out int level, out byte p1, out byte p2, out byte p3, out byte p4))
            {
                slots.Add(new AffixSlotData(i + 1, id, level, p1, p2, p3, p4));
            }
            else
            {
                var error = NativeBridge.GetLastErrorString();
                throw new InvalidOperationException($"Failed to read affix slot {i}: {error}");
            }
        }

        // Cache snapshot for diff writes (only valid while equipment base doesn't change).
        _lastAffixSnapshotBase = equipBase;
        _lastAffixSnapshot = slots;

        return Task.FromResult<IReadOnlyList<AffixSlotData>>(slots);
    }

    public Task WriteAffixesAsync(IReadOnlyList<AffixSlotData> slots, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (!IsAttached)
        {
            throw new InvalidOperationException("Not attached to any process.");
        }

        if (!IsCaptureEnabled)
        {
            throw new InvalidOperationException("Capture not enabled.");
        }

        var equipBase = EquipmentBaseAddress;
        if (equipBase is null)
        {
            throw new InvalidOperationException("尚未捕获到装备基址。请在游戏中移动一次装备选择光标。");
        }

        const uint MaskId = 1u << 0;
        const uint MaskLevel = 1u << 1;
        const uint MaskPrefix1 = 1u << 2;
        const uint MaskPrefix2 = 1u << 3;
        const uint MaskPrefix3 = 1u << 4;
        const uint MaskPrefix4 = 1u << 5;
        const uint MaskAll = MaskId | MaskLevel | MaskPrefix1 | MaskPrefix2 | MaskPrefix3 | MaskPrefix4;

        bool canDiffWrite = _lastAffixSnapshotBase == equipBase
            && _lastAffixSnapshot is { Count: 7 };

        AffixSlotData?[]? baselineBySlot = null;
        if (canDiffWrite)
        {
            baselineBySlot = new AffixSlotData?[8];
            foreach (var s in _lastAffixSnapshot!)
            {
                if (s.SlotIndex >= 1 && s.SlotIndex <= 7)
                {
                    baselineBySlot[s.SlotIndex] = s;
                }
            }
        }

        foreach (var slot in slots)
        {
            // SlotIndex 是 1-based，转换为 0-based
            int index = slot.SlotIndex - 1;
            if (index < 0 || index >= 7)
            {
                throw new ArgumentOutOfRangeException(nameof(slots), $"Invalid slot index: {slot.SlotIndex}");
            }

            uint mask = MaskAll;
            if (baselineBySlot is not null && baselineBySlot[slot.SlotIndex] is { } old)
            {
                mask = 0;
                if (slot.AffixId != old.AffixId) mask |= MaskId;
                if (slot.Level != old.Level) mask |= MaskLevel;
                if (slot.Prefix1 != old.Prefix1) mask |= MaskPrefix1;
                if (slot.Prefix2 != old.Prefix2) mask |= MaskPrefix2;
                if (slot.Prefix3 != old.Prefix3) mask |= MaskPrefix3;
                if (slot.Prefix4 != old.Prefix4) mask |= MaskPrefix4;
            }

            if (mask == 0)
            {
                continue;
            }

            if (!NativeBridge.WriteAffixExMasked(
                index,
                slot.AffixId,
                slot.Level,
                slot.Prefix1,
                slot.Prefix2,
                slot.Prefix3,
                slot.Prefix4,
                mask))
            {
                var error = NativeBridge.GetLastErrorString();
                throw new InvalidOperationException($"Failed to write affix slot {slot.SlotIndex}: {error}");
            }
        }

        // Refresh snapshot after successful write, so repeated Apply doesn't re-write the same fields.
        // Only do this when we have a complete, unique 7-slot set.
        if (TryBuildSnapshot(slots, out var snapshot))
        {
            _lastAffixSnapshotBase = equipBase;
            _lastAffixSnapshot = snapshot;
        }

        return Task.CompletedTask;
    }

    private static bool TryBuildSnapshot(IReadOnlyList<AffixSlotData> slots, out IReadOnlyList<AffixSlotData> snapshot)
    {
        snapshot = Array.Empty<AffixSlotData>();
        if (slots.Count != 7) return false;

        var arr = new AffixSlotData[7];
        var seen = new bool[7];
        foreach (var slot in slots)
        {
            int idx = slot.SlotIndex - 1;
            if (idx < 0 || idx >= 7) return false;
            if (seen[idx]) return false;
            seen[idx] = true;
            arr[idx] = slot;
        }

        for (int i = 0; i < 7; i++)
        {
            if (!seen[i]) return false;
        }

        snapshot = arr;
        return true;
    }

    public Task<EquipmentData> ReadEquipmentAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (!IsAttached)
        {
            throw new InvalidOperationException("Not attached to any process.");
        }

        if (!IsCaptureEnabled)
        {
            throw new InvalidOperationException("Capture not enabled.");
        }

        var equipBase = EquipmentBaseAddress;
        if (equipBase is null)
        {
            throw new InvalidOperationException("尚未捕获到装备基址。请在游戏中移动一次装备选择光标。");
        }

        if (!NativeBridge.TryReadEquipmentBasicsEx(
            out short itemId,
            out short transmogId,
            out short level,
            out byte equipPlusValue,
            out int quality,
            out int underworldSkillId,
            out int familiarity,
            out bool isUnderworld))
        {
            var error = NativeBridge.GetLastErrorString();
            throw new InvalidOperationException($"Failed to read equipment basics: {error}");
        }

        return Task.FromResult(new EquipmentData(
            itemId,
            transmogId,
            level,
            equipPlusValue,
            quality,
            underworldSkillId,
            familiarity,
            isUnderworld
        ));
    }

    public Task WriteEquipmentAsync(EquipmentData data, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (!IsAttached)
        {
            throw new InvalidOperationException("Not attached to any process.");
        }

        if (!IsCaptureEnabled)
        {
            throw new InvalidOperationException("Capture not enabled.");
        }

        var equipBase = EquipmentBaseAddress;
        if (equipBase is null)
        {
            throw new InvalidOperationException("尚未捕获到装备基址。请在游戏中移动一次装备选择光标。");
        }

        if (!NativeBridge.WriteEquipmentBasicsEx(
            data.ItemId,
            data.TransmogId,
            data.Level,
            data.EquipPlusValue,
            data.Quality,
            data.UnderworldSkillId,
            data.Familiarity,
            data.IsUnderworld))
        {
            var error = NativeBridge.GetLastErrorString();
            throw new InvalidOperationException($"Failed to write equipment basics: {error}");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (IsCaptureEnabled)
            {
                NativeBridge.DisableCapture();
            }
            if (IsAttached)
            {
                NativeBridge.DetachProcess();
            }
        }
        catch
        {
            // Best effort cleanup
        }

        _attachedProcess = null;
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
