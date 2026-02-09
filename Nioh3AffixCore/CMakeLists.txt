cmake_minimum_required(VERSION 3.20)
project(Nioh3AffixCore LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 设置为 DLL
add_library(Nioh3AffixCore SHARED
    dllmain.cpp
    exports.cpp
    exports.h
    aob_scanner.cpp
    aob_scanner.h
    code_injector.cpp
    code_injector.h
    memory_layout.h
    skill_bypass_injector.cpp
    skill_bypass_injector.h
)

# 定义导出宏
target_compile_definitions(Nioh3AffixCore PRIVATE NIOH3AFFIXCORE_EXPORTS)

# 链接 psapi
target_link_libraries(Nioh3AffixCore PRIVATE psapi)

# 设置输出目录 (输出到 C# 项目目录)
set_target_properties(Nioh3AffixCore PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../bin"
    RUNTIME_OUTPUT_DIRECTORY_DEBUG "${CMAKE_SOURCE_DIR}/../bin/Debug"
    RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_SOURCE_DIR}/../bin/Release"
)

# Windows 特定设置
if(WIN32)
    target_compile_definitions(Nioh3AffixCore PRIVATE
        WIN32_LEAN_AND_MEAN
        NOMINMAX
        _CRT_SECURE_NO_WARNINGS
    )
endif()
