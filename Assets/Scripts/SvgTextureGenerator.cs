using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Представляет данные о соответствии цветов
/// </summary>
[Serializable]
public class ColorMappingData
{
    public Dictionary<string, string> colorMappings = new Dictionary<string, string>();
}

/// <summary>
/// Основной класс для обработки SVG и генерации текстуры с цветовой картой
/// </summary>
public static class SvgTextureGenerator
{
    /// <summary>
    /// Парсит SVG файл и извлекает пути и их цвета
    /// </summary>
    public class SvgPathData
    {
        public string pathData;
        public Color32 originalColor;
    }

    /// <summary>
    /// Основная функция генерации текстуры из SVG
    /// </summary>
    public static Texture2D GenerateTextureFromSvg(string svgContent, int width, int height, out Dictionary<Color32, Color32> colorMapping)
    {
        colorMapping = new Dictionary<Color32, Color32>();
        
        // Парсим SVG для получения путей и цветов
        List<SvgPathData> paths = ParseSvg(svgContent);
        
        if (paths.Count == 0)
        {
            Debug.LogError("Не найдено путей в SVG файле");
            return null;
        }

        // Создаем текстуру
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // Очищаем текстуру прозрачным цветом
        Color32[] pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(0, 0, 0, 0);
        }

        // Генерируем уникальные цвета для каждого пути в порядке возрастания
        List<Color32> assignedColors = new List<Color32>();
        uint totalPaths = (uint)paths.Count;
        
        for (uint i = 0; i < totalPaths; i++)
        {
            Color32 color = IndexToColor(i, totalPaths);
            assignedColors.Add(color);
            
            // Добавляем соответствие в словарь
            if (!colorMapping.ContainsKey(color))
            {
                colorMapping[color] = paths[(int)i].originalColor;
            }
        }

        // Рендерим каждый путь с назначенным цветом
        for (int i = 0; i < paths.Count; i++)
        {
            RenderPathToTexture(pixels, width, height, paths[i].pathData, assignedColors[i]);
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return texture;
    }

    /// <summary>
    /// Преобразует индекс в цвет в формате RGBA в порядке возрастания
    /// </summary>
    private static Color32 IndexToColor(uint index, uint totalCount)
    {
        if (totalCount == 0)
            return new Color32(0, 0, 0, 0);
        
        if (totalCount == 1)
            return new Color32(128, 128, 128, 128);
        
        // Вычисляем значение цвета на основе индекса
        // Распределяем значения от 0 до 255 равномерно по всем путям
        uint maxValue = 255;
        uint step = maxValue / (totalCount > 1 ? totalCount - 1 : 1);
        
        uint colorValue = index * step;
        
        // Ограничиваем максимальное значение
        if (colorValue > maxValue)
            colorValue = maxValue;
        
        byte byteValue = (byte)colorValue;
        
        return new Color32(byteValue, byteValue, byteValue, byteValue);
    }

    /// <summary>
    /// Парсит SVG контент и извлекает пути с их цветами
    /// </summary>
    private static List<SvgPathData> ParseSvg(string svgContent)
    {
        List<SvgPathData> paths = new List<SvgPathData>();
        
        // Ищем все элементы path в SVG
        int startIndex = 0;
        while (true)
        {
            int pathStart = svgContent.IndexOf("<path", startIndex, StringComparison.OrdinalIgnoreCase);
            if (pathStart == -1)
                break;
            
            int pathEnd = svgContent.IndexOf("/>", pathStart);
            if (pathEnd == -1)
            {
                pathEnd = svgContent.IndexOf(">", pathStart);
                if (pathEnd != -1)
                {
                    // Ищем закрывающий тег
                    int closeTag = svgContent.IndexOf("</path>", pathEnd, StringComparison.OrdinalIgnoreCase);
                    if (closeTag != -1)
                        pathEnd = closeTag + 7;
                }
            }
            
            if (pathEnd == -1)
                break;
            
            string pathTag = svgContent.Substring(pathStart, pathEnd - pathStart);
            
            // Извлекаем атрибут d (данные пути)
            string pathData = ExtractAttribute(pathTag, "d");
            
            // Извлекаем атрибут fill (цвет заполнения)
            string fillColor = ExtractAttribute(pathTag, "fill");
            
            if (!string.IsNullOrEmpty(pathData))
            {
                Color32 color = ParseColor(fillColor);
                paths.Add(new SvgPathData
                {
                    pathData = pathData,
                    originalColor = color
                });
            }
            
            startIndex = pathEnd + 1;
        }
        
        return paths;
    }

    /// <summary>
    /// Извлекает значение атрибута из XML тега
    /// </summary>
    private static string ExtractAttribute(string tag, string attributeName)
    {
        // Пробуем различные форматы атрибутов
        string[] patterns = new string[]
        {
            attributeName + "=\"",
            attributeName + "='",
            attributeName + " =\"",
            attributeName + " ='"
        };
        
        foreach (string pattern in patterns)
        {
            int attrIndex = tag.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (attrIndex != -1)
            {
                int valueStart = attrIndex + pattern.Length;
                char quoteChar = tag[valueStart - 1];
                int valueEnd = tag.IndexOf(quoteChar, valueStart);
                
                if (valueEnd != -1)
                {
                    return tag.Substring(valueStart, valueEnd - valueStart);
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// Парсит строку цвета в Color32
    /// </summary>
    private static Color32 ParseColor(string colorString)
    {
        if (string.IsNullOrEmpty(colorString))
            return new Color32(0, 0, 0, 255); // Черный по умолчанию
        
        colorString = colorString.Trim().ToLower();
        
        // Обработка named colors
        if (colorString == "none" || colorString == "transparent")
            return new Color32(0, 0, 0, 0);
        
        // Обработка hex цветов
        if (colorString.StartsWith("#"))
        {
            string hex = colorString.Substring(1);
            
            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return new Color32(r, g, b, 255);
            }
            else if (hex.Length == 8)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                byte a = Convert.ToByte(hex.Substring(6, 2), 16);
                return new Color32(r, g, b, a);
            }
            else if (hex.Length == 3)
            {
                byte r = Convert.ToByte(hex.Substring(0, 1) + hex.Substring(0, 1), 16);
                byte g = Convert.ToByte(hex.Substring(1, 1) + hex.Substring(1, 1), 16);
                byte b = Convert.ToByte(hex.Substring(2, 1) + hex.Substring(2, 1), 16);
                return new Color32(r, g, b, 255);
            }
        }
        
        // Обработка rgb/rgba
        if (colorString.StartsWith("rgb"))
        {
            int openParen = colorString.IndexOf('(');
            int closeParen = colorString.IndexOf(')');
            
            if (openParen != -1 && closeParen != -1)
            {
                string values = colorString.Substring(openParen + 1, closeParen - openParen - 1);
                string[] parts = values.Split(',');
                
                if (parts.Length >= 3)
                {
                    byte r = ParseColorValue(parts[0]);
                    byte g = ParseColorValue(parts[1]);
                    byte b = ParseColorValue(parts[2]);
                    byte a = 255;
                    
                    if (parts.Length >= 4)
                    {
                        float alpha = 0f;
                        if (float.TryParse(parts[3], out alpha))
                            a = (byte)(alpha * 255);
                    }
                    
                    return new Color32(r, g, b, a);
                }
            }
        }
        
        return new Color32(0, 0, 0, 255);
    }

    /// <summary>
    /// Парсит отдельное значение цвета (0-255 или процент)
    /// </summary>
    private static byte ParseColorValue(string value)
    {
        value = value.Trim();
        
        if (value.EndsWith("%"))
        {
            float percent = 0f;
            if (float.TryParse(value.Substring(0, value.Length - 1), out percent))
                return (byte)(percent * 255 / 100);
        }
        else
        {
            byte result = 0;
            if (byte.TryParse(value, out result))
                return result;
        }
        
        return 0;
    }

    /// <summary>
    /// Рендерит путь на текстуру
    /// Использует алгоритм заполнения полигона (scanline)
    /// </summary>
    private static void RenderPathToTexture(Color32[] pixels, int width, int height, string pathData, Color32 color)
    {
        // Парсим путь в список точек
        List<Vector2> points = SvgPathParser.ParsePath(pathData);
        
        if (points.Count < 3)
            return;
        
        // Находим bounding box пути
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        foreach (var point in points)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }
        
        // Ограничиваем координаты размерами текстуры
        int startX = Mathf.Max(0, Mathf.FloorToInt(minX));
        int endX = Mathf.Min(width - 1, Mathf.CeilToInt(maxX));
        int startY = Mathf.Max(0, Mathf.FloorToInt(minY));
        int endY = Mathf.Min(height - 1, Mathf.CeilToInt(maxY));
        
        // Заполняем полигон используя алгоритм scanline
        for (int y = startY; y <= endY; y++)
        {
            List<int> intersections = new List<int>();
            
            // Находим пересечения горизонтали с ребрами полигона
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];
                
                // Проверяем, пересекает ли ребро горизонталь
                if ((p1.y <= y && p2.y > y) || (p2.y <= y && p1.y > y))
                {
                    // Вычисляем x координату пересечения
                    int x = Mathf.RoundToInt(p1.x + (y - p1.y) / (p2.y - p1.y) * (p2.x - p1.x));
                    
                    if (x >= startX && x <= endX)
                        intersections.Add(x);
                }
            }
            
            // Сортируем пересечения
            intersections.Sort();
            
            // Заполняем пиксели между парами пересечений
            for (int i = 0; i < intersections.Count - 1; i += 2)
            {
                for (int x = intersections[i]; x <= intersections[i + 1] && x < width; x++)
                {
                    int index = y * width + x;
                    if (index >= 0 && index < pixels.Length)
                        pixels[index] = color;
                }
            }
        }
    }

    /// <summary>
    /// Сохраняет цветовую карту в JSON формат
    /// </summary>
    public static string SaveColorMappingToJson(Dictionary<Color32, Color32> colorMapping)
    {
        var mappingData = new ColorMappingData();
        
        foreach (var kvp in colorMapping)
        {
            string textureColor = Color32ToString(kvp.Key);
            string originalColor = Color32ToString(kvp.Value);
            
            if (!mappingData.colorMappings.ContainsKey(textureColor))
            {
                mappingData.colorMappings.Add(textureColor, originalColor);
            }
        }
        
        // Простая сериализация в JSON
        return JsonUtility.ToJson(new ColorMappingWrapper { mappings = mappingData.colorMappings }, true);
    }

    [Serializable]
    private class ColorMappingWrapper
    {
        public Dictionary<string, string> mappings;
    }

    /// <summary>
    /// Преобразует Color32 в строку формата "RRGGBBAA"
    /// </summary>
    public static string Color32ToString(Color32 color)
    {
        return $"{color.r:X2}{color.g:X2}{color.b:X2}{color.a:X2}";
    }

    /// <summary>
    /// Преобразует строку формата "RRGGBBAA" или "RRGGBB" в Color32
    /// </summary>
    public static Color32 StringToColor32(string colorString)
    {
        if (string.IsNullOrEmpty(colorString))
            return new Color32(0, 0, 0, 255);
        
        colorString = colorString.Replace("#", "");
        
        if (colorString.Length == 8)
        {
            byte r = Convert.ToByte(colorString.Substring(0, 2), 16);
            byte g = Convert.ToByte(colorString.Substring(2, 2), 16);
            byte b = Convert.ToByte(colorString.Substring(4, 2), 16);
            byte a = Convert.ToByte(colorString.Substring(6, 2), 16);
            return new Color32(r, g, b, a);
        }
        else if (colorString.Length == 6)
        {
            byte r = Convert.ToByte(colorString.Substring(0, 2), 16);
            byte g = Convert.ToByte(colorString.Substring(2, 2), 16);
            byte b = Convert.ToByte(colorString.Substring(4, 2), 16);
            return new Color32(r, g, b, 255);
        }
        
        return new Color32(0, 0, 0, 255);
    }
}
