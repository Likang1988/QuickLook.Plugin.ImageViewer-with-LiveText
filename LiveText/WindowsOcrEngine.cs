using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace QuickLook.Plugin.ImageViewer.LiveText
{
    /// <summary>
    /// Windows 内置OCR引擎实现
    /// </summary>
    public class WindowsOcrEngine : IOcrEngine
    {
        private OcrEngine _engine;
        private readonly List<string> _supportedLanguages;
        
        public string EngineName => "Windows OCR";
        
        public bool IsAvailable { get; private set; }
        
        public List<string> SupportedLanguages => _supportedLanguages;
        
        public WindowsOcrEngine()
        {
            _supportedLanguages = new List<string>();
            _ = InitializeAsync();
        }
        
        public async Task<bool> IsAvailableAsync()
        {
            if (IsAvailable)
                return true;
                
            await InitializeAsync().ConfigureAwait(false);
            return IsAvailable;
        }
        
        private Task InitializeAsync()
        {
            return Task.Run(() =>
        {
            try
            {
                // 检查是否支持OCR
                var availableLanguages = OcrEngine.AvailableRecognizerLanguages;
                if (availableLanguages.Any())
                {
                    // 使用系统默认语言或英语
                    var defaultLanguage = availableLanguages.FirstOrDefault(lang => 
                        lang.LanguageTag.StartsWith("en")) ?? availableLanguages.First();
                    
                    _engine = OcrEngine.TryCreateFromLanguage(defaultLanguage);
                    
                    if (_engine != null)
                    {
                        IsAvailable = true;
                        _supportedLanguages.AddRange(availableLanguages.Select(lang => lang.LanguageTag));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Windows OCR初始化失败: {ex.Message}");
                IsAvailable = false;
            }
            });
        }
        
        public async Task<List<TextRegion>> RecognizeTextAsync(BitmapSource image, string language = "en", CancellationToken cancellationToken = default)
        {
            if (!IsAvailable || image == null)
            {
                return new List<TextRegion>();
            }
            
            try
            {
                // 如果指定了不同的语言，尝试切换OCR引擎
                if (!string.IsNullOrEmpty(language) && _supportedLanguages.Contains(language))
                {
                    var targetLanguage = OcrEngine.AvailableRecognizerLanguages
                        .FirstOrDefault(lang => lang.LanguageTag == language);
                    
                    if (targetLanguage != null)
                    {
                        var newEngine = OcrEngine.TryCreateFromLanguage(targetLanguage);
                        if (newEngine != null)
                        {
                            _engine = newEngine;
                        }
                    }
                }
                
                // 转换BitmapSource为SoftwareBitmap
                var softwareBitmap = await ConvertToSoftwareBitmapAsync(image);
                if (softwareBitmap == null)
                {
                    return new List<TextRegion>();
                }
                
                // 执行OCR识别
                cancellationToken.ThrowIfCancellationRequested();
                var result = await _engine.RecognizeAsync(softwareBitmap);
                
                // 转换结果为TextRegion列表
                return ConvertOcrResult(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OCR识别失败: {ex.Message}");
                return new List<TextRegion>();
            }
        }
        
        private async Task<SoftwareBitmap> ConvertToSoftwareBitmapAsync(BitmapSource bitmapSource)
        {
            try
            {
                // 将BitmapSource转换为字节数组
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));
                
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;
                    
                    // 创建IRandomAccessStream
                    var randomAccessStream = stream.AsRandomAccessStream();
                    
                    // 创建BitmapDecoder
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(randomAccessStream);
                    
                    // 获取SoftwareBitmap
                    var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                    
                    return softwareBitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"图片转换失败: {ex.Message}");
                return null;
            }
        }
        
        private List<TextRegion> ConvertOcrResult(OcrResult result)
        {
            var regions = new List<TextRegion>();
            
            if (result?.Lines == null)
            {
                return regions;
            }
            
            int lineIndex = 0;
            foreach (var line in result.Lines)
            {
                int wordIndex = 0;
                foreach (var word in line.Words)
                {
                    var boundingRect = word.BoundingRect;
                    var textRegion = new TextRegion
                    {
                        Text = word.Text,
                        BoundingBox = new Rect(
                            boundingRect.X, 
                            boundingRect.Y, 
                            boundingRect.Width, 
                            boundingRect.Height),
                        Confidence = 1.0, // Windows OCR不提供置信度信息
                        LineIndex = lineIndex,
                        WordIndex = wordIndex
                    };
                    
                    // 设置角点坐标（矩形的四个角）
                    textRegion.Corners = new List<Point>
                    {
                        new Point(boundingRect.Left, boundingRect.Top),
                        new Point(boundingRect.Right, boundingRect.Top),
                        new Point(boundingRect.Right, boundingRect.Bottom),
                        new Point(boundingRect.Left, boundingRect.Bottom)
                    };
                    
                    regions.Add(textRegion);
                    wordIndex++;
                }
                lineIndex++;
            }
            
            return regions;
        }
        
        /// <summary>
        /// 获取指定语言的OCR引擎
        /// </summary>
        /// <param name="language">语言标签</param>
        /// <returns>OCR引擎实例</returns>
        public static async Task<WindowsOcrEngine> CreateForLanguageAsync(string language)
        {
            var engine = new WindowsOcrEngine();
            await engine.InitializeAsync();
            
            if (engine.IsAvailable && engine.SupportedLanguages.Contains(language))
            {
                var targetLanguage = OcrEngine.AvailableRecognizerLanguages
                    .FirstOrDefault(lang => lang.LanguageTag == language);
                
                if (targetLanguage != null)
                {
                    var ocrEngine = OcrEngine.TryCreateFromLanguage(targetLanguage);
                    if (ocrEngine != null)
                    {
                        engine._engine = ocrEngine;
                    }
                }
            }
            
            return engine;
        }
    }
}