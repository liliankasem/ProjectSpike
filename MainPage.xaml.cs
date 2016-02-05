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
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ProjectSpike
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        string room;
        private CoreDispatcher dispatcher;

        private static uint HResultRecognizerNotFound = 0x8004503a;
        private static int NoCaptureDevicesHResult = -1072845856;
        private SpeechRecognizer speechRecognizer;
        private ResourceContext speechContext;
        private ResourceMap speechResourceMap;

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SelectRoom.SelectedIndex = 0;
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
            room = SelectRoom.SelectedValue.ToString();
        }

        private async Task InitializeRecognizer(Language recognizerLanguage)
        {
            if (speechRecognizer != null)
            {
                // cleanup prior to re-initializing this scenario.
                //speechRecognizer.StateChanged -= SpeechRecognizer_StateChanged;
                //speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
                speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;

                this.speechRecognizer.Dispose();
                this.speechRecognizer = null;
            }

            try
            {
                this.speechRecognizer = new SpeechRecognizer(recognizerLanguage);

                // Provide feedback to the user about the state of the recognizer. This can be used to provide visual feedback in the form
                // of an audio indicator to help the user understand whether they're being heard.
                //speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;

                // Build a command-list grammar. Commands should ideally be drawn from a resource file for localization, and 
                // be grouped into tags for alternate forms of the same command.
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("CheckRoom", speechContext).ValueAsString
                        }, "Check"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("BookRoom", speechContext).ValueAsString
                        }, "Book"));

                SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();
                if (result.Status != SpeechRecognitionResultStatus.Success)
                {
                    // Let the user know that the grammar didn't compile properly.
                    resultTextBlock.Visibility = Visibility.Visible;
                    resultTextBlock.Text = "Unable to compile grammar.";
                }
                else
                {

                    resultTextBlock.Visibility = Visibility.Collapsed;


                    // Handle continuous recognition events. Completed fires when various error states occur. ResultGenerated fires when
                    // some recognized phrases occur, or the garbage rule is hit.
                    //speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
                    speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
                }
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == HResultRecognizerNotFound)
                {

                    resultTextBlock.Visibility = Visibility.Visible;
                    resultTextBlock.Text = "Speech Language pack for selected language not installed.";
                }
                else
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                    await messageDialog.ShowAsync();
                }
            }

        }

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            // The garbage rule will not have a tag associated with it, the other rules will return a string matching the tag provided
            // when generating the grammar.
            string tag = "unknown";
            if (args.Result.Constraint != null)
            {
                tag = args.Result.Constraint.Tag;
            }

            // Developers may decide to use per-phrase confidence levels in order to tune the behavior of their 
            // grammar based on testing.
            if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                args.Result.Confidence == SpeechRecognitionConfidence.High)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    resultTextBlock.Visibility = Visibility.Visible;
                    resultTextBlock.Text = string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})", args.Result.Text, tag, args.Result.Confidence.ToString());
                    switch (tag)
                    {
                        case "Check":
                            CheckRoom();
                            break;
                        case "Book":
                            BookRoom();
                            break;
                        default:
                            break;
                    }
                });
            }
            else
            {
                // In some scenarios, a developer may choose to ignore giving the user feedback in this case, if speech
                // is not the primary input mechanism for the application.
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    resultTextBlock.Visibility = Visibility.Visible;
                    //resultTextBlock.Text = string.Format("Sorry, I didn't catch that. (Heard: '{0}', Tag: {1}, Confidence: {2})", args.Result.Text, tag, args.Result.Confidence.ToString());
                });
            }
        }

        public async static Task<bool> RequestMicrophonePermission()
        {
            try
            {
                // Request access to the microphone only, to limit the number of capabilities we need
                // to request in the package manifest.
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
                settings.MediaCategory = MediaCategory.Speech;
                MediaCapture capture = new MediaCapture();

                await capture.InitializeAsync(settings);
            }
            catch (UnauthorizedAccessException)
            {
                // The user has turned off access to the microphone. If this occurs, we should show an error, or disable
                // functionality within the app to ensure that further exceptions aren't generated when 
                // recognition is attempted.
                return false;
            }
            catch (Exception exception)
            {
                // This can be replicated by using remote desktop to a system, but not redirecting the microphone input.
                // Can also occur if using the virtual machine console tool to access a VM instead of using remote desktop.
                if (exception.HResult == NoCaptureDevicesHResult)
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog("No Audio Capture devices are present on this system.");
                    await messageDialog.ShowAsync();
                    return false;
                }
                else
                {
                    throw;
                }
            }
            return true;
        }
        private int CheckRoom()
        {
            //Go to cloud (or office 365?)
            //Check if {room} is "free" or "busy"
            //return "free" or "busy"
            SelectRoom.SelectedIndex = 1; // Just to give visual feedback for testing purposes
            return 1;
        }

        private int BookRoom()
        {
            SelectRoom.SelectedIndex = 0; // for visual feedback

            return 1;
        }

        private int PlayMusic(string item)
        {
            

            return 1;
        }

        private int CallContact(string item)
        {
            

            return 1;
        }
    }
}
