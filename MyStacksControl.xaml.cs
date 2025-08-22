using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Air_Folder
{
    public partial class MyStacksControl : UserControl
    {
        private ObservableCollection<StackConfiguration> _stacks;

        public event EventHandler<StackConfiguration> StackDeleted;
        public event EventHandler<StackConfiguration> RequestStackEdit; // НОВОЕ СОБЫТИЕ ДЛЯ РЕДАКТИРОВАНИЯ
        public event EventHandler RequestStackCreate; // НОВОЕ СОБЫТИЕ ДЛЯ СОЗДАНИЯ


        public MyStacksControl(ObservableCollection<StackConfiguration> stacks)
        {
            InitializeComponent();
            _stacks = stacks;
            DataContext = _stacks;
        }

        // Обработчик клика по ИКОНКЕ стека для открытия его настроек
        private void StackIcon_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Image image && image.DataContext is StackConfiguration stack)
            {
                RequestStackEdit?.Invoke(this, stack); // Вызываем событие запроса редактирования
            }
        }

        private void CreateStack_Click(object sender, RoutedEventArgs e)
        {
            RequestStackCreate?.Invoke(this, EventArgs.Empty); // Вызываем событие запроса создания
        }

        private void DeleteStack_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var stackConfig = button?.Tag as StackConfiguration;

            if (stackConfig != null)
            {
                var result = MessageBox.Show($"Вы точно хотите удалить стек '{stackConfig.Name}'?", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _stacks.Remove(stackConfig);
                    StackDeleted?.Invoke(this, stackConfig);
                }
            }
        }

        private void StacksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Здесь можно добавить логику, если нужно что-то делать при выборе элемента в списке
        }
    }
}
