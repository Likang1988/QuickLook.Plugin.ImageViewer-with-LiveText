using QuickLook.Common.Helpers;

namespace QuickLook.Plugin.ImageViewer.LiveText
{
    /// <summary>
    /// 实况文本功能设置管理
    /// </summary>
    public class LiveTextSettings
    {
        private const string SettingsNamespace = "QuickLook.Plugin.ImageViewer.LiveText";
        
        /// <summary>
        /// 获取或设置实况文本功能是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => SettingHelper.Get("LiveTextEnabled", false, SettingsNamespace);
            set => SettingHelper.Set("LiveTextEnabled", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 获取或设置首选识别语言
        /// </summary>
        public string PreferredLanguage
        {
            get => SettingHelper.Get("LiveTextLanguage", "en", SettingsNamespace);
            set => SettingHelper.Set("LiveTextLanguage", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 获取或设置是否自动检测文本（打开图片时自动运行OCR）
        /// </summary>
        public bool AutoDetectText
        {
            get => SettingHelper.Get("LiveTextAutoDetect", false, SettingsNamespace);
            set => SettingHelper.Set("LiveTextAutoDetect", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 获取或设置是否显示文本边界框
        /// </summary>
        public bool ShowTextBounds
        {
            get => SettingHelper.Get("LiveTextShowBounds", true, SettingsNamespace);
            set => SettingHelper.Set("LiveTextShowBounds", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 获取或设置文本边界框的透明度 (0.0 - 1.0)
        /// </summary>
        public double BoundsOpacity
        {
            get => SettingHelper.Get("LiveTextBoundsOpacity", 0.3, SettingsNamespace);
            set => SettingHelper.Set("LiveTextBoundsOpacity", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 获取或设置选中文本的高亮颜色（ARGB格式）
        /// </summary>
        public uint SelectionColor
        {
            get => SettingHelper.Get("LiveTextSelectionColor", 0x500078D4u, SettingsNamespace);
            set => SettingHelper.Set("LiveTextSelectionColor", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 获取或设置文本边界框颜色（ARGB格式）
        /// </summary>
        public uint BoundsColor
        {
            get => SettingHelper.Get("LiveTextBoundsColor", 0x80ADD8E6u, SettingsNamespace);
            set => SettingHelper.Set("LiveTextBoundsColor", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 获取或设置最小文本置信度阈值
        /// </summary>
        public double MinConfidence
        {
            get => SettingHelper.Get("LiveTextMinConfidence", 0.5, SettingsNamespace);
            set => SettingHelper.Set("LiveTextMinConfidence", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 获取或设置是否启用快捷键
        /// </summary>
        public bool EnableHotkeys
        {
            get => SettingHelper.Get("LiveTextEnableHotkeys", true, SettingsNamespace);
            set => SettingHelper.Set("LiveTextEnableHotkeys", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 获取或设置OCR处理的最大图片尺寸（像素）
        /// </summary>
        public int MaxImageSize
        {
            get => SettingHelper.Get("LiveTextMaxImageSize", 2048, SettingsNamespace);
            set => SettingHelper.Set("LiveTextMaxImageSize", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 获取或设置是否缓存OCR结果
        /// </summary>
        public bool EnableCache
        {
            get => SettingHelper.Get("LiveTextEnableCache", true, SettingsNamespace);
            set => SettingHelper.Set("LiveTextEnableCache", value, SettingsNamespace);
        }
        
        /// <summary>
        /// 重置所有设置为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            IsEnabled = false;
            PreferredLanguage = "en";
            AutoDetectText = false;
            ShowTextBounds = true;
            BoundsOpacity = 0.3;
            SelectionColor = 0x500078D4u;
            BoundsColor = 0x80ADD8E6u;
            MinConfidence = 0.5;
            EnableHotkeys = true;
            MaxImageSize = 2048;
            EnableCache = true;
        }
    }
}