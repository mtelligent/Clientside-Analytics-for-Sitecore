# Client Side Analytics for Sitecore

This project adds a Web API for raising Sitecore Marketing events from client side javaScript. You can use the Web API directly or use the provided JavaScript library to call methods to interact with Sitecore's Marketing API including:
- Raise Goal
- Raise Page Events (with or without data)
- Trigger Campaigns
- Raise Outcomes
- Register Interactions (that show up in Path Analyzer)

You can also end the session to see the results immediately.

It also exposes a method to return all the server side invoked events with basic data that can then be sent to a third party.

This was the basis for building a Segment Analytics.js plugin to send events to third parties.

