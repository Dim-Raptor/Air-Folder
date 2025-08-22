using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Air_Folder
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool stackOpenedByShortcut = false;

            if (e.Args.Any())
            {
                int flagIndex = Array.FindIndex(e.Args, arg => arg.Equals("--open-stack", StringComparison.OrdinalIgnoreCase));

                if (flagIndex != -1 && e.Args.Length > flagIndex + 1)
                {
                    string targetFolderPathFromShortcut = e.Args[flagIndex + 1];

                    try
                    {
                        var allLoadedStacks = StackManager.LoadAll();
                        var stackToOpen = allLoadedStacks.FirstOrDefault(s =>
                            s.TargetFolderPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            .Equals(targetFolderPathFromShortcut?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) == true);

                        if (stackToOpen != null)
                        {
                            // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
                            // НЕ СОЗДАЁМ И НЕ ПОКАЗЫВАЕМ MainWindow, если открываем пузырь по ярлыку.
                            // Вместо этого Owner для BubbleWindow будет null (или другим фоновым окном).
                            var bubble = new BubbleWindow(stackToOpen.TargetFolderPath, stackToOpen.PosX, stackToOpen.PosY, stackToOpen.OpeningDirection);
                            bubble.Show(); // Показываем только пузырь
                            stackOpenedByShortcut = true;
                        }
                        else
                        {
                            // Если стек не найден, то показываем MainWindow, чтобы пользователь мог разобраться
                            MessageBox.Show($"Не удалось найти стек, соответствующий папке '{targetFolderPathFromShortcut}'.", "Ошибка запуска ярлыка", MessageBoxButton.OK, MessageBoxImage.Error);
                            new MainWindow().Show(); // Открываем главное окно для отладки/настройки
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Произошла ошибка при запуске стека по ярлыку: {ex.Message}", "Ошибка запуска ярлыка", MessageBoxButton.OK, MessageBoxImage.Error);
                        new MainWindow().Show(); // Открываем главное окно при ошибке
                    }
                }
            }

            // Если стек не был открыт по ярлыку или возникла ошибка, запускаем обычное главное окно
            if (!stackOpenedByShortcut)
            {
                new MainWindow().Show();
            }
        }
    }
}
