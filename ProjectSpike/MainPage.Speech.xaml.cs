using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
using Windows.Media.Capture;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace ProjectSpike
{
    public partial class MainPage
    {

        private async void Speak(string text)
        {
            synthesizer = new SpeechSynthesizer();

            speechContext = ResourceContext.GetForCurrentView();
            speechContext.Languages = new string[] { SpeechSynthesizer.DefaultVoice.Language };

            speechResourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("LocalizationTTSResources");
            // If the media is playing, the user has pressed the button to stop the playback.
            if (media.CurrentState.Equals(MediaElementState.Playing))
            {
                media.Stop();
            }
            else
            {
                try
                {
                    SpeechSynthesisStream synthesisStream = await synthesizer.SynthesizeTextToStreamAsync(text);
                    media.AutoPlay = true;
                    media.SetSource(synthesisStream, synthesisStream.ContentType);
                    media.Play();
                }
                catch (Exception)
                {
                }
            }
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
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("SetRoom", speechContext).ValueAsString
                        }, "Set"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("pomegranate", speechContext).ValueAsString
                        }, "pomegranate"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("raspberry", speechContext).ValueAsString
                        }, "raspberry"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("melon", speechContext).ValueAsString
                        }, "melon"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("Hello", speechContext).ValueAsString
                        }, "Hello"));

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
                    switch (tag)
                    {
                        case "Check":
                            CheckRoom();
                            break;
                        case "Book":
                            BookRoom();
                            break;
                        case "Set":
                            SetRoom();
                            break;
                        case "pomegranate":
                            if (settingRoomMode)
                                Activate("pomegranate");
                            settingRoomMode = false;
                            break;
                        case "raspberry":
                            if (settingRoomMode)
                                Activate("raspberry");
                            settingRoomMode = false;
                            break;
                        case "melon":
                            if (settingRoomMode)
                                Activate("melon");
                            settingRoomMode = false;
                            break;
                        case "Hello":
                            TakePicture();
                            break;

                        default:
                            break;
                    }
                });

            }
        }

    }
}
