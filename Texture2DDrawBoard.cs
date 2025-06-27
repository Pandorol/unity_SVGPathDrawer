using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class Texture2DDrawBoard : MonoBehaviour
    {
        public int textureWidth = 1024;
        public int textureHeight = 1024;
        public int brushSize = 8;
        public Color brushColor = Color.black;

        private Texture2D drawTexture;
        private RawImage rawImage;
        private RectTransform rectTransform;

        private Camera _camera;
        void Start()
        {
            rawImage = GetComponent<RawImage>();
            rectTransform = GetComponent<RectTransform>();

            drawTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            drawTexture.filterMode = FilterMode.Point;

            Debug.Log($"[Init] Created drawTexture ({textureWidth}x{textureHeight})");

            ClearTexture();

            rawImage.texture = drawTexture;
            Debug.Log($"[Init] Assigned drawTexture to RawImage.");
            Debug.Log($"[Canvas Check] Canvas: {GetComponentInParent<Canvas>()?.renderMode}");
            Canvas canvas = GetComponentInParent<Canvas>();
            _camera = canvas != null ? canvas.worldCamera : Camera.main;

        }

        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                Debug.Log($"[Rect] Width: {rectTransform.rect.width}, Height: {rectTransform.rect.height}");

                Vector2 localPos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, _camera, out localPos))
                {
                    Debug.Log($"[Draw] Local Point: {localPos}");

                    float normX = (localPos.x + rectTransform.rect.width / 2f) / rectTransform.rect.width;
                    float normY = (localPos.y + rectTransform.rect.height / 2f) / rectTransform.rect.height;

                    int texX = Mathf.RoundToInt(normX * textureWidth);
                    int texY = Mathf.RoundToInt(normY * textureHeight);

                    Debug.Log($"[Draw] Texture Pos: ({texX},{texY})");

                    DrawCircle(texX, texY);
                }
                else
                {
                    Debug.Log("[Draw] Pointer not over drawing area.");
                }
            }
        }

        void DrawCircle(int cx, int cy)
        {
            Debug.Log($"[DrawCircle] Drawing at ({cx},{cy})");

            for (int x = -brushSize; x <= brushSize; x++)
            {
                for (int y = -brushSize; y <= brushSize; y++)
                {
                    if (x * x + y * y <= brushSize * brushSize)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                        {
                            drawTexture.SetPixel(px, py, brushColor);
                        }
                    }
                }
            }

            drawTexture.Apply();
            Debug.Log($"[DrawCircle] Applied texture changes.");
        }

        public void ClearTexture()
        {
            Debug.Log("[Clear] Resetting texture to white.");

            Color[] fillColor = new Color[textureWidth * textureHeight];
            for (int i = 0; i < fillColor.Length; i++) fillColor[i] = Color.white;

            drawTexture.SetPixels(fillColor);
            drawTexture.Apply();
        }
    }
}
