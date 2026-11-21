<#
.SYNOPSIS
给任意 Unity 工程一键装上 AI Game Developer（AI-GameDevelop，包名 com.ivanmurzak.unity.mcp）。

.DESCRIPTION
做两件事，都是改 Packages/manifest.json：
  1) 在 dependencies 里加 "com.ivanmurzak.unity.mcp": "<版本>"
  2) 加 OpenUPM scoped registry（scopes 必须含 com.ivanmurzak 和 extensions.unity，
     否则主包依赖的 extensions.unity.playerprefsex 解析不到）
已经存在的内容会跳过（可重复执行）。改之前会把原文件备份成 manifest.json.bak。

.EXAMPLE
.\Install-AIGameDevMcp.ps1 -ProjectPath D:\git_workSpace\MirrorWaveGame

.EXAMPLE
.\Install-AIGameDevMcp.ps1 -ProjectPath D:\git_workSpace\NewGame -PackageVersion 1.12.0
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [string]$PackageVersion = "1.12.0"
)

$manifestPath = Join-Path $ProjectPath "Packages\manifest.json"
if (-not (Test-Path $manifestPath))
{
    Write-Host "[错误] 找不到 $manifestPath" -ForegroundColor Red
    Write-Host "       -ProjectPath 要指向 Unity 工程根目录（含 Assets、Packages 的那一层）" -ForegroundColor Red
    exit 1
}

$text = Get-Content $manifestPath -Raw -Encoding UTF8
$changed = $false

# 1) 依赖
if ($text -match '"com\.ivanmurzak\.unity\.mcp"\s*:')
{
    Write-Host "[跳过] manifest 里已有 com.ivanmurzak.unity.mcp" -ForegroundColor DarkGray
}
else
{
    $anchor = '"dependencies": {'
    if (-not $text.Contains($anchor))
    {
        Write-Host "[错误] manifest 里找不到 $anchor，格式不符合预期，脚本不做猜测，请手工添加" -ForegroundColor Red
        exit 1
    }
    $line = '    "com.ivanmurzak.unity.mcp": "' + $PackageVersion + '",'
    $text = $text.Replace($anchor, $anchor + "`r`n" + $line)
    $changed = $true
    Write-Host "[添加] com.ivanmurzak.unity.mcp $PackageVersion"
}

# 2) OpenUPM scoped registry
if ($text -match '"scopedRegistries"')
{
    if ($text -match '"com\.ivanmurzak"' -and $text -match '"extensions\.unity"')
    {
        Write-Host "[跳过] scopedRegistries 已包含 com.ivanmurzak / extensions.unity" -ForegroundColor DarkGray
    }
    else
    {
        Write-Host "[错误] manifest 里已有 scopedRegistries，但缺少 com.ivanmurzak 或 extensions.unity 的 scope" -ForegroundColor Red
        Write-Host "       脚本不自动合并已有 registry，请手工检查 $manifestPath" -ForegroundColor Red
        exit 2
    }
}
else
{
    $block = @(
        '  "scopedRegistries": ['
        '    {'
        '      "name": "package.openupm.com",'
        '      "url": "https://package.openupm.com",'
        '      "scopes": ['
        '        "com.ivanmurzak",'
        '        "extensions.unity"'
        '      ]'
        '    }'
        '  ]'
    ) -join "`r`n"

    $close = $text.LastIndexOf('}')
    if ($close -lt 0)
    {
        Write-Host "[错误] manifest 结尾找不到 }，格式不符合预期" -ForegroundColor Red
        exit 1
    }
    $head = $text.Substring(0, $close).TrimEnd()
    $tail = $text.Substring($close)
    $text = $head + ",`r`n" + $block + "`r`n" + $tail
    $changed = $true
    Write-Host "[添加] OpenUPM scoped registry（com.ivanmurzak / extensions.unity）"
}

if (-not $changed)
{
    Write-Host "[完成] 无需修改，工程已经装好了" -ForegroundColor Green
    exit 0
}

# 写回前先校验 JSON，避免把工程写坏
try
{
    $null = $text | ConvertFrom-Json
}
catch
{
    Write-Host "[错误] 改动后的 JSON 不合法，已放弃写入：$($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Copy-Item $manifestPath "$manifestPath.bak" -Force
[System.IO.File]::WriteAllText($manifestPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "[完成] 已写入 $manifestPath（原文件备份为 manifest.json.bak）" -ForegroundColor Green
Write-Host "下一步：" -ForegroundColor Yellow
Write-Host "  1. 打开 Unity，等 Package Manager 解析完（会自动下载包和 extensions.unity.playerprefsex）"
Write-Host "  2. 菜单 Window/AI Game Dev（Ctrl+Alt+A）确认窗口出现"
Write-Host "  3. 窗口里 Connection 的 Server URL 复制出来，覆盖 C:\Users\<你>\.codebuddy\mcp.json 的 ai-game-developer.url"
