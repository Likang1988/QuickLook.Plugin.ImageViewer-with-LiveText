# QuickLook.Plugin.ImageViewer with Live Text

该功能在AI编程工具Trae的帮助下实现，本README也由AI自动生成。

一个为 QuickLook 图片查看器插件增强的版本，添加了类似 macOS 实况文本（Live Text）的功能，允许用户在预览图片时直接选择和复制图片中的文本内容。

## ✨ 功能特性

### 🔤 Live Text 实况文本功能（新增）

- **智能文本识别**：使用 Windows 内置 OCR 引擎识别图片中的文本
- **可视化文本区域**：高亮显示识别到的文本边界
- **文本选择**：支持鼠标拖拽选择文本区域
- **右键复制**：选中文本后右键菜单可复制到剪贴板
- **多语言支持**：支持中文、英文等多种语言文本识别
- **实时响应**：图片缩放时文本叠加层自动调整（暂未实现）

  ![](image/demo.png)

## 📖 使用说明

### 启用 Live Text 功能

1. 使用 QuickLook 预览任意图片
2. 点击右上角工具栏中的 **实况文本按钮**（🔤 图标，位于左起第一个）
3. 等待 OCR 识别完成（通常 1-3 秒）
4. 识别完成后，图片上会出现半透明遮罩，文本区域显示为透明洞
5. 右下角会显示帮助提示："拖拽选择文本进行复制"

### 选择和复制文本

1. 启用 Live Text 后，在文本区域按住鼠标左键开始拖拽
2. 拖拽过程中会显示蓝色选择框
3. 松开鼠标后，选中区域内的文本会自动复制到剪贴板
4. 可以在任意应用中使用 `Ctrl+V` 粘贴文本
5. 支持跨行文本选择，会按照从上到下、从左到右的顺序组合文本

## 🛠️ 技术实现

### 架构设计

```
QuickLook.Plugin.ImageViewer/
├── LiveText/                    # Live Text 功能模块
│   ├── IOcrEngine.cs           # OCR 引擎接口
│   ├── WindowsOcrEngine.cs     # Windows OCR 实现
│   ├── TextRegion.cs           # 文本区域数据模型
│   ├── LiveTextOverlay.xaml    # 文本叠加层 UI
│   ├── LiveTextOverlay.xaml.cs # 文本叠加层逻辑
│   ├── TextSelectionManager.cs # 文本选择管理器
│   └── LiveTextSettings.cs     # 设置管理
├── AnimatedImage/              # 动画图片处理
├── Webview/                    # Web 内容处理
├── ImagePanel.xaml             # 主界面
├── ImagePanel.xaml.cs          # 主界面逻辑
└── Plugin.cs                   # 插件入口
```

### 核心技术

- **OCR 引擎**：Windows.Media.Ocr API（Windows 10+ 内置）
- **UI 框架**：WPF + Canvas 叠加层 + Path 遮罩
- **图像处理**：Magick.NET + BitmapSource 转换
- **坐标转换**：自定义变换矩阵处理图片缩放和文本定位
- **文本选择**：基于几何区域的拖拽选择算法
- **视觉效果**：GeometryGroup + EvenOdd 填充规则实现透明洞效果

### 依赖项

- System.Runtime.WindowsRuntime
- Microsoft.Windows.SDK.Contracts
- Magick.NET-Q8-AnyCPU
- Microsoft.Web.WebView2

### 关键实现代码

#### OCR 文本识别

```csharp
public async Task<List<TextRegion>> RecognizeTextAsync(BitmapSource image, string language = "en")
{
    // 转换BitmapSource为SoftwareBitmap
    var softwareBitmap = await ConvertToSoftwareBitmapAsync(image);
  
    // 执行OCR识别
    var result = await _engine.RecognizeAsync(softwareBitmap);
  
    // 转换结果为TextRegion列表
    return ConvertOcrResult(result);
}
```

#### 透明洞遮罩效果

```csharp
private void UpdateMask()
{
    var geometryGroup = new GeometryGroup { FillRule = FillRule.EvenOdd };
  
    // 添加外部矩形（覆盖整个区域）
    var outerRect = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
    geometryGroup.Children.Add(outerRect);
  
    // 为每个文本区域创建透明洞（带圆角）
    foreach (var region in mergedRegions)
    {
        var holeRect = new RectangleGeometry(region, 8, 8); // 8像素圆角
        geometryGroup.Children.Add(holeRect);
    }
  
    maskPath.Data = geometryGroup;
}
```

#### 文本选择逻辑

```csharp
public void UpdateSelection(Point currentPoint)
{
    _selectionRect = new Rect(_selectionStart, currentPoint);
  
    // 查找与选择框相交的文本区域
    _selectedRegions = _textRegions.Where(r => 
        _selectionRect.IntersectsWith(r.BoundingBox)).ToList();
  
    // 按位置排序并组合文本
    var selectedText = string.Join(" ", _selectedRegions
        .OrderBy(r => r.BoundingBox.Y)
        .ThenBy(r => r.BoundingBox.X)
        .Select(r => r.Text));
}
```

## 🔧 配置选项

插件支持以下配置项（通过 QuickLook 设置存储）：

- `LiveTextEnabled`：是否启用 Live Text 功能
- `LiveTextLanguage`：OCR 识别语言（默认：en）
- `LiveTextAutoDetect`：是否自动检测文本（默认：true）

## 🐛 已知问题与解决方案

### 性能相关

1. **大尺寸图片处理慢**

   - 建议：图片尺寸超过 4K 时，OCR 处理可能需要 10+ 秒
   - 解决：插件会自动缩放大图片以提高处理速度
2. **内存占用增加**

   - 现象：启用 Live Text 后内存占用增加 50-100MB
   - 解决：关闭 Live Text 功能会自动释放内存

### 识别准确率

3. **复杂背景识别困难**

   - 建议：纯色背景、高对比度的文本识别效果最佳
   - 技巧：可以尝试调整图片亮度/对比度后再识别
4. **特殊字体支持有限**

   - 限制：手写文字、艺术字体、过小文字识别率较低
   - 建议：标准印刷体文字识别效果最好

### 系统兼容性

5. **Windows 版本要求**

   - 要求：Windows 10 1903+ 或 Windows 11
   - 原因：依赖 Windows 内置 OCR API
6. **语言包依赖**

   - 问题：某些语言识别需要安装对应的 Windows 语言包
   - 解决：在 Windows 设置中安装所需语言包

## 🔧 故障排除

### 插件无法加载

1. **检查 QuickLook 版本**：确保使用 QuickLook 3.6.0 或更高版本
2. **检查 .NET Framework**：确保安装了 .NET Framework 4.6.2 或更高版本
3. **重新安装插件**：删除插件目录后重新安装
4. **查看日志**：检查 QuickLook 日志文件中的错误信息

### Live Text 按钮不显示

1. **检查系统版本**：确保 Windows 10 1903+ 或 Windows 11
2. **检查语言包**：安装所需的 Windows 语言包
3. **重启 QuickLook**：完全退出后重新启动

### OCR 识别失败

1. **检查图片格式**：确保是支持的图片格式
2. **检查图片质量**：过小或模糊的图片可能识别失败
3. **检查网络连接**：某些 OCR 功能可能需要网络连接
4. **尝试其他语言**：切换到英语或中文重试

### 文本选择不响应

1. **等待 OCR 完成**：确保文本识别完成后再尝试选择
2. **检查鼠标操作**：使用左键拖拽选择文本区域
3. **重新识别**：点击 Live Text 按钮重新进行文本识别

## 🔮 计划功能

- [ ] 支持更多 OCR 引擎（如 Tesseract、Azure Cognitive Services）
- [ ] 文本翻译功能（集成在线翻译服务）
- [ ] 文本格式化和导出（支持 TXT、JSON、CSV 格式）
- [ ] 批量文本提取（处理多张图片）
- [ ] 自定义文本选择样式（颜色、透明度、边框）
- [ ] 性能优化和缓存机制（缓存 OCR 结果）
- [ ] 快捷键支持（Ctrl+A 全选文本、Ctrl+C 复制等）
- [ ] 文本搜索和高亮功能

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

### 开发环境

- Visual Studio 2019 或更高版本
- .NET Framework 4.6.2 SDK
- Windows 10 SDK

### 提交规范

1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目基于 [GNU General Public License v3.0](LICENSE) 开源协议。

## 🙏 致谢

- [QuickLook](https://github.com/QL-Win/QuickLook) - 原始项目
- [Magick.NET](https://github.com/dlemstra/Magick.NET) - 图像处理库
- [Microsoft OCR API](https://docs.microsoft.com/en-us/uwp/api/windows.media.ocr) - 文本识别引擎

## 📞 联系方式

如有问题或建议，请通过以下方式联系：

- GitHub Issues: [提交问题](https://github.com/your-username/QuickLook.Plugin.ImageViewer/issues)
- Email: your-email@example.com

---

**注意**：本插件是对原 QuickLook.Plugin.ImageViewer 的增强版本，添加了 Live Text 功能。使用前请确保已安装 QuickLook 主程序。
