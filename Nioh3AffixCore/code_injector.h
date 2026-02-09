#pragma once

#include <windows.h>
#include <cstdint>

typedef uint64_t QWORD;

// Hook类型枚举
enum class HookType {
    Weapon,  // 武器Hook: 捕获rbp, 6字节原始指令
    Armor    // 装备Hook: 捕获rbx, 8字节原始指令
};

// 代码注入器类
class CodeInjector {
public:
    CodeInjector();
    ~CodeInjector();

    // 初始化注入器
    // process: 目标进程句柄
    // injectionPoint: 注入点地址 (AOB 扫描结果)
    // hookType: Hook类型 (武器或装备)
    bool Initialize(HANDLE process, QWORD injectionPoint, HookType hookType = HookType::Weapon);

    // 启用 hook
    bool Enable();

    // 禁用 hook (恢复原始代码)
    bool Disable();

    // 获取捕获的装备基址
    QWORD GetEquipmentBase() const;

    // 是否已启用
    bool IsEnabled() const { return m_enabled; }

    // 获取Hook类型
    HookType GetHookType() const { return m_hookType; }

private:
    HANDLE m_process;
    QWORD m_injectionPoint;
    QWORD m_allocatedMemory;
    QWORD m_equipmentVarAddr;
    bool m_enabled;
    HookType m_hookType;

    // 原始代码备份 (最多16字节，支持8字节指令)
    BYTE m_originalBytes[16];
    int m_originalBytesCount;

    // 清理资源
    void Cleanup();

    // 生成武器Hook代码
    int GenerateWeaponHookCode(BYTE* buffer, QWORD returnAddr);

    // 生成装备Hook代码
    int GenerateArmorHookCode(BYTE* buffer, QWORD returnAddr);
};
