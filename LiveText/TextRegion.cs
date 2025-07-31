using System;
using System.Collections.Generic;
using System.Windows;

namespace QuickLook.Plugin.ImageViewer.LiveText
{
    /// <summary>
    /// 表示OCR识别到的文本区域
    /// </summary>
    public class TextRegion
    {
        /// <summary>
        /// 识别到的文本内容
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// 文本区域的边界框
        /// </summary>
        public Rect BoundingBox { get; set; }
        
        /// <summary>
        /// 识别置信度 (0.0 - 1.0)
        /// </summary>
        public double Confidence { get; set; }
        
        /// <summary>
        /// 文本区域的四个角点坐标（用于处理旋转文本）
        /// </summary>
        public List<Point> Corners { get; set; }
        
        /// <summary>
        /// 文本行索引（用于排序）
        /// </summary>
        public int LineIndex { get; set; }
        
        /// <summary>
        /// 文本在行内的索引（用于排序）
        /// </summary>
        public int WordIndex { get; set; }
        
        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected { get; set; }
        
        public TextRegion()
        {
            Text = string.Empty;
            BoundingBox = Rect.Empty;
            Confidence = 0.0;
            Corners = new List<Point>();
            LineIndex = 0;
            WordIndex = 0;
            IsSelected = false;
        }
        
        public TextRegion(string text, Rect boundingBox, double confidence = 1.0)
        {
            Text = text ?? string.Empty;
            BoundingBox = boundingBox;
            Confidence = confidence;
            Corners = new List<Point>();
            LineIndex = 0;
            WordIndex = 0;
            IsSelected = false;
        }
        
        /// <summary>
        /// 检查指定点是否在文本区域内
        /// </summary>
        /// <param name="point">要检查的点</param>
        /// <returns>如果点在区域内返回true</returns>
        public bool Contains(Point point)
        {
            return BoundingBox.Contains(point);
        }
        
        /// <summary>
        /// 检查指定矩形是否与文本区域相交
        /// </summary>
        /// <param name="rect">要检查的矩形</param>
        /// <returns>如果相交返回true</returns>
        public bool IntersectsWith(Rect rect)
        {
            return BoundingBox.IntersectsWith(rect);
        }
        
        public override string ToString()
        {
            return $"Text: '{Text}', BoundingBox: {BoundingBox}, Confidence: {Confidence:F2}";
        }
    }
}