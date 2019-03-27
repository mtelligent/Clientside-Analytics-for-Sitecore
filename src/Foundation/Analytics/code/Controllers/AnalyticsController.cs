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
using Sitecore.XConnect;
using Sitecore.XConnect.Collection.Model;
using Sitecore.XConnect.Client.Configuration;
using Sitecore.XConnect.Client;

namespace SF.Foundation.Analytics
{   

    public class AnalyticsController : ServicesApiController
    {
        [HttpGet]
        public HttpResponseMessage Index()
        {
            return Request.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        [HttpPost]
        public TrackerDetails GetTracker(PageDetails data)
        {
            try
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
                    var contact = Tracker.Contact;
                    IContactPersonalInfo personal = contact.GetFacet<IContactPersonalInfo>("Personal");
                    details.name = string.Format("{0} {1} {2}", personal.FirstName, personal.MiddleName, personal.Surname);

                }
                catch (Exception ex)
                {
                    Sitecore.Diagnostics.Log.Error("Could not get Name", ex, this);
                }

                try
                {
                    var contact = Tracker.Contact;
                    IContactEmailAddresses emails = contact.GetFacet<IContactEmailAddresses>("Personal");
                    details.email = emails.Entries[emails.Preferred].SmtpAddress;
                }
                catch (Exception ex)
                {
                    Sitecore.Diagnostics.Log.Error("Could not get Preferred Email Address", ex, this);
                }
              

                return details;
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Error Getting Tracking details for {0}", data.pageId), ex, this);
                throw;
            }
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

                using (new Sitecore.SecurityModel.SecurityDisabler())
                {
                    var item = Sitecore.Context.Database.GetItem(new ID(id));

                    if (item == null)
                    {
                        return Request.CreateErrorResponse(System.Net.HttpStatusCode.NotFound, "Interaction Id not found");
                    }

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
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Error Tracking Interaction for {0}", data.interactionId), ex, this);
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, "Failed to Register Interaction");
            }

            return Request.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        [HttpPost]
        public HttpResponseMessage IdentifyContact(ContactDetails data)
        {
            var Tracker = this.GetTracker(false);
            if (Tracker == null || !Tracker.IsActive)
            {
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
            }

            try
            {
                using (var client = CreateClient())
                {

                    Tracker.Session.IdentifyAs(data.identifierSource, data.identifier);


                    var trackerIdentifier = new IdentifiedContactReference(data.identifierSource, data.identifier);
                    var expandOptions = new ContactExpandOptions(
                        CollectionModel.FacetKeys.PersonalInformation,
                        CollectionModel.FacetKeys.EmailAddressList);

                    var contact = client.Get(trackerIdentifier, expandOptions);

                    if (!string.IsNullOrEmpty(data.firstName) || !string.IsNullOrEmpty(data.lastName))
                    {
                        SetPersonalInformation(data.firstName, data.lastName, contact, client);
                    }
                    if (!string.IsNullOrEmpty(data.email))
                    {
                        SetEmail(data.email, contact, client);
                    }

                    client.Submit();

                }
                
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Error Identifying Contact for {0}", data.identifier), ex, this);
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, "Failed to Identify Contact");
            }

            return Request.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        [HttpPost]
        public HttpResponseMessage RegisterGoal(GoalDetails goal)
        {
            try
            {
                var Tracker = this.GetTracker(false);
                if (Tracker == null || !Tracker.IsActive)
                {
                    return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
                }

                using (new Sitecore.SecurityModel.SecurityDisabler())
                {
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
                        return Request.CreateResponse(System.Net.HttpStatusCode.OK);
                    }
                    else
                    {
                        return Request.CreateErrorResponse(System.Net.HttpStatusCode.NotFound, "Goal is not valid.");
                    }
                }
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Error Registering goal for {0}", goal.goal), ex, this);
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, "Failed to register goal");
            }
        }

        [HttpPost]
        public HttpResponseMessage RegisterOutcome(OutcomeDetails outcome)
        {
            try
            {
                var Tracker = this.GetTracker(false);
                if (Tracker == null || !Tracker.IsActive)
                {
                    return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
                }

                using (new Sitecore.SecurityModel.SecurityDisabler())
                {

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
                        return Request.CreateResponse(System.Net.HttpStatusCode.OK);
                    }
                    else
                    {
                        return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Outcome is not valid.");
                    }
                }
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Error Registering Outcome for {0}", outcome.outcome), ex, this);
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, "Failed to register outcome");
            }

        }

        [HttpPost]
        public HttpResponseMessage TriggerCampaign(CampaignDetails campaign)
        {
            try
            {
                var Tracker = this.GetTracker(false);
                if (Tracker == null || !Tracker.IsActive)
                {
                    return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
                }

                using (new Sitecore.SecurityModel.SecurityDisabler())
                {
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
                        return Request.CreateResponse(System.Net.HttpStatusCode.OK);
                    }
                    else
                    {
                        return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Campaign is not valid.");
                    }
                }
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Error Triggering Campaign for {0}", campaign.campaign), ex, this);
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, "Failed to trigger campaign");
            }
        }

        [HttpPost]
        public HttpResponseMessage RegisterPageEvent(PageEventDetails pageEvent)
        {
            try
            {
                var Tracker = this.GetTracker(false);
                if (Tracker == null || !Tracker.IsActive)
                {
                    return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
                }
                using (new Sitecore.SecurityModel.SecurityDisabler())
                {
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
                        return Request.CreateResponse(System.Net.HttpStatusCode.OK);
                    }
                    else
                    {
                        return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Page Event is not valid.");
                    }
                }
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Error Registering Page Event for {0}", pageEvent.name), ex, this);
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, "Failed to register page event");
            }

        }
        
        [HttpPost]
        public HttpResponseMessage RegisterCustomEvent(PageEventDetails data)
        {
            try
            {
                var Tracker = this.GetTracker(false);
                if (Tracker == null || !Tracker.IsActive)
                {
                    return Request.CreateErrorResponse(System.Net.HttpStatusCode.PreconditionFailed, "Tracker is not active.");
                }
                using (new Sitecore.SecurityModel.SecurityDisabler())
                {
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

                    return Request.CreateResponse(System.Net.HttpStatusCode.OK);
                }
            }
            catch(Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(string.Format("Error Registering Custom Event for {0}", data.name), ex, this);
                return Request.CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError, "Failed to register custom event");
            }
        }

        [HttpGet]
        public HttpResponseMessage EndSession()
        {
            var Tracker = this.GetTracker(false);

            System.Web.HttpContext.Current.Session.Abandon();
            return Request.CreateResponse(System.Net.HttpStatusCode.OK);
        }


        ///<summary>
        ///     Sets the  facet of the specified .
        /// </summary>

        /// The first name.
        /// The last name.
        /// The contact.
        /// The client.
        private static void SetPersonalInformation(string firstName, string lastName, Sitecore.XConnect.Contact contact,
            IXdbContext client)
        {
            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
                return;
            var personalInfoFacet = contact.Personal() ?? new PersonalInformation();
            if (personalInfoFacet.FirstName == firstName && personalInfoFacet.LastName == lastName)
                return;
            personalInfoFacet.FirstName = firstName;
            personalInfoFacet.LastName = lastName;
            client.SetPersonal(contact, personalInfoFacet);
        }

        ///<summary>
        ///     Sets the  facet of the specified .
        /// </summary>

        /// The email address.
        /// The contact.
        /// The client.
        private static void SetEmail(string email, Sitecore.XConnect.Contact contact, IXdbContext client)
        {
            if (string.IsNullOrEmpty(email))
                return;
            var emailFacet = contact.Emails();
            if (emailFacet == null)
            {
                emailFacet = new EmailAddressList(new EmailAddress(email, false), "Preferred");
            }
            else
            {
                if (emailFacet.PreferredEmail?.SmtpAddress == email)
                    return;
                emailFacet.PreferredEmail = new EmailAddress(email, false);
            }
            client.SetEmails(contact, emailFacet);
        }

        protected virtual IXdbContext CreateClient()
        {
            return SitecoreXConnectClientConfiguration.GetClient();
        }
    }
}
