#pragma once

#include <windows.h>
#include <cstdint>

typedef uint64_t QWORD;

#ifdef NIOH3AFFIXCORE_EXPORTS
#define NIOH3AFFIXCORE_API __declspec(dllexport)
#else
#define NIOH3AFFIXCORE_API __declspec(dllimport)
#endif

// 装备类型枚举
enum EquipmentType {
    EQUIP_TYPE_UNKNOWN = 0,
    EQUIP_TYPE_WEAPON = 1,
    EQUIP_TYPE_ARMOR = 2
};

extern "C" {
    // 进程管理
    NIOH3AFFIXCORE_API bool __cdecl AttachProcess(DWORD processId);
    NIOH3AFFIXCORE_API void __cdecl DetachProcess();
    NIOH3AFFIXCORE_API bool __cdecl IsAttached();

    // Hook 管理
    NIOH3AFFIXCORE_API bool __cdecl EnableCapture();
    NIOH3AFFIXCORE_API void __cdecl DisableCapture();
    NIOH3AFFIXCORE_API bool __cdecl IsCaptureEnabled();

    // 装备类型查询
    NIOH3AFFIXCORE_API int __cdecl GetCurrentEquipmentType();
    NIOH3AFFIXCORE_API bool __cdecl IsWeaponMode();

    // 装备基址
    NIOH3AFFIXCORE_API QWORD __cdecl GetEquipmentBase();

    // 调试函数 - 分别获取武器和装备的Hook状态和基址
    NIOH3AFFIXCORE_API bool __cdecl IsWeaponHookEnabled();
    NIOH3AFFIXCORE_API bool __cdecl IsArmorHookEnabled();
    NIOH3AFFIXCORE_API QWORD __cdecl GetWeaponBase();
    NIOH3AFFIXCORE_API QWORD __cdecl GetArmorBase();

    // 词条读写
    NIOH3AFFIXCORE_API bool __cdecl ReadAffix(int slotIndex, int* outId, int* outLevel);
    NIOH3AFFIXCORE_API bool __cdecl WriteAffix(int slotIndex, int id, int level);

    // Extended affix read/write (includes 4 single-byte prefix fields).
    NIOH3AFFIXCORE_API bool __cdecl ReadAffixEx(
        int slotIndex,
        int* outId,
        int* outLevel,
        uint8_t* outPrefix1,
        uint8_t* outPrefix2,
        uint8_t* outPrefix3,
        uint8_t* outPrefix4
    );

    // Field mask bits for WriteAffixExMasked:
    // bit0: id, bit1: level, bit2..bit5: prefix1..prefix4
    NIOH3AFFIXCORE_API bool __cdecl WriteAffixExMasked(
        int slotIndex,
        int id,
        int level,
        uint8_t prefix1,
        uint8_t prefix2,
        uint8_t prefix3,
        uint8_t prefix4,
        uint32_t fieldMask
    );

    // 装备基础属性读写
    NIOH3AFFIXCORE_API bool __cdecl ReadEquipmentBasics(
        short* outItemId,
        short* outTransmogId,
        short* outLevel,
        int* outUnderworldSkillId,
        int* outFamiliarity,
        bool* outIsUnderworld
    );

    NIOH3AFFIXCORE_API bool __cdecl ReadEquipmentBasicsEx(
        short* outItemId,
        short* outTransmogId,
        short* outLevel,
        uint8_t* outEquipPlusValue,
        int* outQuality,
        int* outUnderworldSkillId,
        int* outFamiliarity,
        bool* outIsUnderworld
    );

    NIOH3AFFIXCORE_API bool __cdecl WriteEquipmentBasics(
        short itemId,
        short transmogId,
        short level,
        int underworldSkillId,
        int familiarity,
        bool isUnderworld
    );

    NIOH3AFFIXCORE_API bool __cdecl WriteEquipmentBasicsEx(
        short itemId,
        short transmogId,
        short level,
        uint8_t equipPlusValue,
        int quality,
        int underworldSkillId,
        int familiarity,
        bool isUnderworld
    );

    // 获取最后一次错误信息
    NIOH3AFFIXCORE_API const char* __cdecl GetLastErrorMessage();

    // 技能学习条件绕过
    NIOH3AFFIXCORE_API bool __cdecl EnableSkillBypass();
    NIOH3AFFIXCORE_API bool __cdecl DisableSkillBypass();
    NIOH3AFFIXCORE_API bool __cdecl IsSkillBypassEnabled();
}
