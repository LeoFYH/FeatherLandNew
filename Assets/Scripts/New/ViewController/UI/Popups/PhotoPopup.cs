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
            // 每个 AddListener 独立 try/catch,防止某个 button 引用为 null 导致
            // NullReferenceException 让后面的 AddListener 都不执行。
            // (之前 Mac 壁纸模式下用户报告 "选择文件夹/复制" 无反应,根因疑似就是
            //  其中一个 button 字段未连线, Start() 抛 NRE 导致后续 listener 没装上)
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            else Debug.LogError("[PhotoPopup] closeButton 字段未连线");

            if (saveButton != null) saveButton.onClick.AddListener(OnSaveClicked);
            else Debug.LogError("[PhotoPopup] saveButton 字段未连线");

            if (selectFolderButton != null) selectFolderButton.onClick.AddListener(OnSelectFolderClicked);
            else Debug.LogError("[PhotoPopup] selectFolderButton 字段未连线");

            if (copyButton != null) copyButton.onClick.AddListener(OnCopyClicked);
            else Debug.LogError("[PhotoPopup] copyButton 字段未连线");

            Debug.Log($"[PhotoPopup] Start done. close={closeButton} save={saveButton} folder={selectFolderButton} copy={copyButton}");
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
            Debug.Log("[PhotoPopup] OnCloseClicked");
            this.GetSystem<IUISystem>().HidePopup(UIPopup.PhotoPopup);
        }

        private void OnCopyClicked()
        {
            Debug.Log($"[PhotoPopup] OnCopyClicked, _photo={(_photo != null ? _photo.width + "x" + _photo.height : "null")}");
            if (_photo == null) return;
            var loc = this.GetSystem<ILocalizationSystem>();
            bool success = TryCopyImageToClipboard(_photo);
            Debug.Log($"[PhotoPopup] OnCopyClicked done, success={success}");
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
            ShowFolderDialogAsync(loc.GetString("Choose save folder"), current, picked =>
            {
                if (string.IsNullOrEmpty(picked))
                    return; // 用户取消 或 对话框失败

                PlayerPrefs.SetString(FOLDER_PREF_KEY, picked);
                PlayerPrefs.Save();
                this.GetSystem<IUISystem>().ShowPrompt(loc.GetString("Storage location changed"));
            });
        }

        private void OnSaveClicked()
        {
            Debug.Log("[PhotoPopup] OnSaveClicked");
            if (_photo == null) return;
            var loc = this.GetSystem<ILocalizationSystem>();

            if (!PlayerPrefs.HasKey(FOLDER_PREF_KEY))
            {
                // 第一次保存：异步弹文件夹选择，选完在回调里落盘（主线程不冻结）
                ShowFolderDialogAsync(loc.GetString("Choose save folder"), GetSaveFolder(), picked =>
                {
                    if (string.IsNullOrEmpty(picked))
                        return; // 用户取消，不保存
                    PlayerPrefs.SetString(FOLDER_PREF_KEY, picked);
                    PlayerPrefs.Save();
                    SavePhotoToFolder(loc);
                });
                return;
            }

            SavePhotoToFolder(loc);
        }

        private void SavePhotoToFolder(ILocalizationSystem loc)
        {
            if (_photo == null) return; // 异步回调回来时照片可能已被销毁

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

        // ---- 异步文件夹对话框 ----
        // 旧实现是开线程后 t.Join() 同步等待：主线程装着 WH_MOUSE_LL 全局鼠标钩子，
        // Join 把主线程冻住 → 钩子回调无法处理 → 壁纸模式点"保存"时全系统鼠标卡顿约2秒
        //（对话框首次打开要枚举 shell 命名空间，最慢），钩子超时还可能被 Windows 摘除。
        // 现改为纯异步：对话框在后台线程自己跑，结果写回字段，Update() 轮询后回主线程执行回调。
        private volatile bool _dialogDone;
        private bool _dialogRunning;
        private string _dialogResult;
        private Action<string> _dialogCallback;

        private void Update()
        {
            if (_dialogDone)
            {
                _dialogDone = false;
                _dialogRunning = false;
                var cb = _dialogCallback;
                _dialogCallback = null;
                cb?.Invoke(_dialogResult);
            }
        }

        /// <summary>
        /// 异步弹系统文件夹选择框；用户取消或失败时回调 null。主线程全程不阻塞。
        /// </summary>
        private void ShowFolderDialogAsync(string title, string initialDir, Action<string> onPicked)
        {
            if (_dialogRunning) return; // 防重入：对话框已开着时忽略再次点击
            _dialogRunning = true;
            _dialogCallback = onPicked;
            _dialogResult = null;

#if UNITY_STANDALONE_WIN
            var t = new System.Threading.Thread(() =>
            {
                string r = null;
                try { r = BrowseForFolderSTA(title); }
                catch (Exception e) { Debug.LogError($"[PhotoPopup] Folder dialog thread failed: {e}"); }
                _dialogResult = r;
                _dialogDone = true; // 最后置位（volatile），保证主线程能读到结果
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA); // BIF_NEWDIALOGSTYLE 需要 STA
            t.IsBackground = true;
            t.Start();
#elif UNITY_STANDALONE_OSX
            var t = new System.Threading.Thread(() =>
            {
                string r = null;
                try { r = ShowMacFolderDialog(title, initialDir); }
                catch (Exception e) { Debug.LogError($"[PhotoPopup] Folder dialog thread failed: {e}"); }
                _dialogResult = r;
                _dialogDone = true;
            });
            t.IsBackground = true;
            t.Start();
#else
            _dialogDone = true; // 不支持的平台：下一帧直接回调 null
#endif
        }

#if UNITY_STANDALONE_WIN
        private static string BrowseForFolderSTA(string title)
        {
            IntPtr pidl = IntPtr.Zero;
            int hr = OleInitialize(IntPtr.Zero); // 在 STA 线程上初始化 OLE，启用新对话框样式
            try
            {
                // 壁纸模式下游戏窗口永远不是前台窗口(激活它会破坏壁纸层)，本进程因此没有
                // 前台激活权：无 owner 弹出的对话框会被 Windows 压在任务栏里闪烁而不弹出，
                // 玩家必须手动点任务栏才能看到。BFFM_INITIALIZED 回调里拿到对话框句柄后
                // 把它设为 TOPMOST(顺带躲开壁纸窗口的周期性置顶刷新)并用 Alt 键戏法夺前台。
                // 只操作对话框自己的 HWND，不碰游戏窗口，壁纸层级不受影响。
                var callback = new BrowseCallback(BringDialogToFront);
                var bi = new BROWSEINFOW
                {
                    // 不设跨线程 owner：若拿主线程窗口当 owner，模态会对主线程 EnableWindow 发同步消息，
                    // 而主线程正卡在 Join → 互相阻塞死锁。无 owner 时对话框独立运行，安全。
                    hwndOwner = IntPtr.Zero,
                    pidlRoot = IntPtr.Zero,
                    pszDisplayName = IntPtr.Zero,
                    lpszTitle = title,
                    ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE,
                    lpfn = Marshal.GetFunctionPointerForDelegate(callback),
                    lParam = IntPtr.Zero,
                    iImage = 0,
                };

                pidl = SHBrowseForFolderW(ref bi);
                GC.KeepAlive(callback); // 模态期间防止委托被 GC 回收
                if (pidl == IntPtr.Zero)
                    return null; // 用户取消

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
                if (hr >= 0) // OleInitialize 成功(S_OK/S_FALSE)才配对 Uninitialize
                    OleUninitialize();
            }
            return null;
        }

        private const uint BIF_RETURNONLYFSDIRS = 0x00000001;
        private const uint BIF_NEWDIALOGSTYLE = 0x00000040;
        private const uint BFFM_INITIALIZED = 1;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const byte VK_MENU = 0x12;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int BrowseCallback(IntPtr hwnd, uint uMsg, IntPtr lParam, IntPtr lpData);

        private static int BringDialogToFront(IntPtr hwnd, uint uMsg, IntPtr lParam, IntPtr lpData)
        {
            if (uMsg == BFFM_INITIALIZED)
            {
                // TOPMOST：既保证首次弹出可见，也不会被壁纸窗口的周期性置顶刷新盖回去
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                // Alt 键按下状态下 SetForegroundWindow 不受前台锁限制（经典夺前台手法），
                // 让对话框直接获得键盘焦点而不是在任务栏闪烁
                keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
                SetForegroundWindow(hwnd);
                keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            return 0;
        }

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

        [DllImport("ole32.dll")]
        private static extern int OleInitialize(IntPtr pvReserved);

        [DllImport("ole32.dll")]
        private static extern void OleUninitialize();

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
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
