[

General Information About the Service



](/azurgames/EN-AppsFlyer-EP-2198b3b3dc3681c58234d1402abd93af?pvs=25#2198b3b3dc36815781ecded088c5575f)

[

Step 1: Installing the AppsFlyer SDK



](/azurgames/EN-AppsFlyer-EP-2198b3b3dc3681c58234d1402abd93af?pvs=25#2198b3b3dc368101b7fdf34f1082ede5)

[

Step 2: Initializing AppsFlyer



](/azurgames/EN-AppsFlyer-EP-2198b3b3dc3681c58234d1402abd93af?pvs=25#2198b3b3dc3681068bfed0b49ac5f4a2)

[

Step 2.1: Setting up sending SCAN postback copies (iOS 15+)



](/azurgames/EN-AppsFlyer-EP-2198b3b3dc3681c58234d1402abd93af?pvs=25#2798b3b3dc3680ffa94ac95db6d15cc4)

[

Step 3: Integration of Purchase Events (af\_purchase)



](/azurgames/EN-AppsFlyer-EP-2198b3b3dc3681c58234d1402abd93af?pvs=25#2198b3b3dc368161b808eb015c5d6191)

[

Step 4: Subscriptions – Purchase Connector



](/azurgames/EN-AppsFlyer-EP-2198b3b3dc3681c58234d1402abd93af?pvs=25#2198b3b3dc36812d9275d7d70152c25a)

[

Step 5: Integration of Ad Revenue Events (af\_ad\_revenue)



](/azurgames/EN-AppsFlyer-EP-2198b3b3dc3681c58234d1402abd93af?pvs=25#2198b3b3dc3681519ea3d76d7ba03624)

[

Step 6. Sandbox and Debug Build



](/azurgames/EN-AppsFlyer-EP-2198b3b3dc3681c58234d1402abd93af?pvs=25#2198b3b3dc368101af0cd033b801b79b)

[

Useful Links



](/azurgames/EN-AppsFlyer-EP-2198b3b3dc3681c58234d1402abd93af?pvs=25#2198b3b3dc36810aab1ece42a1229a26)

[

FAQ & TroubleShooting



](/azurgames/EN-AppsFlyer-EP-2198b3b3dc3681c58234d1402abd93af?pvs=25#2198b3b3dc368172991ef5547bc535a5)

## General Information About the Service

AppsFlyer is an analytics service. With its help, projects can track various metrics, but we most often use it for:

Attribution of installs — AppsFlyer determines from which source the user came;

Tracking user behavior — in-app purchases made by users, frequency of users returning to the game;

Evaluating advertising ROI, forecasting revenue from new players using events;

Additionally, we can transfer data from AppsFlyer to our BI system, which allows us to conduct high-quality and consolidated analytics.

#### Step 1: Installing the AppsFlyer SDK

This is sufficient for basic integration and initial metrics

The project in the AppsFlyer dashboard is created by us.

Your Integration Manager will send the AppsFlyer Dev Key in the Slack channel.

Integration into the project is carried out according to the official AppsFlyer documentation:

General documentation for Unity integration: [![](https://files.readme.io/07bafb0-devhub.ico)AppsFlyer developer hubInstallation](https://dev.appsflyer.com/hc/docs/installation)​

AppsFlyer Help Center: [![](https://support.appsflyer.com/hc/theming_assets/01J8ETNGEQ2APTA2SGE3FQVWN3)Integrate the AppsFlyer SDK – Help Center](https://support.appsflyer.com/hc/en-us/sections/6551164458257-Integrate-the-AppsFlyer-SDK)​

AppsFlyer SDK Unity: ​

Add the downloaded SDK to your Unity project via Assets → Import Package → Custom Package.

Getting the Dev Key and App ID

If you have not used AppsFlyer in the project before, you need to integrate it using our developer key

r9vNC83N8nYpCzYGigyjUh

(a single key for all apps)

The App ID is for iOS only. You can find it on the app page in App Store Connect:

Open your app in App Store Connect

Go to App Information

In the Apple ID field, you will find the ID you need

📌

If the project has already gone through first tests, then it is already created in the AppsFlyer Dashboard.

#### Step 2: Initializing AppsFlyer

Once the SDK is installed, you must manually initialize AppsFlyer. More details here: [![](https://github.com/fluidicon.png)GitHubappsflyer-unity-plugin/docs/BasicIntegration.md at master · …](https://github.com/AppsFlyerSDK/appsflyer-unity-plugin/blob/master/docs/BasicIntegration.md#manual-integration)​

Initialization example:

If you are using the ApplicationIdentity package, you can get the UserId through this package. See details in the section below [![](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F05463045-ba39-4f1f-8ac4-4ceb470e1182%2F%25D1%2581%25D0%25BF%25D0%25B8%25D1%2581%25D0%25BE%25D0%25BA.png?table=custom_emoji&id=17c8b3b3-dc36-802e-b29d-007a5b14655c&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=200&userId=&cache=v2&imgBuildSrc=renderMentionIcon)\[EN\] SDK Init-Flow & Tech funnel \[EP\] - 1. Application Identity (appset\_id)](/1-Application-Identity-appset_id-2d58b3b3dc368001bb8efcc5874283f8?pvs=24#2d58b3b3dc3680dcb231f71f982ac0dc)

public async UniTask Initialize() { Debug.Log("\[Appsflyer\] Init start"); AppsFlyerSDK.AppsFlyer.initSDK(\_appsFlyerSettings.devKey, \_appsFlyerSettings.appID, \_appsFlyerMarker); var result \= await ApplicationIdentity.RequestAsync()(); AppsFlyerSDK.AppsFlyer.setCustomerUserId(result); AppsFlyerSDK.AppsFlyer.setIsDebug(Debug.isDebugBuild); #if UNITY\_IOS && !UNITY\_EDITOR Version ver \= Version.Parse(UnityEngine.iOS.Device.systemVersion); if (ver.Major \> 14 || (ver.Major \== 14 && ver.Minor \>= 5)) { AppsFlyerSDK.AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(30); } #endif AppsFlyerSDK.AppsFlyer.startSDK(); Debug.Log("\[Appsflyer\] Init complete"); await UniTask.NextFrame(); }

​

Initialize AppsFlyer after displaying the module: [![](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F2427b2a7-e196-4e7b-8bb2-b6ec2dfe3fd9%2Fanalytics.png?table=custom_emoji&id=19e8b3b3-dc36-8046-88b8-007a4ad7d363&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=200&userId=&cache=v2&imgBuildSrc=renderMentionIcon)User Consent Manager (PP/ToU+CMP+ATT)](/User-Consent-Manager-PP-ToU-CMP-ATT-1a68b3b3dc36801da477db28cc3b6d93?pvs=24)

Always use the

waitForATTUserAuthorizationWithTimeoutInterval

method for iOS. More details can be found in the technical funnel documentation [![](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F05463045-ba39-4f1f-8ac4-4ceb470e1182%2F%25D1%2581%25D0%25BF%25D0%25B8%25D1%2581%25D0%25BE%25D0%25BA.png?table=custom_emoji&id=17c8b3b3-dc36-802e-b29d-007a5b14655c&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=200&userId=&cache=v2&imgBuildSrc=renderMentionIcon)\[EN\] SDK Init-Flow & Tech funnel \[EP\]](/EN-SDK-Init-Flow-Tech-funnel-EP-2d58b3b3dc368001bb8efcc5874283f8?pvs=24)

#if UNITY\_IOS && !UNITY\_EDITOR Version ver \= Version.Parse(UnityEngine.iOS.Device.systemVersion); if (ver.Major \> 14 || (ver.Major \== 14 && ver.Minor \>= 5)) { AppsFlyerSDK.AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(30); }#endif

​

If subscriptions are present, initialize the PurchaseConnector between the

Init

and

Start

methods of AppsFlyer. Initialization code:

AppsFlyerPurchaseConnector.init(\_appsFlyerMarker, Store.GOOGLE); AppsFlyerPurchaseConnector.setIsSandbox(Debug.isDebugBuild); AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue( AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions, AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases); AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true); AppsFlyerPurchaseConnector.build(); AppsFlyerPurchaseConnector.startObservingTransactions();

​

Example of a full AppsFlyer initialization, If PurchaseConnector is used - refer to the [PurchaseConnector integration section](/2198b3b3dc3681c58234d1402abd93af?pvs=25#2198b3b3dc36818fa05cee34af8411f0)

Configure the necessary permissions in your

AndroidManifest.xml

.

<manifest xmlns:android\="http://schemas.android.com/apk/res/android" xmlns:tools\="http://schemas.android.com/tools" package\=YOUR\_PACKAGE\_NAME\> //permissions that need to be added, if they are not already included. <uses\-permission android:name\="android.permission.INTERNET" /\> <uses\-permission android:name\="android.permission.ACCESS\_NETWORK\_STATE" /\> <uses\-permission android:name\="com.google.android.gms.permission.AD\_ID" tools:node\="remove"/\> ... </manifest\>

​

After this, your app will be ready to send event data to AppsFlyer.

#### Step 2.1: Setting up sending SCAN postback copies (iOS 15+)

To ensure correct measurements in iOS 15+, you need to configure sending copies of the SKAdNetwork postback directly to AppsFlyer.

Official documentation: [Send SCAN postback copies directly to AppsFlyer (iOS 15+)](https://dev.appsflyer.com/hc/docs/integrate-ios-sdk#sending-skan-postback-copies-to-appsflyer)

This setting is required for all iOS apps to ensure that the SCAN attribution works correctly

In Unity, this can be implemented by adding this PostProcess script

using System.IO; using UnityEngine; using UnityEditor; using UnityEditor.Callbacks; using UnityEditor.iOS.Xcode; public class AppsflyerPostprocessor { \[PostProcessBuildAttribute\] public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) { if (target \== BuildTarget.iOS) { string plistPath \= pathToBuiltProject + "/Info.plist"; PlistDocument plist \= new PlistDocument(); plist.ReadFromString(File.ReadAllText(plistPath)); PlistElementDict rootDict \= plist.root; rootDict.SetString("NSAdvertisingAttributionReportEndpoint", "https://appsflyer-skadnetwork.com/"); File.WriteAllText(plistPath, plist.WriteToString()); Debug.Log("Info.plist updated with NSAdvertisingAttributionReportEndpoint"); } } }

​

Additional materials for reference:

#### Step 3: Integration of Purchase Events (af\_purchase)

Required if purchases are present

For accurate analytics and user action tracking, it is necessary to send information about key purchase events to AppsFlyer.

⚠️

It is not allowed to send subscription data via the

af\_purchase

event

When a user makes an in-app purchase, this event should be sent to AppsFlyer. To do this, use the

sendEvent

method and pass the required parameters.

An example of a correct integration is shown below:

using AppsFlyerSDK; void LogPurchaseEvent(string productName, string currency, float price, string transactionId, ) { Dictionary<string, string\> eventValue \= new Dictionary<string, string\>(); eventValue\["af\_content\_id"\] \= productName; // product ID eventValue\["af\_currency"\] \= currency; // currency eventValue\["af\_revenue"\] \= price; // price/cost eventValue\["af\_transaction\_id"\] \= transactionId; // transaction ID eventValue\["af\_quantity"\] \= "1"; // quantity AppsFlyer.sendEvent("af\_purchase", eventValue); }

​

Parameters of the

af\_purchase

event:

af\_content\_id

: Identifier of the purchased content.

af\_currency

: Currency in which the purchase is made.

af\_revenue

: Price of the item. The decimal separator must be a

.

and not a comma!

af\_transaction\_id

: Unique identifier of the transaction.

af\_quantity

: Number of items purchased

If you have alternative payment methods, such as Yookassa, you need to add the

inapp\_source

parameter. The value of this parameter should reflect the payment method used — for Yookassa it should be

yookassa

, and for the standard in-app payment method it should be

in-game

.

You can read more about this event and its parameters in the official documentation: [![](https://support.appsflyer.com/hc/theming_assets/01J8ETNGEQ2APTA2SGE3FQVWN3)Help CenterRecommended gaming app events](https://support.appsflyer.com/hc/en-us/articles/360018941117-Recommended-gaming-app-events#purchase-af_purchase)​

For

af\_purchase

\- it is not necessary to enable debug logs during the QA phase, as tracking will be performed via the AppsFlyer console in Live Events.

⚠️

Event and parameter names for purchases must be exact. For example, using

af\_price

,

price

, or other variants instead of

af\_revenue

is incorrect! Additionally, AppsFlyer event names are case-sensitive. For example,

af\_purchase

and

af\_PURCHASE

are two completely different events.

Additional materials for reference

The decimal separator for revenue must be a

.

instead of a comma! Otherwise, the data will be recorded incorrectly. See the example below:

![](/image/attachment%3Ad0987857-ce7e-40a1-938d-1cc693cb89c8%3Aimage.png?table=block&id=34c8b3b3-dc36-8056-af45-c7d2c3bae5d9&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=1420&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

#### Step 4: Subscriptions – Purchase Connector

Required if subscriptions are present

We do not send subscription events from the client. If we send them from the client, there is a risk of duplication, since subscription data is received by AppsFlyer directly from the stores (server-to-server from Google Play and the App Store).

If you are using AppsFlyer Unity SDK 6.17.1 or higher, you must remove all references to the AppsFlyer Purchase Connector from the project. No additional integration is required.

Make sure everything is correctly configured in the AppsFlyer admin panel, and that the required endpoints are properly set up in both the App Store and Google Play. To complete this setup, please fill out the following form: ![asana](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F6c7431e4-705c-44a1-8af8-740f33b3c8ae%2Fasana.png?table=custom_emoji&id=13e8b3b3-dc36-805b-b445-007aec7b3924&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=100&userId=&cache=v2&imgBuildSrc=CustomEmoji) [Integrations & Release team form](https://form.asana.com/?hash=7ffdf49b668819fea1de16c4d2e54d90d054a3b528cdc754edce40648a6cf600&id=1190201569564993)

Example inicialization with Purchase Connector

public async UniTask Initialize() { Debug.Log("\[Appsflyer\] Init start"); AppsFlyerSDK.AppsFlyer.initSDK(\_appsFlyerSettings.devKey, \_appsFlyerSettings.appID, \_appsFlyerMarker); var result \= await ApplicationIdentity.RequestAsync(); AppsFlyerSDK.AppsFlyer.setCustomerUserId(result); AppsFlyerSDK.AppsFlyer.setIsDebug(Debug.isDebugBuild); #if UNITY\_IOS && !UNITY\_EDITOR Version ver \= Version.Parse(UnityEngine.iOS.Device.systemVersion); if (ver.Major \> 14 || (ver.Major \== 14 && ver.Minor \>= 5)) { AppsFlyerSDK.AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(30); } #endif AppsFlyerPurchaseConnector.init(\_appsFlyerMarker, Store.GOOGLE); AppsFlyerPurchaseConnector.setIsSandbox(Debug.isDebugBuild); AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue( AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions, AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases); AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true); AppsFlyerPurchaseConnector.build(); AppsFlyerPurchaseConnector.startObservingTransactions(); AppsFlyerSDK.AppsFlyer.startSDK(); Debug.Log("\[Appsflyer\] Init complete"); await UniTask.NextFrame(); }

​

#### Step 5: Integration of Ad Revenue Events (

af\_ad\_revenue

)

Required if ads are present

To track ad revenue generated through mobile advertising (e.g., AdMob or Facebook Audience Network, etc), the

AppsFlyer.logAdRevenue

method must be used

If you are using AppsFlyer Unity SDK 6.15.0 or higher, there is no need to integrate the Ad Revenue connector separately, as it is already included in the main AppsFlyer package.

Everything must be configured correctly in the AppsFlyer admin panel. This should be requested from your Integration Manager.

Code example below:

//Method called from MaxSdk on rewarded/interstitial ad revenue paid event public void SendAdRevenue(MaxSdkBase.AdInfo adInfo) { Debug.Log($"\[AppsFlyer\] Sending ad revenue for {adInfo.AdFormat}"); var parameters \= new Dictionary<string, string\> { { AFAdRevenueEvent.AD\_UNIT, adInfo.AdUnitIdentifier }, { AFAdRevenueEvent.AD\_TYPE, adInfo.AdFormat }, { AFAdRevenueEvent.PLACEMENT, adInfo.Placement }, { AFAdRevenueEvent.COUNTRY, MaxSdk.GetSdkConfiguration().CountryCode }, }; var data \= new AFAdRevenueData(adInfo.NetworkName, MediationNetwork.ApplovinMax, "USD", adInfo.Revenue); AppsFlyer.logAdRevenue(data, parameters); }

​

For further clarification, please refer to the following materials:

#### Step 6. Sandbox and Debug Build

To test all integrations (purchases, subscriptions, ads), your application must be connected in Sandbox mode on both platforms (iOS and Android). This allows you to perform testing without spending real money and ensures that events are correctly transmitted to the system.

iOS Sandbox: To test purchases and subscriptions, use Sandbox accounts in App Store Connect.

Android Sandbox: To test purchases, use a test account in the Google Play Console.

Once your application is running in Sandbox mode, you will be able to track events in real time and verify that the data is being transmitted correctly.

![Callout icon](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F2bba85ce-e788-4225-8788-51840e9f471d%2Fpushpin.png?table=custom_emoji&id=2008b3b3-dc36-8030-867c-007ab0bc3e17&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=50&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

Tools required in debug builds for AppsFlyer integration testing

Detailed AppsFlyer SDK logs

Additional AppsFlyer debug logs (custom logs)

af\_ad\_revenue logs from AppsFlyer SDK connector

Tool descriptions

#### Useful Links

📖

You can find more details about all events in the official AppsFlyer documentation.

[![](https://support.appsflyer.com/hc/theming_assets/01J8ETNGEQ2APTA2SGE3FQVWN3)Help CenterRecommended gaming app events](https://support.appsflyer.com/hc/en-us/articles/360018941117-Recommended-gaming-app-events)​

[Integrate the ROI360 ad revenue SDK API](https://support.appsflyer.com/hc/en-us/articles/4416353506833-Integrate-the-ROI360-ad-revenue-SDK-API)

[![](https://support.google.com/favicon.ico)Advertising ID - Play Console Help](https://support.google.com/googleplay/android-developer/answer/6048248?hl=en)​

## FAQ & TroubleShooting

<table style="isolation: auto;"><tbody style="isolation: auto;"><tr class="notion-table-row" style="isolation: auto; height: 34px;"><th scope="col" dir="ltr" style="background: var(--cd-tabHeaRowColBac); font-weight: 500; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 350.999px; max-width: 350.999px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r3s:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">Question/Issue</div></div></th><th scope="col" dir="ltr" style="background: var(--cd-tabHeaRowColBac); font-weight: 500; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 355px; max-width: 355px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r3t:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">Answer/Solution</div></div></th></tr><tr class="notion-table-row" style="isolation: auto; height: 34px;"><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 350.999px; max-width: 350.999px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r3u:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">What events do we send to AppsFlyer?</div></div></td><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 355px; max-width: 355px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r3v:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">Sessions and installs (primarily, if AppsFlyer is integrated correctly, these will be active by default),<div class="notion-inline-code-container" style="display:inline"><span style="font-family:&quot;SFMono-Regular&quot;, Menlo, Consolas, &quot;PT Mono&quot;, &quot;Liberation Mono&quot;, Courier, monospace;line-height:normal;background:rgba(135,131,120,.15);color:#EB5757;border-radius:4px;font-size:85%;padding:0.2em 0.4em;position:relative;bottom:0.065em" data-token-index="1" spellcheck="false" class="notion-enable-hover">af_purchase</span></div>(you need to write the code yourself if your game has in-app purchases; more details in the documentation).</div></div></td></tr><tr class="notion-table-row" style="isolation: auto; height: 34px;"><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 350.999px; max-width: 350.999px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r40:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">Why are parameters needed for<div class="notion-inline-code-container" style="display:inline"><span style="font-family:&quot;SFMono-Regular&quot;, Menlo, Consolas, &quot;PT Mono&quot;, &quot;Liberation Mono&quot;, Courier, monospace;line-height:normal;background:rgba(135,131,120,.15);color:#EB5757;border-radius:4px;font-size:85%;padding:0.2em 0.4em;position:relative;bottom:0.065em" data-token-index="1" spellcheck="false" class="notion-enable-hover">af_purchase</span></div>if they don’t appear in the AppsFlyer dashboard?</div></div></td><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 355px; max-width: 355px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r41:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">These parameters can be fetched by our BI system, which allows our analysts to work with this data later.</div></div></td></tr><tr class="notion-table-row" style="isolation: auto; height: 34px;"><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 350.999px; max-width: 350.999px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r42:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">What is an App ID? In which cases should it be used?</div></div></td><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 355px; max-width: 355px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r43:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">App ID is a unique identifier of the application in Apple’s system. It is used to identify the app in the App Store. It is used only for iOS versions. For Android versions, leave this field empty. This ID is taken from App Store Connect. You need to go to the created app in App Store Connect → App Information → there you will find the Apple ID, which should be used.</div></div></td></tr></tbody></table>

![](/image/attachment%3A41768e91-eeaa-448d-bee8-f2dd2187e487%3A%D0%BF%D0%B8%D1%81%D1%8C%D0%BC%D0%BE.png?table=block&id=2198b3b3-dc36-8134-a685-d4cf02ad5727&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=80&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

If anything is unclear or you have ideas for improvement — leave your comments or write to your Integration Manager.
