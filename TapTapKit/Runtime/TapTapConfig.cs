using UnityEngine;

namespace WeChatGame.TapTapKit
{
    /// <summary>
    /// 全部可配置项。安装器会把它生成到 Assets/TapTapKitGenerated/Resources/TapTapConfig.asset，
    /// 运行时用 Resources.Load 读取，改数值不需要改代码。
    /// </summary>
    [CreateAssetMenu(menuName = "TapTapKit/TapTap Config", fileName = "TapTapConfig")]
    public class TapTapConfig : ScriptableObject
    {
        [Header("TapSDK（开发者后台的 Client ID / Client Token）")]
        public string clientId = "";
        public string clientToken = "";
        public TapTapRegion region = TapTapRegion.CN;
        public bool enableLog = true;

        [Header("TapADN（Dirichlet 广告联盟）")]
        [Tooltip("后台的 MediaId，数字类型")]
        public long mediaId;
        public string mediaKey = "";
        [Tooltip("开发期打开，发布包务必关闭")]
        public bool adnDebug = false;
        [Tooltip("iOS 的 IDFA 授权开关，Android 忽略")]
        public bool allowIdfaAccess = true;
        [Tooltip("摇一摇交互开关")]
        public bool shakeEnabled = true;

        [Header("广告位 ID（SpaceId，后台的纯数字，填 0 表示不展示对应广告）")]
        public long rewardVideoSpaceId;
        public long interstitialSpaceId;
        public long bannerSpaceId;
        public TapTapBannerPosition bannerPosition = TapTapBannerPosition.Bottom;

        [Header("行为开关")]
        [Tooltip("启动时自动初始化（有配置时才生效）")]
        public bool autoInitialize = true;
    }
}
