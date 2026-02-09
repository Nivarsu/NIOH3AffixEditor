namespace Nioh3AffixEditor.Models;

/// <summary>
/// 装备基础属性数据
/// </summary>
public sealed record EquipmentData(
    short ItemId,
    short TransmogId,
    short Level,
    byte EquipPlusValue,
    int Quality,
    int UnderworldSkillId,
    int Familiarity,
    bool IsUnderworld
);
