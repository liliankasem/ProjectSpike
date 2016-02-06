using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FreeMeetingRoom.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/roomname
        public string Get(string id)
        {
            return FreeRoom(id);
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }

        private string FreeRoom(string roomName)
        {
            // ToDo: error stategy to be implemented
            // log into Officee 365

            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2010);
            //service.Credentials = new WebCredentials("alexd@PretzelPalace.onmicrosoft.com", "Passw0rd!");
            service.Credentials = new WebCredentials("alexd@PretzelPalace.onmicrosoft.com", "Passw0rd!");
            //service.Credentials = new WebCredentials("paulfo@microsoft.com", "");
            service.UseDefaultCredentials = false;
            service.AutodiscoverUrl("alexd@PretzelPalace.onmicrosoft.com", RedirectionUrlValidationCallback);
            //EmailMessage email = new EmailMessage(service);
            //email.ToRecipients.Add("paulfo@microsoft.com");
            //email.Subject = "HelloWorld";
            //email.Body = new MessageBody("This is the first email I've sent by using the EWS Managed API.");
            //email.Send();

            // GetRoomLists
            EmailAddressCollection roomGroup = service.GetRoomLists();

            // GetRooms(roomGroup)
            Collection<EmailAddress> rooms = service.GetRooms(roomGroup[0]);

            string response = "No meeting room found";
            //if the room.Address matchaes the one you are looking for then
            foreach (EmailAddress room in rooms)
            {
                if (room.Name == roomName)
                {
                    Mailbox mailBox = new Mailbox(room.Address, "Mailbox");

                    //Mailbox mailBox = new Mailbox("alexd@PretzelPalace.onmicrosoft.com", "Mailbox");
                    // Create a FolderId instance of type WellKnownFolderName.Calendar and a new mailbox with the room's address and routing type
                    FolderId folderID = new FolderId(WellKnownFolderName.Calendar, mailBox);
                    // Create a CalendarView with from and to dates
                    DateTime start = DateTime.Now.ToUniversalTime().AddHours(-8);

                    DateTime end = DateTime.Now.ToUniversalTime().AddHours(5);
                    //end.AddHours(3);
                    CalendarView calendarView = new CalendarView(start, end);

                    // Call findAppointments on FolderId populating CalendarView
                    FindItemsResults<Appointment> appointments = service.FindAppointments(folderID, calendarView);

                    // Iterate the appointments

                    if (appointments.Items.Count == 0)
                        response = "The room is free";
                    else
                    {
                        DateTime appt = appointments.Items[0].Start;
                        TimeSpan test = DateTime.Now.Subtract(appt);
                        int t = (int)Math.Round(Convert.ToDecimal(test.TotalMinutes.ToString()));

                        if (test.TotalMinutes < 0)
                            response = "a meeting is booked at this time";
                        else
                            response = "the room is free for " + t.ToString() + "minutes";
                    }
                    Console.WriteLine(response);
                }
            }
            return response;
        }

        private bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            bool result = false;

            Uri redirectionUri = new Uri(redirectionUrl);

            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                result = true;
            }
            return result;
        }
    }
}
