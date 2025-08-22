using IWshRuntimeLibrary;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;

namespace Air_Folder
{
    public partial class StackSettingsControl : System.Windows.Controls.UserControl
    {
        private StackConfiguration _originalStack; // Ссылка на настоящий стек
        public StackConfiguration EditableStack { get; private set; } // Копия для редактирования, к ней будет привязка
        public event EventHandler<StackConfiguration> StackConfigurationSaved;
        public event EventHandler Canceled;

        public StackConfiguration CurrentStack
        {
            get => DataContext as StackConfiguration;
            set => DataContext = value;
        }

        // --- P/Invoke для глобального хука мыши ---
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_MOUSEMOVE = 0x0200;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc _mouseProc;
        private IntPtr _mouseHookID = IntPtr.Zero;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow); // Для скрытия/показа системного курсора

        // --- P/Invoke для глобального хука клавиатуры ---
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _keyboardProc;
        private IntPtr _keyboardHookID = IntPtr.Zero;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        // --- Конец P/Invoke ---

        private CrosshairWindow _crosshairWindow; // Экземпляр окна с крестиком

        private int _originalPosX; // Для сохранения исходной X-позиции
        private int _originalPosY; // Для сохранения исходной Y-позиции


        public StackSettingsControl()
        {
            InitializeComponent();
            ClearForm();
            Unloaded += StackSettingsControl_Unloaded;
        }

        private void StackSettingsControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopPositionSelection(); // Останавливаем все хуки при выгрузке контрола
        }

        public void ClearForm()
        {
            _originalStack = null; // Мы создаем новый, оригинала нет

            EditableStack = new StackConfiguration
            {
                Name = Path.GetFileName(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
                TargetFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                IconPath = "pack://application:,,,/Air Folder;component/Assets/default_icon.ico",
                OpeningDirection = "справа",
                PosX = 0,
                PosY = 200
            };
            DataContext = EditableStack; // Привязываем UI к нашей новой копии
        }

        public void LoadStack(StackConfiguration stack)
        {
            if (stack == null)
            {
                ClearForm();
                return;
            }
            _originalStack = stack; // Запоминаем оригинал
            EditableStack = _originalStack.Clone(); // Создаем копию для редактирования
            DataContext = EditableStack; // Привязываем UI к нашей копии
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            // 1. Сохраняем имя папки из ТЕКУЩЕГО пути ДО того, как он изменится.
            string oldFolderName = null;
            if (!string.IsNullOrWhiteSpace(CurrentStack.TargetFolderPath))
            {
                try
                {
                    oldFolderName = Path.GetFileName(CurrentStack.TargetFolderPath);
                }
                catch (ArgumentException)
                {
                    // На случай, если CurrentStack.FolderPath был некорректным путем
                    oldFolderName = null;
                }
            }

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку для отображения содержимого";
                dialog.SelectedPath = CurrentStack.TargetFolderPath; // Диалог открывается в текущей папке

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // 2. Обновляем путь папки в CurrentStack
                    CurrentStack.TargetFolderPath = dialog.SelectedPath;

                    // 3. Теперь проверяем, нужно ли обновлять имя:
                    //    - Если имя пустое или состоит из пробелов (никогда не было установлено пользователем)
                    //    - ИЛИ если текущее имя совпадает с именем ПРЕДЫДУЩЕЙ папки (значит, оно было авто генерировано)
                    if (string.IsNullOrWhiteSpace(CurrentStack.Name) || CurrentStack.Name == oldFolderName)
                    {
                        CurrentStack.Name = Path.GetFileName(dialog.SelectedPath);
                    }

                    // Обновляем иконку на дефолтную (это, вероятно, правильно, если вы хотите сбрасывать ее при смене папки)
                    CurrentStack.IconPath = "pack://application:,,,/Air Folder;component/Assets/default_icon.ico";
                }
            }
        }


        private void SelectIcon_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Иконки и изображения (*.ico;*.png)|*.ico;*.png|Все файлы (*.*)|*.*";
                dialog.Title = "Выберите иконку для стека";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    CurrentStack.IconPath = dialog.FileName;
                }
            }
        }

        // Метод для создания ярлыка на рабочем столе
        private void CreateDesktopShortcut_Click(object sender, RoutedEventArgs e)
        {
            // Убедимся, что стек существует и имеет TargetFolderPath
            if (CurrentStack == null || string.IsNullOrEmpty(CurrentStack.TargetFolderPath))
            {
                MessageBox.Show("Не выбран стек или не указана папка для отображения содержимого.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            CreateShortcut(desktopPath);
        }

        // Метод для создания ярлыка в папке программы "Air Folder"
        private void CreateProgramFolderShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentStack == null || string.IsNullOrEmpty(CurrentStack.TargetFolderPath))
            {
                MessageBox.Show("Не выбран стек или не указана папка для отображения содержимого.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Путь к папке программы (там, где лежит твой .exe)
            string programFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            CreateShortcut(programFolderPath);
        }

        // Вспомогательный метод для создания самого ярлыка
        private void CreateShortcut(string targetDirectory)
        {
            try
            {
                // Цель ярлыка - это твой исполняемый файл (.exe)
                string appExecutablePath = System.Reflection.Assembly.GetEntryAssembly().Location; // Путь к AirFolder.exe

                // Аргументы для исполняемого файла (для открытия конкретного стека)
                // Напоминаю: TargetFolderPath может содержать пробелы, поэтому заключаем его в кавычки.
                string arguments = $"--open-stack \"{CurrentStack.TargetFolderPath}\"";

                // Имя ярлыка (берем из имени стека, но делаем его безопасным для файловой системы)
                string shortcutName = CurrentStack.Name;
                // Очищаем имя от недопустимых символов для имени файла
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    shortcutName = shortcutName.Replace(c, '_');
                }
                // Добавляем расширение .lnk
                string shortcutFilePath = Path.Combine(targetDirectory, $"{shortcutName}.lnk");

                // --- Обработка существующего ярлыка ---
                if (File.Exists(shortcutFilePath))
                {
                    var result = MessageBox.Show(
                        $"Ярлык с именем '{shortcutName}.lnk' уже существует в папке '{Path.GetFileName(targetDirectory)}'. " +
                        "Хотите заменить его или отменить создание?",
                        "Ярлык уже существует",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        File.Delete(shortcutFilePath); // Удаляем старый
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        // Если пользователь выбрал "Нет" (изменить имя),
                        // то предложим ему новое имя (Stack Name (2).lnk)
                        int counter = 1;
                        string newShortcutName = shortcutName;
                        while (File.Exists(Path.Combine(targetDirectory, $"{newShortcutName}.lnk")))
                        {
                            counter++;
                            newShortcutName = $"{shortcutName} ({counter})";
                        }
                        shortcutFilePath = Path.Combine(targetDirectory, $"{newShortcutName}.lnk");
                    }
                    else // Cancel
                    {
                        return; // Отменяем создание ярлыка
                    }
                }

                // Создаем COM-объект Shell (Windows Script Host)
                WshShell shell = new WshShell();
                // Создаем объект ярлыка
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutFilePath);

                shortcut.Description = $"Ярлык для стека '{CurrentStack.Name}'"; // Описание ярлыка
                shortcut.TargetPath = appExecutablePath; // Целевой исполняемый файл
                shortcut.Arguments = arguments; // Аргументы командной строки

                // Устанавливаем иконку (если IconPath корректен и существует)
                if (!string.IsNullOrEmpty(CurrentStack.IconPath) && File.Exists(CurrentStack.IconPath))
                {
                    shortcut.IconLocation = CurrentStack.IconPath;
                }
                else
                {
                    // Используем иконку по умолчанию, если IconPath некорректен или пуст
                    shortcut.IconLocation = appExecutablePath + ",0"; // Иконка по умолчанию для .exe
                }

                shortcut.Save(); // Сохраняем ярлык

                MessageBox.Show($"Ярлык '{Path.GetFileName(shortcutFilePath)}' успешно создан!", "Ярлык создан", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании ярлыка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveStack_Click(object sender, RoutedEventArgs e)
        {
            StopPositionSelection(); // Останавливаем все хуки и скрываем крестик

            if (_originalStack == null) // Мы создавали новый стек
            {
                StackManager.Save(CurrentStack);
                StackConfigurationSaved?.Invoke(this, CurrentStack);
                MessageBox.Show("Стек сохранен!", "Создание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else // Мы редактировали существующий
            {
                // Копируем все значения из копии обратно в оригинал
                _originalStack.Name = EditableStack.Name;
                _originalStack.TargetFolderPath = EditableStack.TargetFolderPath;
                _originalStack.IconPath = EditableStack.IconPath;
                _originalStack.PosX = EditableStack.PosX;
                _originalStack.PosY = EditableStack.PosY;
                _originalStack.OpeningDirection = EditableStack.OpeningDirection;
                StackManager.Save(_originalStack);
                MessageBox.Show("Стек обновлен!", "Настройки", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            Window.GetWindow(this)?.Close();
        }



        private void OpenBubble_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(CurrentStack.TargetFolderPath))
            {
                MessageBox.Show("Папка не найдена: " + CurrentStack.TargetFolderPath, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

            if (mainWindow != null)
            {
                var bubble = new BubbleWindow(CurrentStack.TargetFolderPath, CurrentStack.PosX, CurrentStack.PosY, CurrentStack.OpeningDirection);
                bubble.Owner = mainWindow;
                bubble.Show();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // ВОССТАНАВЛИВАЕМ ИСХОДНЫЕ ЗНАЧЕНИЯ ПРИ ОТМЕНЕ
            CurrentStack.PosX = _originalPosX;
            CurrentStack.PosY = _originalPosY;

            StopPositionSelection(); // Останавливаем все хуки и скрываем крестик
            Canceled?.Invoke(this, EventArgs.Empty);
            Window.GetWindow(this)?.Close();
        }

        // --- ЛОГИКА ГЛОБАЛЬНОГО ОТСЛЕЖИВАНИЯ МЫШИ ---
        private void SetPositionInSystem_Click(object sender, RoutedEventArgs e)
        {
            Window ownerWindow = Window.GetWindow(this);
            if (ownerWindow == null) return;

            if (_crosshairWindow == null)
            {
                _crosshairWindow = new CrosshairWindow();
                _crosshairWindow.Owner = ownerWindow;
            }

            // СОХРАНЯЕМ ТЕКУЩИЕ ЗНАЧЕНИЯ ДЛЯ ВОЗМОЖНОГО ОТКАТА ПРИ ОТМЕНЕ
            _originalPosX = (int)CurrentStack.PosX;
            _originalPosY = (int)CurrentStack.PosY;

            // Позиционируем крестик по текущим сохраненным координатам
            _crosshairWindow.Left = CurrentStack.PosX - (_crosshairWindow.Width / 2);
            _crosshairWindow.Top = CurrentStack.PosY - (_crosshairWindow.Height / 2);

            _crosshairWindow.Show();
            ShowCursor(false); // Скрываем системный курсор

            // ЗАПУСК ХУКА МЫШИ
            _mouseProc = HookCallback;
            _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, (LowLevelMouseProc)_mouseProc, GetModuleHandle(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName), 0);

            if (_mouseHookID == IntPtr.Zero)
            {
                MessageBox.Show("Не удалось установить хук мыши.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _crosshairWindow?.Close();
                ShowCursor(true);
                return;
            }

            // ЗАПУСК ХУКА КЛАВИАТУРЫ
            _keyboardProc = KeyboardHookCallback;
            _keyboardHookID = SetWindowsHookEx(WH_KEYBOARD_LL, (LowLevelKeyboardProc)_keyboardProc, GetModuleHandle(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName), 0);

            if (_keyboardHookID == IntPtr.Zero)
            {
                MessageBox.Show("Не удалось установить хук клавиатуры.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StopMouseHook(); // Останавливаем хук мыши, если клавиатурный не запустился
                return;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                // ОБНОВЛЕНИЕ В РЕАЛЬНОМ ВРЕМЕНИ: Прямо обновляем CurrentStack
                if (wParam == (IntPtr)WM_MOUSEMOVE)
                {
                    CurrentStack.PosX = hookStruct.pt.x;
                    CurrentStack.PosY = hookStruct.pt.y;

                    Dispatcher.Invoke(() =>
                    {
                        _crosshairWindow?.SetPosition(new System.Windows.Point(hookStruct.pt.x, hookStruct.pt.y));
                    });
                }
                // ФИКСАЦИЯ КООРДИНАТ: Прямо обновляем CurrentStack
                else if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    CurrentStack.PosX = hookStruct.pt.x;
                    CurrentStack.PosY = hookStruct.pt.y;

                    StopPositionSelection(); // Останавливаем все хуки
                    return (IntPtr)1; // Предотвращаем дальнейшую обработку клика
                }
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam); // Virtual-key code

                if (vkCode == (int)System.Windows.Forms.Keys.Escape) // Проверяем, что нажата клавиша ESC
                {
                    Dispatcher.Invoke(() =>
                    {
                        StopPositionSelection(); // Останавливаем все хуки
                        MessageBox.Show("Выбор координат отменен.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                    return (IntPtr)1; // Предотвращаем дальнейшую обработку нажатия ESC
                }
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        // --- Методы для остановки хуков ---
        private void StopPositionSelection()
        {
            StopMouseHook();
            StopKeyboardHook();
            // На этом этапе CurrentStack.PosX/Y уже содержат последние выбранные координаты
            // (или те, что были изначально, если пользователь просто двигал мышь, но не кликал)
        }

        private void StopMouseHook()
        {
            if (_mouseHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookID);
                _mouseHookID = IntPtr.Zero;
            }

            if (_crosshairWindow != null && _crosshairWindow.IsVisible)
            {
                Dispatcher.Invoke(() =>
                {
                    _crosshairWindow.Hide(); // Предпочтительнее Hide() для повторного использования
                });
            }
            ShowCursor(true); // ВОЗВРАЩАЕМ СИСТЕМНЫЙ КУРСОР ГЛОБАЛЬНО
        }

        private void StopKeyboardHook()
        {
            if (_keyboardHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookID);
                _keyboardHookID = IntPtr.Zero;
            }
        }
    }
}
