using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class QuickTextureExport : Editor
{
    [MenuItem("Assets/Export Textures", false, 1000)]
    static void ExportSelectedTextures()
    {
        string exportPath = EditorUtility.SaveFolderPanel("Select Export Folder", "", "");
        if (string.IsNullOrEmpty(exportPath))
            return;
        
        if (!Directory.Exists(exportPath))
            Directory.CreateDirectory(exportPath);
        
        List<string> filesToExport = new List<string>();
        
        // Get all selected assets
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            
            // Check if it's a texture or sprite
            if (obj is Texture2D || obj is Sprite)
            {
                if (File.Exists(path))
                    filesToExport.Add(path);
            }
            // Also check if it's a folder
            else if (AssetDatabase.IsValidFolder(path))
            {
                // Find all textures in this folder
                string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { path });
                
                foreach (string guid in textureGuids)
                {
                    string texturePath = AssetDatabase.GUIDToAssetPath(guid);
                    if (File.Exists(texturePath))
                        filesToExport.Add(texturePath);
                }
                
                foreach (string guid in spriteGuids)
                {
                    string spritePath = AssetDatabase.GUIDToAssetPath(guid);
                    if (File.Exists(spritePath))
                        filesToExport.Add(spritePath);
                }
            }
        }
        
        // Export all files
        int exportedCount = 0;
        for (int i = 0; i < filesToExport.Count; i++)
        {
            string sourcePath = filesToExport[i];
            
            EditorUtility.DisplayProgressBar("Exporting", 
                $"Exporting {Path.GetFileName(sourcePath)}", 
                (float)i / filesToExport.Count);
            
            try
            {
                string fileName = Path.GetFileName(sourcePath);
                string destPath = Path.Combine(exportPath, fileName);
                
                // Handle duplicate names
                if (File.Exists(destPath))
                {
                    string name = Path.GetFileNameWithoutExtension(fileName);
                    string ext = Path.GetExtension(fileName);
                    int counter = 1;
                    
                    while (File.Exists(destPath))
                    {
                        destPath = Path.Combine(exportPath, $"{name}_{counter}{ext}");
                        counter++;
                    }
                }
                
                File.Copy(sourcePath, destPath, true);
                
                // Copy meta file
                string metaFile = sourcePath + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Copy(metaFile, destPath + ".meta", true);
                }
                
                exportedCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export {sourcePath}: {e.Message}");
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        // Show results
        EditorUtility.DisplayDialog("Export Complete", 
            $"Successfully exported {exportedCount} files to:\n{exportPath}", "OK");
        
        // Open the folder
        EditorUtility.RevealInFinder(exportPath);
    }
    
    [MenuItem("Assets/Export Textures", true)]
    static bool ValidateExportSelectedTextures()
    {
        // Only show menu item if we have something selected
        return Selection.objects.Length > 0;
    }
}