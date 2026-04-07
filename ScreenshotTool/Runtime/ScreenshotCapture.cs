using System;
using System.Collections;
using System.IO;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ScreenshotTool
{
    /// <summary>
    /// Attach this component to any GameObject in your scene.
    /// Supports both Legacy Input Manager and the new Input System package.
    /// Screenshots are saved to Application.dataPath/../Screenshots/
    /// </summary>
    public class ScreenshotCapture : MonoBehaviour
    {
        public enum AutoCaptureInterval
        {
            Off = 0,
            Every5Seconds = 5,
            Every10Seconds = 10,
            Every15Seconds = 15
        }

        [Header("Capture Settings")]

#if ENABLE_INPUT_SYSTEM
        [Tooltip("Key to press for capturing a screenshot (New Input System)")]
        public Key captureKey = Key.F12;
#else
        [Tooltip("Key to press for capturing a screenshot (Legacy Input)")]
        public KeyCode captureKey = KeyCode.F12;
#endif

        [Tooltip("Resolution multiplier (1 = native, 2 = 2x, etc.)")]
        [Range(1, 4)]
        public int superSize = 1;

        [Tooltip("Automatically capture screenshots at fixed intervals")]
        public AutoCaptureInterval autoCaptureInterval = AutoCaptureInterval.Off;

        [Tooltip("Play a sound when screenshot is taken")]
        public AudioClip captureSound;

        [Header("Save Settings")]
        [Tooltip("Custom folder name inside the Screenshots directory")]
        public string folderName = "Screenshots";

        [Tooltip("File name prefix")]
        public string filePrefix = "screenshot";

        [Tooltip("Include timestamp in file name")]
        public bool includeTimestamp = true;

        private string _savePath;
        private AudioSource _audioSource;
        private bool _isCapturing = false;
        private bool _warnedInvalidKey = false;
        private float _nextAutoCaptureTime = -1f;
        private AutoCaptureInterval _lastAutoCaptureInterval = AutoCaptureInterval.Off;

        public static event Action<string> OnScreenshotCaptured;

        void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            SanitizeCaptureKey();
#endif
            _savePath = Path.Combine(Application.dataPath, "..", folderName);

            if (!Directory.Exists(_savePath))
                Directory.CreateDirectory(_savePath);

            if (captureSound != null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.clip = captureSound;
            }

            Debug.Log($"[ScreenshotTool] Save path: {Path.GetFullPath(_savePath)}");

            ResetAutoCaptureTimer();
        }

        void Update()
        {
            if (_isCapturing) return;

            bool keyPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (!IsInputSystemKeyValid(captureKey))
            {
                SanitizeCaptureKey();
                return;
            }

            keyPressed = Keyboard.current != null && Keyboard.current[captureKey].wasPressedThisFrame;
#else
            keyPressed = Input.GetKeyDown(captureKey);
#endif

            if (keyPressed || ShouldAutoCaptureNow())
                StartCoroutine(CaptureScreenshot());
        }

#if ENABLE_INPUT_SYSTEM
        private void OnValidate()
        {
            SanitizeCaptureKey();
        }

        private bool IsInputSystemKeyValid(Key key)
        {
            return Enum.IsDefined(typeof(Key), key);
        }

        private void SanitizeCaptureKey()
        {
            if (IsInputSystemKeyValid(captureKey))
                return;

            int rawValue = (int)captureKey;

            // Handle migrated values from previous KeyCode-based serialization.
            if (rawValue == (int)KeyCode.F12)
            {
                captureKey = Key.F12;
                return;
            }

            captureKey = Key.F12;

            if (!_warnedInvalidKey)
            {
                Debug.LogWarning(
                    $"[ScreenshotTool] Invalid capture key value ({rawValue}) detected. Falling back to {captureKey}. " +
                    "Re-select the capture key in the Inspector.");
                _warnedInvalidKey = true;
            }
        }
#endif

        private IEnumerator CaptureScreenshot()
        {
            _isCapturing = true;

            yield return new WaitForEndOfFrame();

            string fileName = BuildFileName();
            string fullPath = Path.Combine(_savePath, fileName);

            ScreenCapture.CaptureScreenshot(fullPath, superSize);

            _audioSource?.Play();

            yield return new WaitForSeconds(0.1f);

            Debug.Log($"[ScreenshotTool] Screenshot saved: {fullPath}");
            OnScreenshotCaptured?.Invoke(fullPath);

            _isCapturing = false;
            ResetAutoCaptureTimer();
        }

        private string BuildFileName()
        {
            string timestamp = includeTimestamp
                ? DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
                : DateTime.Now.Ticks.ToString();

            return $"{filePrefix}_{timestamp}.png";
        }

        public string GetSavePath() => Path.GetFullPath(_savePath);

        private bool ShouldAutoCaptureNow()
        {
            if (_lastAutoCaptureInterval != autoCaptureInterval)
                ResetAutoCaptureTimer();

            float seconds = GetAutoCaptureSeconds();
            if (seconds <= 0f)
                return false;

            if (_nextAutoCaptureTime < 0f)
                ResetAutoCaptureTimer();

            return Time.unscaledTime >= _nextAutoCaptureTime;
        }

        private void ResetAutoCaptureTimer()
        {
            _lastAutoCaptureInterval = autoCaptureInterval;
            float seconds = GetAutoCaptureSeconds();
            _nextAutoCaptureTime = seconds > 0f ? Time.unscaledTime + seconds : -1f;
        }

        private float GetAutoCaptureSeconds()
        {
            return (float)autoCaptureInterval;
        }
    }
}
