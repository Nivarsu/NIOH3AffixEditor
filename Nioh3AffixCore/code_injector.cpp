#include "code_injector.h"
#include <cstring>

CodeInjector::CodeInjector()
    : m_process(nullptr)
    , m_injectionPoint(0)
    , m_allocatedMemory(0)
    , m_equipmentVarAddr(0)
    , m_enabled(false)
    , m_hookType(HookType::Weapon)
    , m_originalBytesCount(0)
{
    memset(m_originalBytes, 0, sizeof(m_originalBytes));
}

CodeInjector::~CodeInjector() {
    Cleanup();
}

bool CodeInjector::Initialize(HANDLE process, QWORD injectionPoint, HookType hookType) {
    if (m_enabled) {
        return false; // 已经启用，需要先禁用
    }

    m_process = process;
    m_injectionPoint = injectionPoint;
    m_hookType = hookType;

    // 根据Hook类型确定原始指令字节数
    if (hookType == HookType::Weapon) {
        m_originalBytesCount = 6;  // mov rdx,rbp + mov rcx,r10
    } else {
        m_originalBytesCount = 8;  // lea rcx,[r12+00000148]
    }

    // 备份原始代码
    SIZE_T bytesRead;
    if (!ReadProcessMemory(process, (LPCVOID)injectionPoint, m_originalBytes, m_originalBytesCount, &bytesRead)
        || bytesRead != (SIZE_T)m_originalBytesCount) {
        return false;
    }

    // 在目标进程中分配内存 (用于 hook 代码和装备基址变量)
    // 必须在注入点附近分配，以便使用相对跳转 (±2GB 范围内)
    m_allocatedMemory = 0;

    // 尝试在注入点前后 1GB 范围内分配内存
    // 从注入点向下搜索可用地址
    QWORD searchStart = injectionPoint > 0x70000000 ? injectionPoint - 0x70000000 : 0x10000;
    QWORD searchEnd = injectionPoint + 0x70000000;

    // 按 64KB 边界对齐 (Windows 分配粒度)
    searchStart = (searchStart + 0xFFFF) & ~0xFFFF;

    MEMORY_BASIC_INFORMATION mbi;
    for (QWORD addr = searchStart; addr < searchEnd; addr += 0x10000) {
        if (VirtualQueryEx(process, (LPCVOID)addr, &mbi, sizeof(mbi)) == sizeof(mbi)) {
            if (mbi.State == MEM_FREE && mbi.RegionSize >= 0x1000) {
                // 尝试在这个地址分配
                LPVOID allocated = VirtualAllocEx(
                    process,
                    (LPVOID)addr,
                    0x1000,
                    MEM_COMMIT | MEM_RESERVE,
                    PAGE_EXECUTE_READWRITE
                );
                if (allocated != nullptr) {
                    m_allocatedMemory = (QWORD)allocated;
                    break;
                }
            }
        }
    }

    if (m_allocatedMemory == 0) {
        // 如果附近分配失败，尝试让系统自动分配
        m_allocatedMemory = (QWORD)VirtualAllocEx(
            process,
            nullptr,
            0x1000,
            MEM_COMMIT | MEM_RESERVE,
            PAGE_EXECUTE_READWRITE
        );
    }

    if (m_allocatedMemory == 0) {
        return false;
    }

    // 装备基址变量地址 (在分配内存的末尾)
    m_equipmentVarAddr = m_allocatedMemory + 0x100;

    // 初始化装备基址变量为 0
    QWORD zero = 0;
    SIZE_T bytesWritten;
    WriteProcessMemory(process, (LPVOID)m_equipmentVarAddr, &zero, sizeof(zero), &bytesWritten);

    return true;
}

int CodeInjector::GenerateWeaponHookCode(BYTE* buffer, QWORD returnAddr) {
    /*
    武器Hook代码结构:

    newmem:
        ; 保存 rbp 到装备基址变量
        ; mov [m_equipmentVarAddr], rbp
        ; 使用绝对地址方式: mov rax, addr; mov [rax], rbp
        48 B8 [8字节地址]       ; mov rax, m_equipmentVarAddr
        48 89 28                ; mov [rax], rbp

        ; 执行原始指令
        48 8B D5                ; mov rdx, rbp
        49 8B CA                ; mov rcx, r10

        ; 跳回原位置 + 6
        ; jmp [return_addr]
        48 B8 [8字节地址]       ; mov rax, return_addr
        FF E0                   ; jmp rax
    */

    int offset = 0;

    // mov rax, m_equipmentVarAddr
    buffer[offset++] = 0x48;
    buffer[offset++] = 0xB8;
    memcpy(&buffer[offset], &m_equipmentVarAddr, 8);
    offset += 8;

    // mov [rax], rbp
    buffer[offset++] = 0x48;
    buffer[offset++] = 0x89;
    buffer[offset++] = 0x28;

    // mov rdx, rbp (原始指令)
    buffer[offset++] = 0x48;
    buffer[offset++] = 0x8B;
    buffer[offset++] = 0xD5;

    // mov rcx, r10 (原始指令)
    buffer[offset++] = 0x49;
    buffer[offset++] = 0x8B;
    buffer[offset++] = 0xCA;

    // mov rax, return_addr
    buffer[offset++] = 0x48;
    buffer[offset++] = 0xB8;
    memcpy(&buffer[offset], &returnAddr, 8);
    offset += 8;

    // jmp rax
    buffer[offset++] = 0xFF;
    buffer[offset++] = 0xE0;

    return offset;
}

int CodeInjector::GenerateArmorHookCode(BYTE* buffer, QWORD returnAddr) {
    /*
    装备Hook代码结构:

    newmem:
        ; 保存 rbx 到装备基址变量
        ; 使用绝对地址方式: mov rax, addr; mov [rax], rbx
        48 B8 [8字节地址]       ; mov rax, m_equipmentVarAddr
        48 89 18                ; mov [rax], rbx

        ; 执行原始指令
        49 8D 8C 24 48 01 00 00 ; lea rcx,[r12+00000148]

        ; 跳回原位置 + 8
        48 B8 [8字节地址]       ; mov rax, return_addr
        FF E0                   ; jmp rax
    */

    int offset = 0;

    // mov rax, m_equipmentVarAddr
    buffer[offset++] = 0x48;
    buffer[offset++] = 0xB8;
    memcpy(&buffer[offset], &m_equipmentVarAddr, 8);
    offset += 8;

    // mov [rax], rbx
    buffer[offset++] = 0x48;
    buffer[offset++] = 0x89;
    buffer[offset++] = 0x18;

    // lea rcx,[r12+00000148] (原始指令)
    buffer[offset++] = 0x49;
    buffer[offset++] = 0x8D;
    buffer[offset++] = 0x8C;
    buffer[offset++] = 0x24;
    buffer[offset++] = 0x48;
    buffer[offset++] = 0x01;
    buffer[offset++] = 0x00;
    buffer[offset++] = 0x00;

    // mov rax, return_addr
    buffer[offset++] = 0x48;
    buffer[offset++] = 0xB8;
    memcpy(&buffer[offset], &returnAddr, 8);
    offset += 8;

    // jmp rax
    buffer[offset++] = 0xFF;
    buffer[offset++] = 0xE0;

    return offset;
}

bool CodeInjector::Enable() {
    if (m_enabled || m_allocatedMemory == 0) {
        return false;
    }

    BYTE hookCode[64];
    int codeSize;
    QWORD returnAddr = m_injectionPoint + m_originalBytesCount;

    // 根据Hook类型生成不同的Hook代码
    if (m_hookType == HookType::Weapon) {
        codeSize = GenerateWeaponHookCode(hookCode, returnAddr);
    } else {
        codeSize = GenerateArmorHookCode(hookCode, returnAddr);
    }

    // 写入 hook 代码到分配的内存
    SIZE_T bytesWritten;
    if (!WriteProcessMemory(m_process, (LPVOID)m_allocatedMemory, hookCode, codeSize, &bytesWritten)) {
        return false;
    }

    /*
    注入点修改:
    使用相对跳转:
        E9 [4字节相对偏移]  ; jmp rel32 (5 字节)
        90 ...              ; nop (填充剩余字节)

    注意: 如果距离超过 2GB，相对跳转会失败
    */

    // 计算相对偏移
    int64_t relOffset = (int64_t)m_allocatedMemory - (int64_t)(m_injectionPoint + 5);

    // 检查是否在 32 位有符号整数范围内
    if (relOffset < INT32_MIN || relOffset > INT32_MAX) {
        return false;
    }

    // 构建跳转代码
    BYTE jumpCode[16];
    jumpCode[0] = 0xE9; // jmp rel32
    int32_t rel32 = (int32_t)relOffset;
    memcpy(&jumpCode[1], &rel32, 4);

    // 用NOP填充剩余字节
    for (int i = 5; i < m_originalBytesCount; i++) {
        jumpCode[i] = 0x90; // nop
    }

    // 修改内存保护
    DWORD oldProtect;
    if (!VirtualProtectEx(m_process, (LPVOID)m_injectionPoint, m_originalBytesCount, PAGE_EXECUTE_READWRITE, &oldProtect)) {
        return false;
    }

    // 写入跳转代码
    if (!WriteProcessMemory(m_process, (LPVOID)m_injectionPoint, jumpCode, m_originalBytesCount, &bytesWritten)) {
        VirtualProtectEx(m_process, (LPVOID)m_injectionPoint, m_originalBytesCount, oldProtect, &oldProtect);
        return false;
    }

    // 恢复内存保护
    VirtualProtectEx(m_process, (LPVOID)m_injectionPoint, m_originalBytesCount, oldProtect, &oldProtect);

    m_enabled = true;
    return true;
}

bool CodeInjector::Disable() {
    if (!m_enabled) {
        return true; // 已经禁用
    }

    // 恢复原始代码
    DWORD oldProtect;
    if (!VirtualProtectEx(m_process, (LPVOID)m_injectionPoint, m_originalBytesCount, PAGE_EXECUTE_READWRITE, &oldProtect)) {
        return false;
    }

    SIZE_T bytesWritten;
    bool success = WriteProcessMemory(m_process, (LPVOID)m_injectionPoint, m_originalBytes, m_originalBytesCount, &bytesWritten);

    VirtualProtectEx(m_process, (LPVOID)m_injectionPoint, m_originalBytesCount, oldProtect, &oldProtect);

    if (success) {
        m_enabled = false;
    }

    return success;
}

QWORD CodeInjector::GetEquipmentBase() const {
    if (!m_enabled || m_equipmentVarAddr == 0) {
        return 0;
    }

    QWORD value = 0;
    SIZE_T bytesRead;
    if (ReadProcessMemory(m_process, (LPCVOID)m_equipmentVarAddr, &value, sizeof(value), &bytesRead)) {
        return value;
    }
    return 0;
}

void CodeInjector::Cleanup() {
    if (m_enabled) {
        Disable();
    }

    if (m_allocatedMemory != 0 && m_process != nullptr) {
        VirtualFreeEx(m_process, (LPVOID)m_allocatedMemory, 0, MEM_RELEASE);
        m_allocatedMemory = 0;
    }

    m_equipmentVarAddr = 0;
    m_process = nullptr;
    m_injectionPoint = 0;
}
