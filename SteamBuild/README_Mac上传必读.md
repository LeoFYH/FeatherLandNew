# macOS 版 Steam 上传必读（修复 "Missing game executable"）

## 玩家报错

> An error occurred while launching this game : Missing game executable -
> /Users/.../Steam/steamapps/common/Feather Land/FeatherLand.app

## 根因

macOS 的程序必须带 Unix 可执行权限位（`chmod +x`）。**Windows 的 NTFS 没有这个
权限位**，所以只要 `.app` 在 Windows 上构建过、或在 Windows 上解压/压缩/中转过，
`FeatherLand.app/Contents/MacOS/featherlandunit` 的执行位就丢了。
SteamPipe 从 Windows 上传时不会自动补，玩家下载到的就是"不可执行"的 app，
Steam 报 Missing game executable。

## 正确上传流程

1. **构建**：Unity 菜单 `Tools/Build/构建 macOS Steam 版 (FeatherLand.app)`。
   输出固定在 `Builds/macOS/SteamUpload/FeatherLand.app`（名字不能带日期/版本号，
   构建管线会强制校验）。
2. **上传**：用本目录的脚本走 steamcmd（Windows 上传也安全，脚本里已用
   `FileProperties/executable` 打执行位标记）：

   ```
   steamcmd +login <Steamworks账号> +run_app_build "<仓库路径>\SteamBuild\app_build_mac_3661430.vdf" +quit
   ```

   ⚠️ 首次使用前，把脚本里的 Depot ID（预填 `3661432`）核对成
   partner.steamgames.com → App 3661430 → SteamPipe → Depots 里 **macOS Depot 的真实 ID**。
3. **上线**：Steamworks 后台 → SteamPipe → Builds → 把新 build 设到 `default` 分支。

## Steamworks 后台自查清单（一次性核对）

- **Installation → Launch Options**：macOS 的启动项 Executable 必须精确填
  `FeatherLand.app`（区分大小写，无子目录前缀），Operating System 选 macOS。
- **Depots**：macOS Depot 的 OS 选 macOS，架构留空（app 是 Universal，
  Intel + Apple Silicon 通吃）。
- **Packages**：确认商店包（Store package）里同时包含 Windows 和 macOS Depot，
  否则 Mac 玩家只会下载到空内容。
- 上传后在 Builds 页点开该 build 的 depot 文件列表，确认
  `FeatherLand.app/Contents/MacOS/featherlandunit` 存在且带 Executable 标记。

## 验证

- 有 Mac 时：Steam 切到新 build 重新下载，终端执行
  `ls -l ~/Library/Application\ Support/Steam/steamapps/common/Feather\ Land/FeatherLand.app/Contents/MacOS/`
  应显示 `-rwxr-xr-x`（带 x），然后能正常启动。
- 已经踩坑的玩家：更新上线后让玩家 **验证游戏文件完整性**
  （库 → 右键 Feather Land → 属性 → 已安装文件 → 验证游戏文件完整性），
  Steam 会按新 manifest 重新落权限。

## 常见错误对照

| 现象 | 原因 |
| --- | --- |
| Missing game executable | 执行位丢失（本文档主题），或 Launch Options 文件名和实际 app 名不一致 |
| "已损坏，无法打开" | app 未签名/未公证且被 Gatekeeper 拦截（Steam 正版下载一般不触发） |
| 闪退无窗口 | 架构不匹配（必须 Universal，构建管线已强制）|
