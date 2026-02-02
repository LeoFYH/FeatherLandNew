using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class LargeTextureFinder : EditorWindow
{
    private string searchPath = "Assets/";
    private int sizeThreshold = 2048;
    private List<TextureData> largeTextures = new List<TextureData>();
    private Vector2 scrollPosition;
    private bool isSearching = false;

    private class TextureData
    {
        public Texture2D texture;
        public string path;
        public int width;
        public int height;
        public long fileSize; // 文件大小，以字节为单位
        
        public TextureData(Texture2D tex, string p, int w, int h, long fs)
        {
            texture = tex;
            path = p;
            width = w;
            height = h;
            fileSize = fs;
        }
    }

    [MenuItem("Tools/Large Texture Finder")]
    public static void ShowWindow()
    {
        GetWindow<LargeTextureFinder>("Large Texture Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Large Texture Finder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label("Search Settings:", EditorStyles.boldLabel);
        searchPath = EditorGUILayout.TextField("Search Path:", searchPath);
        
        if (GUILayout.Button("Browse..."))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder to Search", searchPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Convert to Unity relative path
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    searchPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogWarning("Selected folder is outside of Assets folder!");
                }
            }
        }

        sizeThreshold = EditorGUILayout.IntField("Size Threshold (pixels):", sizeThreshold);
        EditorGUILayout.HelpBox($"Finding textures with width or height greater than {sizeThreshold}px", MessageType.Info);

        EditorGUILayout.Space();
        
        EditorGUI.BeginDisabledGroup(isSearching);
        if (GUILayout.Button("Find Large Textures"))
        {
            FindLargeTextures();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Select All Large Textures in Project"))
        {
            List<Object> objects = new List<Object>();
            foreach(var texData in largeTextures)
            {
                objects.Add(texData.texture);
            }
            Selection.objects = objects.ToArray();
        }

        EditorGUILayout.Space();

        if (largeTextures.Count > 0)
        {
            GUILayout.Label($"Found {largeTextures.Count} large textures:", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Texture Name", GUILayout.Width(200));
            GUILayout.Label("Dimensions", GUILayout.Width(100));
            GUILayout.Label("File Size", GUILayout.Width(100));
            GUILayout.Label("Path", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var texData in largeTextures)
            {
                EditorGUILayout.BeginHorizontal();
                
                GUILayout.Label(texData.texture.name, GUILayout.Width(200));
                GUILayout.Label($"{texData.width}x{texData.height}", GUILayout.Width(100));
                GUILayout.Label($"{GetFileSizeString(texData.fileSize)}", GUILayout.Width(100));
                GUILayout.Label(texData.path, GUILayout.ExpandWidth(true));
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = texData.texture;
                    EditorGUIUtility.PingObject(texData.texture);
                }
                
                if (GUILayout.Button("Reveal", GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(texData.texture);
                    EditorUtility.RevealInFinder(texData.path);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        else
        {
            if (isSearching)
            {
                GUILayout.Label("Searching... Please wait.");
            }
            else
            {
                GUILayout.Label("No large textures found or search not performed yet.");
            }
        }
    }

    private void FindLargeTextures()
    {
        largeTextures.Clear();
        isSearching = true;
        
        if (string.IsNullOrEmpty(searchPath))
        {
            Debug.LogWarning("Please specify a search path.");
            isSearching = false;
            return;
        }
        
        // 查找所有纹理资源
        string[] guids = AssetDatabase.FindAssets("t:texture", new[] { searchPath });
        
        int totalCount = guids.Length;
        int currentCount = 0;
        
        foreach (string guid in guids)
        {
            // 显示进度条
            if (currentCount % 10 == 0) // 每处理10个更新一次进度，避免频繁更新影响性能
            {
                if (EditorUtility.DisplayCancelableProgressBar("Searching Large Textures", 
                    $"Processing texture {currentCount} of {totalCount}", 
                    (float)currentCount / totalCount))
                {
                    // 用户取消了操作
                    break;
                }
            }
            
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            
            if (texture != null)
            {
                // 获取纹理的真实尺寸（原始导入尺寸）
                int width = texture.width;
                int height = texture.height;
                
                // 检查尺寸是否超过阈值
                if (width > sizeThreshold || height > sizeThreshold)
                {
                    // 获取文件大小
                    string fullPath = Application.dataPath + path.Substring("Assets".Length);
                    FileInfo fileInfo = new FileInfo(fullPath);
                    long fileSize = fileInfo.Exists ? fileInfo.Length : 0;
                    
                    largeTextures.Add(new TextureData(texture, path, width, height, fileSize));
                }
            }
            
            currentCount++;
        }
        
        EditorUtility.ClearProgressBar();
        isSearching = false;
        
        Debug.Log($"Search complete. Found {largeTextures.Count} textures larger than {sizeThreshold}px.");
    }

    private string GetFileSizeString(long fileSizeBytes)
    {
        if (fileSizeBytes < 1024)
            return $"{fileSizeBytes} B";
        else if (fileSizeBytes < 1024 * 1024)
            return $"{fileSizeBytes / 1024f:F2} KB";
        else
            return $"{fileSizeBytes / (1024f * 1024f):F2} MB";
    }
}