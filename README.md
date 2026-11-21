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

## 变更历史

- TapTapKit 原位于 `WechatGame/Shared`（仓库 `WeChatGameShareLib`）内，现迁至本仓库统一维护，原仓库对应文件已删除。
