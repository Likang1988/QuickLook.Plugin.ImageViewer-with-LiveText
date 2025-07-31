using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QuickLook.Plugin.ImageViewer.LiveText
{
    /// <summary>
    /// LiveTextOverlay.xaml 的交互逻辑
    /// </summary>
    public partial class LiveTextOverlay : UserControl
    {
        private TextSelectionManager _selectionManager;
        private List<Rectangle> _boundRectangles;
        private List<Rectangle> _selectedRectangles;
        private bool _isDragging;
        private TextRegion _selectionStartRegion;
        private TextRegion _lastHoveredRegion;
        private DispatcherTimer _helpTipTimer;
        private bool _isInitialized;
        
        /// <summary>
        /// 文本区域列表
        /// </summary>
        public List<TextRegion> TextRegions
        {
            get => _selectionManager?.TextRegions ?? new List<TextRegion>();
            set
            {
                if (_selectionManager != null)
                {
                    _selectionManager.TextRegions = value ?? new List<TextRegion>();
                    UpdateVisualElements();
                }
            }
        }
        
        /// <summary>
        /// 是否显示文本边界框
        /// </summary>
        public bool ShowTextBounds { get; set; } = true;
        
        /// <summary>
        /// 文本选择事件
        /// </summary>
        public event EventHandler<string> TextSelected;
        
        /// <summary>
        /// OCR状态改变事件
        /// </summary>
        public event EventHandler<string> StatusChanged;
        
        public LiveTextOverlay()
        {
            InitializeComponent();
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            _selectionManager = new TextSelectionManager();
            _boundRectangles = new List<Rectangle>();
            _selectedRectangles = new List<Rectangle>();

            // 订阅选择管理器事件
            _selectionManager.SelectionChanged += OnSelectionChanged;
            _selectionManager.TextCopied += OnTextCopied;

            // 初始化帮助提示定时器
            _helpTipTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _helpTipTimer.Tick += (s, e) =>
            {
                HelpTip.Visibility = Visibility.Collapsed;
                _helpTipTimer.Stop();
            };



            // 订阅大小改变事件
            SizeChanged += (s, e) => UpdateMask();

            _isInitialized = true;

            // 加载设置
            LoadSettings();
        }
        
        private void LoadSettings()
        {
            // TODO: 需要从实例获取设置
            ShowTextBounds = true; // LiveTextSettings.ShowTextBounds;
        }
        
        /// <summary>
        /// 显示状态指示器
        /// </summary>
        /// <param name="message">状态消息</param>
        public void ShowStatus(string message)
        {
            StatusText.Text = message;
            StatusIndicator.Visibility = Visibility.Visible;
            StatusChanged?.Invoke(this, message);
        }
        
        /// <summary>
        /// 隐藏状态指示器
        /// </summary>
        public void HideStatus()
        {
            StatusIndicator.Visibility = Visibility.Collapsed;
            StatusChanged?.Invoke(this, string.Empty);
        }
        
        /// <summary>
        /// 显示帮助提示
        /// </summary>
        public void ShowHelpTip()
        {
            if (TextRegions.Any())
            {
                HelpTip.Visibility = Visibility.Visible;
                _helpTipTimer.Start();
            }
        }
        
        /// <summary>
        /// 更新可视化元素
        /// </summary>
        private void UpdateVisualElements()
        {
            if (!_isInitialized)
                return;
            
            ClearVisualElements();
            
            // 移除文本边界框显示
            // if (ShowTextBounds)
            // {
            //     CreateTextBoundRectangles();
            // }
            
            // 更新遮罩效果
            UpdateMask();
            
            ShowHelpTip();
        }
        
        /// <summary>
        /// 清除所有可视化元素
        /// </summary>
        private void ClearVisualElements()
        {
            MainCanvas.Children.Clear();
            _boundRectangles.Clear();
            _selectedRectangles.Clear();
        }
        
        /// <summary>
        /// 清除所有文本区域和可视化元素
        /// </summary>
        public void Clear()
        {
            TextRegions = new List<TextRegion>();
            ClearVisualElements();
            UpdateMask();
        }

        /// <summary>
        /// 更新遮罩效果
        /// </summary>
        private void UpdateMask()
        {
            var maskPath = FindName("MaskPath") as Path;
            if (maskPath == null) return;
            
            // 如果控件尺寸还未确定，延迟更新
            if (ActualWidth <= 0 || ActualHeight <= 0)
            {
                Loaded += (s, e) => UpdateMask();
                return;
            }
            
            var geometryGroup = new GeometryGroup { FillRule = FillRule.EvenOdd };
            
            // 添加外部矩形（覆盖整个区域）
            var outerRect = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
            geometryGroup.Children.Add(outerRect);
            
            if (TextRegions.Any())
            {
                var mergedRegions = MergeAdjacentRegions(TextRegions);

                var combinedGeometry = new PathGeometry();
                foreach (var region in mergedRegions)
                {
                    var inflatedRect = new Rect(region.X - 10, region.Y - 10, region.Width + 20, region.Height + 20);
                    var holeRect = new RectangleGeometry(inflatedRect, 8, 8);
                    combinedGeometry = Geometry.Combine(combinedGeometry, holeRect, GeometryCombineMode.Union, null);
                }
                geometryGroup.Children.Add(combinedGeometry);
            }
            
            maskPath.Data = geometryGroup;
        }
        
        /// <summary>
        /// 合并相邻的文本区域
        /// </summary>
        private List<Rect> MergeAdjacentRegions(IEnumerable<TextRegion> regions)
        {
            var rects = regions.Select(r => r.BoundingBox).ToList();
            var merged = new List<Rect>();
            var processed = new HashSet<int>();
            
            for (int i = 0; i < rects.Count; i++)
            {
                if (processed.Contains(i)) continue;
                
                var currentRect = rects[i];
                processed.Add(i);
                
                // 查找与当前矩形相邻或重叠的矩形
                bool foundAdjacent;
                do
                {
                    foundAdjacent = false;
                    for (int j = 0; j < rects.Count; j++)
                    {
                        if (processed.Contains(j)) continue;
                        
                        var otherRect = rects[j];
                        
                        // 检查是否相邻或重叠（允许一定的间距）
                        if (AreRectsAdjacent(currentRect, otherRect, 0))
                        {
                            currentRect = Rect.Union(currentRect, otherRect);
                            processed.Add(j);
                            foundAdjacent = true;
                        }
                    }
                } while (foundAdjacent);
                
                merged.Add(currentRect);
            }
            
            return merged;
        }
        
        /// <summary>
        /// 检查两个矩形是否相邻或重叠
        /// </summary>
        private bool AreRectsAdjacent(Rect rect1, Rect rect2, double tolerance)
        {
            // 检查是否重叠
            if (rect1.IntersectsWith(rect2))
                return true;

            var verticalTolerance = 5; // 垂直方向上更小的容差
            var horizontalTolerance = 35; // 水平方向上可以大一些

            // 检查水平相邻：Y轴有重叠，且X轴接近
            bool yOverlap = (rect1.Top < rect2.Bottom && rect1.Bottom > rect2.Top);
            if (yOverlap)
            {
                if (Math.Abs(rect1.Right - rect2.Left) < horizontalTolerance || Math.Abs(rect2.Right - rect1.Left) < horizontalTolerance)
                {
                    return true;
                }
            }

            // 检查垂直相邻：X轴有重叠，且Y轴接近
            bool xOverlap = (rect1.Left < rect2.Right && rect1.Right > rect2.Left);
            if (xOverlap)
            {
                if (Math.Abs(rect1.Bottom - rect2.Top) < verticalTolerance || Math.Abs(rect2.Bottom - rect1.Top) < verticalTolerance)
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// 创建文本边界框矩形
        /// </summary>
        private void CreateTextBoundRectangles()
        {
            
        }
        
        /// <summary>
        /// 更新选中区域的可视化
        /// </summary>
        private void UpdateSelectedRegions()
        {
            // 清除之前的选中矩形
            foreach (var rect in _selectedRectangles)
            {
                MainCanvas.Children.Remove(rect);
            }
            _selectedRectangles.Clear();

            if (!_selectionManager.SelectedRegions.Any())
                return;

            // 按行分组
            var lines = _selectionManager.SelectedRegions
                .GroupBy(r => r.LineIndex)
                .OrderBy(g => g.Key);

            foreach (var line in lines)
            {
                var sortedRegions = line.OrderBy(r => r.BoundingBox.X).ToList();
                if (!sortedRegions.Any())
                    continue;

                // 合并同一行内的矩形
                var mergedRects = new List<Rect>();
                var currentRect = sortedRegions[0].BoundingBox;

                for (int i = 1; i < sortedRegions.Count; i++)
                {
                    var nextRect = sortedRegions[i].BoundingBox;
                    // 如果两个矩形在水平方向上足够接近，则合并
                    if (nextRect.Left - currentRect.Right <= 35) // 35像素容差
                    {
                        currentRect = Rect.Union(currentRect, nextRect);
                    }
                    else
                    {
                        mergedRects.Add(currentRect);
                        currentRect = nextRect;
                    }
                }
                mergedRects.Add(currentRect);

                // 创建合并后的矩形
                foreach (var mergedRect in mergedRects)
                {
                    var rect = new Rectangle
                    {
                        Width = mergedRect.Width,
                        Height = mergedRect.Height,
                        Fill = new SolidColorBrush(Color.FromArgb(128, 0, 120, 215)), // 蓝色半透明
                        IsHitTestVisible = false
                    };

                    Canvas.SetLeft(rect, mergedRect.X);
                    Canvas.SetTop(rect, mergedRect.Y);

                    MainCanvas.Children.Add(rect);
                    _selectedRectangles.Add(rect);
                }
            }
        }
        

        
        #region 鼠标事件处理
        
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            
            if (!TextRegions.Any())
                return;
            
            var position = e.GetPosition(MainCanvas);
            var hitRegion = TextRegions.FirstOrDefault(r => r.Contains(position));
            if (hitRegion != null)
            {
                _isDragging = true;
                _selectionStartRegion = hitRegion;
                _selectionManager.StartSelection(hitRegion, Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
                CaptureMouse();
            }
            else
            {
                _selectionManager.ClearSelection();
            }
            
            HelpTip.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (_isDragging)
            {
                _isDragging = false;
                _selectionManager.EndSelection();
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var position = e.GetPosition(MainCanvas);

            if (_isDragging)
            {
                var currentRegion = TextRegions.FirstOrDefault(r => r.Contains(position));
                if (currentRegion != null && currentRegion != _lastHoveredRegion)
                {
                    _selectionManager.UpdateSelection(currentRegion);
                    _lastHoveredRegion = currentRegion;
                }
                e.Handled = true;
            }
            else
            {
                // 检查鼠标是否在文本区域上，设置相应的光标样式
                var hitRegion = TextRegions.FirstOrDefault(r => r.Contains(position));
                if (hitRegion != null)
                {
                    Cursor = Cursors.IBeam; // 文本选择光标
                }
                else
                {
                    Cursor = Cursors.Arrow; // 默认光标
                }
            }
        }
        
        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            
            // 如果有选中的文本，显示右键菜单
            if (_selectionManager.SelectedRegions.Any())
            {
                ShowContextMenu(e.GetPosition(this));
            }
            else
            {
                // 没有选中文本时取消选择
                _selectionManager.CancelSelection();
            }
            e.Handled = true;
        }
        
        /// <summary>
        /// 显示右键上下文菜单
        /// </summary>
        /// <param name="position">菜单显示位置</param>
        private void ShowContextMenu(Point position)
        {
            var contextMenu = new ContextMenu();
            
            // 复制菜单项
            var copyMenuItem = new MenuItem
            {
                Header = "复制文本",
                Icon = new TextBlock { Text = "📋", FontSize = 12 }
            };
            copyMenuItem.Click += (s, e) =>
            {
                var text = _selectionManager.GetSelectedText();
                if (!string.IsNullOrEmpty(text))
                {
                    Clipboard.SetText(text);
                    OnTextCopied(this, text);
                }
            };
            contextMenu.Items.Add(copyMenuItem);
            
            // 全选菜单项
            var selectAllMenuItem = new MenuItem
            {
                Header = "全选",
                Icon = new TextBlock { Text = "🔘", FontSize = 12 }
            };
            selectAllMenuItem.Click += (s, e) => _selectionManager.SelectAll();
            contextMenu.Items.Add(selectAllMenuItem);
            
            // 分隔符
            contextMenu.Items.Add(new Separator());
            
            // 取消选择菜单项
            var clearMenuItem = new MenuItem
            {
                Header = "取消选择",
                Icon = new TextBlock { Text = "❌", FontSize = 12 }
            };
            clearMenuItem.Click += (s, e) => _selectionManager.CancelSelection();
            contextMenu.Items.Add(clearMenuItem);
            
            // 设置菜单位置并显示
            contextMenu.PlacementTarget = this;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            contextMenu.IsOpen = true;
        }
        
        #endregion
        
        #region 键盘事件处理
        
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            // TODO: 需要从实例获取设置
            // if (!LiveTextSettings.EnableHotkeys)
            //     return;
            
            switch (e.Key)
            {
                case Key.A when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                    // Ctrl+A 全选
                    _selectionManager.SelectAll();
                    e.Handled = true;
                    break;
                    
                case Key.Escape:
                    // Esc 取消选择
                    _selectionManager.CancelSelection();
                    e.Handled = true;
                    break;
                    
                case Key.C when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                    // Ctrl+C 复制选中文本
                    if (_selectionManager.SelectedRegions.Any())
                    {
                        var text = _selectionManager.GetSelectedText();
                        if (!string.IsNullOrEmpty(text))
                        {
                            Clipboard.SetText(text);
                            OnTextCopied(this, text);
                        }
                    }
                    e.Handled = true;
                    break;
            }
        }
        
        #endregion
        
        #region 事件处理器
        
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedRegions();
        }

        private void OnTextCopied(object sender, string text)
        {
            TextSelected?.Invoke(this, text);

            // 显示复制成功提示
            ShowStatus($"已复制 {text.Length} 个字符");

            // 2秒后隐藏状态
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, args) =>
            {
                HideStatus();
                timer.Stop();
            };
            timer.Start();
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 清除所有选择
        /// </summary>
        public void ClearSelection()
        {
            _selectionManager?.ClearSelection();
        }
        
        /// <summary>
        /// 获取选中的文本
        /// </summary>
        /// <returns>选中的文本内容</returns>
        public string GetSelectedText()
        {
            return _selectionManager?.GetSelectedText() ?? string.Empty;
        }
        
        /// <summary>
        /// 设置文本区域并更新显示
        /// </summary>
        /// <param name="regions">文本区域列表</param>
        public void SetTextRegions(List<TextRegion> regions)
        {
            TextRegions = regions;
        }
        
        /// <summary>
        /// 重新加载设置
        /// </summary>
        public void ReloadSettings()
        {
            LoadSettings();
            UpdateVisualElements();
        }
        
        #endregion
        
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            
            // 更新画布大小
            MainCanvas.Width = sizeInfo.NewSize.Width;
            MainCanvas.Height = sizeInfo.NewSize.Height;
        }
    }
}