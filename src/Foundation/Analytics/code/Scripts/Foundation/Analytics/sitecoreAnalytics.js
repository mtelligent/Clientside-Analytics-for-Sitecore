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
            var def = $.Deferred();
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
                    def.resolve(data);
                },
                failure: function (errMsg) {
                    console.log('Failure' + errMsg);
                    def.reject();
                }
            });

            return def.promise();
        },
    identifyContact = function (identifier, identifierSource, firstName, lastName, email) {
        console.log('picked up track, posting to service');

        var url = '/api/SF/1.0/analytics/IdentifyContact';

        var data = {
            identifier: identifier,
            identifierSource: identifierSource,
            firstName: firstName,
            lastName: lastName,
            email: email
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
    trackGoal = function (goal) {
            var url = '/api/SF/1.0/analytics/RegisterGoal';

            var data = {
                goal: goal,
                goalId: goal,
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
    trackOutcome = function (outcome, currency, value) {
            var url = '/api/SF/1.0/analytics/RegisterOutcome';

            var data = {
                outcome: outcome,
                outcomeId: outcome,
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
    trackPageEvent = function (event) {
            var url = '/api/SF/1.0/analytics/RegisterPageEvent';

            var data = {
                eventName: event,
                eventId: event,
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
    triggerCampaign = function (campaign) {
            var url = '/api/SF/1.0/analytics/TriggerCampaign';

            var data = {
                campaign: campaign,
                campaignId: campaign,
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
        identifyContact: identifyContact,
        trackInteraction: trackInteraction,
        trackGoal: trackGoal,
        trackPageEvent: trackPageEvent,
        trackOutcome: trackOutcome,
        trackCustomEvent: trackCustomEvent,
        triggerCampaign: triggerCampaign
    }
})();

