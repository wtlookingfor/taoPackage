using System;
using UnityEngine;

namespace WeChatGame.TapTapKit
{
    /// <summary>
    /// 游戏侧唯一入口。不需要往场景里放预制体，启动时自动创建。
    /// 真正的 SDK 调用在安装器生成的适配器里，通过 RegisterAdapter 注入。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TapTapManager : MonoBehaviour
    {
        public const string ConfigResourcePath = "TapTapConfig";

        static TapTapManager s_instance;
        static ITapTapAdapter s_adapter;

        public static TapTapManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    CreateInstance();
                }

                return s_instance;
            }
        }

        /// <summary>由生成的 Bootstrap 调用。没注册时使用空实现，所有接口安全返回失败。</summary>
        public static void RegisterAdapter(ITapTapAdapter adapter)
        {
            s_adapter = adapter;
            Debug.Log($"[TapTapKit] 适配器 = {adapter?.GetType().FullName ?? "null"}");
        }

        public static ITapTapAdapter Adapter
        {
            get
            {
                if (s_adapter == null)
                {
                    s_adapter = new TapTapAdapterBase();
                }

                return s_adapter;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoCreate()
        {
            CreateInstance();
        }

        static void CreateInstance()
        {
            if (s_instance != null)
            {
                return;
            }

            var go = new GameObject("[TapTapManager]");
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<TapTapManager>();
        }

        public TapTapConfig Config { get; private set; }
        public bool IsInitialized { get; private set; }
        public TapTapAccountInfo Account { get; private set; }
        public bool IsMiniGamePlatform => Adapter.IsMiniGamePlatform;

        public event Action<bool, string> InitializeFinished;

        void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
            Config = Resources.Load<TapTapConfig>(ConfigResourcePath);
            if (Config == null)
            {
                Debug.LogWarning($"[TapTapKit] 找不到 Resources/{ConfigResourcePath}.asset，请先运行 Tools/TapTapKit/安装器");
            }
        }

        void Start()
        {
            if (Config != null && Config.autoInitialize)
            {
                Init();
            }
        }

        public void Init(Action<bool, string> onComplete = null)
        {
            if (IsInitialized)
            {
                onComplete?.Invoke(true, null);
                return;
            }

            if (Config == null)
            {
                onComplete?.Invoke(false, "找不到 TapTapConfig");
                return;
            }

            Adapter.Init(Config, (ok, error) =>
            {
                IsInitialized = ok;
                if (ok)
                {
                    Debug.Log("[TapTapKit] 初始化成功");
                }
                else
                {
                    Debug.LogWarning($"[TapTapKit] 初始化失败：{error}");
                }

                onComplete?.Invoke(ok, error);
                InitializeFinished?.Invoke(ok, error);
            });
        }

        public void Login(Action<TapTapLoginResult> onComplete)
        {
            Adapter.Login(null, result =>
            {
                if (result.status == TapTapLoginStatus.Success)
                {
                    Account = result.account;
                    Debug.Log($"[TapTapKit] 登录成功 {Account}");
                }
                else
                {
                    Debug.LogWarning($"[TapTapKit] 登录结束：{result.status} {result.error}");
                }

                onComplete?.Invoke(result);
            });
        }

        public void Login(string[] scopes, Action<TapTapLoginResult> onComplete)
        {
            Adapter.Login(scopes, onComplete);
        }

        public void GetCurrentAccount(Action<TapTapAccountInfo> onComplete)
        {
            Adapter.GetCurrentAccount(account =>
            {
                if (account != null)
                {
                    Account = account;
                }

                onComplete?.Invoke(account);
            });
        }

        public void Logout()
        {
            Account = null;
            Adapter.Logout();
        }

        public void ShowRewardVideo(Action<bool> onComplete, long spaceId = 0)
        {
            long id = spaceId > 0 ? spaceId : (Config != null ? Config.rewardVideoSpaceId : 0);
            if (id <= 0)
            {
                Debug.LogWarning("[TapTapKit] 激励视频没有配置广告位 SpaceId");
                onComplete?.Invoke(false);
                return;
            }

            Adapter.ShowRewardVideo(id, onComplete);
        }

        public void ShowInterstitial(Action<bool> onComplete = null, long spaceId = 0)
        {
            long id = spaceId > 0 ? spaceId : (Config != null ? Config.interstitialSpaceId : 0);
            if (id <= 0)
            {
                Debug.LogWarning("[TapTapKit] 插屏没有配置广告位 SpaceId");
                onComplete?.Invoke(false);
                return;
            }

            Adapter.ShowInterstitial(id, onComplete);
        }

        public void ShowBanner(Action<bool> onComplete = null, long spaceId = 0)
        {
            long id = spaceId > 0 ? spaceId : (Config != null ? Config.bannerSpaceId : 0);
            if (id <= 0)
            {
                Debug.LogWarning("[TapTapKit] Banner 没有配置广告位 SpaceId");
                onComplete?.Invoke(false);
                return;
            }

            TapTapBannerPosition pos = Config != null ? Config.bannerPosition : TapTapBannerPosition.Bottom;
            Adapter.ShowBanner(id, pos, onComplete);
        }

        public void HideBanner()
        {
            Adapter.HideBanner();
        }
    }
}
