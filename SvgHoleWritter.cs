using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Vector2ArrayWrapper
{
    public Vector2[] points;
}

namespace GameLogic
{
    [RequireComponent(typeof(RawImage))]
    public class SvgHoleWritter : SvgHoleMask
    {
        [Header("中点路径数组，数量与svgPaths对应，每个对应一条路径")]
        public List<Vector2ArrayWrapper> medianPaths;

        [Header("中点路径填充颜色")]
        public Color medianColor = Color.red;

        [Header("填充动画时长（秒）")]
        public float fillDuration = 2f;

        protected override void Start()
        {
            base.Start();
            StartCoroutine(AnimateAllMedianFills());
        }

        private IEnumerator AnimateAllMedianFills()
        {
            if (medianPaths == null || medianPaths.Count == 0 || holePixelRegions == null)
                yield break;

            int count = Mathf.Min(medianPaths.Count, holePixelRegions.Count);

            for (int i = 0; i < count; i++)
            {
                Vector2[] medianPath = medianPaths[i].points;  // <-- 这里改正

                if (medianPath == null || medianPath.Length < 2)
                    continue;

                // 转换中点路径为纹理坐标
                List<Vector2> medianPoints = new List<Vector2>();
                foreach (var p in medianPath)
                {
                    medianPoints.Add(LocalPointToTextureCoords(p));
                }

                // 计算中点路径总长度
                float totalLength = 0f;
                List<float> segmentLengths = new List<float>();
                for (int j = 1; j < medianPoints.Count; j++)
                {
                    float segLen = Vector2.Distance(medianPoints[j - 1], medianPoints[j]);
                    segmentLengths.Add(segLen);
                    totalLength += segLen;
                }

                List<Vector2Int> pixels = holePixelRegions[i];
                if (pixels == null || pixels.Count == 0)
                    continue;

                // 计算每个像素点相对于路径的进度
                List<(Vector2Int pixel, float progress)> pixelProgresses = new List<(Vector2Int, float)>();
                foreach (var pix in pixels)
                {
                    Vector2 pixelPos = new Vector2(pix.x, pix.y);
                    float minDist = float.MaxValue;
                    float progressAtMinDist = 0f;

                    float accLen = 0f;
                    for (int j = 1; j < medianPoints.Count; j++)
                    {
                        Vector2 p1 = medianPoints[j - 1];
                        Vector2 p2 = medianPoints[j];
                        Vector2 seg = p2 - p1;
                        float segLen = seg.magnitude;
                        if (segLen < 0.001f) continue;
                        Vector2 dir = seg / segLen;

                        Vector2 toPix = pixelPos - p1;
                        float proj = Vector2.Dot(toPix, dir);
                        proj = Mathf.Clamp(proj, 0, segLen);
                        Vector2 projPoint = p1 + dir * proj;
                        float dist = Vector2.Distance(pixelPos, projPoint);

                        if (dist < minDist)
                        {
                            minDist = dist;
                            progressAtMinDist = (accLen + proj) / totalLength;
                        }
                        accLen += segLen;
                    }
                    pixelProgresses.Add((pix, progressAtMinDist));
                }

                // 按进度排序
                pixelProgresses.Sort((a, b) => a.progress.CompareTo(b.progress));

                // 动画填充
                float timer = 0f;
                int totalPixels = pixelProgresses.Count;
                while (timer < fillDuration)
                {
                    float t = timer / fillDuration;
                    int fillCount = Mathf.FloorToInt(t * totalPixels);

                    for (int k = 0; k < fillCount; k++)
                    {
                        var p = pixelProgresses[k].pixel;
                        maskTexture.SetPixel(p.x, p.y, medianColor);
                    }
                    maskTexture.Apply();

                    timer += Time.deltaTime;
                    yield return null;
                }

                // 确保填满
                foreach (var p in pixelProgresses)
                {
                    maskTexture.SetPixel(p.pixel.x, p.pixel.y, medianColor);
                }
                maskTexture.Apply();
            }
        }
    }
}
