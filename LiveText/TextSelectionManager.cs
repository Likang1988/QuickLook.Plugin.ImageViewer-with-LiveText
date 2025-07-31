using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace QuickLook.Plugin.ImageViewer.LiveText
{
    /// <summary>
    /// 文本选择管理器，处理文本区域的选择逻辑
    /// </summary>
    public class TextSelectionManager
    {
        private List<TextRegion> _textRegions;
        private List<TextRegion> _selectedRegions;
         private TextRegion _startRegion;

        
        /// <summary>
        /// 当前的文本区域列表
        /// </summary>
        public List<TextRegion> TextRegions
        {
            get => _textRegions ?? new List<TextRegion>();
            set
            {
                _textRegions = value ?? new List<TextRegion>();
                ClearSelection();
            }
        }
        
        /// <summary>
        /// 当前选中的文本区域
        /// </summary>
        public List<TextRegion> SelectedRegions => _selectedRegions ?? new List<TextRegion>();
        
        /// <summary>
        /// 是否正在进行选择操作
        /// </summary>

        
        /// <summary>
        /// 选择状态改变事件
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;
        
        /// <summary>
        /// 文本复制事件
        /// </summary>
        public event EventHandler<string> TextCopied;
        
        public TextSelectionManager()
        {
            _textRegions = new List<TextRegion>();
            _selectedRegions = new List<TextRegion>();

        }
        
        public void StartSelection(TextRegion startRegion, bool isCtrlPressed)
        {
            _startRegion = startRegion;
            if (!isCtrlPressed)
            {
                ClearSelection();
            }

            if (!startRegion.IsSelected)
            {
                startRegion.IsSelected = true;
                _selectedRegions.Add(startRegion);
            }

            OnSelectionChanged(new SelectionChangedEventArgs(SelectedRegions, Rect.Empty, true));
        }

        public void UpdateSelection(TextRegion endRegion)
        {
            if (_startRegion == null) return;
            
            ClearSelection();

            var startIndex = TextRegions.IndexOf(_startRegion);
            var endIndex = TextRegions.IndexOf(endRegion);

            if (startIndex < 0 || endIndex < 0) return;

            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex);
            }

            for (int i = startIndex; i <= endIndex; i++)
            {
                var region = TextRegions[i];
                region.IsSelected = true;
                _selectedRegions.Add(region);
            }
            
            OnSelectionChanged(new SelectionChangedEventArgs(SelectedRegions, Rect.Empty, true));
        }

        public void EndSelection(bool copyToClipboard = false)
        {
            _startRegion = null;

            if (copyToClipboard && SelectedRegions.Any())
            {
                var selectedText = GetSelectedText();
                CopyTextToClipboard(selectedText);
            }
            
            OnSelectionChanged(new SelectionChangedEventArgs(SelectedRegions, Rect.Empty, false));
        }
        
        /// <summary>
        /// 取消选择操作
        /// </summary>
        public void CancelSelection()
        {
            ClearSelection();
            OnSelectionChanged(new SelectionChangedEventArgs(SelectedRegions, Rect.Empty, false));
        }
        
        /// <summary>
        /// 清除所有选择
        /// </summary>
        public void ClearSelection()
        {
            if (_selectedRegions != null)
            {
                foreach (var region in _selectedRegions)
                {
                    region.IsSelected = false;
                }
                _selectedRegions.Clear();
            }
            else
            {
                _selectedRegions = new List<TextRegion>();
            }
        }
        

        
        /// <summary>
        /// 选择所有文本
        /// </summary>
        public void SelectAll()
        {
            ClearSelection();
            
            foreach (var region in TextRegions)
            {
                region.IsSelected = true;
                _selectedRegions.Add(region);
            }
            
            OnSelectionChanged(new SelectionChangedEventArgs(SelectedRegions, Rect.Empty, false));
        }
        
        /// <summary>
        /// 获取选中的文本内容
        /// </summary>
        /// <returns>按阅读顺序排列的文本</returns>
        public string GetSelectedText()
        {
            if (!SelectedRegions.Any())
                return string.Empty;
            
            // 按行和列的顺序排序
            var sortedRegions = SelectedRegions
                .OrderBy(r => r.LineIndex)
                .ThenBy(r => r.WordIndex)
                .ToList();
            
            var lines = new List<string>();
            var currentLine = new List<string>();
            int currentLineIndex = -1;
            
            foreach (var region in sortedRegions)
            {
                if (region.LineIndex != currentLineIndex)
                {
                    // 新的一行
                    if (currentLine.Any())
                    {
                        // 检查是否为中文，如果是则不用空格连接
                        var lineText = string.Join("", currentLine);
                        if (!IsChinese(lineText))
                        {
                            lineText = string.Join(" ", currentLine);
                        }
                        lines.Add(lineText);
                    }
                    currentLine.Clear();
                    currentLineIndex = region.LineIndex;
                }
                
                currentLine.Add(region.Text);
            }
            
            // 添加最后一行
            if (currentLine.Any())
            {
                // 检查是否为中文，如果是则不用空格连接
                var lineText = string.Join("", currentLine);
                if (!IsChinese(lineText))
                {
                    lineText = string.Join(" ", currentLine);
                }
                lines.Add(lineText);
            }
            
            return string.Join(Environment.NewLine, lines);
        }

        private bool IsChinese(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            int chineseCharCount = 0;
            foreach (char c in text)
            {
                // CJK Unified Ideographs U+4E00..U+9FFF
                if (c >= 0x4E00 && c <= 0x9FFF)
                {
                    chineseCharCount++;
                }
            }

            // 如果中文字符超过一半，则认为是中文
            return chineseCharCount * 2 > text.Length;
        }
        
        /// <summary>
        /// 复制文本到剪贴板
        /// </summary>
        /// <param name="text">要复制的文本</param>
        private void CopyTextToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            
            try
            {
                System.Windows.Clipboard.SetText(text);
                OnTextCopied(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"复制文本到剪贴板失败: {ex.Message}");
            }
        }
        

        
        /// <summary>
        /// 触发选择状态改变事件
        /// </summary>
        private void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// 触发文本复制事件
        /// </summary>
        private void OnTextCopied(string text)
        {
            TextCopied?.Invoke(this, text);
        }
    }
    
    /// <summary>
    /// 选择状态改变事件参数
    /// </summary>
    public class SelectionChangedEventArgs : EventArgs
    {
        public List<TextRegion> SelectedRegions { get; }
        public Rect SelectionRect { get; }
        public bool IsSelecting { get; }
        
        public SelectionChangedEventArgs(List<TextRegion> selectedRegions, Rect selectionRect, bool isSelecting)
        {
            SelectedRegions = selectedRegions ?? new List<TextRegion>();
            SelectionRect = selectionRect;
            IsSelecting = isSelecting;
        }
    }
}