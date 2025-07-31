using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ImageViewer.LiveText
{
    /// <summary>
    /// OCR引擎接口，定义文本识别的基本功能
    /// </summary>
    public interface IOcrEngine
    {
        /// <summary>
        /// 异步识别图片中的文本
        /// </summary>
        /// <param name="image">要识别的图片</param>
        /// <param name="language">识别语言，默认为英语</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>识别到的文本区域列表</returns>
        Task<List<TextRegion>> RecognizeTextAsync(BitmapSource image, string language = "en", CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取OCR引擎是否可用
        /// </summary>
        bool IsAvailable { get; }
        
        /// <summary>
        /// 异步检查OCR引擎是否可用
        /// </summary>
        /// <returns>是否可用</returns>
        Task<bool> IsAvailableAsync();
        
        /// <summary>
        /// 获取支持的语言列表
        /// </summary>
        List<string> SupportedLanguages { get; }
        
        /// <summary>
        /// 获取引擎名称
        /// </summary>
        string EngineName { get; }
    }
}