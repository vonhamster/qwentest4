# SVG Texture Generator для Unity3D

## Описание
Проект для генерации текстур из SVG файлов с автоматическим созданием цветовой карты.

## Структура проекта

```
Assets/
├── Editor/
│   └── SvgImporterWindow.cs      # Editor окно для импорта SVG
├── Scripts/
│   ├── SvgTextureGenerator.cs    # Основной генератор текстур
│   ├── SvgPathParser.cs          # Парсер SVG путей
│   └── SvgTextureGeneratorRuntime.cs  # Runtime компонент
```

## Возможности

1. **Парсинг SVG файлов**
   - Поддержка элементов `<path>`
   - Извлечение атрибутов `d` (данные пути) и `fill` (цвет)
   - Поддержка форматов цветов: hex (#RGB, #RRGGBB, #RRGGBBAA), rgb(), rgba()

2. **Генерация текстуры**
   - Каждый контур получает уникальный цвет в формате RGBA
   - Цвета распределяются от 0x00000000 до 0xFFFFFFFF равномерно
   - Используется алгоритм scanline для заполнения полигонов

3. **Создание JSON карты цветов**
   - Соответствие цветов текстуры оригинальным цветам из SVG
   - Формат: `{"RRGGBBAA": "оригинальный_цвет", ...}`

## Использование

### Через Editor (рекомендуется)

1. Откройте Unity Editor
2. Перейдите в меню `Tools → SVG Importer`
3. Выберите SVG файл в поле "SVG File"
4. Настройте размер текстуры (по умолчанию 1024x1024)
5. Нажмите "Generate Texture & JSON"

### Через код (Runtime)

```csharp
// Добавление компонента на GameObject
var generator = gameObject.AddComponent<SvgTextureGeneratorRuntime>();

// Генерация из файла
string svgPath = "Assets/Input/image.svg";
string texturePath, jsonPath;
bool success = generator.ProcessSvgFile(svgPath, out texturePath, out jsonPath);

if (success)
{
    Debug.Log($"Текстура: {texturePath}");
    Debug.Log($"JSON: {jsonPath}");
}
```

### Прямой вызов API

```csharp
using System.Collections.Generic;
using UnityEngine;

// Чтение SVG файла
string svgContent = File.ReadAllText("path/to/file.svg");

// Генерация текстуры
Dictionary<Color32, Color32> colorMapping;
Texture2D texture = SvgTextureGenerator.GenerateTextureFromSvg(
    svgContent, 
    1024,  // ширина
    1024,  // высота
    out colorMapping
);

// Сохранение JSON
string json = SvgTextureGenerator.SaveColorMappingToJson(colorMapping);
File.WriteAllText("output.json", json);

// Сохранение текстуры
byte[] pngData = texture.EncodeToPNG();
File.WriteAllBytes("output.png", pngData);
```

## Формат выходных данных

### Текстура (PNG)
- Формат: RGBA32
- Каждый замкнутый контур закрашен уникальным цветом
- Цвета распределены равномерно от черного к белому

### JSON файл
```json
{
    "mappings": {
        "00000000": "FF0000FF",
        "1A1A1A1A": "00FF00FF",
        "33333333": "0000FFFF",
        ...
    }
}
```

Где:
- Ключ - цвет на сгенерированной текстуре (формат RRGGBBAA)
- Значение - оригинальный цвет из SVG (формат RRGGBBAA)

## Поддерживаемые SVG команды

- `M/m` - MoveTo (перемещение)
- `L/l` - LineTo (линия)
- `H/h` - Horizontal line (горизонтальная линия)
- `V/v` - Vertical line (вертикальная линия)
- `C/c` - Cubic Bezier (кубическая кривая Безье)
- `Q/q` - Quadratic Bezier (квадратичная кривая)
- `Z/z` - ClosePath (закрытие пути)

## Требования

- Unity 2019.4 или выше
- .NET Standard 2.0 или .NET Framework

## Примечания

1. Все контуры должны быть замкнутыми для корректного заполнения
2. Порядок контуров в SVG файле определяет порядок назначения цветов
3. Прозрачные области остаются прозрачными (RGBA = 00000000)
