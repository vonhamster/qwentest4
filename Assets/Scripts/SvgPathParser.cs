using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Расширенный парсер SVG путей с поддержкой основных команд
/// </summary>
public static class SvgPathParser
{
    /// <summary>
    /// Парсит SVG path данные и возвращает список точек для растеризации
    /// </summary>
    public static List<Vector2> ParsePath(string pathData, float scale = 1f)
    {
        List<Vector2> points = new List<Vector2>();
        
        if (string.IsNullOrEmpty(pathData))
            return points;
        
        // Разбиваем на команды
        int i = 0;
        char currentCommand = '\0';
        char previousCommand = '\0';
        
        Vector2 currentPoint = Vector2.zero;
        Vector2 startPoint = Vector2.zero;
        Vector2 controlPoint1 = Vector2.zero;
        Vector2 controlPoint2 = Vector2.zero;
        
        while (i < pathData.Length)
        {
            char c = pathData[i];
            
            // Пропускаем пробелы и запятые
            if (char.IsWhiteSpace(c) || c == ',')
            {
                i++;
                continue;
            }
            
            // Проверяем, является ли символ командой
            if (char.IsLetter(c))
            {
                currentCommand = c;
                i++;
                
                // Обработка команд
                switch (currentCommand)
                {
                    case 'M': // MoveTo absolute
                    case 'm': // MoveTo relative
                        {
                            List<float> coords = ParseNumbers(pathData, ref i, 2);
                            if (coords.Count >= 2)
                            {
                                if (currentCommand == 'm' && points.Count > 0)
                                {
                                    currentPoint.x += coords[0] * scale;
                                    currentPoint.y += coords[1] * scale;
                                }
                                else
                                {
                                    currentPoint.x = coords[0] * scale;
                                    currentPoint.y = coords[1] * scale;
                                }
                                startPoint = currentPoint;
                                points.Add(currentPoint);
                            }
                        }
                        break;
                        
                    case 'L': // LineTo absolute
                    case 'l': // LineTo relative
                        {
                            List<float> coords = ParseNumbers(pathData, ref i, 2);
                            if (coords.Count >= 2)
                            {
                                if (currentCommand == 'l')
                                {
                                    currentPoint.x += coords[0] * scale;
                                    currentPoint.y += coords[1] * scale;
                                }
                                else
                                {
                                    currentPoint.x = coords[0] * scale;
                                    currentPoint.y = coords[1] * scale;
                                }
                                points.Add(currentPoint);
                            }
                        }
                        break;
                        
                    case 'H': // Horizontal line absolute
                    case 'h': // Horizontal line relative
                        {
                            List<float> coords = ParseNumbers(pathData, ref i, 1);
                            if (coords.Count >= 1)
                            {
                                if (currentCommand == 'h')
                                    currentPoint.x += coords[0] * scale;
                                else
                                    currentPoint.x = coords[0] * scale;
                                points.Add(currentPoint);
                            }
                        }
                        break;
                        
                    case 'V': // Vertical line absolute
                    case 'v': // Vertical line relative
                        {
                            List<float> coords = ParseNumbers(pathData, ref i, 1);
                            if (coords.Count >= 1)
                            {
                                if (currentCommand == 'v')
                                    currentPoint.y += coords[0] * scale;
                                else
                                    currentPoint.y = coords[0] * scale;
                                points.Add(currentPoint);
                            }
                        }
                        break;
                        
                    case 'C': // Cubic Bezier absolute
                    case 'c': // Cubic Bezier relative
                        {
                            List<float> coords = ParseNumbers(pathData, ref i, 6);
                            if (coords.Count >= 6)
                            {
                                Vector2 cp1, cp2, end;
                                
                                if (currentCommand == 'c')
                                {
                                    cp1 = new Vector2(currentPoint.x + coords[0], currentPoint.y + coords[1]) * scale;
                                    cp2 = new Vector2(currentPoint.x + coords[2], currentPoint.y + coords[3]) * scale;
                                    end = new Vector2(currentPoint.x + coords[4], currentPoint.y + coords[5]) * scale;
                                }
                                else
                                {
                                    cp1 = new Vector2(coords[0], coords[1]) * scale;
                                    cp2 = new Vector2(coords[2], coords[3]) * scale;
                                    end = new Vector2(coords[4], coords[5]) * scale;
                                }
                                
                                // Интерполируем кривую Безье в точки
                                List<Vector2> bezierPoints = SampleBezierCurve(currentPoint, cp1, cp2, end, 20);
                                points.AddRange(bezierPoints);
                                
                                currentPoint = end;
                                controlPoint1 = cp1;
                                controlPoint2 = cp2;
                            }
                        }
                        break;
                        
                    case 'Q': // Quadratic Bezier absolute
                    case 'q': // Quadratic Bezier relative
                        {
                            List<float> coords = ParseNumbers(pathData, ref i, 4);
                            if (coords.Count >= 4)
                            {
                                Vector2 cp, end;
                                
                                if (currentCommand == 'q')
                                {
                                    cp = new Vector2(currentPoint.x + coords[0], currentPoint.y + coords[1]) * scale;
                                    end = new Vector2(currentPoint.x + coords[2], currentPoint.y + coords[3]) * scale;
                                }
                                else
                                {
                                    cp = new Vector2(coords[0], coords[1]) * scale;
                                    end = new Vector2(coords[2], coords[3]) * scale;
                                }
                                
                                // Интерполируем квадратичную кривую в точки
                                List<Vector2> quadPoints = SampleQuadraticCurve(currentPoint, cp, end, 20);
                                points.AddRange(quadPoints);
                                
                                currentPoint = end;
                            }
                        }
                        break;
                        
                    case 'Z': // ClosePath
                    case 'z': // ClosePath
                        {
                            points.Add(startPoint);
                            currentPoint = startPoint;
                        }
                        break;
                }
                
                previousCommand = currentCommand;
            }
            else
            {
                i++;
            }
        }
        
        return points;
    }
    
    /// <summary>
    /// Парсит числа из строки path данных
    /// </summary>
    private static List<float> ParseNumbers(string data, ref int index, int maxCount)
    {
        List<float> numbers = new List<float>();
        
        while (index < data.Length && numbers.Count < maxCount)
        {
            // Пропускаем пробелы и запятые
            while (index < data.Length && (char.IsWhiteSpace(data[index]) || data[index] == ','))
                index++;
            
            if (index >= data.Length)
                break;
            
            // Читаем число
            int start = index;
            bool hasDigit = false;
            bool hasDot = false;
            bool hasMinus = false;
            
            if (data[index] == '-')
            {
                hasMinus = true;
                index++;
            }
            
            while (index < data.Length)
            {
                char c = data[index];
                
                if (char.IsDigit(c))
                {
                    hasDigit = true;
                    index++;
                }
                else if (c == '.' && !hasDot)
                {
                    hasDot = true;
                    index++;
                }
                else if (c == 'e' || c == 'E')
                {
                    // Научная нотация
                    index++;
                    if (index < data.Length && (data[index] == '+' || data[index] == '-'))
                        index++;
                }
                else
                {
                    break;
                }
            }
            
            if (hasDigit)
            {
                string numStr = data.Substring(start, index - start);
                float value;
                if (float.TryParse(numStr, out value))
                {
                    numbers.Add(value);
                }
            }
            else if (hasMinus)
            {
                // Отрицательный знак без цифры
                break;
            }
        }
        
        return numbers;
    }
    
    /// <summary>
    /// Сэмплирует кривую Безье в набор точек
    /// </summary>
    private static List<Vector2> SampleBezierCurve(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int samples)
    {
        List<Vector2> points = new List<Vector2>();
        
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float u = 1 - t;
            
            Vector2 point = u * u * u * p0 +
                           3 * u * u * t * p1 +
                           3 * u * t * t * p2 +
                           t * t * t * p3;
            
            points.Add(point);
        }
        
        return points;
    }
    
    /// <summary>
    /// Сэмплирует квадратичную кривую в набор точек
    /// </summary>
    private static List<Vector2> SampleQuadraticCurve(Vector2 p0, Vector2 p1, Vector2 p2, int samples)
    {
        List<Vector2> points = new List<Vector2>();
        
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float u = 1 - t;
            
            Vector2 point = u * u * p0 +
                           2 * u * t * p1 +
                           t * t * p2;
            
            points.Add(point);
        }
        
        return points;
    }
}
