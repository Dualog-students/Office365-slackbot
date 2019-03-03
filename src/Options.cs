using System;

namespace DuaBot
{
    public class Options
    {
        private static Options _instance = null;
        public static Options Default
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Options();
                    return _instance;
                }

                return _instance;
            }
        }

        private Options()
        {
            UseCalendarSubject = true;


            // Run calendar every 30 minutes to queue tasks
            CalendarServiceInterval = TimeSpan.FromMinutes(30);

            // The slack service consumes all the tasks
            SlackServiceInterval = TimeSpan.FromMinutes(1);

            // Just to keep the database neat and clean
            SlackServiceDeleteInterval = TimeSpan.FromMinutes(60);

            MsGraphScopes = "user.read calendars.read";
            MsGraphRedirectUri = "http://localhost:5000/api/msgraph/authenticate";
        }

        public string SlackAppToken { get; set; }
        public string SlackAuthToken { get; set; }

        public string MsGraphScopes { get; set; }
        public string MsGraphClientId { get; set; }
        public string MsGraphRedirectUri { get; set; }
        public string MsGraphClientSecret { get; set; }

        /// <summary>
        /// Wheter to include the event subject in the slack status description
        /// </summary>
        public bool UseCalendarSubject { get; set; }
        public TimeSpan CalendarServiceInterval { get; set; }
        public TimeSpan SlackServiceInterval { get; set; }
        public TimeSpan SlackServiceDeleteInterval { get; set; }
    }
}
