SF = window.SF || {};
SF.PageID = SF.PageID || "";
SF.SitecoreData = null;

SF.Analytics = (function () {
    
    endSession = function () {
        var url = '/api/SF/1.0/analytics/EndSession';

        $.ajax({
            url: url,
            success: function (data) {
                console.log('Success' + data);
            },
            failure: function (errMsg) {
                console.log('Failure' + errMsg);
            }
        });
    },
    getSitecoreEvents = function () {
       
        var url = '/api/SF/1.0/analytics/GetTracker';

        var data = {
            pageId: SF.PageID
        };

        $.ajax({
            type: "POST",
            url: url,
            data: data,
            success: function (data) {

                console.log('Retreived Data from Sitecore');
                
                SF.SitecoreData = data;
            },
            failure: function (errMsg) {
                console.log('Failure' + errMsg);
            }
        });
    },
    trackInteraction = function (id) {

        console.log('picked up track, posting to service');

        var url = '/api/SF/1.0/analytics/RegisterInteraction';

        var data = {
            interactionId: id
        };

        $.ajax({
            type: "POST",
            url: url,
            data: data,
            success: function (data) {
                console.log('Success ' + data);
            },
            failure: function (errMsg) {
                console.log('Failure ' + errMsg);
            }
        });
    },
    trackCustomEvent = function (eventName, eventLabel)
    {
        var url = '/api/SF/1.0/analytics/RegisterCustomEvent';

        var data = {
            name: eventName,
            data: eventLabel,
            pageId: SF.PageID
        };

        $.ajax({
            type: "POST",
            url: url,
            data: data,
            success: function (data) {
                console.log('Success' + data);
            },
            failure: function (errMsg) {
                console.log('Failure' + errMsg);
            }
        });
        },
    trackGoal = function (goalName, goalId) {
            var url = '/api/SF/1.0/analytics/RegisterGoal';

            var data = {
                goal: goalName,
                goalId: goalId,
                pageId: SF.PageID
            };

            $.ajax({
                type: "POST",
                url: url,
                data: data,
                success: function (data) {
                    console.log('Success' + data);
                },
                failure: function (errMsg) {
                    console.log('Failure' + errMsg);
                }
            });
        },
        trackOutcome = function (outcomeName, outcomeId, currency, value) {
            var url = '/api/SF/1.0/analytics/RegisterOutcome';

            var data = {
                outcome: outcomeName,
                outcomeId: outcomeId,
                currency: currency,
                value: value,
                pageId: SF.PageID
            };

            $.ajax({
                type: "POST",
                url: url,
                data: data,
                success: function (data) {
                    console.log('Success' + data);
                },
                failure: function (errMsg) {
                    console.log('Failure' + errMsg);
                }
            });
        },
        trackPageEvent = function (eventName, eventId) {
            var url = '/api/SF/1.0/analytics/RegisterPageEvent';

            var data = {
                eventName: eventName,
                eventId: eventId,
                pageId: SF.PageID
            };

            $.ajax({
                type: "POST",
                url: url,
                data: data,
                success: function (data) {
                    console.log('Success' + data);
                },
                failure: function (errMsg) {
                    console.log('Failure' + errMsg);
                }
            });
        },
        triggerCampaign = function (campaignName, campaignId) {
            var url = '/api/SF/1.0/analytics/TriggerCampaign';

            var data = {
                campaign: campaignName,
                campaignId: campaignId,
                pageId: SF.PageID
            };

            $.ajax({
                type: "POST",
                url: url,
                data: data,
                success: function (data) {
                    console.log('Success' + data);
                },
                failure: function (errMsg) {
                    console.log('Failure' + errMsg);
                }
            });
        },
    init = function () {

        

        

    };

    return{
        init: init,
        endSession: endSession,
        getSitecoreEvents: getSitecoreEvents,
        trackInteraction: trackInteraction,
        trackGoal: trackGoal,
        trackPageEvent: trackPageEvent,
        trackOutcome: trackOutcome,
        trackCustomEvent: trackCustomEvent,
        triggerCampaign: triggerCampaign
    }
})();

