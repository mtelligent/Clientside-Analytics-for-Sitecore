using Sitecore.Analytics;
using Sitecore.Analytics.Data;
using Sitecore.Data;
using Sitecore.Services.Infrastructure.Web.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Sitecore.Analytics.Tracking;
using Sitecore.Analytics.Model.Entities;
using System.Net.Http;
using Sitecore.Marketing.Definitions.Goals;
using Sitecore.Marketing.Definitions.Outcomes.Model;
using Sitecore.Marketing.Definitions.Campaigns;
using Sitecore.Marketing.Definitions.PageEvents;

namespace SF.Foundation.Analytics
{   

    public class AnalyticsController : ServicesApiController
    {
        [HttpGet]
        public HttpResponseMessage Index()
        {
            return Request.CreateResponse(System.Net.HttpStatusCode.Accepted);
        }

        [HttpPost]
        public TrackerDetails GetTracker(PageDetails data)
        {
            var Tracker = this.GetTracker(false);
            if (Tracker == null || !Tracker.IsActive)
            {
                throw new ArgumentException("Context is invalid");
            }

            var pageId = Guid.Empty;
            Guid.TryParse(data.pageId, out pageId);

            var pageInteraction = Tracker.Interaction.Pages.LastOrDefault(a => a.Item.Id == pageId);
            var page = pageInteraction != null ? Tracker.Interaction.GetPage(pageInteraction.VisitPageIndex) : Tracker.Interaction.PreviousPage;
            
            var details = new TrackerDetails();
            foreach (var pageEvent in page.PageEvents)
            {
                EventDetails eventDetails = new EventDetails();
                eventDetails.name = pageEvent.Name;
                eventDetails.text = pageEvent.Text;
                eventDetails.value = pageEvent.Value.ToString();
                eventDetails.isGoal = pageEvent.IsGoal ? "1" : "0";
                details.events.Add(eventDetails);
            }

            details.contactId = Tracker.Contact.ContactId.ToString();
            details.userName = Tracker.Contact.Identifiers.FirstOrDefault()?.Identifier;

            try
            {
                Contact contact = Tracker.Contact;
                IContactPersonalInfo personal = contact.GetFacet<IContactPersonalInfo>("Personal");
                details.name = string.Format("{0} {1} {2}", personal.FirstName, personal.MiddleName, personal.Surname);
                
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error("Could not get Name", ex, this);
            }

            try
            {
                Contact contact = Tracker.Contact;
                IContactEmailAddresses emails = contact.GetFacet<IContactEmailAddresses>("Personal");
                details.email = emails.Entries[emails.Preferred].SmtpAddress;
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error("Could not get Preferred Email Address", ex, this);
            }

            Tracker.CurrentPage.Cancel();

            return details;
        }

        [HttpPost]
        public HttpResponseMessage RegisterInteraction(InteractionDetails data)
        {
            var Tracker = this.GetTracker(false);
            if (Tracker == null || !Tracker.IsActive)
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
            }

            try
            {
                var id = data.interactionId;
                var item = Sitecore.Context.Database.GetItem(new ID(id));

                var url = item.Fields["Name"].Value;
                if (!url.StartsWith("/"))
                {
                    url = "/" + url;
                }

                Uri intUri = new Uri(System.Web.HttpContext.Current.Request.Url, url);

                Tracker.CurrentPage.Url.Path = intUri.AbsolutePath;
                Tracker.CurrentPage.Url.QueryString = intUri.Query;

                //Not showing up in path analyzer unless I set the ID
                Tracker.CurrentPage.SetItemProperties(item.ID.Guid, item.Language.CultureInfo.DisplayName, item.Version.Number);
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Error Tracking Interaction for {0}", data.interactionId), ex, this);
            }

            return Request.CreateResponse(System.Net.HttpStatusCode.Accepted);
        }

        [HttpPost]
        public HttpResponseMessage RegisterGoal(GoalDetails goal)
        {
            var Tracker = this.GetTracker(false);
            if (Tracker == null || !Tracker.IsActive)
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
            }
            //Do not track the API call
            Tracker.CurrentPage.Cancel();

            var goalId = Guid.Empty;
            IGoalDefinition goalDefinition = null;
            if (Guid.TryParse(goal.goalId, out goalId))
            {
                goalDefinition = Sitecore.Analytics.Tracker.MarketingDefinitions.Goals[goalId];
            }
            else
            {
                goalDefinition = Sitecore.Analytics.Tracker.MarketingDefinitions.Goals[goal.goal];
            }

            var pageId = Guid.Empty;
            Guid.TryParse(goal.pageId, out pageId);
            var pageInteraction = Tracker.Interaction.Pages.LastOrDefault(a => a.Item.Id == pageId);
            var page = pageInteraction != null ? Tracker.Interaction.GetPage(pageInteraction.VisitPageIndex) : Tracker.Interaction.PreviousPage;

            if (goalDefinition != null)
            {
                page.RegisterGoal(goalDefinition);
                return Request.CreateResponse(System.Net.HttpStatusCode.Accepted);
            }
            else
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Goal is not valid.");
            }
            
        }

        [HttpPost]
        public HttpResponseMessage RegisterOutcome(OutcomeDetails outcome)
        {
            var Tracker = this.GetTracker(false);
            if (Tracker == null || !Tracker.IsActive)
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
            }

            var outcomeId = Guid.Empty;
            IOutcomeDefinition outcomeDefinition = null;
            
            if (Guid.TryParse(outcome.outcomeId, out outcomeId))
            {
                outcomeDefinition = Sitecore.Analytics.Tracker.MarketingDefinitions.Outcomes[outcomeId];
            }
            else
            {
                outcomeDefinition = Sitecore.Analytics.Tracker.MarketingDefinitions.Outcomes[outcome.outcome];
            }

            var pageId = Guid.Empty;
            Guid.TryParse(outcome.pageId, out pageId);
            var pageInteraction = Tracker.Interaction.Pages.LastOrDefault(a => a.Item.Id == pageId);
            var page = pageInteraction != null ? Tracker.Interaction.GetPage(pageInteraction.VisitPageIndex) : Tracker.Interaction.PreviousPage;


            if (outcomeDefinition != null)
            {
                Tracker.CurrentPage.RegisterOutcome(outcomeDefinition, outcome.currency, outcome.value);
                return Request.CreateResponse(System.Net.HttpStatusCode.Accepted);
            }
            else
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Outcome is not valid.");
            }

        }

        [HttpPost]
        public HttpResponseMessage TriggerCampaign(CampaignDetails campaign)
        {
            var Tracker = this.GetTracker(false);
            if (Tracker == null || !Tracker.IsActive)
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
            }

            var campaignId = Guid.Empty;
            ICampaignActivityDefinition campaignDefinition = null;

            if (Guid.TryParse(campaign.campaign, out campaignId))
            {
                campaignDefinition = Sitecore.Analytics.Tracker.MarketingDefinitions.Campaigns[campaignId];
            }
            else
            {
                campaignDefinition = Sitecore.Analytics.Tracker.MarketingDefinitions.Campaigns[campaign.campaign];
            }

            var pageId = Guid.Empty;
            Guid.TryParse(campaign.pageId, out pageId);
            var pageInteraction = Tracker.Interaction.Pages.LastOrDefault(a => a.Item.Id == pageId);
            var page = pageInteraction != null ? Tracker.Interaction.GetPage(pageInteraction.VisitPageIndex) : Tracker.Interaction.PreviousPage;


            if (campaignDefinition != null)
            {
                Tracker.CurrentPage.TriggerCampaign(campaignDefinition);
                return Request.CreateResponse(System.Net.HttpStatusCode.Accepted);
            }
            else
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Campaign is not valid.");
            }

        }

        [HttpPost]
        public HttpResponseMessage RegisterPageEvent(PageEventDetails pageEvent)
        {
            var Tracker = this.GetTracker(false);
            if (Tracker == null || !Tracker.IsActive)
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
            }

            var pageEventId = Guid.Empty;
            IPageEventDefinition pageEventDefinition = null;

            if (Guid.TryParse(pageEvent.eventId, out pageEventId))
            {
                pageEventDefinition = Sitecore.Analytics.Tracker.MarketingDefinitions.PageEvents[pageEventId];
            }
            else
            {
                pageEventDefinition = Sitecore.Analytics.Tracker.MarketingDefinitions.PageEvents[pageEvent.eventName];
            }

            var pageId = Guid.Empty;
            Guid.TryParse(pageEvent.pageId, out pageId);
            var pageInteraction = Tracker.Interaction.Pages.LastOrDefault(a => a.Item.Id == pageId);
            var page = pageInteraction != null ? Tracker.Interaction.GetPage(pageInteraction.VisitPageIndex) : Tracker.Interaction.PreviousPage;


            if (pageEventDefinition != null)
            {
                Tracker.CurrentPage.RegisterPageEvent(pageEventDefinition);
                return Request.CreateResponse(System.Net.HttpStatusCode.Accepted);
            }
            else
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Page Event is not valid.");
            }

        }
        
        [HttpPost]
        public HttpResponseMessage RegisterCustomEvent(PageEventDetails data)
        {
            var Tracker = this.GetTracker(false);
            if (Tracker == null || !Tracker.IsActive)
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
            }

            var pageId = Guid.Empty;
            Guid.TryParse(data.pageId, out pageId);

            var pageInteraction = Tracker.Interaction.Pages.LastOrDefault(a => a.Item.Id == pageId);
            var page = pageInteraction != null ? Tracker.Interaction.GetPage(pageInteraction.VisitPageIndex) : Tracker.Interaction.PreviousPage;

            var pageEventData = new PageEventData(data.name);
            pageEventData.ItemId = page.Item.Id;
            pageEventData.Data = data.data;
            pageEventData.Text = data.text;
            pageEventData.DataKey = data.dataKey;
                       

            var peDefId = Guid.Empty;
            if (!string.IsNullOrEmpty(data.eventId) && Guid.TryParse(data.eventId, out peDefId))
            {
                pageEventData.PageEventDefinitionId = peDefId;
            }

            page.Register(pageEventData);

            //Do not track the API call
            Tracker.CurrentPage.Cancel();

            return Request.CreateResponse(System.Net.HttpStatusCode.Accepted);

        }

        [HttpGet]
        public string EndSession()
        {
            var Tracker = this.GetTracker(false);

            System.Web.HttpContext.Current.Session.Abandon();
            return "OK";
        }

       



    }
}
