using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HorseBetting.Data;

namespace HorseBetting.UI
{
    /// <summary>
    /// 2D race scene using Unity Sprite system.
    /// Displays 8 horse sprites in lanes animating left-to-right based on Final_Speed data.
    /// Shows event trigger notifications during race stages.
    /// </summary>
    public class RaceView : MonoBehaviour
    {
        [Header("Race Settings")]
        [SerializeField] private float _raceDuration = 5f;
        [SerializeField] private float _startX = -7f;
        [SerializeField] private float _finishX = 7f;

        [Header("Track Background")]
        [SerializeField] private SpriteRenderer _trackBackground;

        [Header("Event Notification")]
        [SerializeField] private float _eventNotificationDuration = 2f;
        [SerializeField] private float _eventNotificationYOffset = 4f;

        /// <summary>
        /// Invoked when the race animation completes.
        /// </summary>
        public event Action OnRaceComplete;

        private RaceHorseSprite[] _horses;
        private List<GameObject> _activeNotifications = new List<GameObject>();
        private bool _isAnimating;
        private Coroutine _raceCoroutine;

        // Default horse colors for 8 horses (distinct, visually separable)
        private static readonly Color[] DefaultHorseColors = new Color[]
        {
            new Color(0.9f, 0.2f, 0.2f, 1f),  // Red
            new Color(0.2f, 0.5f, 0.9f, 1f),  // Blue
            new Color(0.2f, 0.8f, 0.3f, 1f),  // Green
            new Color(0.9f, 0.8f, 0.1f, 1f),  // Yellow
            new Color(0.7f, 0.3f, 0.9f, 1f),  // Purple
            new Color(1.0f, 0.5f, 0.0f, 1f),  // Orange
            new Color(0.0f, 0.8f, 0.8f, 1f),  // Cyan
            new Color(0.9f, 0.4f, 0.7f, 1f),  // Pink
        };

        // Track background colors per type
        private static readonly Dictionary<TrackType, Color> TrackColors = new Dictionary<TrackType, Color>
        {
            { TrackType.Grass, new Color(0.3f, 0.6f, 0.2f, 1f) },
            { TrackType.Mud, new Color(0.5f, 0.35f, 0.2f, 1f) },
            { TrackType.Snow, new Color(0.85f, 0.9f, 0.95f, 1f) },
        };

        /// <summary>
        /// Whether the race animation is currently playing.
        /// </summary>
        public bool IsAnimating => _isAnimating;

        /// <summary>
        /// The configured race animation duration in seconds.
        /// </summary>
        public float RaceDuration
        {
            get => _raceDuration;
            set => _raceDuration = value;
        }

        private void Awake()
        {
            InitializeHorses();
        }

        /// <summary>
        /// Create and initialize 8 horse GameObjects with placeholder sprites.
        /// </summary>
        private void InitializeHorses()
        {
            _horses = new RaceHorseSprite[8];

            for (int i = 0; i < 8; i++)
            {
                var horseObj = new GameObject($"Horse_{i + 1}");
                horseObj.transform.SetParent(transform);

                var spriteRenderer = horseObj.AddComponent<SpriteRenderer>();
                spriteRenderer.sortingOrder = i + 1;

                var horseSprite = horseObj.AddComponent<RaceHorseSprite>();
                horseSprite.CreatePlaceholderSprite();
                horseSprite.CreateLabel();
                horseSprite.Setup(i, i, DefaultHorseColors[i], $"Horse {i + 1}");

                // Position at start line
                horseSprite.SetHorizontalPosition(_startX);

                _horses[i] = horseSprite;
            }
        }

        /// <summary>
        /// Set the track type to change the background color/indicator.
        /// </summary>
        /// <param name="trackType">The track type for this race</param>
        public void SetTrackType(TrackType trackType)
        {
            if (_trackBackground != null && TrackColors.TryGetValue(trackType, out var color))
            {
                _trackBackground.color = color;
            }
        }

        /// <summary>
        /// Start the race animation, moving horses from left to right based on their final speeds.
        /// Faster horses reach the finish line first (or more precisely, cover more distance).
        /// </summary>
        /// <param name="result">The race result containing final speeds and stage events</param>
        public void StartRaceAnimation(RaceResult result)
        {
            if (_isAnimating) return;
            if (result.finalSpeeds == null || result.finalSpeeds.Length == 0) return;

            _isAnimating = true;

            // Reset horse positions to start
            for (int i = 0; i < _horses.Length; i++)
            {
                _horses[i].SetHorizontalPosition(_startX);
            }

            _raceCoroutine = StartCoroutine(AnimateRace(result));
        }

        /// <summary>
        /// Coroutine that animates the race over the configured duration.
        /// Horse positions interpolate based on speed ratios (faster = arrives earlier).
        /// Stage events are shown as notifications at 1/3 and 2/3 progress marks.
        /// </summary>
        private IEnumerator AnimateRace(RaceResult result)
        {
            int horseCount = Mathf.Min(result.finalSpeeds.Length, _horses.Length);

            // Find max speed to calculate relative progress ratios
            int maxSpeed = int.MinValue;
            for (int i = 0; i < horseCount; i++)
            {
                if (result.finalSpeeds[i] > maxSpeed)
                    maxSpeed = result.finalSpeeds[i];
            }

            // Prevent division by zero
            if (maxSpeed <= 0) maxSpeed = 1;

            // Calculate speed ratios: each horse's speed relative to the maximum
            float[] speedRatios = new float[horseCount];
            for (int i = 0; i < horseCount; i++)
            {
                // Ratio ranges from ~0.5 to 1.0 so all horses visibly move
                speedRatios[i] = Mathf.Clamp(result.finalSpeeds[i] / (float)maxSpeed, 0.4f, 1.0f);
            }

            float totalDistance = _finishX - _startX;

            // Track which stage events have been shown
            bool[] stageNotified = new bool[3];
            float[] stageThresholds = { 0.33f, 0.66f, 0.95f };

            float elapsed = 0f;

            while (elapsed < _raceDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _raceDuration);

                // Update each horse position
                for (int i = 0; i < horseCount; i++)
                {
                    // Use eased progress with speed ratio
                    float horseProgress = progress * speedRatios[i];
                    float x = _startX + horseProgress * totalDistance;
                    _horses[i].SetHorizontalPosition(x);
                }

                // Show stage event notifications at threshold points
                for (int stage = 0; stage < 3; stage++)
                {
                    if (!stageNotified[stage] && progress >= stageThresholds[stage])
                    {
                        stageNotified[stage] = true;
                        ShowStageEvents(result.stageEvents, stage);
                    }
                }

                yield return null;
            }

            // Snap all horses to final positions
            for (int i = 0; i < horseCount; i++)
            {
                float finalX = _startX + speedRatios[i] * totalDistance;
                _horses[i].SetHorizontalPosition(finalX);
            }

            _isAnimating = false;

            // Clean up any remaining notifications
            yield return new WaitForSeconds(1f);
            ClearNotifications();

            OnRaceComplete?.Invoke();
        }

        /// <summary>
        /// Display floating text notifications for events that occurred in a given stage.
        /// </summary>
        /// <param name="stageEvents">All stage events from the race result</param>
        /// <param name="stageIndex">The stage index (0, 1, or 2)</param>
        private void ShowStageEvents(StageEventResult[][] stageEvents, int stageIndex)
        {
            if (stageEvents == null || stageIndex >= stageEvents.Length) return;
            if (stageEvents[stageIndex] == null || stageEvents[stageIndex].Length == 0) return;

            var events = stageEvents[stageIndex];
            int displayCount = Mathf.Min(events.Length, 3); // Show max 3 events at a time

            for (int i = 0; i < displayCount; i++)
            {
                var evt = events[i];
                if (string.IsNullOrEmpty(evt.eventName)) continue;

                string text = evt.wasProtected
                    ? $"[Stage {stageIndex + 1}] {evt.eventName} → Horse {evt.horseIndex + 1} (BLOCKED!)"
                    : $"[Stage {stageIndex + 1}] {evt.eventName} → Horse {evt.horseIndex + 1} ({evt.speedModifier:+#;-#;0})";

                SpawnEventNotification(text, i);
            }
        }

        /// <summary>
        /// Spawn a floating text notification above the race track.
        /// </summary>
        private void SpawnEventNotification(string text, int offsetIndex)
        {
            var notifObj = new GameObject("EventNotification");
            notifObj.transform.SetParent(transform);
            notifObj.transform.localPosition = new Vector3(
                0f,
                _eventNotificationYOffset + offsetIndex * 0.6f,
                0f
            );

            var textMesh = notifObj.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 20;
            textMesh.characterSize = 0.12f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.yellow;

            _activeNotifications.Add(notifObj);

            // Auto-destroy notification after duration
            StartCoroutine(DestroyAfterDelay(notifObj, _eventNotificationDuration));
        }

        /// <summary>
        /// Destroy a notification GameObject after a delay.
        /// </summary>
        private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
            {
                _activeNotifications.Remove(obj);
                Destroy(obj);
            }
        }

        /// <summary>
        /// Remove all active notification objects.
        /// </summary>
        private void ClearNotifications()
        {
            foreach (var notif in _activeNotifications)
            {
                if (notif != null)
                    Destroy(notif);
            }
            _activeNotifications.Clear();
        }

        /// <summary>
        /// Stop the current race animation if running.
        /// </summary>
        public void StopAnimation()
        {
            if (_raceCoroutine != null)
            {
                StopCoroutine(_raceCoroutine);
                _raceCoroutine = null;
            }
            _isAnimating = false;
            ClearNotifications();
        }

        /// <summary>
        /// Reset all horses to the starting position.
        /// </summary>
        public void ResetPositions()
        {
            StopAnimation();
            if (_horses == null) return;

            for (int i = 0; i < _horses.Length; i++)
            {
                if (_horses[i] != null)
                    _horses[i].SetHorizontalPosition(_startX);
            }
        }

        private void OnDestroy()
        {
            ClearNotifications();
        }
    }
}
