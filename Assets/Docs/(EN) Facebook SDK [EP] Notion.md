[

About the Service



](/azurgames/EN-Facebook-SDK-EP-2198b3b3dc3681379105f4dcd9b5bace?pvs=25#2198b3b3dc3681c1bfe4ee30b4025cba)

[

Step 1. Preparing for Integration



](/azurgames/EN-Facebook-SDK-EP-2198b3b3dc3681379105f4dcd9b5bace?pvs=25#34c8b3b3dc368081abdcc7d1a423cb36)

[

Step 2. Integration into the project



](/azurgames/EN-Facebook-SDK-EP-2198b3b3dc3681379105f4dcd9b5bace?pvs=25#34c8b3b3dc368077ade6e5c8385b5abf)

[

FAQ & Troubleshooting



](/azurgames/EN-Facebook-SDK-EP-2198b3b3dc3681379105f4dcd9b5bace?pvs=25#34c8b3b3dc3680d29523c5a51ed5cd76)

### About the Service

The Facebook SDK is required to run UA campaigns on the Facebook platform.

### Step 1. Preparing for Integration

We have created an app in Facebook Developers and sent you the keys:

App ID

Client Token

If you need access to the app, send us your Facebook profile ID (example: [https://www.facebook.com/gelmanovruslan/](https://www.facebook.com/gelmanovruslan/) →

gelmanovruslan

), and we will grant you access.

What you need to provide us

Follow the [instructions for Android](https://developers.facebook.com/docs/unity/getting-started/android/) to find:

Class Name

Key Hashes

Package Names

If you already have an app in Facebook Developers

Do not link it to Business Manager.

If it's already linked:

Go to [Business Manager settings](https://business.facebook.com/settings/)

Select the app

Click "Remove"

Grant us access by adding our profiles as administrators:

gelmanovruslan

100092745698487

61582098669153

61584430028382

Send us:

Class Name

Key Hashes

Package Names

If you created the app yourself, you can manually enter the Android platform in the app settings.

### Step 2. Integration into the project

Integrate the Facebook SDK according to the documentation: [![](https://static.xx.fbcdn.net/rsrc.php/yB/r/2sFJRNmJ5OP.ico)Meta for DevelopersGetting Started - Unity SDK - Documentation - Meta for Devel…](https://developers.facebook.com/docs/unity/gettingstarted) We recommend using SDK version 18.0.0 or higher

but considering our instructions below

✅

The standard Facebook events package is sufficient; there is no need to track custom metrics at this stage. It is sufficient for the Facebook SDK to be initialized.

Configure the platform(s) Go to

Settings

→ Select

General

→ Scroll down → Select

Add Platform

→ Fill in the required information. If your app is on Android, then:

Bundle ID

Class Name

Key Hashes

And if your app is on iOS, then:

Bundle ID

Store IDs

The specifics of initializing the Facebook SDK are listed below:

public void Initialize() { if (FB.IsInitialized) { FB.ActivateApp(); } else { if (!FB.IsInitialized) { FB.Init(() \=> { FB.ActivateApp(); #if UNITY\_IOS FB.Mobile.SetAdvertiserTrackingEnabled(true); #endif }); } } }

​

Sending ad\_revenue

When sending ad\_revenue to Facebook, keep in mind that Facebook may round down revenue that is too small.

Because of this, there may be discrepancies in revenue metrics across different analytics services.

To address this, it makes sense to implement an event buffer—collect revenue data from 10–15 events, then aggregate it and send it as a single event.

Code example below:

public void SendFacebookAdImpression(MaxSdkBase.AdInfo adInfo) { #if FACEBOOK\_SUPPORTED if (FB.IsInitialized) { var revenue \= adInfo.Revenue; var networkName \= adInfo.NetworkName; var adFormat \= adInfo.AdFormat; if (string.Equals(adFormat, "BANNER", StringComparison.OrdinalIgnoreCase) || string.Equals(adFormat, "LEADER", StringComparison.OrdinalIgnoreCase)) return; double roundedToDecimal \= revenue \>= 0.01 ? revenue : 0.01; Dictionary<string, object\> parameters \= new Dictionary<string, object\>() { {"value", roundedToDecimal}, {AppEventParameterName.NumItems, 1}, {AppEventParameterName.Currency, "USD"}, {AppEventParameterName.ContentType, adFormat}, {AppEventParameterName.ContentID, networkName} }; FB.LogAppEvent("ad\_revenue\_max", (float) roundedToDecimal, parameters); Debug.LogFormat( "\[AdRevenue - Facebook\] Event ad\_revenue\_max for module {0} logged with: value = {1} (before round was: {3}); currency = USD; fb\_content\_id = {2};", adFormat, roundedToDecimal, networkName, revenue); FacebookEventsTracker.fbPurchaseEvents.Add(new FBPurchaseEvent {revenue \= revenue}); if (FacebookEventsTracker.IsBufferFull()) { var totalRevenue \= (float) FacebookEventsTracker.fbPurchaseEvents.Sum(e \=> e.revenue); FB.LogPurchase(totalRevenue, "USD", new Dictionary<string, object\> { {AppEventParameterName.NumItems, FacebookEventsTracker.fbPurchaseEvents.Count}, {AppEventParameterName.Currency, "USD"}, {AppEventParameterName.ContentType, "ad\_revenue"} } ); Debug.LogFormat( "\[AdRevenue - Facebook\] Event Purchase for module {0} logged with: fb\_num\_items = {2}; value = {1}; currency = USD; fb\_content\_type = ad\_revenue;", adFormat, totalRevenue, FacebookEventsTracker.fbPurchaseEvents.Count); FacebookEventsTracker.fbPurchaseEvents.Clear(); } else { Debug.LogFormat("\[AdRevenue - Facebook\] Ad impressions collected: {0}. Buffer size is: {1}", FacebookEventsTracker.fbPurchaseEvents.Count, FacebookEventsTracker.FbPurchaseEventsBufferSize); } FacebookEventsTracker.SaveFbEventsBuffer(); } #endif }

​

Example of buffer implementation:

\[Serializable\] public class FBPurchaseEvent { public double revenue; } public static class FacebookEventsTracker { public const int FbPurchaseEventsBufferSize \= 15; public static List<FBPurchaseEvent\> fbPurchaseEvents; private static string \_path; public static bool IsBufferFull() \=> fbPurchaseEvents.Count \== FbPurchaseEventsBufferSize; public static void LoadFbEventsBuffer() { \_path \= Application.persistentDataPath + "/fb.data"; if (File.Exists(\_path)) { using var reader \= new StreamReader(\_path); var serializedData \= reader.ReadLine(); if (string.IsNullOrWhiteSpace(serializedData)) { fbPurchaseEvents \= new List<FBPurchaseEvent\>(); } else { var deserialized \= Newtonsoft.Json.JsonConvert.DeserializeObject<List<FBPurchaseEvent\>\>(serializedData); fbPurchaseEvents \= deserialized ?? new List<FBPurchaseEvent\>(); } } else { fbPurchaseEvents \= new List<FBPurchaseEvent\>(); } } public static void SaveFbEventsBuffer() { try { StreamWriter writer \= new StreamWriter(\_path, false); writer.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(fbPurchaseEvents)); writer.Close(); } catch (Exception exception) { Debug.LogWarning($"Exception on try save facebook events buffer - {exception.Message}"); } } }

​

Sending a purchase event

Example of sending a purchase event:

public void SendPurchase(IStoreItem item) { if (FB.IsInitialized) { FB.LogPurchase(Convert.ToDecimal(item.RealPrice), item.CurrencyCode); } }

​

For iOS 14.5 —initialize the ATE flag for Facebook Ads: [![](https://static.xx.fbcdn.net/rsrc.php/yB/r/2sFJRNmJ5OP.ico)Meta for DevelopersAdvertising Tracking Enabled - Meta App Events - Documentati…](https://developers.facebook.com/docs/app-events/guides/advertising-tracking-enabled)​

⚠️

Even though Facebook Analytics is no longer available as a website, you still need to integrate

the Facebook SDK

and set up

App Events

, because without them, your ad campaigns won’t work!

### FAQ & Troubleshooting

<table style="isolation: auto;"><tbody style="isolation: auto;"><tr class="notion-table-row" style="isolation: auto; height: 34px;"><th scope="col" dir="ltr" style="background: var(--cd-tabHeaRowColBac); font-weight: 500; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 332px; max-width: 332px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r26:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">Question</div></div></th><th scope="col" dir="ltr" style="background: var(--cd-tabHeaRowColBac); font-weight: 500; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 332px; max-width: 332px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r27:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">Answer</div></div></th></tr><tr class="notion-table-row" style="isolation: auto; height: 34px;"><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 332px; max-width: 332px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r28:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">Why are Key Hashes needed?</div></div></td><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 332px; max-width: 332px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r29:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">Key Hashes help Facebook verify that traffic is coming from a real app; without them, UA campaigns won’t work correctly</div></div></td></tr><tr class="notion-table-row" style="isolation: auto; height: 34px;"><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 332px; max-width: 332px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r2a:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">Where can I find Key Hashes and Class Names?</div></div></td><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 332px; max-width: 332px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r2b:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">The process for obtaining them is described in the App Events settings. Link to the guide: <a href="https://developers.facebook.com/docs/unity/getting-started/android/#:~:text=Find%20and%20note%20the%20value%20of%20the%20%27Debug%20Android%20Key%20Hash%27%20in%20the%20%27Android%20Build%20Facebook%20Settings%27%20panel.%20Also%20note%20the%20value%20of%20the%20%27Class%20Name%27." style="cursor:pointer;color:inherit;word-wrap:break-word;text-decoration:inherit" class="notion-link-token notion-focusable-token notion-enable-hover" rel="noopener noreferrer" data-token-index="1" tabindex="0"><span style="text-decoration:underline;text-decoration-thickness:0.05em;text-decoration-color:var(--ca-opaLinDecCol);text-underline-offset:10%;opacity:0.7" class="link-annotation-34c8b3b3-dc36-80eb-bb32-cc779dd99a68-1479496148">Class Name and Key Hashes</span></a></div></div></td></tr><tr class="notion-table-row" style="isolation: auto; height: 34px;"><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 332px; max-width: 332px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r2c:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">Why is it important to select "Others → Business" when creating an app?</div></div></td><td dir="ltr" style="color: inherit; fill: inherit; border: 1px solid var(--c-borPri); isolation: auto; position: relative; vertical-align: top; text-align: start; min-width: 332px; max-width: 332px; min-height: 32px;"><div class="notion-table-cell" style="isolation: auto;"><div id=":r2d:" class="notion-table-cell-text content-editable-leaf-rtl" spellcheck="true" placeholder=" " contenteditable="false" data-content-editable-leaf="true" style="max-width: 100%; width: 100%; white-space: break-spaces; word-break: break-word; caret-color: var(--c-texPri); padding: 7px 9px; background-color: transparent; font-size: 14px; line-height: 20px;">To avoid unnecessary questionnaires from Facebook, which could block access to the API.</div></div></td></tr></tbody></table>

![](/image/attachment%3A41768e91-eeaa-448d-bee8-f2dd2187e487%3A%D0%BF%D0%B8%D1%81%D1%8C%D0%BC%D0%BE.png?table=block&id=34c8b3b3-dc36-8056-aae6-c119b97e3cb3&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=50&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

If anything is unclear or you have ideas for improvement, leave your comments or contact your Integration Manager
