using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DuaBot.Data
{
    /// <summary>
    /// Slack update task, which is queued up in
    /// the db and is read on a different thread.
    /// </summary>
    public class SlackUpdateTask
    {
        [Key]
        public int Id { get; set; }
        public string Subject { get; set; }
        public string TimeZone { get; set; }
        public string SlackUserId { get; set; }
        public DateTimeOffset End { get; set; }
        public DateTimeOffset Start { get; set; }

        [NotMapped]
        public bool InMeeting
            => (Start - DateTime.UtcNow).Minutes <= 1 &&
               (Start - DateTime.UtcNow).Minutes >= 0;

        [NotMapped]
        public bool ShouldBeDeleted => End < DateTime.UtcNow;
    }
}
