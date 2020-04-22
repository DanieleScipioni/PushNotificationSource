using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PushNotificationSource.AppShell
{
    public sealed partial class SchedulePicker
    {
        public event RoutedEventHandler Click
        {
            add => SendButton.Click += value;
            remove => SendButton.Click -= value;
        }

        public static readonly DependencyProperty DateTimeProperty = DependencyProperty.Register(
            nameof(DateTime), typeof(DateTimeOffset), typeof(SchedulePicker),
            new PropertyMetadata(default(DateTimeOffset), (o, args) =>
            {
                var schedulePicker = (SchedulePicker) o;
                schedulePicker._changinDateTimeProperty = true;
                var newValue = (DateTimeOffset) args.NewValue;
                schedulePicker.DatePicker.Date = newValue.Date;
                TimeSpan newValueTimeOfDay = newValue.TimeOfDay;
                schedulePicker.TimePicker.Time = new TimeSpan(newValueTimeOfDay.Hours, newValueTimeOfDay.Minutes, 0);
                schedulePicker.ResultScheduleTextBlock.Text = newValue.ToString();
                schedulePicker._changinDateTimeProperty = false;
            }));

        private bool _changinDateTimeProperty;

        public DateTimeOffset DateTime
        {
            get => (DateTimeOffset) GetValue(DateTimeProperty);
            set => SetValue(DateTimeProperty, value);
        }

        public SchedulePicker()
        {
            InitializeComponent();
        }

        private void DatePicker_OnSelectedDateChanged(DatePicker sender, DatePickerSelectedValueChangedEventArgs args)
        {
            if (_changinDateTimeProperty) return;
            DateTime = (args.NewDate ?? DateTimeOffset.Now).Add(DateTime.TimeOfDay);
        }

        private void TimePicker_OnSelectedTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
        {
            if (_changinDateTimeProperty) return;
            DateTime = DateTime.Date.Add(args.NewTime ?? TimeSpan.Zero);
        }

        private void ButtonPlus5_OnClick(object sender, RoutedEventArgs e)
        {
            DateTime = DateTime.AddSeconds(5);
        }

        private void ButtonMinus5_OnClick(object sender, RoutedEventArgs e)
        {
            DateTime = DateTime.AddSeconds(-5);
        }
    }
}
