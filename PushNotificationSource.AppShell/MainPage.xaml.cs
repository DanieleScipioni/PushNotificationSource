using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebPush;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PushNotificationSource.AppShell
{
    public sealed partial class MainPage
    {
        private static readonly WebPushClient WebPushClient = new WebPushClient();

        private const string DefaultPrivateKey = "HByTkaPfAXU1TA5eIVqQfLKYAXwRYo388Lx63vXI5OA";
        private const string DefaultPublicKey = "BBrspYRJxJQYsqzVp8FuRPqOQ9L6F5vRbIMQu9CeiYGEP68aEVem5b6e1QK2BQYgNIKh4nORAyjKYEZAE7D5SAA";

        private readonly DispatcherTimer _vaidatorTimer;
        private readonly string _currentKeysFileName = "currentKeys.txt";

        public MainPage()
        {
            InitializeComponent();

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;

            SchedlulerFlayout.Opened += SchedlulerFlayoutOnOpened;
            Loaded += OnLoaded;
            Unloaded += (sender, args) => { dataTransferManager.DataRequested -= DataTransferManager_DataRequested; };

            _vaidatorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _vaidatorTimer.Tick += VaidatorTimerOnTick;
        }

        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            (string privateKey, string publicKey) = await ReadCurrentKeys();
            PublicKeyTextBox.Text = publicKey;
            PrivateKeyTextBox.Text = privateKey;
        }

        private async void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            await SendScheduled(DateTime.Now);
        }

        private async void ButtonSendcheduled_Click(object sender, RoutedEventArgs e)
        {
            DateTimeOffset schedulePickerDateTime = SchedulePicker.DateTime;
            await SendScheduled(schedulePickerDateTime.DateTime);
        }

        private async Task SendScheduled(DateTime sendSchedule)
        {
            MessageTextBox.Focus(FocusState.Programmatic);
            DateTime now = DateTime.Now;
            if (sendSchedule < now)
            {
                await Send();
            }
            else
            {
                ThreadPoolTimer.CreateTimer(TimerElapsedHandler, sendSchedule - now);
            }
        }

        private async void TimerElapsedHandler(ThreadPoolTimer handler)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await Send());
        }

        private async Task Send()
        {
            try
            {
                await SendAsync(SubscriptionTextBox.Text, MessageTextBox.Text,
                    PublicKeyTextBox.Text, PrivateKeyTextBox.Text);
            }
            catch (WebPushException ex)
            {
                var exceptionMessage = new StringBuilder(ex.Message).AppendLine(ex.Headers.ToString()).ToString();
                await new MessageDialog(exceptionMessage).ShowAsync();
            }
        }

        public class Subscription
        {
            public string ChannelUri;
            public SubscriptionKeys Keys { get; set; }
        }

        public class SubscriptionKeys
        {
            public string P256Dh;
            public string Auth;
        }

        private static async Task SendAsync(string subscriptionJson, string payload, string publicKey,
            string privateKey)
        {
            var subscription = JsonConvert.DeserializeObject<Subscription>(subscriptionJson);

            var pushSubscription =
                new PushSubscription(subscription.ChannelUri, subscription.Keys.P256Dh, subscription.Keys.Auth);
            var vapidDetails = new VapidDetails("mailto:daniele.scipioni@gmail.com", publicKey, privateKey);
            await WebPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
        }

        private async void CreateKeys_OnClick(object sender, RoutedEventArgs e)
        {
            VapidDetails keys = VapidHelper.GenerateVapidKeys();
            PrivateKeyTextBox.Text = keys.PrivateKey;
            PublicKeyTextBox.Text = keys.PublicKey;

            await PersistCurrentKeys(keys);
        }

        private async void ResetKeys_OnClick(object sender, RoutedEventArgs e)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            if (await localFolder.TryGetItemAsync(_currentKeysFileName) is StorageFile storageFile)
            {
                await storageFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            PublicKeyTextBox.Text = DefaultPublicKey;
            PrivateKeyTextBox.Text = DefaultPrivateKey;
        }

        private void Share_OnClick(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            string value = $"Public: {PublicKeyTextBox.Text}\nPrivate: {PrivateKeyTextBox.Text}";
            request.Data.SetText(value);
            request.Data.Properties.Title = "Share keys";
            request.Data.Properties.Description = "Be carefull, sensitive data!";
        }

        private void Copy_OnClick(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            string value = $"Public: {PublicKeyTextBox.Text}\nPrivate: {PrivateKeyTextBox.Text}";
            dataPackage.SetText(value);
            Clipboard.SetContent(dataPackage);
        }

        private void SchedlulerFlayoutOnOpened(object sender, object e)
        {
            SchedulePicker.DateTime = DateTimeOffset.Now.AddSeconds(5);
        }

        private async void VaidatorTimerOnTick(object sender, object e)
        {
            _vaidatorTimer.Stop();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    var subscription = JsonConvert.DeserializeObject<Subscription>(SubscriptionTextBox.Text);
                    SendButton.IsEnabled = ScheduleButton.IsEnabled =
                        subscription?.ChannelUri != null && subscription.Keys != null;
                }
                catch
                {
                    SendButton.IsEnabled = ScheduleButton.IsEnabled = false;
                }
            });
        }

        private void SubscriptionTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _vaidatorTimer.Stop();
            _vaidatorTimer.Start();
        }

        private async Task PersistCurrentKeys(VapidDetails keys)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile storageFile =
                await localFolder.CreateFileAsync(_currentKeysFileName, CreationCollisionOption.OpenIfExists);
            await FileIO.WriteLinesAsync(storageFile, new[] { keys.PrivateKey, keys.PublicKey });
        }

        private async Task<(string privateKey, string publicKey)> ReadCurrentKeys()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            if (await localFolder.TryGetItemAsync(_currentKeysFileName) is StorageFile storageFile)
            {
                IList<string> lines = await FileIO.ReadLinesAsync(storageFile);
                if (lines.Count == 2)
                {
                    string privateKey = lines[0];
                    string publicKey = lines[1];
                    return (privateKey, publicKey);
                }

                return (null, null);
            }

            return (DefaultPrivateKey, DefaultPublicKey);
        }
    }
}
