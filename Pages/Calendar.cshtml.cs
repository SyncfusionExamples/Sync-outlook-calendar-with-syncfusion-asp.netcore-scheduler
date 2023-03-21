using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreRazor_MSGraph.Graph;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using DotNetCoreRazor_MSGraph.Models;

namespace DotNetCoreRazor_MSGraph.Pages
{
    [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
    public class CalendarModel : PageModel
    {
        private readonly ILogger<CalendarModel> _logger;
        private readonly GraphCalendarClient _graphCalendarClient;
        private readonly GraphProfileClient _graphProfileClient;
        private MailboxSettings MailboxSettings { get; set; }
        public List<AppointmentData> Events  { get; private set; }

        public CalendarModel(ILogger<CalendarModel> logger, GraphCalendarClient graphCalendarClient, GraphProfileClient graphProfileClient)
        {
            _logger = logger;
            _graphCalendarClient = graphCalendarClient;
            _graphProfileClient = graphProfileClient;
        }

        public async Task<JsonResult> OnPostLoadData([FromBody] Params param) {
            Events = new List<AppointmentData>();
            MailboxSettings = await _graphCalendarClient.GetUserMailboxSettings();
            var userTimeZone = (String.IsNullOrEmpty(MailboxSettings.WorkingHours.TimeZone.Name)) ? "Pacific Standard Time" : MailboxSettings.WorkingHours.TimeZone.Name;
            // Loading appointments for the currently selected week date range.
            IEnumerable<Event> OutlookEvents = await _graphCalendarClient.GetEvents(userTimeZone, DateTime.Parse(param.StartDate), DateTime.Parse(param.EndDate));
            // Converting the Outlook appointments Start and End fields to EJ2 Schedule appointment StartTime and EndTime fields
            foreach (Event calendarEvent in OutlookEvents) {
                Events.Add(new AppointmentData {
                    Id = calendarEvent.Id,
                    Subject = calendarEvent.Subject,
                    StartTime = DateTime.Parse(FormatDateTimeTimeZone(calendarEvent.Start)),
                    EndTime = DateTime.Parse(FormatDateTimeTimeZone(calendarEvent.End)),
                    IsAllDay = false
                });
            }
            return new JsonResult(Events);
        }

        public void OnGet()
        {
        }

        public string FormatDateTimeTimeZone(DateTimeTimeZone value)
        {
            // Parse the date/time string from Graph into a DateTime
            var graphDatetime = value.DateTime;
            if (DateTime.TryParse(graphDatetime, out DateTime dateTime)) 
            {
                var dateTimeFormat = $"{MailboxSettings.DateFormat} {MailboxSettings.TimeFormat}".Trim();
                if (!String.IsNullOrEmpty(dateTimeFormat)) {
                    return dateTime.ToString(dateTimeFormat);
                }
                else 
                {
                    return $"{dateTime.ToShortDateString()} {dateTime.ToShortTimeString()}";
                }
            }
            else
            {
                return graphDatetime;
            }
        }
    }
    public class Params {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}
