# Test 构建说明

本项目已在 `Nioh3AffixEditor.csproj` 中添加 `Test` 配置（定义 `TEST_BUILD`）。

## 重要提示

- 请直接针对 **项目文件** 构建：`Nioh3AffixEditor.csproj`
- 不要对 `Nioh3AffixEditor.sln` 使用 `-c Test`（解决方案未声明 `Test|Any CPU`）

## 构建测试版

```powershell
dotnet build .\Nioh3AffixEditor.csproj -c Test
```

输出目录：

- `bin\Test\net9.0-windows\win-x64\`

## 发布测试版（单文件）

```powershell
dotnet publish .\Nioh3AffixEditor.csproj -c Test -o publish-test
```

输出目录：

- `publish-test\Nioh3AffixEditor.exe`

