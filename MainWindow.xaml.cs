using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Air_Folder
{

    public partial class MainWindow : Window
    {
        private GeneralSettingsControl _generalSettingsControl;
        private MyStacksControl _myStacksControl;

        public ObservableCollection<StackConfiguration> Stacks { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Stacks = new ObservableCollection<StackConfiguration>();
            LoadStacks();

            _generalSettingsControl = new GeneralSettingsControl();
            _myStacksControl = new MyStacksControl(Stacks);

            _myStacksControl.StackDeleted += MyStacksControl_StackDeleted;
            _myStacksControl.RequestStackEdit += MyStacksControl_RequestStackEdit;
            _myStacksControl.RequestStackCreate += MyStacksControl_RequestStackCreate;

            SettingsContent.Content = _generalSettingsControl;
            VerticalTabControl.SelectionChanged += VerticalTabControl_SelectionChanged;

            Closed += MainWindow_Closed;
        }

        private void VerticalTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VerticalTabControl.SelectedIndex == 0) // Общие
                SettingsContent.Content = _generalSettingsControl;
            else if (VerticalTabControl.SelectedIndex == 1) // Мои стеки
                SettingsContent.Content = _myStacksControl;
            else if (VerticalTabControl.SelectedIndex == 2) // Этой вкладки больше нет
            {
                VerticalTabControl.SelectedIndex = 1;
            }
        }

        private void MyStacksControl_RequestStackCreate(object sender, EventArgs e)
        {
            ShowStackSettingsWindow(null);
        }

        private void MyStacksControl_RequestStackEdit(object sender, StackConfiguration stackToEdit)
        {
            ShowStackSettingsWindow(stackToEdit);
        }

        private void ShowStackSettingsWindow(StackConfiguration stack = null)
        {
            var settingsWindow = new StackSettingsWindow(stack);
            settingsWindow.Owner = this;

            if (settingsWindow.ShowDialog() == true) // Только если пользователь нажал "Сохранить"
            {
                StackConfiguration savedStack = settingsWindow.ResultStack;
                if (savedStack != null) // Результат не null, значит стек сохранен
                {
                    bool updated = false;
                    for (int i = 0; i < Stacks.Count; i++)
                    {
                        // Ищем существующий стек по StackNumberId (это более надёжно, чем по Name)
                        // Важно: StackSettingsWindow должна возвращать StackNumberId для редактируемых стеков.
                        if (Stacks[i].StackNumberId == savedStack.StackNumberId && savedStack.StackNumberId != 0)
                        {
                            // Обновляем существующий стек в коллекции
                            // (SavedStack содержит все новые значения из окна настроек)
                            Stacks[i] = savedStack;
                            updated = true;

                            // --- ВАЖНОЕ ИЗМЕНЕНИЕ: СОХРАНЯЕМ ОБНОВЛЁННЫЙ СТЕК НА ДИСК ---
                            StackManager.Save(Stacks[i]); // Сохраняем индивидуальный файл стека
                            break;
                        }
                    }

                    if (!updated) // Это сценарий создания НОВОГО стека (StackNumberId у него будет 0)
                    {
                        // Проверяем, что для нового стека указан путь к папке отображения (TargetFolderPath)
                        if (string.IsNullOrEmpty(savedStack.TargetFolderPath))
                        {
                            MessageBox.Show("Для нового стека необходимо указать путь к папке для отображения содержимого.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return; // Не добавляем и не сохраняем, если путь не указан
                        }

                        // Нет необходимости вручную создавать папку для Stack N.json здесь.
                        // StackManager.Save() позаботится о создании RootStacksDirectory.
                        // Также нет необходимости вручную создавать папку savedStack.TargetFolderPath здесь,
                        // если она уже выбрана пользователем или подразумевается, что она существует.

                        Stacks.Add(savedStack);
                        // --- ВАЖНОЕ ИЗМЕНЕНИЕ: СОХРАНЯЕМ НОВЫЙ СТЕК НА ДИСК ---
                        // StackManager.Save() присвоит ему StackNumberId и FolderPath.
                        StackManager.Save(savedStack);
                    }
                    // --- УДАЛИЛИ SaveStacks(); отсюда, т.к. каждый стек сохраняется индивидуально ---
                }
            }
        }

        private void MyStacksControl_StackDeleted(object sender, StackConfiguration deletedStack)
        {
            if (deletedStack != null)
            {
                Stacks.Remove(deletedStack);
                StackManager.Delete(deletedStack);
            }
        }
        private void MainWindow_Closed(object sender, EventArgs e)
        {
        }

        private void LoadStacks()
        {
            try
            {
                // Используем новый метод из StackManager для загрузки всех стеков
                var loadedStacksList = StackManager.LoadAll();

                // Очищаем существующую коллекцию и добавляем в неё загруженные стеки
                Stacks.Clear();
                foreach (var stack in loadedStacksList)
                {
                    Stacks.Add(stack);
                }
                Console.WriteLine($"Загружено {Stacks.Count} стеков из {StackManager.RootStacksDirectory}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке стеков: {ex.Message}", "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
