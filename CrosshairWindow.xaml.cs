using System;
using System.Windows;
using System.Windows.Input; // Для System.Windows.Input.Cursor

namespace Air_Folder
{
    public partial class CrosshairWindow : Window
    {
        public CrosshairWindow()
        {
            InitializeComponent();
            // Устанавливаем курсор для этого окна (не будет виден из-за IsHitTestVisible)
            // Cursor = Cursors.Cross; // Эта строка не нужна, так как IsHitTestVisible="False"
        }

        // Метод для позиционирования окна по координатам курсора
        public void SetPosition(Point screenCoordinates)
        {
            // Учитываем размер окна, чтобы центр крестика был на курсоре
            this.Left = screenCoordinates.X - (this.Width / 2);
            this.Top = screenCoordinates.Y - (this.Height / 2);
        }
    }
}
