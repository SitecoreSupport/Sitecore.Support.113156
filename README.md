# Sitecore.Support.113156

The Sitecore ExM module might fail to send an email due to the `NullReferenceException` in the `Sitecore.Modules.EmailCampaign.Core.Gateways.DefaultAnalyticsGateway.UpdateAutomationState` method.

## Main

This repository contains Sitecore Patch #113156, which fixes the `NullReferenceException` issue and adds extra debugging information to exception caused by the `WebClient.DownloadString(...)` method call.

## Deployment

To apply the patch, perform the following steps on the CM server:

1. Place the `Sitecore.Support.113156.dll` assembly into the `\bin` directory.
2. Place the `Sitecore.Support.113156.config` file into the `\App_Config\Include\zzz` directory.

## Content 

Sitecore Patch includes the following files:

1. `\bin\Sitecore.Support.113156.dll`
2. `\App_Config\Include\zzz\Sitecore.Support.113156.config`
