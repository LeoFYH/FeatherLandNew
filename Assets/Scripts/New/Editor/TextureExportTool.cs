using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine.U2D;
using UnityEngine.UI;

public class TextureExportTool : EditorWindow
{
    private string exportPath = "";
    private Vector2 scrollPos;
    private List<string> logMessages = new List<string>();
    private int exportCount = 0;
    private int totalFound = 0;
    
    // Options
    private bool exportSprites = true;
    private bool exportTextures = true;
    private bool exportFromAtlases = true;
    private bool exportFromPrefabs = true;
    private bool keepFolderStructure = true;
    private bool includeMaterials = false;
    
    [MenuItem("Tools/Texture Export Tool")]
    public static void ShowWindow()
    {
        GetWindow<TextureExportTool>("Texture Export Tool");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Texture Export Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // Export Path
        GUILayout.Label("Export Path:");
        EditorGUILayout.BeginHorizontal();
        exportPath = EditorGUILayout.TextField(exportPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.SaveFolderPanel("Select Export Folder", "", "");
            if (!string.IsNullOrEmpty(path))
                exportPath = path;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Options
        GUILayout.Label("Options:", EditorStyles.boldLabel);
        exportSprites = EditorGUILayout.Toggle("Export Sprites", exportSprites);
        exportTextures = EditorGUILayout.Toggle("Export Textures", exportTextures);
        exportFromAtlases = EditorGUILayout.Toggle("Export from Sprite Atlases", exportFromAtlases);
        exportFromPrefabs = EditorGUILayout.Toggle("Export from Prefabs", exportFromPrefabs);
        keepFolderStructure = EditorGUILayout.Toggle("Keep Folder Structure", keepFolderStructure);
        includeMaterials = EditorGUILayout.Toggle("Include Materials", includeMaterials);
        
        EditorGUILayout.Space(10);
        
        // Buttons
        if (GUILayout.Button("Scan Project", GUILayout.Height(30)))
        {
            ScanProject();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("Export All Textures", GUILayout.Height(40)))
        {
            if (string.IsNullOrEmpty(exportPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select an export path first!", "OK");
                return;
            }
            ExportAllTextures();
        }
        
        EditorGUILayout.Space(10);
        
        // Results
        if (exportCount > 0)
        {
            EditorGUILayout.LabelField($"Exported: {exportCount} files", EditorStyles.boldLabel);
        }
        
        // Log
        if (logMessages.Count > 0)
        {
            GUILayout.Label("Export Log:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (string msg in logMessages)
            {
                EditorGUILayout.LabelField(msg, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        }
    }
    
    void ScanProject()
    {
        logMessages.Clear();
        totalFound = 0;
        
        // Count textures
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D");
        logMessages.Add($"Found {textureGuids.Length} Texture2D assets");
        totalFound += textureGuids.Length;
        
        // Count sprites
        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite");
        logMessages.Add($"Found {spriteGuids.Length} Sprite assets");
        totalFound += spriteGuids.Length;
        
        // Count sprite atlases
        string[] atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas");
        logMessages.Add($"Found {atlasGuids.Length} SpriteAtlas assets");
        
        // Count prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        logMessages.Add($"Found {prefabGuids.Length} Prefab assets");
        
        Repaint();
    }
    
    void ExportAllTextures()
    {
        logMessages.Clear();
        exportCount = 0;
        
        if (!Directory.Exists(exportPath))
            Directory.CreateDirectory(exportPath);
        
        HashSet<string> allTexturePaths = new HashSet<string>();
        
        try
        {
            // 1. Collect from textures
            if (exportTextures)
            {
                CollectTextures(allTexturePaths);
            }
            
            // 2. Collect from sprites
            if (exportSprites)
            {
                CollectSprites(allTexturePaths);
            }
            
            // 3. Collect from sprite atlases
            if (exportFromAtlases)
            {
                CollectFromSpriteAtlases(allTexturePaths);
            }
            
            // 4. Collect from prefabs
            if (exportFromPrefabs)
            {
                CollectFromPrefabs(allTexturePaths);
            }
            
            // 5. Export all collected files
            ExportFiles(allTexturePaths);
            
            // 6. Log completion
            logMessages.Add("=========================================");
            logMessages.Add($"Export completed successfully!");
            logMessages.Add($"Total files exported: {exportCount}");
            logMessages.Add($"Export location: {exportPath}");
            
            // Open folder
            EditorUtility.RevealInFinder(exportPath);
        }
        catch (System.Exception e)
        {
            logMessages.Add($"Error during export: {e.Message}");
            logMessages.Add($"Stack trace: {e.StackTrace}");
        }
        
        Repaint();
    }
    
    void CollectTextures(HashSet<string> texturePaths)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                texturePaths.Add(path);
            }
        }
        logMessages.Add($"Collected {guids.Length} textures");
    }
    
    void CollectSprites(HashSet<string> texturePaths)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                texturePaths.Add(path);
            }
        }
        logMessages.Add($"Collected {guids.Length} sprites");
    }
    
    void CollectFromSpriteAtlases(HashSet<string> texturePaths)
    {
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas");
        int collected = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            
            if (atlas != null)
            {
                // Get sprites from atlas
                Sprite[] sprites = new Sprite[atlas.spriteCount];
                atlas.GetSprites(sprites);
                
                foreach (Sprite sprite in sprites)
                {
                    if (sprite != null)
                    {
                        string spritePath = AssetDatabase.GetAssetPath(sprite);
                        if (!string.IsNullOrEmpty(spritePath) && File.Exists(spritePath))
                        {
                            texturePaths.Add(spritePath);
                            collected++;
                        }
                    }
                }
            }
        }
        
        logMessages.Add($"Collected {collected} textures from sprite atlases");
    }
    
    void CollectFromPrefabs(HashSet<string> texturePaths)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int collected = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                // Check all components in prefab
                Component[] components = prefab.GetComponentsInChildren<Component>(true);
                foreach (Component component in components)
                {
                    if (component is Image image && image.sprite != null)
                    {
                        string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                        if (!string.IsNullOrEmpty(spritePath))
                        {
                            texturePaths.Add(spritePath);
                            collected++;
                        }
                    }
                    else if (component is SpriteRenderer spriteRenderer && spriteRenderer.sprite != null)
                    {
                        string spritePath = AssetDatabase.GetAssetPath(spriteRenderer.sprite);
                        if (!string.IsNullOrEmpty(spritePath))
                        {
                            texturePaths.Add(spritePath);
                            collected++;
                        }
                    }
                    else if (component is RawImage rawImage && rawImage.texture != null)
                    {
                        string texturePath = AssetDatabase.GetAssetPath(rawImage.texture);
                        if (!string.IsNullOrEmpty(texturePath))
                        {
                            texturePaths.Add(texturePath);
                            collected++;
                        }
                    }
                }
            }
        }
        
        logMessages.Add($"Collected {collected} textures from prefabs");
    }
    
    void ExportFiles(HashSet<string> filePaths)
    {
        int total = filePaths.Count;
        int current = 0;
        
        foreach (string sourcePath in filePaths)
        {
            current++;
            EditorUtility.DisplayProgressBar("Exporting Textures", 
                $"Exporting {Path.GetFileName(sourcePath)} ({current}/{total})", 
                (float)current / total);
            
            try
            {
                string destPath = "";
                
                if (keepFolderStructure)
                {
                    // Remove Assets/ from the beginning
                    string relativePath = sourcePath;
                    if (sourcePath.StartsWith("Assets/"))
                        relativePath = sourcePath.Substring(7);
                    
                    destPath = Path.Combine(exportPath, relativePath);
                }
                else
                {
                    string fileName = Path.GetFileName(sourcePath);
                    destPath = Path.Combine(exportPath, fileName);
                    
                    // Make filename unique if needed
                    if (File.Exists(destPath))
                    {
                        string name = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        int counter = 1;
                        
                        do
                        {
                            string newName = $"{name}_{counter}{ext}";
                            destPath = Path.Combine(exportPath, newName);
                            counter++;
                        } while (File.Exists(destPath));
                    }
                }
                
                // Create directory if needed
                string destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                
                // Copy the file
                File.Copy(sourcePath, destPath, true);
                
                // Also copy meta file
                string metaFile = sourcePath + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Copy(metaFile, destPath + ".meta", true);
                }
                
                exportCount++;
                
                if (logMessages.Count < 50) // Don't flood the log
                    logMessages.Add($"Exported: {Path.GetFileName(sourcePath)}");
            }
            catch (System.Exception e)
            {
                logMessages.Add($"Failed to export {sourcePath}: {e.Message}");
            }
        }
        
        EditorUtility.ClearProgressBar();
    }
}