using Air_Folder;
using Newtonsoft.Json; // Нужен для сериализации/десериализации
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions; // Нужен для регулярных выражений, чтобы парсить номер из имени файла
using System.Windows; // Нужен для MessageBox, в случае ошибок

public static class StackManager
{
    private const string ConfigFileNamePrefix = "Stack "; // Префикс для имени файла: "Stack "
    private const string ConfigFileExtension = ".json"; // Расширение файла

    // Определяем корневую папку для всех стеков.
    // Она будет находиться в папке, где запущена программа.
    public static string RootStacksDirectory { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Stacks");

    /// <summary>
    /// Сохраняет объект стека в его JSON-файл.
    /// Для новых стеков генерирует уникальный StackNumberId и имя файла.
    /// </summary>
    /// <param name="stack">Объект StackConfiguration для сохранения.</param>
    public static void Save(StackConfiguration stack)
    {
        // Если это новый стек (StackNumberId == 0), присваиваем ему следующий доступный номер
        if (stack.StackNumberId == 0)
        {
            stack.StackNumberId = GetNextStackNumberId();
        }

        // Формируем имя файла на основе номера стека
        string fileName = $"{ConfigFileNamePrefix}{stack.StackNumberId}{ConfigFileExtension}";
        string configFilePath = Path.Combine(RootStacksDirectory, fileName);

        // Убедимся, что корневая папка для стеков существует
        if (!Directory.Exists(RootStacksDirectory))
        {
            Directory.CreateDirectory(RootStacksDirectory);
        }

        // Устанавливаем свойство FolderPath в объекте стека.
        // Теперь FolderPath - это полный путь к файлу JSON-конфигурации стека на диске.
        // Это свойство [JsonIgnore], поэтому оно не будет сохранено в файл,
        // но будет доступно в объекте после загрузки или создания/сохранения.
        stack.FolderPath = configFilePath;

        // Сериализуем объект стека в строку JSON с форматированием для читаемости
        string jsonString = JsonConvert.SerializeObject(stack, Formatting.Indented);

        // Записываем JSON-строку в файл. Если файл уже существует, он будет перезаписан.
        File.WriteAllText(configFilePath, jsonString);
        Console.WriteLine($"Стек '{stack.Name}' (ID: {stack.StackNumberId}) сохранен в {configFilePath}");
    }

    /// <summary>
    /// Загружает все стеки из корневой директории.
    /// </summary>
    /// <returns>Список загруженных объектов StackConfiguration.</returns>
    public static List<StackConfiguration> LoadAll()
    {
        var stacks = new List<StackConfiguration>();

        // Если корневой папки нет, создаем её и возвращаем пустой список.
        if (!Directory.Exists(RootStacksDirectory))
        {
            Directory.CreateDirectory(RootStacksDirectory);
            return stacks;
        }

        // Получаем все файлы, соответствующие нашему шаблону "Stack N.json"
        string searchPattern = $"{ConfigFileNamePrefix}*{ConfigFileExtension}";
        string[] configFiles = Directory.GetFiles(RootStacksDirectory, searchPattern);

        foreach (string filePath in configFiles)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                StackConfiguration loadedStack = JsonConvert.DeserializeObject<StackConfiguration>(jsonContent);

                if (loadedStack != null)
                {
                    // Извлекаем номер стека из имени файла (например, из "Stack 5.json" получаем 5)
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    Match match = Regex.Match(fileName, $"{ConfigFileNamePrefix.Replace(" ", "\\s*")}(\\d+)"); // Учитываем пробел в префиксе
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int stackNum))
                    {
                        loadedStack.StackNumberId = stackNum;
                    }
                    else
                    {
                        // Если имя файла не соответствует шаблону, это не наш файл или он поврежден.
                        // Можно пропустить или присвоить временный ID и логировать.
                        loadedStack.StackNumberId = 0; // Временный ID для таких случаев
                        Console.WriteLine($"Предупреждение: имя файла '{filePath}' не соответствует ожидаемому шаблону 'Stack N.json'.");
                    }

                    // Устанавливаем FolderPath объекта - это полный путь к текущему файлу JSON
                    loadedStack.FolderPath = filePath;
                    stacks.Add(loadedStack);
                }
            }
            catch (Exception ex)
            {
                // Логируем или выводим ошибку, если файл JSON поврежден
                Console.WriteLine($"Ошибка загрузки стека из файла '{filePath}': {ex.Message}");
                MessageBox.Show($"Ошибка при загрузке стека из файла:\n{filePath}\n\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        return stacks;
    }

    /// <summary>
    /// Удаляет файл конфигурации стека с диска.
    /// </summary>
    /// <param name="stack">Объект StackConfiguration, чей файл нужно удалить. FolderPath должен быть установлен.</param>
    public static void Delete(StackConfiguration stack)
    {
        // FolderPath теперь должен содержать полный путь к файлу Stack N.json
        if (!string.IsNullOrEmpty(stack.FolderPath) && File.Exists(stack.FolderPath))
        {
            try
            {
                File.Delete(stack.FolderPath);
                Console.WriteLine($"Файл стека '{stack.Name}' (ID: {stack.StackNumberId}) успешно удален: {stack.FolderPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении файла стека '{stack.Name}' по пути '{stack.FolderPath}': {ex.Message}");
                MessageBox.Show($"Не удалось удалить файл стека '{stack.Name}'. Возможно, он используется другой программой или нет прав.\n\n{ex.Message}", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Находит следующий доступный уникальный номер для нового стека.
    /// </summary>
    /// <returns>Следующий доступный уникальный ID номера стека.</returns>
    private static int GetNextStackNumberId()
    {
        if (!Directory.Exists(RootStacksDirectory))
        {
            return 1; // Если папки нет, начинаем нумерацию с 1
        }

        int maxNumber = 0;
        string searchPattern = $"{ConfigFileNamePrefix}*{ConfigFileExtension}";
        string[] configFiles = Directory.GetFiles(RootStacksDirectory, searchPattern);

        // Проходим по всем файлам, соответствующим шаблону, и находим максимальный номер
        foreach (string filePath in configFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            Match match = Regex.Match(fileName, $"{ConfigFileNamePrefix.Replace(" ", "\\s*")}(\\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int currentNum))
            {
                if (currentNum > maxNumber)
                {
                    maxNumber = currentNum;
                }
            }
        }
        return maxNumber + 1; // Возвращаем следующий номер
    }
}
