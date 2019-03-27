# Client Side Analytics for Sitecore

Sitecore doesn't provide a client side API library for working with it's analytics features. 

This project adds a REST endpoint and javaScript library for working with Siteocore's analytics features clientside.

# Rest End Point

When installed the main API consists of a set of methods under "/api/SF/1.0/analytics/". These methods include:

* GetTracker - Returns details about the current visitors and any goals that have been triggered by them for the current page.
* RegisterInteraction - Adds a new interaction that will be tracked in Sitecore like a page view.
* IdentifyContact - Identifies the contact as a known visitor given an identifier and other optional details.
* RegisterGoal - Registers a Sitecore Goal by name or Id
* RegisterOutcome - Registers a Sitecore Outcome by name or Id
* RegisterPageEvent - Registers a Sitecore Page Event by name or Id
* RegisterCustomEvent - Registers a custom Sitecore Page Event allowing you to track additional data
* TriggerCampaign - Triggers a Sitecore Marketing Campaign by name or Id
* EndSession - Ends the session and flushed data to xDB.

# Tracking Interactions

In order to properly track interactions, Sitecore requires an Item Id. While you can use any valid Sitecore Item Id, we have added a new folder to the marketing control panel called "Client Side Interactions", which will enable you to define actions or states that you would like to see as if they were page views. They will even show up in the Path Analyzer.

# Swagger Documentation

We've included swagger to make it easy to look and test the raw api directly. If using this project, you can use the swagger url below to explore it's methods.'
http://clientanalytics.dev.local/swagger/ui/index#/Analytics

# JavaScript Library

Instead of working the Rest API directly, we've also created a robust javaScript library that will allow you work with all of the rest API's.

To understand the API details and how to invoke each method, we've included an html test page that documents the API.

http://clientanalytics.dev.local/tests/clientanalyticstests.html

# Segment Analytics.js Plugin

We've included the script library for segment's analytics JS that allows you to publish client side events to multiple destinations easily. We've written a Analytics.js plugin for Sitecore that pushes data to Sitecore when raised and sends data from Sitecore to analytics.js. This would enable you to trigger single events that track in both Sitecore and Google Analytics as well as push server side goals from Sitecore to Google Analytics.

To use, reference the following scripts in the /scripts/Foundation/analytics folder:
* analytics.min.js
* sitecorePlugin.js
* loadAnalytics.js

For more on Segment and Analytics.js, see their documentation here: 
https://segment.com/docs/sources/website/analytics.js/
