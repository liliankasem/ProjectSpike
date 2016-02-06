using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage;

using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Controls;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml;
using System.Runtime.InteropServices.WindowsRuntime;

namespace ProjectSpike
{
    public partial class MainPage
    {
        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient("92854f62cd6d4823acddf63846fd788e");
        string personGroupId = "jani9";

        private bool frontCam;
        private MediaCapture mediaCapture;
        private InMemoryRandomAccessStream fPhotoStream = new InMemoryRandomAccessStream();

        private async void SetupPersonGroup()
        {

            const string lilianImageDir = @"Assets\PersonGroup\Lilian\";
            const string paulImageDir = @"Assets\PersonGroup\Paul\";
            const string janiImageDir = @"Assets\PersonGroup\Jani\";
            const string edImageDir = @"Assets\PersonGroup\Ed\";
            const string johnImageDir = @"Assets\PersonGroup\John\";

            try
            {
                await faceServiceClient.CreatePersonGroupAsync(personGroupId, "spike");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }

            // Define Users
            CreatePersonResult lilian = await faceServiceClient.CreatePersonAsync(
                personGroupId,
                "Lilian"
            );
            CreatePersonResult paul = await faceServiceClient.CreatePersonAsync(
                personGroupId,
                "Paul"
            );
            CreatePersonResult jani = await faceServiceClient.CreatePersonAsync(
                personGroupId,
                "Jani"
            );
            CreatePersonResult ed = await faceServiceClient.CreatePersonAsync(
                personGroupId,
                "Ed"
            );
            CreatePersonResult john = await faceServiceClient.CreatePersonAsync(
                personGroupId,
                "Paul"
            );

            foreach (string imagePath in Directory.GetFiles(lilianImageDir, "*.jpg"))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    // Detect faces in the image and add to Lilian
                    await faceServiceClient.AddPersonFaceAsync(
                        personGroupId, lilian.PersonId, s);
                }
            }
            foreach (string imagePath in Directory.GetFiles(paulImageDir, "*.jpg"))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    await faceServiceClient.AddPersonFaceAsync(
                        personGroupId, paul.PersonId, s);
                }
            }
            foreach (string imagePath in Directory.GetFiles(janiImageDir, "*.jpg"))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    await faceServiceClient.AddPersonFaceAsync(
                        personGroupId, jani.PersonId, s);
                }
            }
            foreach (string imagePath in Directory.GetFiles(edImageDir, "*.jpg"))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    await faceServiceClient.AddPersonFaceAsync(
                        personGroupId, ed.PersonId, s);
                }
            }
            foreach (string imagePath in Directory.GetFiles(johnImageDir, "*.jpg"))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    await faceServiceClient.AddPersonFaceAsync(
                        personGroupId, john.PersonId, s);
                }
            }


            //Train model
            try
            {
                await faceServiceClient.TrainPersonGroupAsync(personGroupId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

                if (trainingStatus.Status.ToString() != "running")
                {
                    break;
                }

                await Task.Delay(1000);
            }
        }

        private async void IdentifyUser(string imagePath)
        {
            StorageFolder appFolder = await KnownFolders.PicturesLibrary.CreateFolderAsync("Capture", CreationCollisionOption.OpenIfExists);
            StorageFile myfile = await appFolder.GetFileAsync(imagePath);
            using (var randomAccessStream = await myfile.OpenReadAsync())
            using (Stream s = randomAccessStream.AsStreamForRead())
            {
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    Debug.WriteLine("Result of face: {0}", identifyResult.FaceId);

                    if (identifyResult.Candidates.Length == 0)
                    {
                        Debug.WriteLine("No one identified");
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                        Debug.WriteLine("Identified as {0}", person.Name);
                        Speak("Hello, " + person.Name);
                    }
                }
            }
        }

        private async void TakePicture()
        {
            //Take picture
            mediaCapture = new MediaCapture();
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            // Use the front camera if found one
            if (devices == null) return;

            DeviceInformation info = devices[0];

            foreach (var devInfo in devices)
            {
                if (devInfo.Name.ToLowerInvariant().Contains("front"))
                {
                    info = devInfo;
                    frontCam = true;
                    continue;
                }
            }

            await mediaCapture.InitializeAsync(
                new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = info.Id
                });

            captureElement.Source = mediaCapture;
            captureElement.FlowDirection = frontCam ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            await mediaCapture.StartPreviewAsync();

            DisplayInformation displayInfo = DisplayInformation.GetForCurrentView();
            displayInfo.OrientationChanged += DisplayInfo_OrientationChanged;

            DisplayInfo_OrientationChanged(displayInfo, null);
            await Task.Delay(TimeSpan.FromSeconds(2));
            OnTap();
        }

        private void DisplayInfo_OrientationChanged(DisplayInformation sender, object args)
        {
            if (mediaCapture != null)
            {
                mediaCapture.SetPreviewRotation(frontCam
                ? VideoRotationLookup(sender.CurrentOrientation, true)
                : VideoRotationLookup(sender.CurrentOrientation, false));
                var rotation = VideoRotationLookup(sender.CurrentOrientation, false);
                mediaCapture.SetRecordRotation(rotation);
            }
        }

        private VideoRotation VideoRotationLookup(DisplayOrientations displayOrientation, bool counterclockwise)
        {
            switch (displayOrientation)
            {
                case DisplayOrientations.Landscape:
                    return VideoRotation.None;

                case DisplayOrientations.Portrait:
                    return (counterclockwise) ? VideoRotation.Clockwise270Degrees : VideoRotation.Clockwise90Degrees;

                case DisplayOrientations.LandscapeFlipped:
                    return VideoRotation.Clockwise180Degrees;

                case DisplayOrientations.PortraitFlipped:
                    return (counterclockwise) ? VideoRotation.Clockwise90Degrees :
                    VideoRotation.Clockwise270Degrees;

                default:
                    return VideoRotation.None;
            }
        }

        private async void OnTap()
        {
            ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();

            mediaCapture.CapturePhotoToStreamAsync(imageProperties, fPhotoStream).AsTask().Wait();

            fPhotoStream.FlushAsync().AsTask().Wait();
            fPhotoStream.Seek(0);
            WriteableBitmap writeableBitmap = new WriteableBitmap(300, 300);
            writeableBitmap.SetSource(fPhotoStream);

            await mediaCapture.StopPreviewAsync();

            Guid photoID = System.Guid.NewGuid();
            string photolocation = "face.jpg";  //file name
            StorageFolder appFolder = await KnownFolders.PicturesLibrary.CreateFolderAsync("Capture", CreationCollisionOption.OpenIfExists);
            StorageFile myfile = await appFolder.CreateFileAsync(photolocation, CreationCollisionOption.ReplaceExisting);

            using (IRandomAccessStream stream = await myfile.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Get pixels of the WriteableBitmap object 
                Stream pixelStream = writeableBitmap.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                // Save the image file with jpg extension 
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint) writeableBitmap.PixelWidth,
                    (uint) writeableBitmap.PixelHeight, 96.0, 96.0, pixels);
                await encoder.FlushAsync();
            }
            IdentifyUser(myfile.Name);
            
        }
    }
}
