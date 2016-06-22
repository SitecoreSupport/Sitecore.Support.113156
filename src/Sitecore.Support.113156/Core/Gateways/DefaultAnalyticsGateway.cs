namespace Sitecore.Support.EmailCampaign.Core.Gateways
{
  using System;
  using System.Linq;
  using System.Net;
  using System.Web;
  using Newtonsoft.Json;
  using Sitecore.Analytics.Automation.MarketingAutomation;
  using Sitecore.Analytics.DataAccess;
  using Sitecore.Analytics.Model;
  using Sitecore.Data;
  using Sitecore.Diagnostics;
  using Sitecore.EmailCampaign.Analytics.Model;
  using Sitecore.Modules.EmailCampaign;
  using Sitecore.Modules.EmailCampaign.Core.Analytics;
  using Sitecore.Modules.EmailCampaign.Core.Gateways;
  using Sitecore.Modules.EmailCampaign.Exceptions;

  public class DefaultAnalyticsGateway : Sitecore.Modules.EmailCampaign.Core.Gateways.DefaultAnalyticsGateway
  {
    [UsedImplicitly]
    public DefaultAnalyticsGateway()
    {
    }

    public override bool UpdateAutomationState(Guid contactId, Guid planId, Guid automationStateId, EcmCustomValues customValues = null, params string[] validStates)
    {
      string webClusterName;
      bool flag;
      var leaseOwner = new LeaseOwner("UpdateAutomationState-" + Guid.NewGuid(), LeaseOwnerType.OutOfRequestWorker);
      var leaseDuration = TimeSpan.FromSeconds(15.0);
      var timeout = leaseDuration;
      Analytics.Tracking.Contact contact;

      switch (this.TryGetContactForUpdate(new ID(contactId), leaseOwner, leaseDuration, timeout, out contact, out webClusterName))
      {
        case ContactLockingStatus.InCurrentTracker:
        {
          return this.UpdateAutomationState(contact, planId, automationStateId, customValues, validStates);
        }

        case ContactLockingStatus.LockAcquired:
          try
          {
            return this.UpdateAutomationState(contact, planId, automationStateId, customValues, validStates);
          }
          finally
          {
            this.ContactRepository.SaveContact(contact, new ContactSaveOptions(true, leaseOwner, leaseDuration));
          }

        case ContactLockingStatus.LockedByWebCluster:
        {
          break;
        }

        case ContactLockingStatus.NotFound:
        {
          throw new EmailCampaignException("No contact was found by the id '{0}'.", contactId);
        }

        default:
        {
          return false;
        }
      }

      var parameters = new AutomationStatesHandlerParameters(ContactAutomationAction.Update, ContactRepository.LoadContactReadOnly(contactId).Identifiers.Identifier, planId, automationStateId, customValues, validStates);
      var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
      var encodedParameters = HttpUtility.UrlEncode(JsonConvert.SerializeObject(parameters, Formatting.None, settings));
      var client = new WebClient();
      var uriString = Util.EnsureUrlContainsScheme(webClusterName ?? GlobalSettings.RendererUrl, GlobalSettings.Instance.AutomationStatesHandlerRequestScheme);
      var uri = new Uri(new Uri(uriString), $"sitecore/AutomationStates.ashx?p={encodedParameters}");

      // catch download exception to log requested Url
      string responseText;
      try
      {
        responseText = client.DownloadString(uri);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Failed to get response string from url: {uri}", ex);
      }

      return bool.TryParse(responseText, out flag) && flag;
    }


    private bool UpdateAutomationState([NotNull] Analytics.Tracking.Contact contact, Guid planId, Guid stateId, [CanBeNull] EcmCustomValues ecmCustomValues, params string[] validStates)
    {
      // prevent nullref
      Assert.ArgumentNotNull(contact, nameof(contact));

      var plan = new ID(planId);
      var state = new ID(stateId);

      // prevent nullref
      var manager = contact.AutomationStates();
      Assert.IsNotNull(manager, "manager is null");

      var currentStateInPlan = manager.GetCurrentStateInPlan(plan);
      if (currentStateInPlan == null)
      {
        return false;
      }

      // prevent nullref
      var stateItem = currentStateInPlan.StateItem;
      Assert.IsNotNull(stateItem, "stateItem is null");

      if (!ValidStateToMove(stateItem.Name, validStates))
      {
        return false;
      }

      manager.MoveToEngagementState(plan, state);
      if (ecmCustomValues != null)
      {
        currentStateInPlan.UpdateCustomData("sc.ecm", ecmCustomValues);
      }

      return true;
    }


    private bool ValidStateToMove(string currentStateName, [CanBeNull] string[] validStates)
    {
      return validStates == null || !validStates.Any() || validStates.Any(validState => string.Equals(validState, currentStateName, StringComparison.OrdinalIgnoreCase));
    }
  }
}