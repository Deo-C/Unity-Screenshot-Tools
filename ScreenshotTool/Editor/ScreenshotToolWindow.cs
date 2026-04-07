using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScreenshotTool.Editor
{
    public class ScreenshotToolWindow : EditorWindow
    {
        private const string PREFS_FOLDER = "ScreenshotTool_Folder";
        private const string PREFS_PREFIX = "ScreenshotTool_Prefix";
        private const string PREFS_SUPERSIZE = "ScreenshotTool_SuperSize";
        private const string PREFS_KEY = "ScreenshotTool_Key";
        private const string PREFS_AUTO_CAPTURE = "ScreenshotTool_AutoCapture";

        private string _folderName;
        private string _filePrefix;
        private int _superSize;
        private KeyCode _captureKey;
        private ScreenshotCapture.AutoCaptureInterval _autoCaptureInterval;

        private List<ScreenshotEntry> _screenshots = new List<ScreenshotEntry>();
        private Vector2 _scrollPos;
        private string _savePath;

        private Texture2D _previewTexture;
        private string _previewPath;

        private bool _settingsFoldout = true;
        private bool _listFoldout = true;

        private static readonly Color HeaderColor = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color ItemEvenColor = new Color(0.22f, 0.22f, 0.25f);
        private static readonly Color ItemOddColor = new Color(0.19f, 0.19f, 0.22f);
        private static readonly Color AccentColor = new Color(0.3f, 0.75f, 0.6f);

        [MenuItem("Tools/Screenshot Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<ScreenshotToolWindow>("Screenshot Tool");
            window.minSize = new Vector2(380, 500);
            window.Show();
        }

        void OnEnable()
        {
            _folderName = EditorPrefs.GetString(PREFS_FOLDER, "Screenshots");
            _filePrefix = EditorPrefs.GetString(PREFS_PREFIX, "screenshot");
            _superSize = EditorPrefs.GetInt(PREFS_SUPERSIZE, 1);
            _captureKey = (KeyCode)EditorPrefs.GetInt(PREFS_KEY, (int)KeyCode.F12);
            _autoCaptureInterval = (ScreenshotCapture.AutoCaptureInterval)EditorPrefs.GetInt(
                PREFS_AUTO_CAPTURE,
                (int)ScreenshotCapture.AutoCaptureInterval.Off);

            if (!Enum.IsDefined(typeof(ScreenshotCapture.AutoCaptureInterval), _autoCaptureInterval))
                _autoCaptureInterval = ScreenshotCapture.AutoCaptureInterval.Off;

            RefreshSavePath();
            RefreshList();
            ApplyAutoCaptureToScene();

            ScreenshotCapture.OnScreenshotCaptured += OnNewScreenshot;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnDisable()
        {
            ScreenshotCapture.OnScreenshotCaptured -= OnNewScreenshot;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            DestroyPreviewTexture();
        }

        private void OnNewScreenshot(string path)
        {
            RefreshList();
            Repaint();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ApplyAutoCaptureToScene();
                RefreshList();
            }
        }

        void OnGUI()
        {
            DrawHeader();
            DrawSettings();
            EditorGUILayout.Space(4);
            DrawScreenshotList();
        }

        private void DrawHeader()
        {
            var rect = EditorGUILayout.GetControlRect(false, 48);
            EditorGUI.DrawRect(rect, HeaderColor);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = AccentColor },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 0, 0, 0)
            };

            var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.65f) },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 0, 0, 0)
            };

            GUI.Label(new Rect(rect.x, rect.y + 6, rect.width, 22), "Screenshot Tool", titleStyle);
            GUI.Label(new Rect(rect.x, rect.y + 26, rect.width, 18), $"{_screenshots.Count} captures  •  {Path.GetFullPath(_savePath)}", subtitleStyle);
        }

        private void DrawSettings()
        {
            _settingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_settingsFoldout, "Settings");
            if (_settingsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                _folderName = EditorGUILayout.TextField("Save Folder", _folderName);
                _filePrefix = EditorGUILayout.TextField("File Prefix", _filePrefix);
                _superSize = EditorGUILayout.IntSlider("Resolution Multiplier", _superSize, 1, 4);
                _captureKey = (KeyCode)EditorGUILayout.EnumPopup("Capture Key", _captureKey);
                _autoCaptureInterval = (ScreenshotCapture.AutoCaptureInterval)EditorGUILayout.EnumPopup("Auto Capture", _autoCaptureInterval);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetString(PREFS_FOLDER, _folderName);
                    EditorPrefs.SetString(PREFS_PREFIX, _filePrefix);
                    EditorPrefs.SetInt(PREFS_SUPERSIZE, _superSize);
                    EditorPrefs.SetInt(PREFS_KEY, (int)_captureKey);
                    EditorPrefs.SetInt(PREFS_AUTO_CAPTURE, (int)_autoCaptureInterval);
                    RefreshSavePath();
                    ApplyAutoCaptureToScene();
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 15);

                if (GUILayout.Button("Open Folder", GUILayout.Height(24)))
                    OpenFolder();

                if (GUILayout.Button("Refresh List", GUILayout.Height(24)))
                    RefreshList();

                if (EditorApplication.isPlaying)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.8f, 0.5f);
                    if (GUILayout.Button($"Capture Now ({_captureKey})", GUILayout.Height(24)))
                        TakeEditorScreenshot();
                    GUI.backgroundColor = Color.white;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawScreenshotList()
        {
            _listFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_listFoldout, $"Screenshots ({_screenshots.Count})");

            if (_listFoldout)
            {
                if (_screenshots.Count == 0)
                {
                    EditorGUILayout.HelpBox("No screenshots yet. Enter Play Mode and press " + _captureKey + " to capture.", MessageType.Info);
                }
                else
                {
                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));

                    for (int i = 0; i < _screenshots.Count; i++)
                    {
                        DrawScreenshotEntry(_screenshots[i], i);
                    }

                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.Space(4);
                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button("Clear All Screenshots", GUILayout.Height(28)))
                    {
                        if (EditorUtility.DisplayDialog("Clear Screenshots",
                            "Delete all screenshot files? This cannot be undone.", "Delete All", "Cancel"))
                        {
                            ClearAllScreenshots();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawScreenshotEntry(ScreenshotEntry entry, int index)
        {
            var bgColor = index % 2 == 0 ? ItemEvenColor : ItemOddColor;
            var rect = EditorGUILayout.GetControlRect(false, 42);
            EditorGUI.DrawRect(rect, bgColor);

            float thumbSize = 38f;
            float padding = 4f;

            if (entry.Thumbnail != null)
            {
                GUI.DrawTexture(
                    new Rect(rect.x + padding, rect.y + 2, thumbSize, thumbSize - 2),
                    entry.Thumbnail, ScaleMode.ScaleToFit);
            }

            float textX = rect.x + thumbSize + padding * 2;
            float textWidth = rect.width - thumbSize - padding * 3 - 120f;

            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            var dateStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.65f) }
            };

            GUI.Label(new Rect(textX, rect.y + 5, textWidth, 18), entry.FileName, nameStyle);
            GUI.Label(new Rect(textX, rect.y + 23, textWidth, 14), entry.DateFormatted, dateStyle);

            float btnX = rect.xMax - 116f;
            float btnY = rect.y + 9f;

            if (GUI.Button(new Rect(btnX, btnY, 52, 22), "Open"))
                OpenScreenshot(entry.FullPath);

            if (GUI.Button(new Rect(btnX + 56, btnY, 52, 22), "Delete"))
            {
                if (EditorUtility.DisplayDialog("Delete Screenshot",
                    $"Delete {entry.FileName}?", "Delete", "Cancel"))
                {
                    File.Delete(entry.FullPath);
                    RefreshList();
                }
            }
        }

        private void RefreshSavePath()
        {
            _savePath = Path.Combine(Application.dataPath, "..", _folderName);
            if (!Directory.Exists(_savePath))
                Directory.CreateDirectory(_savePath);
        }

        private void RefreshList()
        {
            _screenshots.Clear();

            if (!Directory.Exists(_savePath)) return;

            var files = Directory.GetFiles(_savePath, "*.png")
                .Concat(Directory.GetFiles(_savePath, "*.jpg"))
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            foreach (var file in files)
            {
                var entry = new ScreenshotEntry(file);
                _screenshots.Add(entry);
            }

            Repaint();
        }

        private void OpenFolder()
        {
            string path = Path.GetFullPath(_savePath);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

#if UNITY_EDITOR_WIN
            Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX
            Process.Start("open", path);
#else
            Process.Start("xdg-open", path);
#endif
        }

        private void OpenScreenshot(string path)
        {
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Not Found", "File no longer exists.", "OK");
                RefreshList();
                return;
            }

#if UNITY_EDITOR_WIN
            Process.Start("explorer.exe", $"/select,\"{path.Replace("/", "\\")}\"");
#elif UNITY_EDITOR_OSX
            Process.Start("open", $"-R \"{path}\"");
#else
            Process.Start("xdg-open", Path.GetDirectoryName(path));
#endif
        }

        private void TakeEditorScreenshot()
        {
            var capture = FindFirstObjectByType<ScreenshotCapture>();
            if (capture == null)
            {
                EditorUtility.DisplayDialog("Screenshot Tool",
                    "No ScreenshotCapture component found in the scene.\nAdd it to a GameObject first.", "OK");
                return;
            }

            string fileName = $"{_filePrefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string fullPath = Path.Combine(_savePath, fileName);
            ScreenCapture.CaptureScreenshot(fullPath, _superSize);

            EditorApplication.delayCall += () =>
            {
                RefreshList();
                Repaint();
            };
        }

        private void ApplyAutoCaptureToScene()
        {
            var capture = FindFirstObjectByType<ScreenshotCapture>();
            if (capture == null)
                return;

            if (capture.autoCaptureInterval == _autoCaptureInterval)
                return;

            capture.autoCaptureInterval = _autoCaptureInterval;
            EditorUtility.SetDirty(capture);
        }

        private void ClearAllScreenshots()
        {
            foreach (var entry in _screenshots)
            {
                if (File.Exists(entry.FullPath))
                    File.Delete(entry.FullPath);
            }
            _screenshots.Clear();
            DestroyPreviewTexture();
            Repaint();
        }

        private void DestroyPreviewTexture()
        {
            if (_previewTexture != null)
            {
                DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }
        }

        private class ScreenshotEntry
        {
            public string FullPath { get; }
            public string FileName { get; }
            public string DateFormatted { get; }
            public Texture2D Thumbnail { get; }

            public ScreenshotEntry(string fullPath)
            {
                FullPath = fullPath;
                FileName = Path.GetFileName(fullPath);
                DateFormatted = File.GetCreationTime(fullPath).ToString("dd MMM yyyy  HH:mm:ss");

                try
                {
                    byte[] bytes = File.ReadAllBytes(fullPath);
                    var tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        Thumbnail = tex;
                    }
                }
                catch { /* thumbnail load failed, skip */ }
            }
        }
    }
}
