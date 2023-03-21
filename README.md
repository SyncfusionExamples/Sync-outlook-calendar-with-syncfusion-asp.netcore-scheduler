# Sync-outlook-calendar-with-syncfusion-asp.netcore-scheduler
The application explains how to get events from Office 365 Outlook and display it in ASP.NET Core Scheduler using Microsoft Graph API and Azure AD.

## Prerequisites

* Visual Studio 2019
* .NET Core 5.0

## How to run the project

* Checkout this project to a location in your disk.
* Open the solution file using the `Visual Studio 2019`.
* Restore the NuGet packages by rebuilding the solution.
* Generate the `ClientId` and `ClientSecret` by following the steps provided in the [Configure an Azure AD app to connect to Microsoft 365](https://learn.microsoft.com/en-us/training/modules/msgraph-dotnet-core-show-user-emails/2-exercise-configure-azure-ad-app) page and keep the `ClientId` and `ClientSecret` to perform the next steps.
* Open the `Package Manager Console` in the project and run the following commands with your generated `ClientId` and `ClientSecret` values.
```console
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientId" "YOUR_APP_ID"
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_APP_SECRET"
```
* Sign in to your Microsoft 365 account.
* Run the project the permission prompt will be opened. Click the Accept button.
* The home page of the project will be opened. Navigate to the `Calendar` page the Scheduler loaded with the Outlook appointments.