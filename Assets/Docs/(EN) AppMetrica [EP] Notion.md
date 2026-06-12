[

General Information About the Service



](/azurgames/EN-AppMetrica-EP-2198b3b3dc36811b9ba0d7440321a5ad?pvs=25#2198b3b3dc36817094dcf3b469ed9034)

[

Step 1. Preparation for Integration



](/azurgames/EN-AppMetrica-EP-2198b3b3dc36811b9ba0d7440321a5ad?pvs=25#2198b3b3dc36810aa129f08ea3dbb366)

[

Step 2. Integration into the Project



](/azurgames/EN-AppMetrica-EP-2198b3b3dc36811b9ba0d7440321a5ad?pvs=25#2198b3b3dc3681e4b256fd9c639c846b)

### General Information About the Service

This service is used for product analytics of the project through embedded events.

### Step 1. Preparation for Integration

A project in the AppMetrica dashboard is created on our side

The AppMetrica API key will be provided by your Integration Manager in the Slack channel

The key is valid for both Android and iOS versions.

📌

If the project has passed the first tests, the project is already registered in the AppMetrica Dashboard.

Selecting Events for Integration

Determine the list of events planned to be sent to AppMetrica. Provide them to your developers.

[Document with basic events for prototypes](https://docs.google.com/spreadsheets/d/1eeU46jdKNCtpG7DHX3XqxndS9ZLtGneeM5unIONBffQ/edit#gid=0)

[Document with basic events for IDLE-projects](https://docs.google.com/spreadsheets/d/1aNZp3vcGDki3PE3tEPHXzxDPE1r6ntNVvnEbW_poVAI/edit?gid=0#gid=0)

If the project requires additional custom events, they must be pre-approved with Product Analytics via the form:

![asana-](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2Fd7f4d751-5944-42ee-bb2c-a69476617150%2Fasana.png?table=custom_emoji&id=13e8b3b3-dc36-8092-b9c8-007af1194897&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=100&userId=&cache=v2&imgBuildSrc=CustomEmoji) [Request for custom events](https://form.asana.com/?k=1Y1Yu_5jUQtAtX3GOaR7JA&d=713732988947687) (

Task type

→

Product Analytics task

).

⚠️

Any custom events not described in the specifications must be agreed upon with the Product Analytics department and your Producer/Product Manager before integration.

### Step 2. Integration into the Project

Official documentation for integrating the AppMetrica SDK in Unity:

[General integration steps (official guide)](https://appmetrica.yandex.com/docs/en/common/quick-start)

[Integration instructions for Unity](https://appmetrica.yandex.com/docs/en/sdk/unity/analytics/quick-start)

SDK Requirements:

Use a current SDK version — one of the latest available.

SDK can be downloaded here:

[![](/images/external_integrations/github-icon.png)https://github.com/appmetrica/appmetrica-unity-plugin/releas…](https://github.com/appmetrica/appmetrica-unity-plugin/releases)​

If the project uses Applovin and Yandex network, compatible versions must be selected.

Instructions for selecting the AppMetrica version and Yandex adapter version

SDK Inicialization

Inicialization example:

If you are using the ApplicationIdentity package, you can get the UserId through this package. See details in the section below [![](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F05463045-ba39-4f1f-8ac4-4ceb470e1182%2F%25D1%2581%25D0%25BF%25D0%25B8%25D1%2581%25D0%25BE%25D0%25BA.png?table=custom_emoji&id=17c8b3b3-dc36-802e-b29d-007a5b14655c&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=200&userId=&cache=v2&imgBuildSrc=renderMentionIcon)\[EN\] SDK Init-Flow & Tech funnel \[EP\] - 1. Application Identity (appset\_id)](/1-Application-Identity-appset_id-2d58b3b3dc368001bb8efcc5874283f8?pvs=24#2d58b3b3dc3680dcb231f71f982ac0dc)

public async UniTask Initialize() { AppMetrica.Activate(new AppMetricaConfig("your-api-key") { CrashReporting \= true, SessionTimeout \= 300, LocationTracking \= false, Logs \= true, FirstActivationAsUpdate \= !FirstLaunch,//проверка, что не первый запуск UserProfileID \= appSetId,//кэшированный AppsetId полученный через пакет Application Identity DataSendingEnabled \= false//выключена отправка событий аналитики, пока не пройден UCM }); await UniTask.WaitWhile(() \=> !AppMetrica.IsActivated()); }

​

After completing the UCM flow, you need to enable analytics tracking:

AppMetrica.SetDataSendingEnabled(hasConsent);

​

You can find more details in the technical funnel documentation [![](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F05463045-ba39-4f1f-8ac4-4ceb470e1182%2F%25D1%2581%25D0%25BF%25D0%25B8%25D1%2581%25D0%25BE%25D0%25BA.png?table=custom_emoji&id=17c8b3b3-dc36-802e-b29d-007a5b14655c&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=200&userId=&cache=v2&imgBuildSrc=renderMentionIcon)\[EN\] SDK Init-Flow & Tech funnel \[EP\] - 1. User Consent Manager](/1-User-Consent-Manager-2d58b3b3dc368001bb8efcc5874283f8?pvs=24#2d58b3b3dc36805a9432eda8b0208aac)

Integration of User Events:

Implement sending of standard events:

level\_start

level\_finish

And required events from:

[Document with basic events for prototypes](https://docs.google.com/spreadsheets/d/1eeU46jdKNCtpG7DHX3XqxndS9ZLtGneeM5unIONBffQ/edit#gid=0)

[Document with basic events for IDLE-projects](https://docs.google.com/spreadsheets/d/1aNZp3vcGDki3PE3tEPHXzxDPE1r6ntNVvnEbW_poVAI/edit?gid=0#gid=0)

Example of sending event:

public void SendEvent(string eventName, Dictionary<string, object\> parameters, bool sendBuffer \= false) { AppMetrica.ReportEvent(eventName, JsonConvert.SerializeObject(parameters)); }

​

Forced Sending of Events:

For

level\_start

and

level\_finish

events, configure forced sending of events from the buffer. Otherwise, data may be lost if the player exits the game during or immediately after the level and does not return.

Use this method -

void SendEventsBuffer()

[Description of this method in the official documentation](https://appmetrica.yandex.com/docs/en/sdk/ios/analytics/swift/AppMetricaReporting#method_sendEventsBuffer)

💡

Once the user has given consent in the GDPR and CMP windows, it is necessary to enable data’s sending.

More details about the method in the instructions: [![](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F05463045-ba39-4f1f-8ac4-4ceb470e1182%2F%25D1%2581%25D0%25BF%25D0%25B8%25D1%2581%25D0%25BE%25D0%25BA.png?table=custom_emoji&id=17c8b3b3-dc36-802e-b29d-007a5b14655c&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=200&userId=&cache=v2&imgBuildSrc=renderMentionIcon)\[EN\] SDK Init-Flow & Tech funnel \[EP\] - UserConsentManager.Instance.StartFlow(() =>](/UserConsentManager-Instance-StartFlow-MaxSdk-SetHasUserConsent-UserConsentManager-Instan-2d58b3b3dc368001bb8efcc5874283f8?pvs=24#2d58b3b3dc3680f7934ecec3932e73ae)

AppMetrica.SetDataSendingEnabled(hasConsent)

Correct Handling of the First Session:

To prevent the old audience from generating installs after adding AppMetrica, implement logic for determining the first session using the

handleFirstActivationAsUpdate

method.

[Documentation for handleFirstActivationAsUpdate](https://appmetrica.yandex.com/docs/en/sdk/ios/analytics/swift/AppMetricaConfiguration#property_handleFirstActivationAsUpdate)

[Usage Examples](https://appmetrica.yandex.com/docs/en/sdk/android/analytics/android-operations#:~:text=Tracking%20new%20users)

For example, you can check for the presence of a prefs file and if it exists, set this setting to True — this way an old user will not generate an install on the first game release with AppMetrica.

💡

If the game has no old audience, i.e., the game is released immediately with AppMetrica — this step can be skipped.

![](/image/attachment%3A41768e91-eeaa-448d-bee8-f2dd2187e487%3A%D0%BF%D0%B8%D1%81%D1%8C%D0%BC%D0%BE.png?table=block&id=2198b3b3-dc36-8141-8f13-ed9cf3472b1f&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=80&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

If anything is unclear or you have ideas for improvement — leave your comments or write to your Integration Manager.
