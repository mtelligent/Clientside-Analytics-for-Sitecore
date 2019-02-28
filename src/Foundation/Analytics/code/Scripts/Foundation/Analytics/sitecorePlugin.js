SF = window.SF || {};
SF.SitecoreData = null;

SF.AnalyticsPlugin = (function () {
    
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

                analytics.identify(data.contactId, {
                    name: data.name,
                    email: data.email,
                    userName: data.userName
                });

                $.each(data.events, function (index, event) {
                    
                    analytics.track(event.name, {
                        data: event.text,
                        text: event.text,
                        value: event.value,
                        origin: 'Sitecore'
                    });

                });

                SF.SitecoreData = data;
            },
            failure: function (errMsg) {
                console.log('Failure' + errMsg);
            }
        });
    },
    
    init = function () {

        if (!analytics){
            console.log('analytics not defined');
        }

        analytics.on('track', function (event, properties, options) {
            
            if (properties.origin != "Sitecore") {

                console.log('picked up track, posting to service');
                //todo, POST to SF end point

                var url = '/api/SF/1.0/analytics/RegisterEvent';

                var data = {
                    name: event,
                    data: properties.label,
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

            }

        });

    };

    return{
        init: init,
        getSitecoreEvents: getSitecoreEvents
    }
})();

analytics.ready(function () {
    SF.AnalyticsPlugin.init();
    SF.AnalyticsPlugin.getSitecoreEvents();
});