# taoPackage

跨工程共享的本地 UPM 包仓库（Unity），各游戏工程通过 `Packages/manifest.json` 的 `file:` 相对路径原地引用，Unity 不会拷贝进 `Library/PackageCache`，改一处所有工程生效。

## 包列表

| 包名 | 目录 | 说明 |
|---|---|---|
| `com.wechatgame.taptapkit` | `TapTapKit/` | TapTap（TapSDK / 小游戏）接入工具包：运行时管理器、配置与编辑器安装器 |

## 引用方式

`file:` 相对路径的基准是工程的 `Packages` 目录，路径要按工程与本仓库的相对位置写：

```json
// D:\git_workSpace\MirrorWaveGame\Packages\manifest.json
"com.wechatgame.taptapkit": "file:../../taoPackage/TapTapKit"

// D:\git_workSpace\WechatGame\RougeLikeGame\Packages\manifest.json
"com.wechatgame.taptapkit": "file:../../../taoPackage/TapTapKit"
```

## 工具脚本

`Tools/` 下是给 Unity 工程做一次性配置的脚本，直接对目标工程的 `Packages/manifest.json` 动手，改前自动备份为 `manifest.json.bak`，可重复执行（幂等）。

| 脚本 | 作用 |
|---|---|
| `Install-AIGameDevMcp.ps1` | 给工程装上 AI Game Developer（`com.ivanmurzak.unity.mcp`）：加依赖 + 加 OpenUPM scoped registry（`com.ivanmurzak` / `extensions.unity`） |
| `serve_webgl.py` | 本地预览 Unity WebGL 构建（处理 `.gz` / `.br` 的 `Content-Encoding`，普通静态服务器会白屏） |

```powershell
.\Tools\Install-AIGameDevMcp.ps1 -ProjectPath D:\git_workSpace\新工程
```

装完后打开 Unity，`Window/AI Game Dev`（`Ctrl+Alt+A`）出现即成功；窗口里的 `Server URL` 要写进本机 `~/.codebuddy/mcp.json` 的 `ai-game-developer.url`（端口按工程目录生成，每个工程不同）。

### WebGL 本地预览

```powershell
python .\Tools\serve_webgl.py D:\git_workSpace\MirrorWaveGame\Builds\WebGL --port 8700
```

要点：Unity 用 Gzip/Brotli 压缩时产物是 `*.wasm.gz` / `*.data.gz`，必须由服务端返回 `Content-Encoding: gzip` / `br`，否则浏览器解析失败（表现为一直卡加载）。脚本还会按内层扩展名返回正确的 MIME（`.wasm` → `application/wasm`）并禁用缓存，重新构建后刷新即可看到新版本。

工程侧可挂在自己的构建脚本里，例如 MirrorWaveGame 的 `Tools/MirrorWave/构建并本地预览` 菜单：构建完成后自动拉起本脚本并打开浏览器。

## 变更历史

- TapTapKit 原位于 `WechatGame/Shared`（仓库 `WeChatGameShareLib`）内，现迁至本仓库统一维护，原仓库对应文件已删除。
- 新增 `Tools/Install-AIGameDevMcp.ps1`（AI Game Developer MCP 一键安装脚本）。
