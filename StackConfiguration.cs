using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace Air_Folder
{
    public class StackConfiguration : INotifyPropertyChanged // Убедись, что INotifyPropertyChanged реализован!
    {
        public StackConfiguration Clone()
        {
            // MemberwiseClone создает поверхностную копию. Для ваших свойств (строки, числа)
            // это то, что нужно.
            return (StackConfiguration)this.MemberwiseClone();
        }

        // Приватные поля для хранения значений свойств
        private string _name;
        private string _folderPath;
        private string _iconPath;
        private double _posX;
        private double _posY;
        private BitmapSource _currentIconSource;
        private string _previousIconPath; // Добавлено для INotifyPropertyChanged
        private string _openingDirection;
        private int _stackNumberId;
        private string _targetFolderPath;
        public string OpeningDirection

        {
            get => _openingDirection;
            set
            {
                var trimmedValue = value?.Trim();
                if (_openingDirection != trimmedValue)
                {
                    _openingDirection = trimmedValue;
                    OnPropertyChanged(); // Вызываем событие при изменении        
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public string FolderPath
        {
            get => _folderPath;
            set
            {
                if (_folderPath != value)
                {
                    _folderPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TargetFolderPath // Это путь к папке, которую нужно отобразить
        {
            get => _targetFolderPath;
            set
            {
                if (_targetFolderPath != value)
                {
                    _targetFolderPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public int StackNumberId
        {
            get => _stackNumberId;
            set
            {
                if (_stackNumberId != value)
                {
                    _stackNumberId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string IconPath
        {
            get => _iconPath;
            set
            {
                if (_iconPath != value)
                {
                    _iconPath = value;
                    OnPropertyChanged();
                    UpdateBitmapSource(); // Обновляем BitmapSource при изменении IconPath
                }
            }
        }

        public double PosX
        {
            get => _posX;
            set
            {
                if (_posX != value)
                {
                    _posX = value;
                    OnPropertyChanged();
                }
            }
        }

        public double PosY
        {
            get => _posY;
            set
            {
                if (_posY != value)
                {
                    _posY = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public BitmapSource IconSource
        {
            get
            {
                if (_currentIconSource == null || _previousIconPath != _iconPath)
                {
                    UpdateBitmapSource();
                }
                return _currentIconSource;
            }
        }

        private void UpdateBitmapSource()
        {
            try
            {
                if (!string.IsNullOrEmpty(_iconPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_iconPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    _currentIconSource = bitmap;
                }
                else
                {
                    // ИСПРАВЛЕНО ИМЯ ИКОНКИ: default_stack.ico
                    _currentIconSource = new BitmapImage(new Uri("pack://application:,,,/Air Folder;component/Assets/default_stack.ico"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки иконки '{_iconPath}': {ex.Message}");
                try
                {
                    // ИСПРАВЛЕНО ИМЯ ИКОНКИ: default_stack.ico
                    _currentIconSource = new BitmapImage(new Uri("pack://application:,,,/Air Folder;component/Assets/default_stack.ico"));
                }
                catch (Exception defaultEx)
                {
                    Console.WriteLine($"Ошибка загрузки дефолтной иконки: {defaultEx.Message}");
                    _currentIconSource = null;
                }
            }

            _previousIconPath = _iconPath;
            OnPropertyChanged(nameof(IconSource)); // Уведомляем UI, что свойство IconSource изменилось
        }

        public StackConfiguration()
        {
            OpeningDirection = "справа"; // Значение по умолчанию
        }

        // Реализация INotifyPropertyChanged:
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
