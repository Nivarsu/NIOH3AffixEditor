using System.Runtime.InteropServices;

namespace Nioh3AffixEditor.Engine;

/// <summary>
/// P/Invoke 桥接类，用于调用 Nioh3AffixCore.dll
/// </summary>
internal static partial class NativeBridge
{
    private const string DllName = "Nioh3AffixCore.dll";

    /// <summary>
    /// 静态构造函数，确保在任何P/Invoke调用之前加载DLL
    /// </summary>
    static NativeBridge()
    {
        NativeDllLoader.EnsureLoaded();
    }

    // 进程管理
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachProcess(uint processId);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void DetachProcess();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsAttached();

    // Hook 管理
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnableCapture();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void DisableCapture();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsCaptureEnabled();

    // 装备类型查询
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int GetCurrentEquipmentType();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWeaponMode();

    // 装备基址
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial ulong GetEquipmentBase();

    // 调试函数 - 分别获取武器和装备的Hook状态和基址
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWeaponHookEnabled();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsArmorHookEnabled();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial ulong GetWeaponBase();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial ulong GetArmorBase();

    // 词条读写
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ReadAffix(int slotIndex, int* outId, int* outLevel);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteAffix(int slotIndex, int id, int level);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ReadAffixEx(
        int slotIndex,
        int* outId,
        int* outLevel,
        byte* outPrefix1,
        byte* outPrefix2,
        byte* outPrefix3,
        byte* outPrefix4);

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteAffixExMasked(
        int slotIndex,
        int id,
        int level,
        byte prefix1,
        byte prefix2,
        byte prefix3,
        byte prefix4,
        uint fieldMask);

    // 装备基础属性读写
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ReadEquipmentBasics(
        short* outItemId,
        short* outTransmogId,
        short* outLevel,
        int* outUnderworldSkillId,
        int* outFamiliarity,
        byte* outIsUnderworld
    );

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ReadEquipmentBasicsEx(
        short* outItemId,
        short* outTransmogId,
        short* outLevel,
        byte* outEquipPlusValue,
        int* outQuality,
        int* outUnderworldSkillId,
        int* outFamiliarity,
        byte* outIsUnderworld
    );

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteEquipmentBasics(
        short itemId,
        short transmogId,
        short level,
        int underworldSkillId,
        int familiarity,
        [MarshalAs(UnmanagedType.U1)] bool isUnderworld
    );

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteEquipmentBasicsEx(
        short itemId,
        short transmogId,
        short level,
        byte equipPlusValue,
        int quality,
        int underworldSkillId,
        int familiarity,
        [MarshalAs(UnmanagedType.U1)] bool isUnderworld
    );

    // 错误信息
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nint GetLastErrorMessage();

    // 技能学习条件绕过
    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnableSkillBypass();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DisableSkillBypass();

    [LibraryImport(DllName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsSkillBypassEnabled();

    /// <summary>
    /// 获取最后一次错误信息的托管字符串
    /// </summary>
    public static string GetLastErrorString()
    {
        var ptr = GetLastErrorMessage();
        return ptr == nint.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
    }

    /// <summary>
    /// 安全读取词条数据
    /// </summary>
    public static bool TryReadAffix(int slotIndex, out int id, out int level)
    {
        unsafe
        {
            int tempId = 0;
            int tempLevel = 0;
            bool result = ReadAffix(slotIndex, &tempId, &tempLevel);
            id = tempId;
            level = tempLevel;
            return result;
        }
    }

    public static bool TryReadAffixEx(
        int slotIndex,
        out int id,
        out int level,
        out byte prefix1,
        out byte prefix2,
        out byte prefix3,
        out byte prefix4)
    {
        unsafe
        {
            int tempId = 0;
            int tempLevel = 0;
            byte tempPrefix1 = 0;
            byte tempPrefix2 = 0;
            byte tempPrefix3 = 0;
            byte tempPrefix4 = 0;

            bool result = ReadAffixEx(
                slotIndex,
                &tempId,
                &tempLevel,
                &tempPrefix1,
                &tempPrefix2,
                &tempPrefix3,
                &tempPrefix4
            );

            id = tempId;
            level = tempLevel;
            prefix1 = tempPrefix1;
            prefix2 = tempPrefix2;
            prefix3 = tempPrefix3;
            prefix4 = tempPrefix4;
            return result;
        }
    }

    /// <summary>
    /// 安全读取装备基础属性
    /// </summary>
    public static bool TryReadEquipmentBasics(
        out short itemId,
        out short transmogId,
        out short level,
        out int underworldSkillId,
        out int familiarity,
        out bool isUnderworld)
    {
        unsafe
        {
            short tempItemId = 0;
            short tempTransmogId = 0;
            short tempLevel = 0;
            int tempUnderworldSkillId = 0;
            int tempFamiliarity = 0;
            byte tempIsUnderworld = 0;

            bool result = ReadEquipmentBasics(
                &tempItemId,
                &tempTransmogId,
                &tempLevel,
                &tempUnderworldSkillId,
                &tempFamiliarity,
                &tempIsUnderworld
            );

            itemId = tempItemId;
            transmogId = tempTransmogId;
            level = tempLevel;
            underworldSkillId = tempUnderworldSkillId;
            familiarity = tempFamiliarity;
            isUnderworld = tempIsUnderworld != 0;

            return result;
        }
    }

    public static bool TryReadEquipmentBasicsEx(
        out short itemId,
        out short transmogId,
        out short level,
        out byte equipPlusValue,
        out int quality,
        out int underworldSkillId,
        out int familiarity,
        out bool isUnderworld)
    {
        unsafe
        {
            short tempItemId = 0;
            short tempTransmogId = 0;
            short tempLevel = 0;
            byte tempEquipPlusValue = 0;
            int tempQuality = 0;
            int tempUnderworldSkillId = 0;
            int tempFamiliarity = 0;
            byte tempIsUnderworld = 0;

            bool result = ReadEquipmentBasicsEx(
                &tempItemId,
                &tempTransmogId,
                &tempLevel,
                &tempEquipPlusValue,
                &tempQuality,
                &tempUnderworldSkillId,
                &tempFamiliarity,
                &tempIsUnderworld
            );

            itemId = tempItemId;
            transmogId = tempTransmogId;
            level = tempLevel;
            equipPlusValue = tempEquipPlusValue;
            quality = tempQuality;
            underworldSkillId = tempUnderworldSkillId;
            familiarity = tempFamiliarity;
            isUnderworld = tempIsUnderworld != 0;

            return result;
        }
    }
}
