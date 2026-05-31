using System;
using System.Runtime.InteropServices;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    /// <summary>
    /// 拍照后展示捕获的图片。运行时构建UI，不依赖Addressables预制体。
    /// </summary>
    public class PhotoPopup : UIBase
    {
        private Texture2D _photo;
        private Sprite _photoSprite;

        public void Init(Texture2D photo)
        {
            _photo = photo;
            BuildUI();
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

        private void BuildUI()
        {
            // 全屏半透明遮罩（拦截后面的点击，但不会因为点击关闭）
            var bg = CreateChild("Background", transform);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);
            bgImage.raycastTarget = true;
            StretchFill(bg.GetComponent<RectTransform>());

            // 居中相框容器（米色背景）
            const float photoW = 800f;
            const float photoH = photoW * 9f / 16f; // 16:9
            const float padding = 24f;
            const float headerH = 70f;

            var container = CreateChild("Container", transform);
            var containerImage = container.AddComponent<Image>();
            containerImage.color = new Color(0.96f, 0.93f, 0.86f, 1f);
            containerImage.raycastTarget = true;
            var containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(photoW + padding * 2f, photoH + padding * 2f + headerH);
            containerRect.anchoredPosition = Vector2.zero;

            // 顶部按钮区，从右往左：X | 复制 | 选择文件夹 | 保存
            var closeBtn = CreateButton(container.transform, "X", new Vector2(50f, 50f),
                new Vector2(1f, 1f), new Vector2(-padding, -padding));
            closeBtn.onClick.AddListener(OnCloseClicked);

            var copyBtn = CreateButton(container.transform, "复制", new Vector2(120f, 50f),
                new Vector2(1f, 1f), new Vector2(-padding - 60f, -padding));
            copyBtn.onClick.AddListener(OnCopyClicked);

            var pickFolderBtn = CreateButton(container.transform, "选择文件夹", new Vector2(180f, 50f),
                new Vector2(1f, 1f), new Vector2(-padding - 190f, -padding));
            pickFolderBtn.onClick.AddListener(OnSelectFolderClicked);

            var saveBtn = CreateButton(container.transform, "保存", new Vector2(120f, 50f),
                new Vector2(1f, 1f), new Vector2(-padding - 380f, -padding));
            saveBtn.onClick.AddListener(OnSaveClicked);

            // 照片
            var photoGo = CreateChild("Photo", container.transform);
            var photoImage = photoGo.AddComponent<Image>();
            _photoSprite = Sprite.Create(_photo,
                new Rect(0, 0, _photo.width, _photo.height),
                new Vector2(0.5f, 0.5f), 100f);
            photoImage.sprite = _photoSprite;
            photoImage.preserveAspect = true;
            var photoRect = photoGo.GetComponent<RectTransform>();
            photoRect.anchorMin = new Vector2(0.5f, 0f);
            photoRect.anchorMax = new Vector2(0.5f, 0f);
            photoRect.pivot = new Vector2(0.5f, 0f);
            photoRect.sizeDelta = new Vector2(photoW, photoH);
            photoRect.anchoredPosition = new Vector2(0f, padding);
        }

        private void OnCloseClicked()
        {
            this.GetSystem<IUISystem>().HidePopup(UIPopup.PhotoPopup);
        }

        private void OnCopyClicked()
        {
            if (_photo == null) return;
            bool success = TryCopyImageToClipboard(_photo);
            this.GetSystem<IUISystem>().ShowPrompt(success ? "已复制到剪贴板" : "复制失败");
        }

        private const string FOLDER_PREF_KEY = "PhotoSaveFolder";

        /// <summary>
        /// 返回当前保存文件夹。优先读PlayerPrefs；若未设置或不存在，回退到 我的图片/FeatherLand 并自动创建。
        /// </summary>
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
            catch { /* 创建失败回退到Pictures根目录 */ return pictures; }
            return defaultFolder;
        }

        private void OnSelectFolderClicked()
        {
            Debug.Log("[PhotoPopup] OnSelectFolderClicked");
            string current = GetSaveFolder();
            string picked = ShowFolderDialog("选择保存文件夹", current);
            if (string.IsNullOrEmpty(picked))
                return; // 用户取消 或 对话框失败

            PlayerPrefs.SetString(FOLDER_PREF_KEY, picked);
            PlayerPrefs.Save();
            this.GetSystem<IUISystem>().ShowPrompt("已变更储存位置");
        }

        private void OnSaveClicked()
        {
            Debug.Log("[PhotoPopup] OnSaveClicked");
            if (_photo == null) return;

            // 第一次保存：还没设过储存位置 → 弹文件夹选择
            if (!PlayerPrefs.HasKey(FOLDER_PREF_KEY))
            {
                string picked = ShowFolderDialog("选择保存文件夹", GetSaveFolder());
                if (string.IsNullOrEmpty(picked))
                    return; // 用户取消，不保存
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
                this.GetSystem<IUISystem>().ShowPrompt("已保存");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhotoPopup] Save failed: {e}");
                this.GetSystem<IUISystem>().ShowPrompt("保存失败");
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
            Vector2 anchor, Vector2 anchoredPos)
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
            text.fontSize = 28;
            text.color = new Color(0.18f, 0.12f, 0.08f, 1f);
            StretchFill(textGo.GetComponent<RectTransform>());

            return btn;
        }

        #region Windows Clipboard Image Copy

        private static bool TryCopyImageToClipboard(Texture2D tex)
        {
#if UNITY_STANDALONE_WIN
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

                // BITMAPINFOHEADER
                Marshal.WriteInt32(ptr, 0, headerSize);
                Marshal.WriteInt32(ptr, 4, width);
                Marshal.WriteInt32(ptr, 8, height); // 正数: bottom-up, 与Unity Texture2D一致
                Marshal.WriteInt16(ptr, 12, 1);
                Marshal.WriteInt16(ptr, 14, 32);
                Marshal.WriteInt32(ptr, 16, 0);
                Marshal.WriteInt32(ptr, 20, pixelDataSize);
                Marshal.WriteInt32(ptr, 24, 0);
                Marshal.WriteInt32(ptr, 28, 0);
                Marshal.WriteInt32(ptr, 32, 0);
                Marshal.WriteInt32(ptr, 36, 0);

                // 像素数据 BGRA，Unity Texture2D 的 (0,0) 是左下角，DIB 正数高度也是左下角
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
#else
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN
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

        #endregion

        #region Windows Folder Picker Dialog

        /// <summary>
        /// 弹出系统"选择文件夹"对话框，返回用户选择的文件夹完整路径；取消返回 null。
        /// 用的是 SHBrowseForFolderW（经典Win32 API），不带回调、不带BIF_NEWDIALOGSTYLE，
        /// 避免 Unity 主线程 COM apartment 状态导致 silent failure。
        /// </summary>
        private static string ShowFolderDialog(string title, string initialDir)
        {
#if UNITY_STANDALONE_WIN
            IntPtr pidl = IntPtr.Zero;
            try
            {
                var bi = new BROWSEINFOW
                {
                    hwndOwner = GetActiveWindow(), // 把对话框parent到Unity窗口，保证它能拿焦点（壁纸模式很关键）
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
            }
#endif
            return null;
        }

#if UNITY_STANDALONE_WIN
        private const uint BIF_RETURNONLYFSDIRS = 0x00000001;

        // 显式 Unicode 版本，绑死 W 后缀，避免 CharSet.Auto 在某些环境下解析错误
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BROWSEINFOW
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public IntPtr pszDisplayName;          // 不需要回填，置零
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszTitle;
            public uint   ulFlags;
            public IntPtr lpfn;
            public IntPtr lParam;
            public int    iImage;
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

        #endregion
    }
}
