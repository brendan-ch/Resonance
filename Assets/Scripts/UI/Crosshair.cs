using UnityEngine;
using UnityEngine.UI;

namespace Resonance.UI
{
    public class Crosshair : MonoBehaviour
    {
        [Header("Crosshair Settings")]
        [SerializeField] private Color crosshairColor = Color.white;
        [SerializeField] private float crosshairSize = 10f;
        [SerializeField] private float crosshairThickness = 2f;
        [SerializeField] private float crosshairGap = 5f;
        
        private Image[] crosshairLines;

        private void Start()
        {
            CreateCrosshair();
        }

        private void CreateCrosshair()
        {
            crosshairLines = new Image[4];
            
            // Top line
            crosshairLines[0] = CreateLine("Top", 0, crosshairGap + crosshairSize / 2, crosshairThickness, crosshairSize);
            
            // Bottom line
            crosshairLines[1] = CreateLine("Bottom", 0, -(crosshairGap + crosshairSize / 2), crosshairThickness, crosshairSize);
            
            // Left line
            crosshairLines[2] = CreateLine("Left", -(crosshairGap + crosshairSize / 2), 0, crosshairSize, crosshairThickness);
            
            // Right line
            crosshairLines[3] = CreateLine("Right", crosshairGap + crosshairSize / 2, 0, crosshairSize, crosshairThickness);
        }

        private Image CreateLine(string name, float xPos, float yPos, float width, float height)
        {
            GameObject line = new GameObject(name);
            line.transform.SetParent(transform, false);
            
            Image img = line.AddComponent<Image>();
            img.color = crosshairColor;
            
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(xPos, yPos);
            rect.sizeDelta = new Vector2(width, height);
            
            return img;
        }

        public void SetCrosshairColor(Color color)
        {
            crosshairColor = color;
            foreach (var line in crosshairLines)
            {
                if (line != null)
                {
                    line.color = color;
                }
            }
        }
    }
}