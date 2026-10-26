# Plain Craft Launcher 2 ReBron

> 基于 [PCL2](https://github.com/Meloong-Git/PCL)（Hex-Dragon/PCL2，即 Meloong-Git/PCL）2.99.0.0 正式版的修改版启动器。
> **原作者：龙腾猫跃** ｜ **ReBron 版作者：Cherry5033MC**

---

## 这是什么

Plain Craft Launcher 2 ReBron（下称 ReBron）是 PCL2 的修改版，在官方正式版的基础上做了以下调整：

| # | 改动 | 说明 |
|---|------|------|
| 1 | **开启 MC 下载功能** | 原版因合规原因限制了下载功能，ReBron 已将其开启 |
| 2 | **更名为 ReBron** | 主窗口标题、单实例检测、关于页均已更名，避免与官方版混淆 |
| 3 | **崩溃修复** | 修复资源页可能触发的 `Embedded resource "Resource\WikiEntries.txt" was not found` 崩溃（资源读取改为三级回退，最坏情况只损失 Mod 中文译名，不再崩溃） |
| 4 | **关于页更新** | 新增 ReBron 版作者信息，标明原作者与修改版作者 |
| 5 | **标题栏标识** | 标题栏 PCL 标志右侧新增 ReBorn 字样 |
| 6 | **跨平台构建支持** | 提供 Linux 下使用 .NET SDK 构建 .NET Framework 4.8 WPF 工程的完整方案 |
| 7 | **百宝箱上线** | 新增「今日人品」（当日稳定的 MD5 种子算法）与「自定义下载」（多线程下载引擎 + 任务栏进度） |
| 8 | **自动更新** | 基于 GitHub Releases 的在线更新：检查更新、后台下载、自动替换重启、更新完成窗口（仅作者发布的成品启用；从源码构建的为开源版，提示前往 GitHub） |
| 9 | **反馈与新版本号** | 反馈入口改为本仓库 Issue；版本号采用 `2.99.0.0-reborn.x.x.x.x(N.x)` 系统 |

> ⚠️ ReBron 是**第三方修改版**，与龙腾猫跃官方无关联。使用中遇到的 ReBron 特有问题请提交到本仓库 Issues，**不要**提交到官方仓库。
> 本仓库仅供学习交流，PCL2 及其源码版权归 龙腾猫跃 所有。

---

## 下载使用

前往 [Releases](../../releases) 页面下载最新的 `Plain Craft Launcher 2.exe`。

- 系统要求：Windows 7 SP1 及以上
- 运行环境：.NET Framework 4.8（Win10 1903+ 已内置，低版本系统首次运行会引导安装）
- 单文件、免安装，放到任意有写权限的目录运行即可

> 建议放在**独立的空文件夹**中运行，PCL 会在同目录生成日志、缓存等文件。

---

## 从源码构建

源码不含任何第三方闭源组件，可完整复现构建。

### Windows（CMD，无需 Visual Studio）

依赖：[.NET SDK 9](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)（x64），以及 .NET Framework 4.8 引用程序集（两种方式任选其一）：

- 方式 A：安装 [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/zh-cn/download/dotnet-framework/net48)
- 方式 B：NuGet 引用程序集（免安装，脚本见下）

```bat
:: 方式 B：下载并解压 net48 引用程序集（只需执行一次）
curl -L -o net48.zip https://www.nuget.org/api/v2/package/Microsoft.NETFramework.ReferenceAssemblies.net48/1.0.3
mkdir net48ref
tar -xf net48.zip -C net48ref

:: 构建
dotnet restore PCLCS\PCLCS.csproj
dotnet msbuild "Plain Craft Launcher 2\Plain Craft Launcher 2.vbproj" -p:Configuration=Release ^
  -p:TargetFrameworkRootPath=%cd%\net48ref\build\ ^
  -p:IncludePackageReferencesDuringMarkupCompilation=false
```

产物：`Plain Craft Launcher 2\bin\Plain Craft Launcher 2.exe`（单文件，内嵌全部依赖）

> 若用方式 A（Developer Pack），去掉 `-p:TargetFrameworkRootPath` 参数即可。
> 工程内置的 `Directory.Build.targets` 使用 `$(MSBuildSDKsPath)` 自动定位 WPF 编译目标，Windows（含 x86/x64 SDK）与 Linux 均可直接使用。

### Linux / macOS（跨平台构建）

依赖：.NET SDK 9+、net48 引用程序集（同上方式 B）

```bash
dotnet restore PCLCS/PCLCS.csproj
dotnet msbuild "Plain Craft Launcher 2/Plain Craft Launcher 2.vbproj" \
  -p:Configuration=Release \
  -p:TargetFrameworkRootPath=$PWD/net48ref/build/ \
  -p:IncludePackageReferencesDuringMarkupCompilation=false
```

编译应为 0 错误；出现 1 条 `MSB3884 无法找到规则集文件` 警告属正常，可忽略。

### 构建成功验证

1. 产物约 6.7 MB
2. 关于页显示 4 行：龙腾猫跃（原作者）/ HerobrineXia / Cherry5033MC（ReBron 版作者）/ 版本行
3. “下载 → 模组”可正常打开
4. 标题栏 PCL 标志右侧有白底蓝字的 ReBorn 徽标
5. 百宝箱页有「今日人品」「自定义下载」两张卡片

---

## 目录结构

```
├── MeloongCore/            # 核心库（Shared 跨平台部分 + Wpf 界面部分）
├── PCLCS/                  # CurseForge/百科数据与启动辅助库
│   └── Resource/WikiEntries.txt   # MC 百科内置数据库
├── Plain Craft Launcher 2/ # 主工程（VB.NET + WPF）
│   ├── Directory.Build.targets    # 跨平台 WPF 构建支持（勿删）
│   └── Images/Heads/              # 关于页头像资源
├── patches/                # 官方历史版本补丁（供参考）
└── 构建说明.txt             # 修改内容与构建说明
```

---

## 反馈

- ReBron 相关的 Bug 或建议 → 提交 [Issue](../../issues/new/choose)
- PCL2 本身的功能问题 → 建议前往 [官方仓库](https://github.com/Meloong-Git/PCL/issues)

提交 Issue 前请先搜索已有 Issue，并附上 PCL 导出的错误报告或 `Log1~5.txt`。

---

## 致谢

- **龙腾猫跃** —— Plain Craft Launcher 原作者，以及所有 PCL 社区贡献者
- [bangbang93](https://bmclapi.bangbang93.com) —— BMCLAPI 镜像源
- [MC 百科](https://www.mcmod.cn) —— Mod 中文名翻译数据
- 其他鸣谢见启动器内“关于”页

## 许可

本仓库遵循 PCL 原版许可（见 [LICENCE](./LICENCE)）。PCL 允许在遵守条款的前提下进行非商用二次分发。
