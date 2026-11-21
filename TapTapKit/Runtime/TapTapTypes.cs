using System;
using UnityEngine;

namespace WeChatGame.TapTapKit
{
    /// <summary>TapSDK 的地区配置。用自有枚举是为了在没装 TapSDK 时也能编译。</summary>
    public enum TapTapRegion
    {
        CN = 0,
        Overseas = 1,
    }

    /// <summary>Banner 展示位置。</summary>
    public enum TapTapBannerPosition
    {
        Top = 0,
        Bottom = 1,
    }

    public enum TapTapLoginStatus
    {
        Success = 0,
        Canceled = 1,
        Failed = 2,
    }

    /// <summary>登录返回的账号信息。字段名与 TapTapAccount 保持一致，方便对照。</summary>
    [Serializable]
    public class TapTapAccountInfo
    {
        public string openId;
        public string unionId;
        public string name;
        public string avatar;

        public override string ToString()
        {
            return $"openId={openId}, unionId={unionId}, name={name}";
        }
    }

    public struct TapTapLoginResult
    {
        public TapTapLoginStatus status;
        public TapTapAccountInfo account;
        public string error;

        public static TapTapLoginResult Success(TapTapAccountInfo account)
        {
            return new TapTapLoginResult { status = TapTapLoginStatus.Success, account = account };
        }

        public static TapTapLoginResult Canceled()
        {
            return new TapTapLoginResult { status = TapTapLoginStatus.Canceled, error = "用户取消" };
        }

        public static TapTapLoginResult Failed(string error)
        {
            return new TapTapLoginResult { status = TapTapLoginStatus.Failed, error = error };
        }
    }

    /// <summary>
    /// 平台适配层。真正调用 TapSDK / TapADN 的代码由安装器生成到工程里（Assets/TapTapKitGenerated），
    /// 这样没装 SDK 时整个包依然能编译。
    /// </summary>
    public interface ITapTapAdapter
    {
        bool IsMiniGamePlatform { get; }

        void Init(TapTapConfig config, Action<bool, string> onComplete);

        void Login(string[] scopes, Action<TapTapLoginResult> onComplete);

        void GetCurrentAccount(Action<TapTapAccountInfo> onComplete);

        void Logout();

        void ShowRewardVideo(long spaceId, Action<bool> onComplete);

        void ShowInterstitial(long spaceId, Action<bool> onComplete);

        void ShowBanner(long spaceId, TapTapBannerPosition position, Action<bool> onComplete);

        void HideBanner();
    }

    /// <summary>空实现基类。生成的适配器继承它，只覆盖自己用得到的方法。</summary>
    public class TapTapAdapterBase : ITapTapAdapter
    {
        public virtual bool IsMiniGamePlatform => false;

        public virtual void Init(TapTapConfig config, Action<bool, string> onComplete)
        {
            onComplete?.Invoke(false, "TapTapKit 适配层未安装");
        }

        public virtual void Login(string[] scopes, Action<TapTapLoginResult> onComplete)
        {
            onComplete?.Invoke(TapTapLoginResult.Failed("TapTapKit 适配层未安装"));
        }

        public virtual void GetCurrentAccount(Action<TapTapAccountInfo> onComplete)
        {
            onComplete?.Invoke(null);
        }

        public virtual void Logout()
        {
        }

        public virtual void ShowRewardVideo(long spaceId, Action<bool> onComplete)
        {
            onComplete?.Invoke(false);
        }

        public virtual void ShowInterstitial(long spaceId, Action<bool> onComplete)
        {
            onComplete?.Invoke(false);
        }

        public virtual void ShowBanner(long spaceId, TapTapBannerPosition position, Action<bool> onComplete)
        {
            onComplete?.Invoke(false);
        }

        public virtual void HideBanner()
        {
        }
    }
}
