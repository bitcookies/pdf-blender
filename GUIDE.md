# PDF Blender 开发与编译指南

这是给项目维护者使用的快速指南。项目的产品规格和技术约束仍以 `SPEC.md`、`ARCHITECTURE.md` 和 `INTERFACE.md` 为准；这些文件主要供 AI 和开发流程参考，`GUIDE.md` 只说明日常操作。

## 环境要求

- Windows
- PowerShell 7（命令是 `pwsh`）
- .NET SDK 10
- 可访问 NuGet（首次构建需要恢复 PDF 渲染和测试依赖）
- Visual Studio 的 WPF 开发组件
- MSIX 打包需要额外的 Visual Studio Windows Application Packaging Project 支持；当前项目还没有配置 Store 打包工程

在项目根目录打开 PowerShell 7：

```powershell
cd D:\Users\haoning\Documents\GitHub\@bitcookies\pdf-blender
```

## 编译菜单

最简单的方式是不带参数运行脚本：

```powershell
pwsh -NoProfile -File .\build.ps1
```

脚本会显示数字菜单：

| 数字 | 操作 | 输出 |
| --- | --- | --- |
| `1` | 生成便携版 | `publish/portable/` |
| `2` | 生成 MSIX 安装包 | `publish/installer/` |
| `3` | Release 编译 | 使用 .NET 默认输出目录 |
| `4` | Release 测试 | 测试项目输出目录 |
| `0` | 退出 | 不修改文件 |

也可以直接传数字：

```powershell
pwsh -NoProfile -File .\build.ps1 1
pwsh -NoProfile -File .\build.ps1 2
pwsh -NoProfile -File .\build.ps1 3
pwsh -NoProfile -File .\build.ps1 4
```

便携版是用于开发和测试的 `win-x64` 发布目录，包含 EXE、托管程序集以及 PDFium/SkiaSharp 原生组件。它不是 Microsoft Store 安装包。

如果 `publish/portable` 或 `publish/installer` 已经存在文件，脚本会询问：

```text
Delete and recreate it? [y/N]
```

只有输入 `y` 或 `yes` 才会删除旧的发布目录并重新生成；直接回车或输入其他内容都会取消。自动化场景可以使用：

```powershell
pwsh -NoProfile -File .\build.ps1 1 -Force
```

`-Force` 会跳过覆盖确认，只建议用于确认输出目录确实是可删除的构建产物时。

## 返回码

- `0`：成功
- `2`：菜单输入无效
- `3`：用户拒绝覆盖已有发布文件
- `10`：Release 编译失败
- `20`：测试失败
- `30`：便携版发布失败
- `40`：MSIX 未配置或 MSIX 构建失败

当前运行 `2` 会得到明确提示：项目尚未有 `packaging/PDFBlender.Package.wapproj`。等 Store 身份和打包工程准备好后，脚本会把 MSIX 输出到 `publish/installer/`。

## 目录说明

```text
PDFBlender/
├─ assets/                 项目使用的原始品牌资源，例如 Logo
├─ src/                    应用源码，按职责拆分
│  ├─ PDFBlender.App/      WPF 窗口、主题、语言、Profile 和用户交互
│  ├─ PDFBlender.Core/     设置模型、预设、验证规则和处理契约
│  └─ PDFBlender.Pdf/      PDFium 渲染、图片处理和 PDF 重建实现
├─ tests/                  自动化测试，包含模型测试和最小 PDF 管线测试
├─ publish/                发布输出；portable 和 installer 分开存放，已被 Git 忽略
├─ PDF-Poisoning/          外部参考项目，不属于 PDF Blender 源码
├─ 参考图/                 UI 参考图片，不属于发布内容
├─ SPEC.md                 产品规格和 MVP 范围
├─ ARCHITECTURE.md         AI 使用的技术架构约束
├─ DESIGN.md               AI 使用的视觉和产品设计约束
├─ INTERFACE.md            AI 使用的 UI、交互和本地化约束
├─ AGENTS.md               AI 在本仓库中工作时必须遵守的规则
├─ THIRD-PARTY-NOTICES.md  第三方依赖和许可证说明
├─ PDFBlender.slnx         .NET 解决方案
├─ Directory.Build.props   统一版本号，目前是 0.2.0 / 0.2.0.0
└─ build.ps1               数字构建入口
```

`src` 看起来较大，是因为它不是一个单体项目。UI、纯业务模型和 PDF 原生处理被分开，便于测试、替换 PDF 引擎，并避免把渲染细节直接写进窗口代码。`bin/`、`obj/`、`publish/` 和 NuGet 还原文件都是生成内容，不应提交到 Git。

## 常用开发流程

1. 修改源码后运行 `pwsh -NoProfile -File .\build.ps1 3`。
2. 运行 `pwsh -NoProfile -File .\build.ps1 4`。
3. 需要测试便携版时运行 `pwsh -NoProfile -File .\build.ps1 1`，然后启动 `publish/portable/PDFBlender.App.exe`。
4. 提交前确认 `git status` 中没有 `publish/`、`bin/`、`obj/`、参考项目或本地 PDF。

PDF 处理完全在本机执行。不要把 PDF 内容、提取文字或个人文档信息写入日志，也不要把 `PDF-Poisoning/` 或 `参考图/` 加入发布包。
