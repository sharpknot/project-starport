using Starport.Subsystems;
using UnityEngine;

namespace Starport
{
    public class UICPPowerFrequencyTextureRenderer : MonoBehaviour
    {
        public RenderTexture RenderTextureOutput;
        [SerializeField] private int _width = 512;
        [SerializeField] private int _height = 256;

        [SerializeField] private Color32 _targetColor = Color.green;
        [SerializeField] private Color32 _currentColor = Color.cyan;
        [SerializeField] private Color32 _matchedColor = Color.yellow;
        [SerializeField] private Color32 _backgroundColor = Color.black;

        [SerializeField, Range(0.1f, 0.49f)]
        private float _amplitude = 0.45f;
        [SerializeField]
        private float _scrollSpeed = 1f;
        [SerializeField] private float _secondsPerScreen = 1.0f;

        private Texture2D _cpuTexture;
        private PowerRegulatorSubsystem _power;
        private float _time;

        void Awake()
        {
            _power = GetComponent<PowerRegulatorSubsystem>();

            // Create CPU working texture
            _cpuTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            _cpuTexture.filterMode = FilterMode.Point;
            _cpuTexture.wrapMode = TextureWrapMode.Clamp;

            // Create or resize RenderTexture if needed
            if (RenderTextureOutput == null)
            {
                RenderTextureOutput = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGB32);
            }
            else if (RenderTextureOutput.width != _width || RenderTextureOutput.height != _height)
            {
                RenderTextureOutput.Release();
                RenderTextureOutput.width = _width;
                RenderTextureOutput.height = _height;
            }

            RenderTextureOutput.filterMode = FilterMode.Point;
            RenderTextureOutput.wrapMode = TextureWrapMode.Clamp;
            RenderTextureOutput.Create();
        }

        void Update()
        {
            if (_power == null || !_power.IsSpawned)
                return;

            // SAFE TIME WRAP (prevents float precision issues)
            _time += Time.deltaTime * _scrollSpeed;
            if (_time > Mathf.PI * 2f)
                _time -= Mathf.PI * 2f;

            DrawWaveform(
                _power.TargetFrequency,
                _power.CurrentFrequency,
                _power.IsWithinTargetFrequency
            );

            // Push CPU texture into shared RenderTexture
            Graphics.Blit(_cpuTexture, RenderTextureOutput);
        }

        void DrawWaveform(float targetHz, float currentHz, bool isMatched)
        {
            Color32[] pixels = _cpuTexture.GetPixels32();

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = _backgroundColor;

            Color liveColor = isMatched ? _matchedColor : _currentColor;

            float horizontalScale = 1f / Mathf.Max(0.0001f, _secondsPerScreen);

            for (int x = 0; x < _width; x++)
            {
                float t = (float)x / _width;

                float phaseTarget = (t * horizontalScale * targetHz * Mathf.PI * 2f) + _time;
                float phaseCurrent = (t * horizontalScale * currentHz * Mathf.PI * 2f) + _time;

                float targetY = Mathf.Sin(phaseTarget);
                float currentY = Mathf.Sin(phaseCurrent);

                int targetPixelY = Mathf.RoundToInt((targetY * _amplitude + 0.5f) * _height);
                int currentPixelY = Mathf.RoundToInt((currentY * _amplitude + 0.5f) * _height);

                if (targetPixelY >= 0 && targetPixelY < _height)
                    pixels[targetPixelY * _width + x] = _targetColor;

                if (currentPixelY >= 0 && currentPixelY < _height)
                    pixels[currentPixelY * _width + x] = liveColor;
            }

            _cpuTexture.SetPixels32(pixels);
            _cpuTexture.Apply(false);
        }
    }
}
