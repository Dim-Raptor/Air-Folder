using System;
using System.Windows;

namespace Air_Folder
{
    public partial class StackSettingsWindow : Window
    {
        public StackConfiguration ResultStack { get; private set; } // Инициализируется как null по умолчанию

        public StackSettingsWindow(StackConfiguration stackToEdit = null)
        {
            InitializeComponent();

            StackSettingsControlInstance.LoadStack(stackToEdit);

            StackSettingsControlInstance.StackConfigurationSaved += StackSettingsControlInstance_StackConfigurationSaved;
            StackSettingsControlInstance.Canceled += StackSettingsControlInstance_Canceled;
        }

        private void StackSettingsControlInstance_StackConfigurationSaved(object sender, StackConfiguration savedStack)
        {
            ResultStack = savedStack; // Сохраняем результат
            this.DialogResult = true; // Указываем, что операция успешна
            // this.Close() будет вызван автоматически ShowDialog()
        }

        private void StackSettingsControlInstance_Canceled(object sender, EventArgs e)
        {
            ResultStack = null; // При отмене результат сбрасывается
            this.DialogResult = false; // Указываем, что операция отменена
            // this.Close() будет вызван автоматически ShowDialog()
        }

        // Отписка от событий при закрытии окна
        private void Window_Closed(object sender, EventArgs e)
        {
            StackSettingsControlInstance.StackConfigurationSaved -= StackSettingsControlInstance_StackConfigurationSaved;
            StackSettingsControlInstance.Canceled -= StackSettingsControlInstance_Canceled;
        }
    }
}
