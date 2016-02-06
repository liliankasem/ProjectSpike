using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
using Windows.Media.Capture;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Core;
using System.Net.Http;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ProjectSpike
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool settingRoomMode;
        string room;
        private CoreDispatcher dispatcher;

        // Speech recognizition
        private static uint HResultRecognizerNotFound = 0x8004503a;
        private static int NoCaptureDevicesHResult = -1072845856;
        private SpeechRecognizer speechRecognizer;
        private ResourceContext speechContext;
        private ResourceMap speechResourceMap;

        // Speech synthesis
        private SpeechSynthesizer synthesizer;

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetupPersonGroup();

            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            bool permissionGained = await RequestMicrophonePermission();

            if (!permissionGained)
                return; // No permission granted

            Language speechLanguage = SpeechRecognizer.SystemSpeechLanguage;
            string langTag = speechLanguage.LanguageTag;
            speechContext = ResourceContext.GetForCurrentView();
            speechContext.Languages = new string[] { langTag };

            speechResourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("LocalizationSpeechResources");

            await InitializeRecognizer(SpeechRecognizer.SystemSpeechLanguage);

            try
            {
                await speechRecognizer.ContinuousRecognitionSession.StartAsync();
            }
            catch (Exception ex)
            {
                var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                await messageDialog.ShowAsync();
            }

        }

        private void Activate(string v)
        {
            room = v;
            selectedRoom.Text = v.ToUpper();
            Speak("Activated room " + v);
        }

        private void SetRoom()
        {
            settingRoomMode = true;
            Speak("Please select the room");
        }

        private async void CheckRoom()
        {   
            room = selectedRoom.Text;

            Speak("Checking room");

            string page = "http://spikeapi.azurewebsites.net/api/values/{0}" + room;

            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(page))
            using (HttpContent content = response.Content)
            {
                // ... Read the string.
                string result = await content.ReadAsStringAsync();

                // ... Display the result.
                resultTextBlock.Visibility = Visibility.Visible;
                resultTextBlock.Text = result;
                Speak(result);

                if (result != null &&
                result.Length >= 50)
                {
                    Debug.WriteLine(result.Substring(0, 50) + "...");
                }
            }

        }

        

    }
}
