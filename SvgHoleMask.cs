using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [RequireComponent(typeof(RawImage))]
    public class SvgHoleMask : MonoBehaviour
    {
        [Header("SVG路径数组，每个元素是一个Path字符串")]
        [TextArea(3, 20)]
        public string[] svgPaths;

        [Header("生成纹理尺寸")]
        public int textureWidth = 500;
        public int textureHeight = 500;

        [Header("SVG画布最大尺寸（宽高相同）")]
        public float svgSize = 1024f;

        [Header("是否上下翻转Y轴")]
        public bool flipY = true;
        [Header("SVG Y轴预偏移（如有 transform translate）flipY = true是生效") ]
        public float svgYOffset = -900f;

        [Header("遮罩颜色（默认黑色半透明）")]
        public Color maskColor = new Color(0, 0, 0, 0.8f);

        [Header("贝塞尔采样精度，越大越平滑但耗性能")]
        [Range(2, 30)]
        public int bezierSampleCount = 15;

        protected RawImage rawImage;
        protected Texture2D maskTexture;

        protected List<List<Vector2Int>> holePixelRegions = new List<List<Vector2Int>>();


        protected virtual void Start()
        {
            Debug.Log($"flipY={flipY}, svgYOffset={svgYOffset}");

            rawImage = GetComponent<RawImage>();
            Debug.Log("[SvgHoleMask] Start - Begin parsing all SVG paths");

            List<List<Vector2>> allPaths = new List<List<Vector2>>();


            // 1. 解析所有路径并统计全局最大 X/Y 坐标
            foreach (string path in svgPaths)
            {
                List<Vector2> points = ParseSvgPath(path);
                allPaths.Add(points);

 
            }

  

            // 2. 生成遮罩纹理
            maskTexture = GenerateMaskTexture(allPaths);

            rawImage.texture = maskTexture;
            rawImage.raycastTarget = true;

            Debug.Log("[SvgHoleMask] Mask texture applied to RawImage");
        }

        protected List<Vector2> ParseSvgPath(string path)
        {
            List<Vector2> points = new List<Vector2>();
            var tokens = Regex.Matches(path, @"[MLQZmlqz]|-?\d+\.?\d*");
            int idx = 0;
            Vector2 currentPos = Vector2.zero;

            while (idx < tokens.Count)
            {
                string cmd = tokens[idx++].Value;

                if (cmd == "M" || cmd == "L")
                {
                    float x = float.Parse(tokens[idx++].Value);
                    float y = float.Parse(tokens[idx++].Value);
                    currentPos = new Vector2(x, y);
                    points.Add(currentPos);
                }
                else if (cmd == "Q")
                {
                    float cx = float.Parse(tokens[idx++].Value);
                    float cy = float.Parse(tokens[idx++].Value);
                    float x = float.Parse(tokens[idx++].Value);
                    float y = float.Parse(tokens[idx++].Value);
                    Vector2 control = new Vector2(cx, cy);
                    Vector2 end = new Vector2(x, y);

                    for (int i = 1; i <= bezierSampleCount; i++)
                    {
                        float t = i / (float)bezierSampleCount;
                        Vector2 p = CalculateQuadraticBezierPoint(t, currentPos, control, end);
                        points.Add(p);
                    }
                    currentPos = end;
                }
                else if (cmd == "Z" || cmd == "z")
                {
                    if (points.Count > 0)
                        points.Add(points[0]); // 闭合路径
                }
            }
            return points;
        }

        protected Vector2 CalculateQuadraticBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            return uu * p0 + 2 * u * t * p1 + tt * p2;
        }

        protected Texture2D GenerateMaskTexture(List<List<Vector2>> allPaths)
        {
            Debug.Log("[SvgHoleMask] Generating mask texture...");
            Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            // 背景填充为遮罩色
            Color[] fill = new Color[textureWidth * textureHeight];
            for (int i = 0; i < fill.Length; i++) fill[i] = maskColor;
            tex.SetPixels(fill);

            int totalTransparent = 0;

            foreach (var path in allPaths)
            {
                List<Vector2> texPoints = new List<Vector2>();
                foreach (var p in path)
                    texPoints.Add(LocalPointToTextureCoords(p));

                List<Vector2Int> pixelRegion = new List<Vector2Int>();
                // 扫描线填充区域为透明
                for (int y = 0; y < textureHeight; y++)
                {
                    List<int> nodeX = new List<int>();
                    int j = texPoints.Count - 1;
                    for (int i = 0; i < texPoints.Count; i++)
                    {
                        float yi = texPoints[i].y;
                        float yj = texPoints[j].y;

                        if ((yi < y && yj >= y) || (yj < y && yi >= y))
                        {
                            float xi = texPoints[i].x;
                            float xj = texPoints[j].x;
                            float x = xi + (y - yi) / (yj - yi) * (xj - xi);
                            nodeX.Add((int)x);
                        }
                        j = i;
                    }

                    nodeX.Sort();
                    for (int i = 0; i + 1 < nodeX.Count; i += 2)
                    {
                        int startX = Mathf.Clamp(nodeX[i], 0, textureWidth - 1);
                        int endX = Mathf.Clamp(nodeX[i + 1], 0, textureWidth - 1);
                        for (int x = startX; x <= endX; x++)
                        {
                            tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                            pixelRegion.Add(new Vector2Int(x, y));
                            totalTransparent++;
                        }
                    }
                }
                holePixelRegions.Add(pixelRegion);
            }
            
            tex.Apply();
            Debug.Log($"[SvgHoleMask] Mask texture generated. Transparent pixels: {totalTransparent}");
            return tex;
        }

        protected Vector2 LocalPointToTextureCoords(Vector2 point)
        {
            // 计算缩放比例和偏移（用于将 SVG 映射到纹理）
            float scaleX = (textureWidth - 1) / svgSize;
            float scaleY = (textureHeight - 1) / svgSize;
            float scale = Mathf.Min(scaleX, scaleY);

            float offsetX = (textureWidth - svgSize * scale) / 2f;
            float offsetY = (textureHeight - svgSize * scale) / 2f;

            Debug.Log($"point.y: {point.y}");
            // 根据 flipY 决定是否执行 Y 轴翻转
            float py = flipY
                ? textureHeight-((-(svgYOffset + point.y)) * scale) + offsetY  // 翻转Y轴
                :  point.y * scale + offsetY;                // 保持Y轴不变

            /*            float py = flipY
                            ? (point.y - svgYOffset) * -scale + offsetY
                            : point.y * scale + offsetY;*/


            float px = point.x * scale + offsetX;

            return new Vector2(px, py);
        }



    }
}
