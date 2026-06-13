using UnityEngine;

namespace HorseBetting.UI
{
    /// <summary>
    /// Component attached to each horse GameObject in the race view.
    /// Manages the sprite renderer, label, lane position, and color for a single horse.
    /// Uses placeholder colored rectangles with support for future sprite replacement.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class RaceHorseSprite : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TextMesh _nameLabel;

        [Header("Settings")]
        [SerializeField] private float _laneHeight = 1.2f;

        private int _horseIndex;
        private int _laneIndex;

        /// <summary>
        /// The horse index (0-7) this sprite represents.
        /// </summary>
        public int HorseIndex => _horseIndex;

        /// <summary>
        /// The lane index (Y position offset) for this horse.
        /// </summary>
        public int LaneIndex => _laneIndex;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Initialize the horse sprite with its index, lane, color, and name.
        /// </summary>
        /// <param name="horseIndex">Horse index (0-7)</param>
        /// <param name="laneIndex">Lane index for Y positioning</param>
        /// <param name="color">Color for the placeholder sprite</param>
        /// <param name="horseName">Display name (e.g., "Horse 1")</param>
        public void Setup(int horseIndex, int laneIndex, Color color, string horseName)
        {
            _horseIndex = horseIndex;
            _laneIndex = laneIndex;

            SetColor(color);
            SetName(horseName);
            SetLanePosition(laneIndex);
        }

        /// <summary>
        /// Initialize the horse with a loaded sprite asset.
        /// </summary>
        public void SetupWithSprite(int horseIndex, int laneIndex, Sprite sprite, Color tintColor)
        {
            _horseIndex = horseIndex;
            _laneIndex = laneIndex;

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = Color.white; // no tint for real sprites
            transform.localScale = new Vector3(0.8f, 0.8f, 1f); // scale down to fit lanes

            SetLanePosition(laneIndex);
        }

        /// <summary>
        /// Set the color of the placeholder sprite.
        /// </summary>
        public void SetColor(Color color)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = color;
        }

        /// <summary>
        /// Set a custom sprite (for future sprite replacement).
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (_spriteRenderer != null && sprite != null)
                _spriteRenderer.sprite = sprite;
        }

        /// <summary>
        /// Set the horse display name on the label.
        /// </summary>
        public void SetName(string horseName)
        {
            if (_nameLabel != null)
                _nameLabel.text = horseName;
        }

        /// <summary>
        /// Position the horse in its lane (Y offset based on lane index).
        /// </summary>
        public void SetLanePosition(int laneIndex)
        {
            _laneIndex = laneIndex;
            var pos = transform.localPosition;
            pos.y = -laneIndex * _laneHeight;
            transform.localPosition = pos;
        }

        /// <summary>
        /// Set the horizontal (X) position of the horse during animation.
        /// </summary>
        public void SetHorizontalPosition(float x)
        {
            var pos = transform.localPosition;
            pos.x = x;
            transform.localPosition = pos;
        }

        /// <summary>
        /// Creates a 1x1 white pixel sprite to use as a placeholder rectangle.
        /// Call this if no sprite is assigned.
        /// </summary>
        public void CreatePlaceholderSprite()
        {
            if (_spriteRenderer == null) return;

            // Create a simple 1x1 white texture as placeholder
            var texture = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            _spriteRenderer.sprite = sprite;

            // Scale to look like a rectangle (horse-shaped placeholder)
            transform.localScale = new Vector3(1.5f, 0.8f, 1f);
        }

        /// <summary>
        /// Creates the TextMesh label if one doesn't exist.
        /// </summary>
        public void CreateLabel()
        {
            if (_nameLabel != null) return;

            var labelObj = new GameObject("NameLabel");
            labelObj.transform.SetParent(transform);
            labelObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            labelObj.transform.localScale = new Vector3(0.7f, 1f, 1f);

            _nameLabel = labelObj.AddComponent<TextMesh>();
            _nameLabel.fontSize = 24;
            _nameLabel.characterSize = 0.15f;
            _nameLabel.anchor = TextAnchor.MiddleCenter;
            _nameLabel.alignment = TextAlignment.Center;
            _nameLabel.color = Color.white;
        }
    }
}
