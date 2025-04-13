using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages screen fade effects for transitions
/// </summary>
public class ScreenFader : MonoBehaviour
{
    // The image used for fading
    public Image fadeImage;
    
    // Default fade duration
    public float defaultFadeDuration = 0.5f;
    
    // The default color to fade to/from (usually black)
    public Color fadeColor = Color.black;
    
    private void Awake()
    {
        // If no fade image is assigned, try to find one
        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
            
            if (fadeImage == null)
            {
                Debug.LogError("No Image component found for ScreenFader. Please add an Image component.");
            }
        }
        
        // Make sure the image starts fully transparent
        if (fadeImage != null)
        {
            // Set the initial color to transparent
            Color startColor = fadeColor;
            startColor.a = 0;
            fadeImage.color = startColor;
            
            // Make sure the image is displayed but invisible
            fadeImage.enabled = true;
        }
    }
    
    /// <summary>
    /// Fades the screen to the fade color
    /// </summary>
    /// <param name="duration">Duration of the fade in seconds</param>
    /// <returns>Coroutine for yield waiting</returns>
    public IEnumerator FadeOut(float duration = -1)
    {
        if (fadeImage == null) yield break;
        
        // Use default duration if not specified
        if (duration <= 0) duration = defaultFadeDuration;
        
        // Ensure image is enabled
        fadeImage.enabled = true;
        
        // Animate from transparent to opaque
        float elapsedTime = 0;
        Color startColor = fadeImage.color;
        Color targetColor = fadeColor;
        targetColor.a = 1; // Fully opaque
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        // Ensure we end at target color
        fadeImage.color = targetColor;
    }
    
    /// <summary>
    /// Fades the screen from the fade color to transparent
    /// </summary>
    /// <param name="duration">Duration of the fade in seconds</param>
    /// <returns>Coroutine for yield waiting</returns>
    public IEnumerator FadeIn(float duration = -1)
    {
        if (fadeImage == null) yield break;
        
        // Use default duration if not specified
        if (duration <= 0) duration = defaultFadeDuration;
        
        // Animate from opaque to transparent
        float elapsedTime = 0;
        Color startColor = fadeImage.color;
        Color targetColor = fadeColor;
        targetColor.a = 0; // Fully transparent
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        // Ensure we end at target color
        fadeImage.color = targetColor;
        
        // Disable image when fully transparent to avoid blocking raycasts
        if (targetColor.a <= 0)
        {
            fadeImage.enabled = false;
        }
    }
    
    /// <summary>
    /// Static method to get or create a ScreenFader in the scene
    /// </summary>
    /// <returns>A reference to a ScreenFader component</returns>
    public static ScreenFader GetOrCreateFader()
    {
        // Try to find existing fader
        ScreenFader fader = FindObjectOfType<ScreenFader>();
        
        if (fader != null)
            return fader;
            
        // Create new fader if none exists
        GameObject faderObj = new GameObject("ScreenFader");
        Canvas canvas = FindObjectOfType<Canvas>();
        
        // If no canvas exists, create one
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("FaderCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Parent the fader to the canvas
        faderObj.transform.SetParent(canvas.transform, false);
        
        // Set up the fader components
        RectTransform rect = faderObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        // Add image component
        Image image = faderObj.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0);
        
        // Add fader component
        fader = faderObj.AddComponent<ScreenFader>();
        fader.fadeImage = image;
        
        return fader;
    }
} 