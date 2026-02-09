namespace Nioh3AffixEditor.Models;

public sealed record AffixSlotData(
    int SlotIndex,
    int AffixId,
    int Level,
    byte Prefix1,
    byte Prefix2,
    byte Prefix3,
    byte Prefix4
);
