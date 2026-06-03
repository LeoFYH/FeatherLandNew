using System;
using System.Runtime.InteropServices;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class PhotoPopup : UIBase
    {
        public Image photoImage;
        public Button closeButton;
        public Button saveButton;
        public Button selectFolderButton;
        public Button copyButton;
        private Texture2D _photo;
        private Sprite _photoSprite;

        public void Init(Texture2D photo)
        {
            _photo = photo;
            _photoSprite = Sprite.Create(_photo,
                new Rect(0, 0, _photo.width, _photo.height),
                new Vector2(0.5f, 0.5f), 100f);
            photoImage.sprite = _photoSprite;
        }

        private void Start()
        {
            closeButton.onClick.AddListener(OnCloseClicked);
            saveButton.onClick.AddListener(OnSaveClicked);
            selectFolderButton.onClick.AddListener(OnSelectFolderClicked);
            copyButton.onClick.AddListener(OnCopyClicked);
        }

        public override void OnHidePanel(Action onComplete = null)
        {
            base.OnHidePanel(() =>
            {
                if (_photoSprite != null)
                    Destroy(_photoSprite);
                if (_photo != null)
                    Destroy(_photo);
                onComplete?.Invoke();
            });
        }

        private void OnCloseClicked()
        {
            this.GetSystem<IUISystem>().HidePopup(UIPopup.PhotoPopup);
        }

        private void OnCopyClicked()
        {
            if (_photo == null) return;
            var loc = this.GetSystem<ILocalizationSystem>();
            bool success = TryCopyImageToClipboard(_photo);
            this.GetSystem<IUISystem>().ShowPrompt(loc.GetString(success ? "Copied to clipboard" : "Copy failed"));
        }

        private const string FOLDER_PREF_KEY = "PhotoSaveFolder";

        private static string GetSaveFolder()
        {
            string saved = PlayerPrefs.GetString(FOLDER_PREF_KEY, null);
            if (!string.IsNullOrEmpty(saved) && System.IO.Directory.Exists(saved))
                return saved;

            string pictures = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
            string defaultFolder = System.IO.Path.Combine(pictures, "FeatherLand");
            try
            {
                if (!System.IO.Directory.Exists(defaultFolder))
                    System.IO.Directory.CreateDirectory(defaultFolder);
            }
            catch { return pictures; }
            return defaultFolder;
        }

        private void OnSelectFolderClicked()
        {
            Debug.Log("[PhotoPopup] OnSelectFolderClicked");
            var loc = this.GetSystem<ILocalizationSystem>();
            string current = GetSaveFolder();
            string picked = ShowFolderDialog(loc.GetString("Choose save folder"), current);
            if (string.IsNullOrEmpty(picked))
                return;

            PlayerPrefs.SetString(FOLDER_PREF_KEY, picked);
            PlayerPrefs.Save();
            this.GetSystem<IUISystem>().ShowPrompt(loc.GetString("Storage location changed"));
        }

        private void OnSaveClicked()
        {
            Debug.Log("[PhotoPopup] OnSaveClicked");
            if (_photo == null) return;
            var loc = this.GetSystem<ILocalizationSystem>();

            if (!PlayerPrefs.HasKey(FOLDER_PREF_KEY))
            {
                string picked = ShowFolderDialog(loc.GetString("Choose save folder"), GetSaveFolder());
                if (string.IsNullOrEmpty(picked))
                    return;
                PlayerPrefs.SetString(FOLDER_PREF_KEY, picked);
                PlayerPrefs.Save();
            }

            string folder = GetSaveFolder();
            string filename = $"featherland_photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = System.IO.Path.Combine(folder, filename);

            try
            {
                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);
                byte[] png = _photo.EncodeToPNG();
                System.IO.File.WriteAllBytes(fullPath, png);
                this.GetSystem<IUISystem>().ShowPrompt(loc.GetString("Saved"));
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhotoPopup] Save failed: {e}");
                this.GetSystem<IUISystem>().ShowPrompt(loc.GetString("Save failed"));
            }
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 size,
            Vector2 anchor, Vector2 anchoredPos, TMP_FontAsset font = null)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.82f, 0.72f, 0.55f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            btn.colors = colors;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 28f;
            text.color = new Color(0.18f, 0.12f, 0.08f, 1f);
            if (font != null)
                text.font = font;
            StretchFill(textGo.GetComponent<RectTransform>());

            return btn;
        }

        #region Platform Folder Picker Dialog

        private static string ShowFolderDialog(string title, string initialDir)
        {
#if UNITY_STANDALONE_WIN
            return ShowWindowsFolderDialog(title, initialDir);
#elif UNITY_STANDALONE_OSX
            return ShowMacFolderDialog(title, initialDir);
#else
            return null;
#endif
        }

#if UNITY_STANDALONE_WIN
        private static string ShowWindowsFolderDialog(string title, string initialDir)
        {
            IntPtr pidl = IntPtr.Zero;
            try
            {
                var bi = new BROWSEINFOW
                {
                    hwndOwner = GetActiveWindow(),
                    pidlRoot = IntPtr.Zero,
                    pszDisplayName = IntPtr.Zero,
                    lpszTitle = title,
                    ulFlags = BIF_RETURNONLYFSDIRS,
                    lpfn = IntPtr.Zero,
                    lParam = IntPtr.Zero,
                    iImage = 0,
                };

                pidl = SHBrowseForFolderW(ref bi);
                if (pidl == IntPtr.Zero)
                    return null;

                var sb = new System.Text.StringBuilder(260);
                if (SHGetPathFromIDListW(pidl, sb))
                    return sb.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhotoPopup] SHBrowseForFolder failed: {e}");
            }
            finally
            {
                if (pidl != IntPtr.Zero)
                    CoTaskMemFree(pidl);
            }
            return null;
        }

        private const uint BIF_RETURNONLYFSDIRS = 0x00000001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BROWSEINFOW
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public IntPtr pszDisplayName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszTitle;
            public uint ulFlags;
            public IntPtr lpfn;
            public IntPtr lParam;
            public int iImage;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHBrowseForFolderW")]
        private static extern IntPtr SHBrowseForFolderW(ref BROWSEINFOW lpbi);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHGetPathFromIDListW")]
        private static extern bool SHGetPathFromIDListW(IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszPath);

        [DllImport("ole32.dll")]
        private static extern void CoTaskMemFree(IntPtr ptr);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();
#endif

#if UNITY_STANDALONE_OSX
        private static string ShowMacFolderDialog(string title, string initialDir)
        {
            try
            {
                string tempScriptPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "featherland_script.scpt");
                
                using (var writer = new System.IO.StreamWriter(tempScriptPath))
                {
                    writer.WriteLine("tell application \"Finder\" to activate");
                    if (!string.IsNullOrEmpty(initialDir) && System.IO.Directory.Exists(initialDir))
                    {
                        writer.WriteLine("set theFolder to choose folder with prompt \"" + title.Replace("\"", "\\\"") + "\" default location alias POSIX file \"" + initialDir.Replace("\"", "\\\"") + "\"");
                    }
                    else
                    {
                        writer.WriteLine("set theFolder to choose folder with prompt \"" + title.Replace("\"", "\\\"") + "\"");
                    }
                    writer.WriteLine("return POSIX path of theFolder");
                }

                var psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = "/usr/bin/osascript";
                psi.Arguments = "\"" + tempScriptPath.Replace("\"", "\\\"") + "\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    string error = process.StandardError.ReadToEnd().Trim();
                    process.WaitForExit();
                    
                    System.IO.File.Delete(tempScriptPath);
                    
                    Debug.Log($"[PhotoPopup] Folder dialog output: '{output}', error: '{error}', exit code: {process.ExitCode}");
                    
                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        return output;
                    }
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhotoPopup] ShowMacFolderDialog failed: {e}");
                return null;
            }
        }
#endif

        #endregion

        #region Platform Clipboard Copy

        private static bool TryCopyImageToClipboard(Texture2D tex)
        {
            if (tex == null)
                return false;

#if UNITY_STANDALONE_WIN
            return TryCopyImageToWindowsClipboard(tex);
#elif UNITY_STANDALONE_OSX
            return TryCopyImageToMacClipboard(tex);
#else
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN
        private static bool TryCopyImageToWindowsClipboard(Texture2D tex)
        {
            try
            {
                int width = tex.width;
                int height = tex.height;
                int headerSize = 40;
                int pixelDataSize = width * height * 4;
                int totalSize = headerSize + pixelDataSize;

                IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)totalSize);
                if (hGlobal == IntPtr.Zero) return false;

                IntPtr ptr = GlobalLock(hGlobal);
                if (ptr == IntPtr.Zero) { GlobalFree(hGlobal); return false; }

                Marshal.WriteInt32(ptr, 0, headerSize);
                Marshal.WriteInt32(ptr, 4, width);
                Marshal.WriteInt32(ptr, 8, height);
                Marshal.WriteInt16(ptr, 12, 1);
                Marshal.WriteInt16(ptr, 14, 32);
                Marshal.WriteInt32(ptr, 16, 0);
                Marshal.WriteInt32(ptr, 20, pixelDataSize);
                Marshal.WriteInt32(ptr, 24, 0);
                Marshal.WriteInt32(ptr, 28, 0);
                Marshal.WriteInt32(ptr, 32, 0);
                Marshal.WriteInt32(ptr, 36, 0);

                Color32[] pixels = tex.GetPixels32();
                byte[] buffer = new byte[pixelDataSize];
                for (int i = 0; i < pixels.Length; i++)
                {
                    int o = i * 4;
                    Color32 c = pixels[i];
                    buffer[o + 0] = c.b;
                    buffer[o + 1] = c.g;
                    buffer[o + 2] = c.r;
                    buffer[o + 3] = c.a;
                }
                Marshal.Copy(buffer, 0, IntPtr.Add(ptr, headerSize), pixelDataSize);
                GlobalUnlock(hGlobal);

                if (!OpenClipboard(IntPtr.Zero))
                {
                    GlobalFree(hGlobal);
                    return false;
                }
                EmptyClipboard();
                if (SetClipboardData(CF_DIB, hGlobal) == IntPtr.Zero)
                {
                    CloseClipboard();
                    GlobalFree(hGlobal);
                    return false;
                }
                CloseClipboard();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhotoPopup] Copy to clipboard failed: {e}");
                return false;
            }
        }

        private const uint CF_DIB = 8;
        private const uint GMEM_MOVEABLE = 0x0002;

        [DllImport("user32.dll")] private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")] private static extern bool EmptyClipboard();
        [DllImport("user32.dll")] private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        [DllImport("user32.dll")] private static extern bool CloseClipboard();
        [DllImport("kernel32.dll")] private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
        [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr hMem);
        [DllImport("kernel32.dll")] private static extern IntPtr GlobalFree(IntPtr hMem);
#endif

#if UNITY_STANDALONE_OSX
        private static bool TryCopyImageToMacClipboard(Texture2D tex)
        {
            try
            {
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "featherland_clipboard.png");
                byte[] pngData = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes(tempPath, pngData);

                Debug.Log($"[PhotoPopup] Temp file path: {tempPath}, size: {pngData.Length} bytes");

                string escapedPath = tempPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string script = "set f to POSIX file \"" + escapedPath + "\"\ntell application \"System Events\" to set the clipboard to (read f as «class PNGf»)";

                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "/usr/bin/osascript";
                process.StartInfo.Arguments = "-e '" + script.Replace("'", "'\\''") + "'";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Debug.Log($"[PhotoPopup] Copy script: '{script}'");
                Debug.Log($"[PhotoPopup] Copy to clipboard exit code: {process.ExitCode}");
                Debug.Log($"[PhotoPopup] Copy output: '{output}'");
                Debug.Log($"[PhotoPopup] Copy error: '{error}'");

                System.IO.File.Delete(tempPath);

                return process.ExitCode == 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhotoPopup] Copy to Mac clipboard failed: {e}");
                return false;
            }
        }
#endif

        #endregion
    }
}
