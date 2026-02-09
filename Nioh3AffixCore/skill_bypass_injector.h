#pragma once

#include <windows.h>
#include <cstdint>

typedef uint64_t QWORD;

/// <summary>
/// 技能学习条件绕过Hook
/// 通过修改两个跳转指令来绕过技能学习条件检查
/// </summary>
class SkillBypassInjector {
public:
    SkillBypassInjector();
    ~SkillBypassInjector();

    /// <summary>
    /// 初始化注入器
    /// </summary>
    /// <param name="process">目标进程句柄</param>
    /// <returns>成功返回true</returns>
    bool Initialize(HANDLE process);

    /// <summary>
    /// 启用技能学习条件绕过
    /// </summary>
    bool Enable();

    /// <summary>
    /// 禁用技能学习条件绕过
    /// </summary>
    bool Disable();

    /// <summary>
    /// 检查是否已启用
    /// </summary>
    bool IsEnabled() const { return m_enabled; }

    /// <summary>
    /// 清理资源
    /// </summary>
    void Cleanup();

private:
    HANDLE m_process;
    bool m_enabled;

    // Hook点1: jne -> nop+jmp (绕过第一个条件检查)
    QWORD m_hook1Address;
    BYTE m_hook1OriginalBytes[5];
    bool m_hook1Found;

    // Hook点2: jne -> nop*6 (绕过第二个条件检查)
    QWORD m_hook2Address;
    BYTE m_hook2OriginalBytes[6];
    bool m_hook2Found;

    bool FindHookPoints();
    bool ApplyHook1();
    bool ApplyHook2();
    bool RestoreHook1();
    bool RestoreHook2();
};

// AOB patterns
namespace SkillBypassAob {
    // 75 43 0F B7 CF E8 - jne +43, movzx ecx,di, call...
    constexpr const char* HOOK1_AOB = "75 43 0F B7 CF E8";

    // 0F 85 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? BA ?? ?? ?? ?? 41 C6 85 ?? ?? ?? ?? 01 48 8B 89
    constexpr const char* HOOK2_AOB = "0F 85 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? BA ?? ?? ?? ?? 41 C6 85 ?? ?? ?? ?? 01 48 8B 89";
}
