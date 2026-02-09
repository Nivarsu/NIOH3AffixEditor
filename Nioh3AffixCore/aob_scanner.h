#pragma once

#include <windows.h>
#include <cstdint>

typedef uint64_t QWORD;

// AOB 扫描函数
// process: 目标进程句柄
// pattern: AOB 特征码字符串 (支持 ?? 通配符)
// startAddr: 扫描起始地址 (0 表示自动获取主模块起始)
// endAddr: 扫描结束地址 (0 表示自动获取主模块结束)
// 返回: 匹配地址，0 表示未找到
QWORD AobScan(HANDLE process, const char* pattern, QWORD startAddr = 0, QWORD endAddr = 0);

// 获取主模块信息
bool GetMainModuleInfo(HANDLE process, QWORD& baseAddr, QWORD& moduleSize);
