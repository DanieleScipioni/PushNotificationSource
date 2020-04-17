using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using WebPush;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace PushNotificationSource.AppShell
{
    public sealed partial class MainPage
    {
        private static readonly WebPushClient WebPushClient = new WebPushClient();

        public MainPage()
        {
            InitializeComponent();
            PublicKeyTextBox.Text = "BBrspYRJxJQYsqzVp8FuRPqOQ9L6F5vRbIMQu9CeiYGEP68aEVem5b6e1QK2BQYgNIKh4nORAyjKYEZAE7D5SAA";
            PrivateKeyTextBox.Text = "HByTkaPfAXU1TA5eIVqQfLKYAXwRYo388Lx63vXI5OA";

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;

            Unloaded += (sender, args) =>
            {
                dataTransferManager.DataRequested -= DataTransferManager_DataRequested;
            };
        }

        private async void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Focus(FocusState.Programmatic);
            ButtonPushToSelf.IsEnabled = false;

            try
            {
                await SendAsync(SubscriptionTextBox.Text, MessageTextBox.Text,
                    PublicKeyTextBox.Text, PrivateKeyTextBox.Text);
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.ToString()).ShowAsync();
            }

            ButtonPushToSelf.IsEnabled = true;
        }

        public class Subscription
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public string ChannelUri { get; set; }
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public SubscriptionKeys Keys { get; set; }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        public class SubscriptionKeys
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public string P256Dh { get; set; }
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public string Auth { get; set; }
        }

        private static async Task SendAsync(string subscriptionJson, string payload, string publicKey, string privateKey)
        {
            var subscription = JsonConvert.DeserializeObject<Subscription>(subscriptionJson);

            var pushSubscription = new PushSubscription(subscription.ChannelUri, subscription.Keys.P256Dh, subscription.Keys.Auth);
            var vapidDetails = new VapidDetails("mailto:daniele.scipioni@gmail.com", publicKey, privateKey);
            await WebPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
        }

        private void CreateKeys_OnClick(object sender, RoutedEventArgs e)
        {
            VapidDetails keys = VapidHelper.GenerateVapidKeys();
            PrivateKeyTextBox.Text = keys.PrivateKey;
            PublicKeyTextBox.Text = keys.PublicKey;
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
    }
}
