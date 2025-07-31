﻿// Copyright © 2017-2025 QL-Win Contributors
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.Win32;
using QuickLook.Common.Annotations;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.ImageViewer.NativeMethods;
using QuickLook.Plugin.ImageViewer.LiveText;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace QuickLook.Plugin.ImageViewer;

public partial class ImagePanel : UserControl, INotifyPropertyChanged, IDisposable
{
    private Visibility _backgroundVisibility = Visibility.Visible;
    private ContextObject _contextObject;
    private Point? _dragInitPos;
    private Uri _imageSource;
    private bool _isZoomFactorFirstSet = true;
    private DateTime _lastZoomTime = DateTime.MinValue;
    private double _maxZoomFactor = 3d;
    private MetaProvider _meta;
    private double _minZoomFactor = 0.1d;
    private BitmapScalingMode _renderMode = BitmapScalingMode.Linear;
    private bool _showZoomLevelInfo = true;
    private BitmapSource _source;
    private double _zoomFactor = 1d;

    private bool _zoomToFit = true;
    private double _zoomToFitFactor;
    private bool _zoomWithControlKey;

    private int _rotationAngle;
    private BitmapSource _originalSource;

    private Visibility _rotateIconVisibility = Visibility.Visible;
    private Visibility _saveAsVisibility = Visibility.Collapsed;
    private Visibility _reverseColorVisibility = Visibility.Collapsed;
    private Visibility _metaIconVisibility = Visibility.Visible;
    private Visibility _liveTextIconVisibility = Visibility.Visible;

    // LiveText related fields
    private IOcrEngine _ocrEngine;
    private LiveTextSettings _liveTextSettings;
    private bool _isLiveTextEnabled;
    private bool _isOcrProcessing;
    private CancellationTokenSource _ocrCancellationTokenSource;

    public ImagePanel()
    {
        InitializeComponent();

        Resources.MergedDictionaries.Clear();

        buttonRotate.Click += OnRotateOnClick;

        buttonSaveAs.Click += OnSaveAsOnClick;

        buttonReverseColor.Click += OnReverseColorOnClick;

        buttonMeta.Click += (sender, e) =>
            textMeta.Visibility = textMeta.Visibility == Visibility.Collapsed
                ? Visibility.Visible
                : Visibility.Collapsed;

        buttonBackgroundColour.Click += OnBackgroundColourOnClick;
        
        buttonLiveText.Click += OnLiveTextOnClick;

        SizeChanged += ImagePanel_SizeChanged;
        viewPanelImage.DoZoomToFit += (sender, e) => DoZoomToFit();
        viewPanelImage.ImageLoaded += (sender, e) =>
        {
            ContextObject.IsBusy = false;
            _originalSource = viewPanelImage.Source as BitmapSource;
            Source = _originalSource;
            //UpdateSizeInfo();
        };

        viewPanel.PreviewMouseWheel += ViewPanel_PreviewMouseWheel;
        viewPanel.MouseLeftButtonDown += ViewPanel_MouseLeftButtonDown;
        viewPanel.MouseMove += ViewPanel_MouseMove;
        viewPanel.MouseDoubleClick += ViewPanel_MouseDoubleClick;

        viewPanel.ManipulationInertiaStarting += ViewPanel_ManipulationInertiaStarting;
        viewPanel.ManipulationStarting += ViewPanel_ManipulationStarting;
        viewPanel.ManipulationDelta += ViewPanel_ManipulationDelta;
        
        // Initialize LiveText
        InitializeLiveText();

        //Loaded += (s, e) => UpdateSizeInfo();
    }

    internal ImagePanel(ContextObject context, MetaProvider meta) : this()
    {
        ContextObject = context;
        Meta = meta;

        var s = meta.GetSize();
        //_minZoomFactor = Math.Min(200d / s.Height, 400d / s.Width);
        //_maxZoomFactor = Math.Min(9000d / s.Height, 9000d / s.Width);

        ShowMeta();
        Theme = ContextObject.Theme;
        
        // Check if LiveText should be enabled for this image
        CheckLiveTextAvailability();
    }

    private void UpdateSizeInfo()
    {
        //if (SizeInfoTextBlock == null || _source == null)
        //    return;

        //var scaleX = _source.PixelWidth > 0 ? ActualWidth / _source.PixelWidth * ZoomFactor : 0;
        //var scaleY = _source.PixelHeight > 0 ? ActualHeight / _source.PixelHeight * ZoomFactor : 0;

        //var dpiScale = VisualTreeHelper.GetDpi(this);

        //SizeInfoTextBlock.Text = $"Window: {ActualWidth:F0}x{ActualHeight:F0} (DPI: {dpiScale.DpiScaleX * 100:F0}%)" +
        //                         $"\nImage: {_source.PixelWidth}x{_source.PixelHeight} ({(ZoomFactor * 100):F0}%)";
    }

    public bool ZoomWithControlKey
    {
        get => _zoomWithControlKey;
        set
        {
            _zoomWithControlKey = value;
            OnPropertyChanged();
        }
    }

    public bool ShowZoomLevelInfo
    {
        get => _showZoomLevelInfo;
        set
        {
            if (value == _showZoomLevelInfo) return;
            _showZoomLevelInfo = value;
            OnPropertyChanged();
        }
    }

    public Themes Theme
    {
        get => ContextObject?.Theme ?? Themes.Dark;
        set
        {
            ContextObject.Theme = value;
            OnPropertyChanged();
        }
    }

    public BitmapScalingMode RenderMode
    {
        get => _renderMode;
        set
        {
            _renderMode = value;
            OnPropertyChanged();
        }
    }

    public bool ZoomToFit
    {
        get => _zoomToFit;
        set
        {
            _zoomToFit = value;
            OnPropertyChanged();
        }
    }

    public Visibility RotateIconVisibility
    {
        get => _rotateIconVisibility;
        set
        {
            _rotateIconVisibility = value;
            OnPropertyChanged();
        }
    }

    public Visibility SaveAsVisibility
    {
        get => _saveAsVisibility;
        set
        {
            _saveAsVisibility = value;
            OnPropertyChanged();
        }
    }

    public Visibility ReverseColorVisibility
    {
        get => _reverseColorVisibility;
        set
        {
            _reverseColorVisibility = value;
            OnPropertyChanged();
        }
    }

    public Visibility MetaIconVisibility
    {
        get => _metaIconVisibility;
        set
        {
            _metaIconVisibility = value;
            OnPropertyChanged();
        }
    }

    public Visibility LiveTextIconVisibility
    {
        get => _liveTextIconVisibility;
        set
        {
            _liveTextIconVisibility = value;
            OnPropertyChanged();
        }
    }

    public bool IsLiveTextEnabled
    {
        get => _isLiveTextEnabled;
        set
        {
            _isLiveTextEnabled = value;
            OnPropertyChanged();
            
            if (value)
            {
                liveTextOverlay.Visibility = Visibility.Visible;
                liveTextOverlay.IsHitTestVisible = true;
                _ = PerformOcrAsync();
            }
            else
            {
                liveTextOverlay.Visibility = Visibility.Collapsed;
                liveTextOverlay.IsHitTestVisible = false;
                CancelOcr();
            }
        }
    }

    public bool IsOcrProcessing
    {
        get => _isOcrProcessing;
        set
        {
            _isOcrProcessing = value;
            OnPropertyChanged();
        }
    }

    public Visibility BackgroundVisibility
    {
        get => _backgroundVisibility;
        set
        {
            _backgroundVisibility = value;
            OnPropertyChanged();
        }
    }

    public double MinZoomFactor
    {
        get => _minZoomFactor;
        set
        {
            _minZoomFactor = value;
            OnPropertyChanged();
        }
    }

    public double MaxZoomFactor
    {
        get => _maxZoomFactor;
        set
        {
            _maxZoomFactor = value;
            OnPropertyChanged();
        }
    }

    public double ZoomToFitFactor
    {
        get => _zoomToFitFactor;
        private set
        {
            _zoomToFitFactor = value;
            OnPropertyChanged();
        }
    }

    public double ZoomFactor
    {
        get => _zoomFactor;
        private set
        {
            _zoomFactor = value;
            OnPropertyChanged();

            if (_isZoomFactorFirstSet)
            {
                _isZoomFactorFirstSet = false;
                return;
            }

            if (ShowZoomLevelInfo)
                ((Storyboard)zoomLevelInfo.FindResource("StoryboardShowZoomLevelInfo")).Begin();
        }
    }

    public Uri ImageUriSource
    {
        get => _imageSource;
        set
        {
            _imageSource = value;

            OnPropertyChanged();
        }
    }

    public BitmapSource Source
    {
        get => _source;
        set
        {
            _source = value;
            OnPropertyChanged();
            viewPanelImage.Source = _source;
        }
    }

    public ContextObject ContextObject
    {
        get => _contextObject;
        set
        {
            _contextObject = value;
            OnPropertyChanged();
        }
    }

    public MetaProvider Meta
    {
        get => _meta;
        set
        {
            if (Equals(value, _meta)) return;
            _meta = value;
            OnPropertyChanged();
        }
    }

    public void Dispose()
    {
        CancelOcr();
        _ocrEngine = null;
        viewPanelImage?.Dispose();
        viewPanelImage = null;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnRotateOnClick(object sender, RoutedEventArgs e)
    {
        _rotationAngle = (_rotationAngle + 90) % 360;
        ApplyTransformations();
    }

    private void OnSaveAsOnClick(object sender, RoutedEventArgs e)
    {
        if (_source == null)
        {
            return;
        }

        var dialog = new SaveFileDialog()
        {
            Filter = "PNG Image|*.png",
            DefaultExt = ".png",
            FileName = Path.GetFileNameWithoutExtension(ContextObject.Title)
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                if (File.Exists(dialog.FileName))
                {
                    File.Delete(dialog.FileName);
                }

                PngBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(_source));
                using FileStream stream = new(dialog.FileName, FileMode.Create, FileAccess.Write);
                encoder.Save(stream);
            }
            catch
            {
                ///
            }
        }
    }

    private void OnReverseColorOnClick(object sender, RoutedEventArgs e)
    {
        if (_source == null)
        {
            return;
        }

        Source = Source.InvertColors();
        _originalSource = Source;
        _rotationAngle = 0;
    }

    private void ApplyTransformations()
    {
        if (_originalSource == null) return;

        var transformedBitmap = new TransformedBitmap();
        transformedBitmap.BeginInit();
        transformedBitmap.Source = _originalSource;
        transformedBitmap.Transform = new RotateTransform(_rotationAngle);
        transformedBitmap.EndInit();

        Source = transformedBitmap;
    }

    private void OnBackgroundColourOnClick(object sender, RoutedEventArgs e)
    {
        Theme = Theme == Themes.Dark ? Themes.Light : Themes.Dark;

        SettingHelper.Set("LastTheme", (int)Theme, "QuickLook.Plugin.ImageViewer");
    }

    private void ShowMeta()
    {
        textMeta.Inlines.Clear();
        Meta.GetExif().Values.ForEach(m =>
        {
            if (string.IsNullOrWhiteSpace(m.Item1) || string.IsNullOrWhiteSpace(m.Item2))
                return;

            textMeta.Inlines.Add(new Run(m.Item1) { FontWeight = FontWeights.SemiBold });
            textMeta.Inlines.Add(": ");
            textMeta.Inlines.Add(m.Item2);
            textMeta.Inlines.Add("\r\n");
        });
        textMeta.Inlines.Remove(textMeta.Inlines.LastInline);
        if (!textMeta.Inlines.Any())
            MetaIconVisibility = Visibility.Collapsed;
    }

    public event EventHandler<int> ImageScrolled;

    public event EventHandler ZoomChanged;

    private void ImagePanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateZoomToFitFactor();

        if (ZoomToFit)
            DoZoomToFit();

        //UpdateSizeInfo();
    }

    private void ViewPanel_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
    {
        e.TranslationBehavior = new InertiaTranslationBehavior
        {
            InitialVelocity = e.InitialVelocities.LinearVelocity,
            DesiredDeceleration = 10d * 96d / (1000d * 1000d)
        };
    }

    private void ViewPanel_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
    {
        e.ManipulationContainer = viewPanel;
        e.Mode = ManipulationModes.Scale | ManipulationModes.Translate;
    }

    private void ViewPanel_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
    {
        var delta = e.DeltaManipulation;

        var newZoom = ZoomFactor + ZoomFactor * (delta.Scale.X - 1);

        Zoom(newZoom);

        viewPanel.ScrollToHorizontalOffset(viewPanel.HorizontalOffset - delta.Translation.X);
        viewPanel.ScrollToVerticalOffset(viewPanel.VerticalOffset - delta.Translation.Y);

        e.Handled = true;
    }

    private void ViewPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 当实况文本模式启用时，禁用图像平移功能
        if (IsLiveTextEnabled)
        {
            return;
        }

        e.MouseDevice.Capture(viewPanel);

        _dragInitPos = e.GetPosition(viewPanel);
        var temp = _dragInitPos.Value; // Point is a type value
        temp.Offset(viewPanel.HorizontalOffset, viewPanel.VerticalOffset);
        _dragInitPos = temp;
    }

    private void ViewPanel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        DoZoomToFit();
        //UpdateSizeInfo();
    }

    private void ViewPanel_MouseMove(object sender, MouseEventArgs e)
    {
        // 当实况文本模式启用时，禁用图像平移功能
        if (IsLiveTextEnabled)
        {
            return;
        }

        if (!_dragInitPos.HasValue)
            return;

        if (e.LeftButton == MouseButtonState.Released)
        {
            e.MouseDevice.Capture(null);

            _dragInitPos = null;
            return;
        }

        e.Handled = true;

        var delta = _dragInitPos.Value - e.GetPosition(viewPanel);

        viewPanel.ScrollToHorizontalOffset(delta.X);
        viewPanel.ScrollToVerticalOffset(delta.Y);
    }

    private void ViewPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsLiveTextEnabled)
        {
            OnLiveTextOnClick(this, null);
        }
        e.Handled = true;

        // normal scroll when Control is not pressed, useful for PdfViewer
        if (ZoomWithControlKey && (Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            viewPanel.ScrollToVerticalOffset(viewPanel.VerticalOffset - e.Delta);
            ImageScrolled?.Invoke(this, e.Delta);
            return;
        }

        // otherwise, perform normal zooming
        var newZoom = ZoomFactor + ZoomFactor * e.Delta / 120 * 0.1;

        Zoom(newZoom);

        //UpdateSizeInfo();
    }

    public Size GetScrollSize()
    {
        return new Size(viewPanel.ScrollableWidth, viewPanel.ScrollableHeight);
    }

    public Point GetScrollPosition()
    {
        return new Point(viewPanel.HorizontalOffset, viewPanel.VerticalOffset);
    }

    public void SetScrollPosition(Point point)
    {
        viewPanel.ScrollToHorizontalOffset(point.X);
        viewPanel.ScrollToVerticalOffset(point.Y);
    }

    public void DoZoomToFit()
    {
        UpdateZoomToFitFactor();

        Zoom(ZoomToFitFactor, false, true);
    }

    private void UpdateZoomToFitFactor()
    {
        if (viewPanelImage?.Source == null)
        {
            ZoomToFitFactor = 1d;
            return;
        }

        var factor = Math.Min(viewPanel.ActualWidth / viewPanelImage.Source.Width,
            viewPanel.ActualHeight / viewPanelImage.Source.Height);

        ZoomToFitFactor = factor;
    }

    public void ResetZoom()
    {
        ZoomToFitFactor = 1;
        Zoom(1d, true, ZoomToFit);
    }

    public void Zoom(double factor, bool suppressEvent = false, bool isToFit = false)
    {
        if (viewPanelImage?.Source == null)
            return;

        // pause when fit width
        if (ZoomFactor < ZoomToFitFactor && factor > ZoomToFitFactor
            || ZoomFactor > ZoomToFitFactor && factor < ZoomToFitFactor)
        {
            factor = ZoomToFitFactor;
            ZoomToFit = true;
        }
        // pause when 100%
        else if (ZoomFactor < 1 && factor > 1 || ZoomFactor > 1 && factor < 1)
        {
            factor = 1;
            ZoomToFit = false;
        }
        else
        {
            if (!isToFit)
                ZoomToFit = false;
        }

        factor = Math.Max(factor, MinZoomFactor);
        factor = Math.Min(factor, MaxZoomFactor);

        ZoomFactor = factor;

        var position = ZoomToFit
            ? new Point(viewPanelImage.Source.Width / 2, viewPanelImage.Source.Height / 2)
            : Mouse.GetPosition(viewPanelImage);

        viewPanelImage.LayoutTransform = new ScaleTransform(factor, factor);

        viewPanel.InvalidateMeasure();

        // critical for calculating offset
        viewPanel.ScrollToHorizontalOffset(0);
        viewPanel.ScrollToVerticalOffset(0);
        UpdateLayout();

        var offset = viewPanelImage.TranslatePoint(position, viewPanel) - Mouse.GetPosition(viewPanel);
        viewPanel.ScrollToHorizontalOffset(offset.X);
        viewPanel.ScrollToVerticalOffset(offset.Y);
        UpdateLayout();

        if (!suppressEvent)
            FireZoomChangedEvent();
    }

    private void FireZoomChangedEvent()
    {
        _lastZoomTime = DateTime.Now;

        Task.Delay(500).ContinueWith(t =>
        {
            if (DateTime.Now - _lastZoomTime < TimeSpan.FromSeconds(0.5))
                return;

            Debug.WriteLine($"FireZoomChangedEvent fired: {Thread.CurrentThread.ManagedThreadId}");

            Dispatcher.BeginInvoke(new Action(() => ZoomChanged?.Invoke(this, EventArgs.Empty)),
                DispatcherPriority.Background);
        });
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void ScrollToTop()
    {
        viewPanel.ScrollToTop();
    }

    public void ScrollToBottom()
    {
        viewPanel.ScrollToBottom();
    }

    #region LiveText Methods

    private void InitializeLiveText()
    {
        try
        {
            _liveTextSettings = new LiveTextSettings();
            _ocrEngine = new WindowsOcrEngine();
            
            // Set up event handlers for the overlay
            liveTextOverlay.TextSelected += (sender, e) => 
            {
                // Optional: Show a brief notification that text was copied
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize LiveText: {ex.Message}");
            LiveTextIconVisibility = Visibility.Collapsed;
        }
    }

    private async void CheckLiveTextAvailability()
    {
        if (_ocrEngine == null)
            return;

        try
        {
            var isAvailable = await _ocrEngine.IsAvailableAsync();
            LiveTextIconVisibility = isAvailable ? Visibility.Visible : Visibility.Collapsed;
        }
        catch
        {
            LiveTextIconVisibility = Visibility.Collapsed;
        }
    }

    private async void OnLiveTextOnClick(object sender, RoutedEventArgs e)
    {
        IsLiveTextEnabled = !IsLiveTextEnabled;

        // 启用或禁用 LiveTextOverlay 的鼠标事件处理
        liveTextOverlay.IsHitTestVisible = IsLiveTextEnabled;

        if (IsLiveTextEnabled)
        {
            await PerformOcrAsync();
        }
        else
        {
            liveTextOverlay.Clear();
            CancelOcr();
        }
    }

    private async Task PerformOcrAsync()
    {
        if (_ocrEngine == null || IsOcrProcessing)
            return;

        if (viewPanel == null)
            return;

        // Define a scale factor for higher resolution capture
        const double scaleFactor = 2.0;
        var scaledWidth = (int)(viewPanel.ActualWidth * scaleFactor);
        var scaledHeight = (int)(viewPanel.ActualHeight * scaleFactor);

        // Capture the current view of the panel at a higher resolution
        var renderTargetBitmap = new RenderTargetBitmap(scaledWidth, scaledHeight, 96 * scaleFactor, 96 * scaleFactor, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(viewPanel);

        var source = renderTargetBitmap;

        try
        {
            IsOcrProcessing = true;
            CancelOcr();
            _ocrCancellationTokenSource = new CancellationTokenSource();

            var textRegions = await _ocrEngine.RecognizeTextAsync(source, _liveTextSettings.PreferredLanguage, _ocrCancellationTokenSource.Token);

            if (!_ocrCancellationTokenSource.Token.IsCancellationRequested)
            {
                // Filter by confidence threshold and scale back the coordinates
                var scaledRegions = textRegions
                    .Where(r => r.Confidence >= _liveTextSettings.MinConfidence)
                    .Select(r =>
                    {
                        r.BoundingBox = new Rect(
                            r.BoundingBox.X / scaleFactor,
                            r.BoundingBox.Y / scaleFactor,
                            r.BoundingBox.Width / scaleFactor,
                            r.BoundingBox.Height / scaleFactor
                        );
                        return r;
                    }).ToList();

                // Update the overlay with recognized text
                await Dispatcher.InvokeAsync(() =>
                {
                    liveTextOverlay.TextRegions = scaledRegions;
                });
            }
        }
        catch (OperationCanceledException)
        {
            // OCR was cancelled, this is expected
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OCR failed: {ex.Message}");
        }
        finally
        {
            IsOcrProcessing = false;
        }
    }

    private void CancelOcr()
    {
        _ocrCancellationTokenSource?.Cancel();
        _ocrCancellationTokenSource?.Dispose();
        _ocrCancellationTokenSource = null;
    }

    #endregion
}
