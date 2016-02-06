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

using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace ProjectSpike
{
    public partial class MainPage
    {
        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient("0c5c804cfbe345de8a120fe839ea1d9d");

        private void SetupPersonGroup()
        {
            // Create an empty person group
            string personGroupId = "Spike";
            await faceServiceClient.CreatePersonGroupAsync(personGroupId, "Team Spike");

            // Define Lilian
            CreatePersonResult lilian = await faceServiceClient.CreatePersonAsync(
                // Id of the person group that the person belonged to
                personGroupId,
                // Name of the person
                "Lilian"
            );

            CreatePersonResult paul = await faceServiceClient.CreatePersonAsync(
                personGroupId,
                "Paul"
            );


            const string lilianImageDir = @"Assets\PersonGroup\Lilian\";
            const string paulImageDir = @"Assets\PersonGroup\Paul\";

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

            await faceServiceClient.TrainPersonGroupAsync(personGroupId);

            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

                if (trainingStatus.Status != "running")
                {
                    break;
                }

                await Task.Delay(1000);
            }
        }

        private void IdentifyUser()
        {
            string testImageFile = @"D:\Pictures\test_img1.jpg";

            using (Stream s = File.OpenRead(testImageFile))
            {
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceServiceClient.IdentityAsync(personGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        Console.WriteLine("No one identified");
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                        Console.WriteLine("Identified as {0}", person.Name);
                    }
                }
            }
        }

    }
}
