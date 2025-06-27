UI创建一个RawImag，挂上脚本即可
SvgHoleMask.cs
功能说明：
此组件用于根据 SVG 路径生成一个遮罩纹理（Texture2D），并将路径所围成的区域镂空（设为透明）。常用于需要显示“填涂区域”的 UI 场景。

核心功能：

支持解析 SVG Path 格式中的 M/L/Q/Z 命令（不支持 arc、cubic 等）。

自动转换坐标到 Texture2D 空间，支持 Y 轴翻转、偏移等调整。

使用扫描线算法对 SVG 路径所围区域进行像素级“镂空”处理。

将最终结果设置到绑定的 RawImage 上。

重要字段：

svgPaths[]：每条 SVG 路径字符串。

svgSize：SVG 的原始画布大小。

flipY / svgYOffset：控制 SVG 坐标是否翻转及偏移。

maskColor：遮罩背景色。

holePixelRegions（protected）：每条路径对应的透明像素区域（供子类使用，如动画填涂等）。

SvgHoleWritter.cs
功能说明：
继承自 SvgHoleMask，在已镂空的 SVG 路径区域中，根据给定的中点路径（medianPaths），以“进度条式”的动画方式逐步填涂颜色。

核心功能：

每个 svgPath 对应一条 medianPath。

填涂动画按中点路径方向“推进”，像素与路径越接近的部分越早被填充。

动画过程中每帧渐进式填色，实现“书写/绘制”视觉效果。

重要字段：

medianPaths：中点路径数组，每个元素为一条路径对应的 Vector2[]。

fillDuration：每个路径填充动画的时长。

medianColor：填充颜色。

Texture2DDrawBoard.cs（如该类用于调试/绘制用）
功能说明（推测）：
此类如用于调试或绘制 Texture2D 的基础功能，比如画点、画线、更新贴图等，可配合 SvgHoleMask 系统用于可视化、绘图测试等。

常见功能（视实现而定）：

DrawPixel()、DrawLine()：在 Texture2D 上绘制图元。

Clear()：清空贴图。

Apply()：提交修改至 GPU。

注意事项：

使用 SetPixel() 后需调用 Apply() 才会刷新显示。

建议使用 Color[] 批量处理像素，性能更优。

✍️ 建议使用方式
将 SvgHoleMask 挂在带 RawImage 的 UI 节点上，填入 SVG 路径数据。

如需实现“笔顺/填色动画”，派生出 SvgHoleWritter，在 Inspector 中添加对应的中点路径数组（medianPaths）。

在运行时将路径动画按顺序播放，形成“写字”“填色”等效果。
