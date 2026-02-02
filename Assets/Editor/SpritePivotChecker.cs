using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SpritePivotChecker : EditorWindow
{
    private string folderPath = "Assets/";
    private List<SpriteData> spritesWithWrongPivot = new List<SpriteData>();
    private Vector2 scrollPosition;
    private PivotType targetPivot = PivotType.BottomCenter;
    
    private enum PivotType
    {
        BottomLeft,
        BottomCenter,
        BottomRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        TopLeft,
        TopCenter,
        TopRight
    }

    [MenuItem("Tools/Sprite Pivot Checker")]
    public static void ShowWindow()
    {
        GetWindow<SpritePivotChecker>("Sprite Pivot Checker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite Pivot Checker", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Select Folder:", EditorStyles.boldLabel);
        folderPath = EditorGUILayout.TextField("Folder Path:", folderPath);
        
        if (GUILayout.Button("Browse..."))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder Containing Sprites", folderPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Convert to Unity relative path
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogWarning("Selected folder is outside of Assets folder!");
                }
            }
        }

        EditorGUILayout.Space();
        
        GUILayout.Label("Target Pivot Point:", EditorStyles.boldLabel);
        targetPivot = (PivotType)EditorGUILayout.EnumPopup("Target Pivot:", targetPivot);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Scan Sprites"))
        {
            ScanSprites();
        }
        
        if (GUILayout.Button("Select All Wrong Sprites in Project"))
        {
            List<Object> objects = new List<Object>();
            foreach(var spriteData in spritesWithWrongPivot)
            {
                objects.Add(spriteData.sprite);
            }
            Selection.objects = objects.ToArray();
        }
        
        EditorGUILayout.Space();
        
        if (spritesWithWrongPivot.Count > 0)
        {
            GUILayout.Label($"Found {spritesWithWrongPivot.Count} sprites with wrong pivot:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var spriteData in spritesWithWrongPivot)
            {
                EditorGUILayout.BeginHorizontal();
                
                GUILayout.Label(spriteData.sprite.name, GUILayout.Width(200));
                
                string pivotInfo = $"Current: {spriteData.currentPivot} | Expected: {targetPivot}";
                GUILayout.Label(pivotInfo, GUILayout.ExpandWidth(true));
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = spriteData.sprite;
                    EditorGUIUtility.PingObject(spriteData.sprite);
                }
                
                if (GUILayout.Button("Fix", GUILayout.Width(60)))
                {
                    FixSpritePivot(spriteData);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("No sprites with wrong pivot found or scan not performed yet.");
        }
    }

    private void ScanSprites()
    {
        spritesWithWrongPivot.Clear();
        
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("Please specify a folder path.");
            return;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            
            if (sprite != null)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    // Get the sprite import settings
                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    
                    Vector2 actualPivot = Vector2.zero;
                    
                    if (importer.spriteImportMode == SpriteImportMode.Single)
                    {
                        actualPivot = importer.spritePivot;
                    }
                    else if (importer.spriteImportMode == SpriteImportMode.Multiple)
                    {
                        // For spritesheets, we need to check each sprite individually
                        SpriteMetaData[] sprites = importer.spritesheet;
                        foreach (SpriteMetaData spriteData in sprites)
                        {
                            if (spriteData.name == sprite.name)
                            {
                                actualPivot = spriteData.pivot;
                                break;
                            }
                        }
                    }
                    
                    Vector2 expectedPivot = GetPivotVector(targetPivot);
                    
                    if (!Mathf.Approximately(actualPivot.x, expectedPivot.x) || 
                        !Mathf.Approximately(actualPivot.y, expectedPivot.y))
                    {
                        PivotType currentPivotType = GetPivotTypeFromVector(actualPivot);
                        spritesWithWrongPivot.Add(new SpriteData(sprite, path, currentPivotType));
                    }
                }
            }
        }
        
        Debug.Log($"Scan complete. Found {spritesWithWrongPivot.Count} sprites with incorrect pivot.");
    }

    private void FixSpritePivot(SpriteData spriteData)
    {
        TextureImporter importer = AssetImporter.GetAtPath(spriteData.path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            
            if (importer.spriteImportMode == SpriteImportMode.Single)
            {
                importer.spritePivot = GetPivotVector(targetPivot);
                EditorUtility.SetDirty(importer);
                AssetDatabase.ImportAsset(spriteData.path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Fixed pivot for single sprite: {spriteData.sprite.name}");
            }
            else if (importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                // For spritesheets, modify the specific sprite metadata
                SpriteMetaData[] sprites = importer.spritesheet;
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i].name == spriteData.sprite.name)
                    {
                        sprites[i].pivot = GetPivotVector(targetPivot);
                        importer.spritesheet = sprites;
                        EditorUtility.SetDirty(importer);
                        AssetDatabase.ImportAsset(spriteData.path, ImportAssetOptions.ForceUpdate);
                        Debug.Log($"Fixed pivot for spritesheet sprite: {spriteData.sprite.name}");
                        break;
                    }
                }
            }
            
            // Refresh the list after fixing
            ScanSprites();
        }
    }

    private Vector2 GetPivotVector(PivotType pivotType)
    {
        switch (pivotType)
        {
            case PivotType.BottomLeft: return new Vector2(0, 0);
            case PivotType.BottomCenter: return new Vector2(0.5f, 0);
            case PivotType.BottomRight: return new Vector2(1, 0);
            case PivotType.MiddleLeft: return new Vector2(0, 0.5f);
            case PivotType.MiddleCenter: return new Vector2(0.5f, 0.5f);
            case PivotType.MiddleRight: return new Vector2(1, 0.5f);
            case PivotType.TopLeft: return new Vector2(0, 1);
            case PivotType.TopCenter: return new Vector2(0.5f, 1);
            case PivotType.TopRight: return new Vector2(1, 1);
            default: return new Vector2(0.5f, 0);
        }
    }

    private PivotType GetPivotTypeFromVector(Vector2 pivot)
    {
        // Normalize the pivot values to handle small floating point differences
        float x = Mathf.Round(pivot.x * 2) / 2; // Round to nearest 0.5
        float y = Mathf.Round(pivot.y * 2) / 2;
        
        if (Mathf.Approximately(x, 0) && Mathf.Approximately(y, 0)) return PivotType.BottomLeft;
        if (Mathf.Approximately(x, 0.5f) && Mathf.Approximately(y, 0)) return PivotType.BottomCenter;
        if (Mathf.Approximately(x, 1) && Mathf.Approximately(y, 0)) return PivotType.BottomRight;
        if (Mathf.Approximately(x, 0) && Mathf.Approximately(y, 0.5f)) return PivotType.MiddleLeft;
        if (Mathf.Approximately(x, 0.5f) && Mathf.Approximately(y, 0.5f)) return PivotType.MiddleCenter;
        if (Mathf.Approximately(x, 1) && Mathf.Approximately(y, 0.5f)) return PivotType.MiddleRight;
        if (Mathf.Approximately(x, 0) && Mathf.Approximately(y, 1)) return PivotType.TopLeft;
        if (Mathf.Approximately(x, 0.5f) && Mathf.Approximately(y, 1)) return PivotType.TopCenter;
        if (Mathf.Approximately(x, 1) && Mathf.Approximately(y, 1)) return PivotType.TopRight;
        
        return PivotType.MiddleCenter; // Default fallback
    }
    
    private class SpriteData
    {
        public Sprite sprite;
        public string path;
        public PivotType currentPivot;
        
        public SpriteData(Sprite s, string p, PivotType cp)
        {
            sprite = s;
            path = p;
            currentPivot = cp;
        }
    }
}