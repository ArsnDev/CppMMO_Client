using UnityEngine;
using TMPro;
using DG.Tweening;

namespace SimpleMMO.UI
{
    /// <summary>
    /// Simple component for chat message prefab
    /// This script can be attached to chat message prefab for additional functionality
    /// Uses DOTween for smooth animations
    /// </summary>
    public class ChatMessage : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Settings")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private bool autoFadeIn = true;
        [SerializeField] private Ease fadeInEase = Ease.OutQuad;
        [SerializeField] private Ease fadeOutEase = Ease.InQuad;
        
        private Tween currentTween;
        
        void Start()
        {
            if (autoFadeIn)
                FadeIn();
        }
        
        void OnDestroy()
        {
            // Kill any active tweens to prevent memory leaks
            currentTween?.Kill();
        }
        
        public void SetMessage(string message)
        {
            if (messageText != null)
                messageText.text = message;
        }
        
        public void FadeIn()
        {
            if (canvasGroup != null)
            {
                // Kill any existing tween
                currentTween?.Kill();
                
                canvasGroup.alpha = 0f;
                currentTween = canvasGroup.DOFade(1f, fadeInDuration)
                    .SetEase(fadeInEase)
                    .SetUpdate(true); // Use unscaled time
            }
        }
        
        public void FadeOut(float duration = 0.5f, System.Action onComplete = null)
        {
            if (canvasGroup != null)
            {
                // Kill any existing tween
                currentTween?.Kill();
                
                currentTween = canvasGroup.DOFade(0f, duration)
                    .SetEase(fadeOutEase)
                    .SetUpdate(true)
                    .OnComplete(() => 
                    {
                        onComplete?.Invoke();
                        Destroy(gameObject);
                    });
            }
            else
            {
                onComplete?.Invoke();
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Animate message appearance with a slight scale effect
        /// </summary>
        public void AnimateIn()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                transform.localScale = Vector3.one * 0.8f;
                
                // Sequence animation: fade in + scale up
                var sequence = DOTween.Sequence();
                sequence.Join(canvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeInEase));
                sequence.Join(transform.DOScale(Vector3.one, fadeInDuration).SetEase(Ease.OutBack));
                
                currentTween = sequence;
            }
        }
        
        /// <summary>
        /// Quick bounce animation for new messages
        /// </summary>
        public void Bounce()
        {
            currentTween?.Kill();
            currentTween = transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 2, 0.5f);
        }
    }
}