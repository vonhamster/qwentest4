using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor скрипт для импорта SVG файлов и генерации текстур с цветовой картой
/// </summary>
public class SvgImporterWindow : EditorWindow
{
    private TextAsset svgFile;
    private int textureWidth = 1024;
    private int textureHeight = 1024;
    private string outputFolder = "Assets/Generated";

    [MenuItem("Tools/SVG Importer")]
    public static void ShowWindow()
    {
        GetWindow<SvgImporterWindow>("SVG Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("SVG Texture Generator", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        svgFile = (TextAsset)EditorGUILayout.ObjectField("SVG File", svgFile, typeof(TextAsset), false);
        textureWidth = EditorGUILayout.IntField("Texture Width", textureWidth);
        textureHeight = EditorGUILayout.IntField("Texture Height", textureHeight);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate Texture & JSON"))
        {
            GenerateFromSvg();
        }
    }

    private void GenerateFromSvg()
    {
        if (svgFile == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an SVG file", "OK");
            return;
        }

        string svgContent = svgFile.text;
        
        Dictionary<Color32, Color32> colorMapping;
        Texture2D texture = SvgTextureGenerator.GenerateTextureFromSvg(svgContent, textureWidth, textureHeight, out colorMapping);
        
        if (texture == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to generate texture from SVG", "OK");
            return;
        }

        // Создаем папку вывода если не существует
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // Сохраняем текстуру
        string texturePath = Path.Combine(outputFolder, $"{svgFile.name}.png");
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(texturePath, pngData);
        
        // Сохраняем JSON
        string jsonContent = SvgTextureGenerator.SaveColorMappingToJson(colorMapping);
        string jsonPath = Path.Combine(outputFolder, $"{svgFile.name}_colors.json");
        File.WriteAllText(jsonPath, jsonContent);

        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", 
            $"Texture saved to: {texturePath}\nJSON saved to: {jsonPath}", 
            "OK");
    }
}
