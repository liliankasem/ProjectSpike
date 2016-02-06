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

namespace ProjectSpike
{
    public partial class MainPage
    {
        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient("0c5c804cfbe345de8a120fe839ea1d9d");
        string personGroupId = "Spike";

        private async void SetupPersonGroup()
        {

            const string lilianImageDir = @"Assets\PersonGroup\Lilian\";
            const string paulImageDir = @"Assets\PersonGroup\Paul\";
            const string janiImageDir = @"Assets\PersonGroup\Jani\";
            const string edImageDir = @"Assets\PersonGroup\Ed\";
            const string johnImageDir = @"Assets\PersonGroup\John\";

            await faceServiceClient.CreatePersonGroupAsync(personGroupId, "Spike");

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
            await faceServiceClient.TrainPersonGroupAsync(personGroupId);

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
            //string testImageFile = @"D:\Pictures\test_img1.jpg";
            string testImageFile = imagePath;

            using (Stream s = File.OpenRead(testImageFile))
            {
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceServiceClient.IdentityAsync(personGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    //Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        //Console.WriteLine("No one identified");
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                        //Console.WriteLine("Identified as {0}", person.Name);
                        Speak("Hello, " + person.Name);
                    }
                }
            }
        }

        private async void TakePicture()
        {
            //Take picture
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            captureUI.PhotoSettings.CroppedSizeInPixels = new Size(300, 300);

            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (photo == null)
            {
                // User cancelled photo capture
                return;
            }

            IRandomAccessStream stream = await photo.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap,
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(softwareBitmapBGR8);

            //Call IdentifyUser
            IdentifyUser(bitmapSource.ToString());
        }

    }
}
