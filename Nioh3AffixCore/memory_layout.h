#pragma once

// 装备基础属性偏移 (相对于装备基址)
namespace EquipmentLayout {
    // 物品ID (2 bytes, short)
    constexpr int ITEM_ID_OFFSET = 0x00;

    // 幻化ID (2 bytes, short)
    constexpr int TRANSMOG_ID_OFFSET = 0x02;

    // 等级 (2 bytes, short)
    constexpr int LEVEL_OFFSET = 0x06;

    constexpr int EQUIPMENT_PLUS_VALUE_OFFSET = 0x0A;

    // 地狱技能ID (4 bytes, int)
    constexpr int UNDERWORLD_SKILL_ID_OFFSET = 0x10;

    // 爱用度 (4 bytes, int)
    constexpr int FAMILIARITY_OFFSET = 0x14;

    // 品质/稀有度? (4 bytes, int)
    // Verified by runtime diffing: base+0x30 varies across item qualities (white/yellow/etc).
    constexpr int QUALITY_OFFSET = 0x30;

    // 是否是地狱武器 (1 bit at offset 0x1A, bit 4)
    constexpr int UNDERWORLD_FLAG_OFFSET = 0x1A;
    constexpr int UNDERWORLD_FLAG_BIT = 4;  // bit 4 (0x10)
}

// 词条内存布局常量
namespace MemoryLayout {
    // 第一个词条的偏移
    constexpr int FIRST_AFFIX_OFFSET = 0x38;

    // 每个词条槽位的间隔
    constexpr int AFFIX_SLOT_SIZE = 0x18;

    // 词条ID在槽位内的偏移 (相对于槽位起始)
    constexpr int AFFIX_ID_OFFSET = 0x00;

    // 词条等级在槽位内的偏移 (相对于槽位起始)
    constexpr int AFFIX_LEVEL_OFFSET = 0x04;

    // Affix prefix bytes (single-byte fields). Offsets are (AFFIX_LEVEL_OFFSET + 4..+7).
    constexpr int AFFIX_PREFIX1_OFFSET = AFFIX_LEVEL_OFFSET + 4; // 0x08
    constexpr int AFFIX_PREFIX2_OFFSET = AFFIX_LEVEL_OFFSET + 5; // 0x09
    constexpr int AFFIX_PREFIX3_OFFSET = AFFIX_LEVEL_OFFSET + 6; // 0x0A
    constexpr int AFFIX_PREFIX4_OFFSET = AFFIX_LEVEL_OFFSET + 7; // 0x0B

    // 词条槽位数量
    constexpr int AFFIX_SLOT_COUNT = 7;

    // 计算词条ID的绝对偏移
    inline int GetAffixIdOffset(int slotIndex) {
        return FIRST_AFFIX_OFFSET + (slotIndex * AFFIX_SLOT_SIZE) + AFFIX_ID_OFFSET;
    }

    // 计算词条等级的绝对偏移
    inline int GetAffixLevelOffset(int slotIndex) {
        return FIRST_AFFIX_OFFSET + (slotIndex * AFFIX_SLOT_SIZE) + AFFIX_LEVEL_OFFSET;
    }

    inline int GetAffixPrefixOffset(int slotIndex, int prefixIndex0Based) {
        return FIRST_AFFIX_OFFSET + (slotIndex * AFFIX_SLOT_SIZE) + AFFIX_LEVEL_OFFSET + 4 + prefixIndex0Based;
    }
}

// AOB 特征码
namespace AobPatterns {
    // 武器基址捕获的 AOB
    constexpr const char* WEAPON_CAPTURE_AOB =
        "48 8B D5 49 8B CA E8 ?? ?? ?? ?? 48 8B 86 ?? ?? ?? ?? 48 8D 8E ?? ?? ?? ??";

    // 装备基址捕获的 AOB
    constexpr const char* ARMOR_CAPTURE_AOB =
        "49 8D 8C 24 ?? ?? ?? ?? 48 8B D3 E8 ?? ?? ?? ?? 8A 45 6F 8A 4D 67";

    // 保留旧名称以兼容
    constexpr const char* EQUIPMENT_CAPTURE_AOB = WEAPON_CAPTURE_AOB;
}
