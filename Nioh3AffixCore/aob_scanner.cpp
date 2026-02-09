#include "aob_scanner.h"
#include <Psapi.h>
#include <vector>
#include <cstring>

#pragma comment(lib, "psapi.lib")

struct PatternByte {
    BYTE value;
    bool isWild;
};

bool GetMainModuleInfo(HANDLE process, QWORD& baseAddr, QWORD& moduleSize) {
    HMODULE hMods[1024];
    DWORD cbNeeded;

    if (EnumProcessModules(process, hMods, sizeof(hMods), &cbNeeded)) {
        MODULEINFO modInfo;
        if (GetModuleInformation(process, hMods[0], &modInfo, sizeof(modInfo))) {
            baseAddr = (QWORD)modInfo.lpBaseOfDll;
            moduleSize = (QWORD)modInfo.SizeOfImage;
            return true;
        }
    }
    return false;
}

QWORD AobScan(HANDLE process, const char* pattern, QWORD startAddr, QWORD endAddr) {
    // 如果起始和结束地址都为0，自动获取主模块范围
    QWORD actualStartAddr = startAddr;
    QWORD actualEndAddr = endAddr;

    if (startAddr == 0 && endAddr == 0) {
        QWORD baseAddr, moduleSize;
        if (GetMainModuleInfo(process, baseAddr, moduleSize)) {
            actualStartAddr = baseAddr;
            actualEndAddr = baseAddr + moduleSize;
        }
        else {
            return 0;
        }
    }

    // 预处理：移除空格
    int patternLen = (int)strlen(pattern);
    std::vector<char> processedPattern;
    processedPattern.reserve(patternLen);

    for (int i = 0; i < patternLen; i++) {
        if (pattern[i] != ' ') {
            processedPattern.push_back(pattern[i]);
        }
    }

    // 验证长度
    if (processedPattern.size() % 2 != 0) {
        return 0;
    }

    int byteCount = (int)processedPattern.size() / 2;

    // 转换为字节数组和通配符标记
    std::vector<PatternByte> bytes(byteCount);
    for (int i = 0; i < byteCount; i++) {
        char c[3] = { processedPattern[i * 2], processedPattern[i * 2 + 1], '\0' };
        if (strcmp(c, "??") == 0) {
            bytes[i].isWild = true;
            bytes[i].value = 0;
        }
        else {
            char* end;
            bytes[i].value = (BYTE)strtoul(c, &end, 16);
            bytes[i].isWild = (end != c + 2);
            if (bytes[i].isWild) {
                return 0; // 无效的十六进制字符
            }
        }
    }

    // 扫描内存
    const DWORD pageSize = 4096;
    std::vector<BYTE> page(pageSize);

    for (QWORD addr = actualStartAddr; addr < actualEndAddr; addr += pageSize - byteCount) {
        SIZE_T bytesRead;
        if (!ReadProcessMemory(process, (LPCVOID)addr, page.data(), pageSize, &bytesRead)) {
            continue;
        }

        for (SIZE_T i = 0; i < bytesRead - byteCount; i++) {
            bool matched = true;
            for (int j = 0; j < byteCount; j++) {
                if (!bytes[j].isWild && page[i + j] != bytes[j].value) {
                    matched = false;
                    break;
                }
            }
            if (matched) {
                return addr + i;
            }
        }
    }

    return 0;
}
