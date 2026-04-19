using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Runtime компонент для загрузки SVG и генерации текстур
/// Можно использовать в билде игры
/// </summary>
public class SvgTextureGeneratorRuntime : MonoBehaviour
{
    [Header("Settings")]
    public int textureWidth = 1024;
    public int textureHeight = 1024;
    
    [Header("Output")]
    public string outputFolder = "Generated";
    
    /// <summary>
    /// Генерирует текстуру из SVG файла
    /// </summary>
    public Texture2D GenerateFromSvgFile(string svgFilePath, out Dictionary<Color32, Vector2> centroids)
    {
        centroids = new Dictionary<Color32, Vector2>();
        
        if (!File.Exists(svgFilePath))
        {
            Debug.LogError($"SVG file not found: {svgFilePath}");
            return null;
        }
        
        string svgContent = File.ReadAllText(svgFilePath);
        
        Dictionary<Color32, Color32> colorMapping;
        Texture2D texture = SvgTextureGenerator.GenerateTextureFromSvg(
            svgContent, 
            textureWidth, 
            textureHeight, 
            out colorMapping,
            out centroids
        );
        
        return texture;
    }
    
    /// <summary>
    /// Генерирует текстуру из SVG контента
    /// </summary>
    public Texture2D GenerateFromSvgContent(string svgContent, out Dictionary<Color32, Vector2> centroids)
    {
        Dictionary<Color32, Color32> colorMapping;
        Texture2D texture = SvgTextureGenerator.GenerateTextureFromSvg(
            svgContent, 
            textureWidth, 
            textureHeight, 
            out colorMapping,
            out centroids
        );
        
        return texture;
    }
    
    /// <summary>
    /// Сохраняет текстуру в PNG файл
    /// </summary>
    public string SaveTexture(Texture2D texture, string fileName)
    {
        if (texture == null)
        {
            Debug.LogError("Texture is null");
            return null;
        }
        
        string fullPath = Path.Combine(Application.dataPath, outputFolder, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngData);
        
        string relativePath = Path.Combine("Assets", outputFolder, fileName);
        return relativePath;
    }
    
    /// <summary>
    /// Сохраняет цветовую карту в JSON файл
    /// </summary>
    public string SaveColorMapping(Dictionary<Color32, Color32> colorMapping, Dictionary<Color32, Vector2> centroids, string fileName)
    {
        string jsonContent = SvgTextureGenerator.SaveColorMappingToJson(colorMapping, centroids);
        
        string fullPath = Path.Combine(Application.dataPath, outputFolder, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        
        File.WriteAllText(fullPath, jsonContent);
        
        string relativePath = Path.Combine("Assets", outputFolder, fileName);
        return relativePath;
    }
    
    /// <summary>
    /// Полный пайплайн: загрузка SVG, генерация текстуры, сохранение результатов
    /// </summary>
    public bool ProcessSvgFile(string svgFilePath, out string texturePath, out string jsonPath)
    {
        texturePath = null;
        jsonPath = null;
        
        Dictionary<Color32, Vector2> centroids;
        Texture2D texture = GenerateFromSvgFile(svgFilePath, out centroids);
        if (texture == null)
            return false;
        
        // Получаем цветовую карту через повторную генерацию (в реальном проекте лучше рефакторить)
        string svgContent = File.ReadAllText(svgFilePath);
        Dictionary<Color32, Color32> colorMapping;
        Dictionary<Color32, Vector2> tempCentroids;
        SvgTextureGenerator.GenerateTextureFromSvg(svgContent, textureWidth, textureHeight, out colorMapping, out tempCentroids);
        
        string fileName = Path.GetFileNameWithoutExtension(svgFilePath);
        texturePath = SaveTexture(texture, $"{fileName}.png");
        jsonPath = SaveColorMapping(colorMapping, tempCentroids, $"{fileName}_colors.json");
        
        return true;
    }
}
