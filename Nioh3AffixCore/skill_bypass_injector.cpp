#include "skill_bypass_injector.h"
#include "aob_scanner.h"
#include <cstring>

SkillBypassInjector::SkillBypassInjector()
    : m_process(nullptr)
    , m_enabled(false)
    , m_hook1Address(0)
    , m_hook1Found(false)
    , m_hook2Address(0)
    , m_hook2Found(false)
{
    memset(m_hook1OriginalBytes, 0, sizeof(m_hook1OriginalBytes));
    memset(m_hook2OriginalBytes, 0, sizeof(m_hook2OriginalBytes));
}

SkillBypassInjector::~SkillBypassInjector() {
    Cleanup();
}

bool SkillBypassInjector::Initialize(HANDLE process) {
    if (m_enabled) {
        return false;
    }

    m_process = process;
    return FindHookPoints();
}

bool SkillBypassInjector::FindHookPoints() {
    // 查找Hook点1
    m_hook1Address = AobScan(m_process, SkillBypassAob::HOOK1_AOB);
    if (m_hook1Address != 0) {
        // 备份原始字节 (5 bytes: 75 43 0F B7 CF)
        SIZE_T bytesRead;
        if (ReadProcessMemory(m_process, (LPCVOID)m_hook1Address, m_hook1OriginalBytes, 5, &bytesRead) && bytesRead == 5) {
            m_hook1Found = true;
        }
    }

    // 查找Hook点2
    m_hook2Address = AobScan(m_process, SkillBypassAob::HOOK2_AOB);
    if (m_hook2Address != 0) {
        // 备份原始字节 (6 bytes: 0F 85 xx xx xx xx)
        SIZE_T bytesRead;
        if (ReadProcessMemory(m_process, (LPCVOID)m_hook2Address, m_hook2OriginalBytes, 6, &bytesRead) && bytesRead == 6) {
            m_hook2Found = true;
        }
    }

    // 至少找到一个hook点才算成功
    return m_hook1Found || m_hook2Found;
}

bool SkillBypassInjector::ApplyHook1() {
    if (!m_hook1Found) return true; // 没找到就跳过

    /*
    原始代码:
        75 43           ; jne +43 (跳过技能学习)
        0F B7 CF        ; movzx ecx, di

    修改为:
        90              ; nop
        90              ; nop
        0F B7 CF        ; movzx ecx, di (保持不变)

    这样就不会跳过，直接执行后面的代码
    */

    BYTE patchBytes[5] = { 0x90, 0x90, 0x0F, 0xB7, 0xCF };

    DWORD oldProtect;
    if (!VirtualProtectEx(m_process, (LPVOID)m_hook1Address, 5, PAGE_EXECUTE_READWRITE, &oldProtect)) {
        return false;
    }

    SIZE_T bytesWritten;
    bool success = WriteProcessMemory(m_process, (LPVOID)m_hook1Address, patchBytes, 5, &bytesWritten) && bytesWritten == 5;

    VirtualProtectEx(m_process, (LPVOID)m_hook1Address, 5, oldProtect, &oldProtect);

    return success;
}

bool SkillBypassInjector::ApplyHook2() {
    if (!m_hook2Found) return true; // 没找到就跳过

    /*
    原始代码:
        0F 85 xx xx xx xx   ; jne (6 bytes, 跳过技能学习)

    修改为:
        90 90 90 90 90 90   ; 6个nop

    这样就不会跳过，直接执行后面的代码
    */

    BYTE patchBytes[6] = { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };

    DWORD oldProtect;
    if (!VirtualProtectEx(m_process, (LPVOID)m_hook2Address, 6, PAGE_EXECUTE_READWRITE, &oldProtect)) {
        return false;
    }

    SIZE_T bytesWritten;
    bool success = WriteProcessMemory(m_process, (LPVOID)m_hook2Address, patchBytes, 6, &bytesWritten) && bytesWritten == 6;

    VirtualProtectEx(m_process, (LPVOID)m_hook2Address, 6, oldProtect, &oldProtect);

    return success;
}

bool SkillBypassInjector::RestoreHook1() {
    if (!m_hook1Found) return true;

    DWORD oldProtect;
    if (!VirtualProtectEx(m_process, (LPVOID)m_hook1Address, 5, PAGE_EXECUTE_READWRITE, &oldProtect)) {
        return false;
    }

    SIZE_T bytesWritten;
    bool success = WriteProcessMemory(m_process, (LPVOID)m_hook1Address, m_hook1OriginalBytes, 5, &bytesWritten) && bytesWritten == 5;

    VirtualProtectEx(m_process, (LPVOID)m_hook1Address, 5, oldProtect, &oldProtect);

    return success;
}

bool SkillBypassInjector::RestoreHook2() {
    if (!m_hook2Found) return true;

    DWORD oldProtect;
    if (!VirtualProtectEx(m_process, (LPVOID)m_hook2Address, 6, PAGE_EXECUTE_READWRITE, &oldProtect)) {
        return false;
    }

    SIZE_T bytesWritten;
    bool success = WriteProcessMemory(m_process, (LPVOID)m_hook2Address, m_hook2OriginalBytes, 6, &bytesWritten) && bytesWritten == 6;

    VirtualProtectEx(m_process, (LPVOID)m_hook2Address, 6, oldProtect, &oldProtect);

    return success;
}

bool SkillBypassInjector::Enable() {
    if (m_enabled) return true;
    if (m_process == nullptr) return false;

    bool success1 = ApplyHook1();
    bool success2 = ApplyHook2();

    if (success1 && success2) {
        m_enabled = true;
        return true;
    }

    // 如果失败，尝试恢复
    RestoreHook1();
    RestoreHook2();
    return false;
}

bool SkillBypassInjector::Disable() {
    if (!m_enabled) return true;

    bool success1 = RestoreHook1();
    bool success2 = RestoreHook2();

    if (success1 && success2) {
        m_enabled = false;
        return true;
    }

    return false;
}

void SkillBypassInjector::Cleanup() {
    if (m_enabled) {
        Disable();
    }

    m_process = nullptr;
    m_hook1Address = 0;
    m_hook2Address = 0;
    m_hook1Found = false;
    m_hook2Found = false;
}
