using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WeChatGame.TapTapKit.EditorTools
{
    /// <summary>
    /// 一键安装器：写 manifest 依赖 / 写定义符号 / 生成适配器与配置文件。
    /// TapADN（Dirichlet）只有 unitypackage，安装器不下载也不导入，只显示安装状态和下载地址。
    /// 所有 SDK 的真实调用都在生成出来的代码里，没装 SDK 时不会编译报错。
    /// </summary>
    public class TapTapKitInstaller : EditorWindow
    {
        const string GeneratedFolder = "Assets/TapTapKitGenerated";
        const string ConfigAssetPath = GeneratedFolder + "/Resources/TapTapConfig.asset";

        const string SymbolSdk = "TAPTAP_SDK";
        const string SymbolAdn = "TAPTAP_ADN";
        const string SymbolMinigame = "TAPTAP_MINIGAME";

        static readonly BuildTargetGroup[] DefineGroups =
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.WebGL,
        };

        // ---------------- 面板状态 ----------------

        string m_sdkVersion = "4.11.1";
        bool m_installSdk = true;
        bool m_installLogin = true;
        bool m_installEduM4U = true;

        bool m_installMinigame;

        // TapADN 官方只提供 unitypackage，安装器不代下载、不代导入，只给地址
        const string AdnVersion = "5.3.3.0";
        const string AdnDownloadPageUrl = "https://ssp.dirichlet.cn/docs/resource-download/";
        const string AdnUnityPackageUrl =
            "https://tapad-platform.tapimg.com/sdk/unity/dirichlet_ad_unity_" + AdnVersion + ".unitypackage";

        // git UPM 的两个包：安装器会写 manifest，这里同时把链接和步骤显示出来，方便手动装
        const string SdkDocUrl = "https://developer.taptap.cn/docs/sdk/integration-guides/unity/";
        const string MinigameGitUrl = "https://github.com/taptap/minigame-sdk-unity.git";
        const string MinigameDocUrl = "https://developer.taptap.cn/minigameapidoc/dev/engine/unity-adaptation/unity-webGL/";

        Vector2 m_scroll;
        readonly List<string> m_log = new List<string>();
        bool m_hasInstalledOnce;

        [MenuItem("Tools/TapTapKit/安装器")]
        static void Open()
        {
            var window = GetWindow<TapTapKitInstaller>("TapTapKit 安装器");
            window.minSize = new Vector2(520, 520);
            window.Show();
        }

        [MenuItem("Tools/TapTapKit/选中配置文件")]
        static void SelectConfig()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TapTapConfig>(ConfigAssetPath);
            if (asset == null)
            {
                Debug.LogWarning("[TapTapKit] 还没有配置文件，请先运行安装器");
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        void OnGUI()
        {
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

            EditorGUILayout.LabelField("TapTapKit 安装器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "安装器只改这几样东西：Packages/manifest.json 里的依赖、Player Settings 的 Scripting Define Symbols、" +
                "以及生成目录 " + GeneratedFolder + "。\n" +
                "没装某个 SDK 时，对应代码不会参与编译，工程不会报错。",
                MessageType.Info);

            DrawStatus();
            EditorGUILayout.Space();

            DrawTapSdkSection();
            EditorGUILayout.Space();

            DrawTapAdnSection();
            EditorGUILayout.Space();

            DrawMinigameSection();
            EditorGUILayout.Space();

            DrawActions();
            EditorGUILayout.Space();

            DrawLog();

            EditorGUILayout.EndScrollView();
        }

        void DrawStatus()
        {
            EditorGUILayout.LabelField("安装状态", EditorStyles.boldLabel);

            bool hasSdk = HasType("TapSDK.Core.TapTapSDK");
            bool hasLogin = HasType("TapSDK.Login.TapTapLogin");
            bool hasAdn = HasType("Dirichlet.Ad.DirichletAdSdk");
            bool hasMinigame = HasDependency("com.taptap.minigame");

            // TapSDK
            if (!hasSdk)
            {
                EditorGUILayout.HelpBox("TapSDK 未安装：勾选下面第 1 项后点『一键安装 / 更新』。", MessageType.Error);
            }
            else if (!HasSymbol(SymbolSdk))
            {
                EditorGUILayout.HelpBox("TapSDK 已安装，但定义符号 " + SymbolSdk + " 还没写入。点『检测 SDK 并写入定义符号』。", MessageType.Warning);
            }
            else if (!hasLogin)
            {
                EditorGUILayout.HelpBox("TapSDK 安装完成，但没有 Login 模块（无法登录）。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("TapSDK 安装完成", MessageType.Info);
            }

            // TapADN
            if (!hasAdn)
            {
                EditorGUILayout.HelpBox(
                    "TapADN 未安装。\n请到 " + AdnDownloadPageUrl + " 下载 dirichlet_ad_unity_" + AdnVersion +
                    ".unitypackage，拖进 Unity 导入。\n导入编译完成后这里会自动变成『TapADN 安装完成』。",
                    MessageType.Error);
            }
            else if (!HasSymbol(SymbolAdn))
            {
                EditorGUILayout.HelpBox("TapADN 已导入，但定义符号 " + SymbolAdn + " 还没写入。点『检测 SDK 并写入定义符号』。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("TapADN 安装完成", MessageType.Info);
            }

            // 小游戏
            if (hasMinigame && !HasSymbol(SymbolMinigame))
            {
                EditorGUILayout.HelpBox("TapTap 小游戏插件已在 manifest 中，但符号 " + SymbolMinigame + " 还没写入。点『检测 SDK 并写入定义符号』。", MessageType.Warning);
            }
            else if (hasMinigame)
            {
                EditorGUILayout.HelpBox("TapTap 小游戏插件安装完成", MessageType.Info);
            }

            if (!File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "../" + GeneratedFolder + "/TapTapKitAdapter.cs"))))
            {
                EditorGUILayout.HelpBox("适配器代码还没生成，点『一键安装 / 更新』或『只重新生成代码』。", MessageType.Warning);
            }
        }

        void DrawTapSdkSection()
        {
            EditorGUILayout.LabelField("1. TapSDK（登录 / 初始化，git UPM）", EditorStyles.boldLabel);
            m_installSdk = EditorGUILayout.ToggleLeft("一键写入 manifest 依赖（Core + Login）", m_installSdk);
            using (new EditorGUI.DisabledScope(!m_installSdk))
            {
                EditorGUI.indentLevel++;
                m_sdkVersion = EditorGUILayout.TextField("版本号", m_sdkVersion);
                m_installLogin = EditorGUILayout.Toggle("同时添加 Login 模块", m_installLogin);
                m_installEduM4U = EditorGUILayout.Toggle("同时添加 EDM4U（Android 必需）", m_installEduM4U);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("git UPM 链接（手动安装用）", EditorStyles.miniBoldLabel);
            DrawUrlRow(SdkGitUrl("Core"));
            if (m_installLogin)
            {
                DrawUrlRow(SdkGitUrl("Login"));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("复制 Core 链接"))
                {
                    CopyUrl(SdkGitUrl("Core"));
                }

                if (GUILayout.Button("复制 Login 链接"))
                {
                    CopyUrl(SdkGitUrl("Login"));
                }

                if (GUILayout.Button("打开官方文档"))
                {
                    Application.OpenURL(SdkDocUrl);
                }
            }

            EditorGUILayout.HelpBox(
                "安装步骤：\n" +
                "1. Window > Package Manager > 左上角 + > Add package from git URL，粘贴 Core 链接，等导入完成。\n" +
                "2. 需要登录就再粘一次 Login 链接。\n" +
                "3. Android 出包还要 EDM4U（OpenUPM 的 com.google.external-dependency-manager 1.2.179）；勾上上面的选项，安装器会补 scopedRegistry 和依赖。\n" +
                "4. 等 Unity 编译完，点『检测 SDK 并写入定义符号』，上面的状态变成『TapSDK 安装完成』。\n" +
                "GitHub 拉不动时：把仓库下到本地，用 Add package from disk 选 Core 目录里的 package.json。",
                MessageType.None);
        }

        void DrawTapAdnSection()
        {
            EditorGUILayout.LabelField("2. TapADN（广告，Dirichlet SDK，只有 unitypackage）", EditorStyles.boldLabel);
            DrawUrlRow(AdnUnityPackageUrl);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开下载页"))
                {
                    Application.OpenURL(AdnDownloadPageUrl);
                }

                if (GUILayout.Button("复制下载链接"))
                {
                    CopyUrl(AdnUnityPackageUrl);
                }
            }

            EditorGUILayout.HelpBox(
                "安装步骤：\n" +
                "1. 下载 dirichlet_ad_unity_" + AdnVersion + ".unitypackage（用上面链接，或点『打开下载页』）。\n" +
                "2. 双击文件，或把它拖进 Unity 的 Project 窗口，弹出导入窗口后点 Import。\n" +
                "3. 导入后工程里会出现 Assets/DirichletAd 和 Assets/Plugins/Android/DirichletAd。\n" +
                "4. 等编译完，点『检测 SDK 并写入定义符号』，上面的状态变成『TapADN 安装完成』。\n" +
                "5. 出包前删掉 Assets/DirichletAd/Samples（示例场景会被打进包）。",
                MessageType.None);
        }

        void DrawMinigameSection()
        {
            EditorGUILayout.LabelField("3. TapTap 小游戏（WebGL 导出插件，git UPM）", EditorStyles.boldLabel);
            m_installMinigame = EditorGUILayout.ToggleLeft("一键写入 manifest 依赖（com.taptap.minigame）", m_installMinigame);

            DrawUrlRow(MinigameGitUrl);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("复制链接"))
                {
                    CopyUrl(MinigameGitUrl);
                }

                if (GUILayout.Button("打开官方文档"))
                {
                    Application.OpenURL(MinigameDocUrl);
                }
            }

            EditorGUILayout.HelpBox(
                "安装步骤：\n" +
                "1. Window > Package Manager > + > Add package from git URL，粘贴上面的链接。\n" +
                "2. 导入成功后菜单栏出现「TapTap 小游戏」，打包走「TapTap 小游戏 > 构建」。\n" +
                "3. 点『检测 SDK 并写入定义符号』，状态变成『TapTap 小游戏插件安装完成』。\n" +
                "要求 Unity 2022+（或团结引擎 1.2+）；不能和微信小游戏 / minihost SDK 共存；只支持 WebGL，且不支持多线程。",
                MessageType.None);
        }

        void DrawActions()
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            if (GUILayout.Button("一键安装 / 更新", GUILayout.Height(34)))
            {
                InstallAll();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("检测 SDK 并写入定义符号"))
                {
                    m_log.Clear();
                    DetectAndWriteSymbols(true);
                }

                if (GUILayout.Button("只重新生成代码"))
                {
                    m_log.Clear();
                    GenerateCode();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("选中配置文件"))
                {
                    SelectConfig();
                }

                if (GUILayout.Button("打开生成目录"))
                {
                    EditorUtility.RevealInFinder(Path.GetFullPath(Path.Combine(Application.dataPath, "../" + GeneratedFolder)));
                }
            }
        }

        void DrawLog()
        {
            EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
            if (m_log.Count == 0)
            {
                EditorGUILayout.HelpBox("还没有执行操作。", MessageType.None);
                return;
            }

            for (int i = m_log.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.LabelField(m_log[i], EditorStyles.wordWrappedMiniLabel);
            }
        }

        // ---------------- 安装流程 ----------------

        void InstallAll()
        {
            m_log.Clear();
            Log("=== 开始安装 ===");

            try
            {
                if (m_installSdk)
                {
                    EnsureDependency("com.taptap.sdk.core", SdkGitUrl("Core"));
                    if (m_installLogin)
                    {
                        EnsureDependency("com.taptap.sdk.login", SdkGitUrl("Login"));
                    }

                    EnsureDependency("com.unity.nuget.newtonsoft-json", "3.2.2");

                    if (m_installEduM4U)
                    {
                        EnsureScopedRegistry("package.openupm.com", "https://package.openupm.com", "com.google");
                        EnsureDependency("com.google.external-dependency-manager", "1.2.179");
                    }
                }

                if (m_installMinigame)
                {
                    EnsureDependency("com.taptap.minigame", MinigameGitUrl);
                }

                if (!HasType("Dirichlet.Ad.DirichletAdSdk"))
                {
                    Log("TapADN 未安装，需要手动导入 unitypackage：" + AdnUnityPackageUrl);
                }
            }
            catch (Exception e)
            {
                Log("安装过程出错：" + e.Message);
                Debug.LogException(e);
            }

            AssetDatabase.Refresh();
            DetectAndWriteSymbols(true);
            GenerateCode();

            if (!m_hasInstalledOnce)
            {
                m_hasInstalledOnce = true;
                Log("若 manifest 刚被修改，Unity 需要先解析依赖并重新编译，完成后请再点一次『检测 SDK 并写入定义符号』。");
            }

            Log("=== 结束 ===");
        }

        void DetectAndWriteSymbols(bool verbose)
        {
            bool hasSdk = HasType("TapSDK.Core.TapTapSDK");
            bool hasAdn = HasType("Dirichlet.Ad.DirichletAdSdk");
            bool hasMinigame = HasDependency("com.taptap.minigame");

            int changed = 0;
            changed += SetSymbol(SymbolSdk, hasSdk) ? 1 : 0;
            changed += SetSymbol(SymbolAdn, hasAdn) ? 1 : 0;
            changed += SetSymbol(SymbolMinigame, hasMinigame) ? 1 : 0;

            if (verbose)
            {
                Log($"符号检测：{SymbolSdk}={hasSdk}  {SymbolAdn}={hasAdn}  {SymbolMinigame}={hasMinigame}");
                Log(changed > 0 ? "定义符号已更新，等待 Unity 重新编译。" : "定义符号没有变化。");
            }
        }

        void GenerateCode()
        {
            try
            {
                EnsureFolder(GeneratedFolder);
                WriteIfChanged(GeneratedFolder + "/TapTapKitAdapter.cs", LoadTemplate("TapTapKitAdapter.cs.txt"));
                WriteIfChanged(GeneratedFolder + "/TapTapKitBootstrap.cs", LoadTemplate("TapTapKitBootstrap.cs.txt"));
                EnsureConfigAsset();
                AssetDatabase.Refresh();
                Log("代码生成完成：" + GeneratedFolder);
            }
            catch (Exception e)
            {
                Log("代码生成失败：" + e.Message);
                Debug.LogException(e);
            }
        }

        void EnsureConfigAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<TapTapConfig>(ConfigAssetPath) != null)
            {
                return;
            }

            EnsureFolder(GeneratedFolder + "/Resources");
            var config = ScriptableObject.CreateInstance<TapTapConfig>();
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            Log("已创建配置文件：" + ConfigAssetPath + "（填 clientId / clientToken / mediaId / 广告位 ID）");
        }

        static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            string absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + assetFolder));
            Directory.CreateDirectory(absolute);
            AssetDatabase.Refresh();
            if (!AssetDatabase.IsValidFolder(assetFolder))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(assetFolder)?.Replace('\\', '/'), Path.GetFileName(assetFolder));
            }
        }

        static void WriteIfChanged(string assetPath, string content)
        {
            string absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + assetPath));
            if (File.Exists(absolute) && File.ReadAllText(absolute) == content)
            {
                return;
            }

            File.WriteAllText(absolute, content);
        }

        static string LoadTemplate(string fileName)
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TapTapKitInstaller).Assembly);
            string root = info != null ? info.resolvedPath : "Packages/com.wechatgame.taptapkit";
            string path = Path.Combine(root, "Editor~", "Templates", fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("找不到模板文件：" + path);
            }

            return File.ReadAllText(path);
        }

        string SdkGitUrl(string module)
        {
            return $"https://github.com/taptap/tapsdk-unity-dist.git?path=/{module}#{m_sdkVersion}";
        }

        // ---------------- manifest 读写 ----------------

        static string ManifestPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages/manifest.json"));

        static bool HasDependency(string packageName)
        {
            string path = ManifestPath;
            return File.Exists(path) && File.ReadAllText(path).Contains("\"" + packageName + "\"");
        }

        void EnsureDependency(string packageName, string versionOrUrl)
        {
            string path = ManifestPath;
            string text = File.ReadAllText(path);
            if (text.Contains("\"" + packageName + "\""))
            {
                Log("依赖已存在，跳过：" + packageName);
                return;
            }

            int depIndex = text.IndexOf("\"dependencies\"", StringComparison.Ordinal);
            if (depIndex < 0)
            {
                throw new Exception("manifest.json 里找不到 dependencies 节点");
            }

            int brace = text.IndexOf('{', depIndex);
            text = text.Insert(brace + 1, $"\n    \"{packageName}\": \"{versionOrUrl}\",");
            File.WriteAllText(path, text);
            Log("已添加依赖：" + packageName);
        }

        void EnsureScopedRegistry(string name, string url, params string[] scopes)
        {
            string path = ManifestPath;
            string text = File.ReadAllText(path);
            if (text.Contains("\"scopedRegistries\"") && text.Contains("\"" + url + "\""))
            {
                Log("scopedRegistry 已存在，跳过：" + url);
                return;
            }

            string scopeList = string.Join(", ", scopes.Select(s => "\"" + s + "\""));
            string entry =
                "\n    {\n" +
                $"      \"name\": \"{name}\",\n" +
                $"      \"url\": \"{url}\",\n" +
                $"      \"scopes\": [{scopeList}]\n" +
                "    }";

            int regIndex = text.IndexOf("\"scopedRegistries\"", StringComparison.Ordinal);
            if (regIndex >= 0)
            {
                int open = text.IndexOf('[', regIndex);
                int close = FindMatching(text, open, '[', ']');
                string inner = text.Substring(open + 1, close - open - 1).Trim();
                text = text.Insert(close, (inner.Length == 0 ? "" : ",") + entry + "\n  ");
            }
            else
            {
                int lastBrace = text.LastIndexOf('}');
                text = text.Insert(lastBrace, ",\n  \"scopedRegistries\": [" + entry + "\n  ]\n");
            }

            File.WriteAllText(path, text);
            Log("已添加 scopedRegistry：" + url);
        }

        static int FindMatching(string text, int openIndex, char openChar, char closeChar)
        {
            int depth = 0;
            bool inString = false;
            for (int i = openIndex; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\\' && inString)
                {
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                {
                    continue;
                }

                if (c == openChar)
                {
                    depth++;
                }
                else if (c == closeChar)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            throw new Exception("manifest.json 括号不匹配");
        }

        // ---------------- 定义符号 ----------------

        static bool HasSymbol(string symbol)
        {
            string defines;
            try
            {
                defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            }
            catch (Exception)
            {
                return false;
            }

            return defines.Split(';').Contains(symbol);
        }

        static bool SetSymbol(string symbol, bool enabled)
        {
            bool changed = false;
            var groups = DefineGroups.ToList();
            BuildTargetGroup current = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            if (current != BuildTargetGroup.Unknown && !groups.Contains(current))
            {
                groups.Add(current);
            }

            foreach (BuildTargetGroup group in groups)
            {
                string defines;
                try
                {
                    defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                }
                catch (Exception)
                {
                    continue;
                }

                var list = defines.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();
                bool has = list.Contains(symbol);
                if (has == enabled)
                {
                    continue;
                }

                if (enabled)
                {
                    list.Add(symbol);
                }
                else
                {
                    list.Remove(symbol);
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
                changed = true;
            }

            return changed;
        }

        // ---------------- 工具 ----------------

        void DrawUrlRow(string url)
        {
            EditorGUILayout.SelectableLabel(url, EditorStyles.textField, GUILayout.Height(18));
        }

        void CopyUrl(string url)
        {
            GUIUtility.systemCopyBuffer = url;
            Log("已复制：" + url);
        }

        static bool HasType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.GetType(fullName, false) != null)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // 某些动态程序集查询会抛异常，忽略即可
                }
            }

            return false;
        }

        void Log(string message)
        {
            m_log.Add(message);
            Debug.Log("[TapTapKit] " + message);
            Repaint();
        }
    }
}
