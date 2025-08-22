using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media; // *** ВАЖНО: Добавьте этот using для ScaleTransform ***
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Air_Folder
{
    public partial class BubbleWindow : Window
    {
        private readonly double AnimationDurationSeconds = 0.3;

        private string _folderPath;
        private double _initialPosX; // X-координата крестика (точки открытия)
        private double _initialPosY; // Y-координата крестика (точки открытия)
        private string _openingDirection;

        private double _finalWidth;
        private double _finalHeight;
        private bool _isClosingAnimationRunning = false;

        public BubbleWindow(string folderPath, double posX, double posY, string openingDirection)
        {
            InitializeComponent();

            _folderPath = folderPath;
            _initialPosX = posX;
            _initialPosY = posY;
            _openingDirection = openingDirection?.Trim() ?? "справа"; // Если null, то по умолчанию "справа"

            // Начальные состояния окна перед анимацией
            this.Opacity = 0;   // Начинаем полностью прозрачным

            LoadItems(); // Загружаем содержимое папки

            Loaded += BubbleWindow_Loaded; // Событие при загрузке окна
            Closing += BubbleWindow_Closing; // Событие при попытке закрытия окна

            Deactivated += BubbleWindow_Deactivated; // Событие потери фокуса
            MouseLeave += BubbleWindow_MouseLeave; // Событие ухода мыши
        }

        private void LoadItems()
        {
            if (Directory.Exists(_folderPath))
            {
                var files = Directory.GetFiles(_folderPath).Where(file => !File.GetAttributes(file).HasFlag(FileAttributes.Hidden));
                foreach (var file in files)
                {
                    var txt = new System.Windows.Controls.TextBlock
                    {
                        Text = Path.GetFileNameWithoutExtension(file),
                        Margin = new Thickness(5),
                        Cursor = Cursors.Hand
                    };

                    txt.MouseLeftButtonUp += (s, e) =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Ошибка открытия файла: " + ex.Message);
                        }
                        CloseAndActivateMainWindow();
                    };

                    ItemsPanel.Children.Add(txt);
                }
            }
        }

        private void BubbleWindow_Deactivated(object sender, EventArgs e)
        {
            CloseAndActivateMainWindow();
        }

        private void BubbleWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseAndActivateMainWindow();
        }

        private void CloseAndActivateMainWindow()
        {
            // Проверяем флаг, чтобы не запускать анимацию закрытия многократно
            if (!_isClosingAnimationRunning)
            {
                AnimateClose(); // Запускаем анимацию закрытия
            }
        }

        private void BubbleWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Здесь получаем фактические размеры окна после загрузки XAML и определения его размеров
            _finalWidth = this.ActualWidth;
            _finalHeight = this.ActualHeight;

            // Устанавливаем начальные Left/Top окна и RenderTransformOrigin 
            // до начала анимации, чтобы окно "появилось" из правильной точки
            SetInitialWindowPositionAndOrigin();

            AnimateOpen(); // Запускаем анимацию открытия
        }

        private void BubbleWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Отменяем стандартное закрытие, чтобы выполнить анимацию
            AnimateClose(); // Запускаем анимацию закрытия
        }

        // --- Методы анимации ---

        // Устанавливает начальную позицию окна и точку трансформации для анимации
        private void SetInitialWindowPositionAndOrigin()
        {
            // Убеждаемся, что LayoutRoot.RenderTransform - это TransformGroup
            TransformGroup transformGroup = LayoutRoot.RenderTransform as TransformGroup;
            if (ContentScaleTransform == null)
            {
                throw new InvalidOperationException("ContentScaleTransform не был инициализирован. Проверьте BubbleWindow.xaml.");
            }

            // Ищем существующий ScaleTransform или создаем новый
            ContentScaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
            if (ContentScaleTransform == null)
            {
                ContentScaleTransform = new ScaleTransform();
                transformGroup.Children.Add(ContentScaleTransform);
            }

            // Получаем ScaleTransform из LayoutRoot
            // LayoutRoot - это ваш Border из XAML (с x:Name="LayoutRoot")
            ScaleTransform scaleTransform = LayoutRoot.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                // Если ScaleTransform не найден (например, забыли добавить в XAML),
                // добавляем его программно. Это менее предпочтительно, но делает код более устойчивым.
                scaleTransform = new ScaleTransform();
                LayoutRoot.RenderTransform = scaleTransform;
            }

            switch (_openingDirection)
            {
                case "справа":
                    // Открывается вправо от крестика: Начало анимации от левого края крестика
                    LayoutRoot.RenderTransformOrigin = new Point(0, 0.5); // Масштабирование от левого центра LayoutRoot
                    this.Left = _initialPosX; // Левый край окна совпадает с X-позицией крестика
                    this.Top = _initialPosY - _finalHeight / 2; // Окно центрировано по Y относительно крестика
                    break;

                case "слева":
                    // Открывается влево от крестика: Начало анимации от правого края крестика
                    LayoutRoot.RenderTransformOrigin = new Point(1, 0.5); // Масштабирование от правого центра LayoutRoot
                    this.Left = _initialPosX; // <--- ИЗМЕНЕНО: Начальная позиция Left (правый край окна совпадает с X крестика)
                    this.Top = _initialPosY - _finalHeight / 2; // Окно центрировано по Y относительно крестика
                    break;

                case "сверху":
                    // Открывается вверх от крестика: Начало анимации от нижнего края крестика
                    LayoutRoot.RenderTransformOrigin = new Point(0.5, 1); // Масштабирование от нижнего центра LayoutRoot
                    this.Left = _initialPosX - _finalWidth / 2; // Окно центрировано по X относительно крестика
                    this.Top = _initialPosY; // <--- ИЗМЕНЕНО: Начальная позиция Top (нижний край окна совпадает с Y крестика)
                    break;

                case "снизу":
                    // Открывается вниз от крестика: Начало анимации от верхнего края крестика
                    LayoutRoot.RenderTransformOrigin = new Point(0.5, 0); // Масштабирование от верхнего центра LayoutRoot
                    this.Left = _initialPosX - _finalWidth / 2; // Окно центрировано по X относительно крестика
                    this.Top = _initialPosY; // Верхний край окна совпадает с Y-позицией крестика
                    break;

                default: // Если _openingDirection каким-то образом не совпал
                    // Можно использовать поведение по умолчанию, например "справа"
                    goto case "справа";
            }

            // Устанавливаем ScaleTransform в начальное состояние (0,0)
            // Это важно, чтобы анимация роста начиналась с нулевого размера
            ContentScaleTransform.ScaleX = 0; // <-- Используем напрямую
            ContentScaleTransform.ScaleY = 0; // <-- Используем напрямую
        }

        private void AnimateOpen()
        {
            var storyboard = new Storyboard();
            storyboard.Duration = TimeSpan.FromSeconds(AnimationDurationSeconds);

            // Анимация прозрачности (всегда от 0 до 1)
            var opacityAnimation = new DoubleAnimation(0, 1, storyboard.Duration);
            Storyboard.SetTarget(opacityAnimation, this);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(Window.OpacityProperty));
            storyboard.Children.Add(opacityAnimation);

            // Анимации размеров и положения окна, а также масштабирования LayoutRoot
            // в зависимости от выбранного направления
            switch (_openingDirection)
            {
                case "справа":
                    // Анимация ширины окна
                    var widthRightAnimation = new DoubleAnimation(0, _finalWidth, storyboard.Duration);
                    Storyboard.SetTarget(widthRightAnimation, this);
                    Storyboard.SetTargetProperty(widthRightAnimation, new PropertyPath(Window.WidthProperty));
                    storyboard.Children.Add(widthRightAnimation);

                    // Анимация масштаба по X для LayoutRoot
                    var scaleXRightAnimation = new DoubleAnimation(0, 1, storyboard.Duration);
                    Storyboard.SetTarget(scaleXRightAnimation, ContentScaleTransform); // <-- Используем _contentScaleTransform
                    Storyboard.SetTargetProperty(scaleXRightAnimation, new PropertyPath(ScaleTransform.ScaleXProperty)); // <-- Явное свойство
                    storyboard.Children.Add(scaleXRightAnimation);
                    break;

                case "слева":
                    // Анимация ширины окна (без изменений, т.к. она просто увеличивает ширину)
                    var widthLeftAnimation = new DoubleAnimation(0, _finalWidth, storyboard.Duration);
                    Storyboard.SetTarget(widthLeftAnimation, this);
                    Storyboard.SetTargetProperty(widthLeftAnimation, new PropertyPath(Window.WidthProperty));
                    storyboard.Children.Add(widthLeftAnimation);

                    // Анимация положения Left окна (перемещается влево при росте)
                    // From: _initialPosX (текущее Left, т.к. мы его изменили выше)
                    // To: _initialPosX - _finalWidth (конечное Left положение)
                    var leftLeftAnimation = new DoubleAnimation(_initialPosX, _initialPosX - _finalWidth, storyboard.Duration); // <--- ИЗМЕНЕНО
                    Storyboard.SetTarget(leftLeftAnimation, this);
                    Storyboard.SetTargetProperty(leftLeftAnimation, new PropertyPath(Window.LeftProperty));
                    storyboard.Children.Add(leftLeftAnimation);

                    // Анимация масштаба по X для LayoutRoot (без изменений)
                    var scaleXLeftAnimation = new DoubleAnimation(0, 1, storyboard.Duration);
                    Storyboard.SetTarget(scaleXLeftAnimation, ContentScaleTransform);
                    Storyboard.SetTargetProperty(scaleXLeftAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));
                    storyboard.Children.Add(scaleXLeftAnimation);
                    break;

                case "сверху":
                    // Анимация высоты окна (без изменений, т.к. она просто увеличивает высоту)
                    var heightUpAnimation = new DoubleAnimation(0, _finalHeight, storyboard.Duration);
                    Storyboard.SetTarget(heightUpAnimation, this);
                    Storyboard.SetTargetProperty(heightUpAnimation, new PropertyPath(Window.HeightProperty));
                    storyboard.Children.Add(heightUpAnimation);

                    // Анимация положения Top окна (перемещается вверх при росте)
                    // From: _initialPosY (текущее Top, т.к. мы его изменили выше)
                    // To: _initialPosY - _finalHeight (конечное Top положение)
                    var topUpAnimation = new DoubleAnimation(_initialPosY, _initialPosY - _finalHeight, storyboard.Duration); // <--- ИЗМЕНЕНО
                    Storyboard.SetTarget(topUpAnimation, this);
                    Storyboard.SetTargetProperty(topUpAnimation, new PropertyPath(Window.TopProperty));
                    storyboard.Children.Add(topUpAnimation);

                    // Анимация масштаба по Y для LayoutRoot (без изменений)
                    var scaleYUpAnimation = new DoubleAnimation(0, 1, storyboard.Duration);
                    Storyboard.SetTarget(scaleYUpAnimation, ContentScaleTransform);
                    Storyboard.SetTargetProperty(scaleYUpAnimation, new PropertyPath(ScaleTransform.ScaleYProperty));
                    storyboard.Children.Add(scaleYUpAnimation);
                    break;

                case "снизу":
                    // Анимация высоты окна
                    var heightDownAnimation = new DoubleAnimation(0, _finalHeight, storyboard.Duration);
                    Storyboard.SetTarget(heightDownAnimation, this);
                    Storyboard.SetTargetProperty(heightDownAnimation, new PropertyPath(Window.HeightProperty));
                    storyboard.Children.Add(heightDownAnimation);

                    // Анимация масштаба по Y для LayoutRoot
                    var scaleYDownAnimation = new DoubleAnimation(0, 1, storyboard.Duration);
                    Storyboard.SetTarget(scaleYDownAnimation, ContentScaleTransform); // <-- Используем _contentScaleTransform
                    Storyboard.SetTargetProperty(scaleYDownAnimation, new PropertyPath(ScaleTransform.ScaleYProperty)); // <-- Явное свойство
                    storyboard.Children.Add(scaleYDownAnimation);
                    break;

                default: // Если _openingDirection каким-то образом не совпал
                    // Можно использовать поведение по умолчанию, например "справа"
                    goto case "справа"; // Это оператор перехода, который просто переходит к case "справа"
            }

            storyboard.Begin(); // Запускаем анимацию
        }

        private void AnimateClose()
        {
            _isClosingAnimationRunning = true;

            var storyboard = new Storyboard();
            storyboard.Duration = TimeSpan.FromSeconds(AnimationDurationSeconds);

            var opacityAnimation = new DoubleAnimation(1, 0, storyboard.Duration);
            Storyboard.SetTarget(opacityAnimation, this);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(Window.OpacityProperty));
            storyboard.Children.Add(opacityAnimation);

            switch (_openingDirection)
            {
                case "справа":
                    var widthRightAnimation = new DoubleAnimation(this.Width, 0, storyboard.Duration); // ИСПОЛЬЗУЕМ this.Width
                    Storyboard.SetTarget(widthRightAnimation, this);
                    Storyboard.SetTargetProperty(widthRightAnimation, new PropertyPath(Window.WidthProperty));
                    storyboard.Children.Add(widthRightAnimation);

                    var scaleXRightAnimation = new DoubleAnimation(
                        ContentScaleTransform.ScaleX, 0, storyboard.Duration); // <-- Используем _contentScaleTransform
                    Storyboard.SetTarget(scaleXRightAnimation, LayoutRoot);
                    Storyboard.SetTargetProperty(scaleXRightAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));
                    storyboard.Children.Add(scaleXRightAnimation);
                    break;

                case "слева":
                    var widthLeftAnimation = new DoubleAnimation(this.Width, 0, storyboard.Duration); // ИСПОЛЬЗУЕМ this.Width
                    Storyboard.SetTarget(widthLeftAnimation, this);
                    Storyboard.SetTargetProperty(widthLeftAnimation, new PropertyPath(Window.WidthProperty));
                    storyboard.Children.Add(widthLeftAnimation);

                    var leftLeftAnimation = new DoubleAnimation(this.Left, _initialPosX, storyboard.Duration); // ИСПОЛЬЗУЕМ this.Left
                    Storyboard.SetTarget(leftLeftAnimation, this);
                    Storyboard.SetTargetProperty(leftLeftAnimation, new PropertyPath(Window.LeftProperty));
                    storyboard.Children.Add(leftLeftAnimation);

                    var scaleXLeftAnimation = new DoubleAnimation(
                        ContentScaleTransform.ScaleX, 0, storyboard.Duration); // <-- Используем _contentScaleTransform
                    Storyboard.SetTarget(scaleXLeftAnimation, ContentScaleTransform);
                    Storyboard.SetTargetProperty(scaleXLeftAnimation, new PropertyPath(ScaleTransform.ScaleXProperty)); // <-- Явное свойство
                    break;

                case "сверху":
                    var heightUpAnimation = new DoubleAnimation(this.Height, 0, storyboard.Duration); // ИСПОЛЬЗУЕМ this.Height
                    Storyboard.SetTarget(heightUpAnimation, this);
                    Storyboard.SetTargetProperty(heightUpAnimation, new PropertyPath(Window.HeightProperty));
                    storyboard.Children.Add(heightUpAnimation);

                    var topUpAnimation = new DoubleAnimation(this.Top, _initialPosY, storyboard.Duration); // ИСПОЛЬЗУЕМ this.Top
                    Storyboard.SetTarget(topUpAnimation, this);
                    Storyboard.SetTargetProperty(topUpAnimation, new PropertyPath(Window.TopProperty));
                    storyboard.Children.Add(topUpAnimation);

                    var scaleYUpAnimation = new DoubleAnimation(
                        ContentScaleTransform.ScaleY, 0, storyboard.Duration); // <-- Используем _contentScaleTransform
                    Storyboard.SetTarget(scaleYUpAnimation, ContentScaleTransform);
                    Storyboard.SetTargetProperty(scaleYUpAnimation, new PropertyPath(ScaleTransform.ScaleYProperty)); // <-- Явное свойство
                    storyboard.Children.Add(scaleYUpAnimation);
                    break;

                case "снизу":
                    var heightDownAnimation = new DoubleAnimation(this.Height, 0, storyboard.Duration); // ИСПОЛЬЗУЕМ this.Height
                    Storyboard.SetTarget(heightDownAnimation, this);
                    Storyboard.SetTargetProperty(heightDownAnimation, new PropertyPath(Window.HeightProperty));
                    storyboard.Children.Add(heightDownAnimation);

                    var scaleYDownAnimation = new DoubleAnimation(
                        ContentScaleTransform.ScaleY, 0, storyboard.Duration); // <-- Используем _contentScaleTransform
                    Storyboard.SetTarget(scaleYDownAnimation, ContentScaleTransform);
                    Storyboard.SetTargetProperty(scaleYDownAnimation, new PropertyPath(ScaleTransform.ScaleYProperty)); // <-- Явное свойство
                    storyboard.Children.Add(scaleYDownAnimation);
                    break;

                default:
                    goto case "справа";
            }

            storyboard.Completed += (s, e) =>
            {
                this.Closing -= BubbleWindow_Closing;
                base.Close();
                _isClosingAnimationRunning = false;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Application.Current.MainWindow?.Activate();
                }), DispatcherPriority.ApplicationIdle);
            };
            storyboard.Begin();
        }
    }
}
