[

General Information



](/azurgames/EN-Firebase-Google-Analytics-EP-2198b3b3dc36818e9de4fef8079d0b49?pvs=25#2198b3b3dc3681a4a194e5fe074c5c5a)

[

Step 1. Preparing for integration



](/azurgames/EN-Firebase-Google-Analytics-EP-2198b3b3dc36818e9de4fef8079d0b49?pvs=25#34c8b3b3dc3680ca8b5fc29e2eb3d975)

[

Step 2. Integrating Firebase into the project



](/azurgames/EN-Firebase-Google-Analytics-EP-2198b3b3dc36818e9de4fef8079d0b49?pvs=25#34c8b3b3dc368033bf95c9531074c08f)

[

1 - Adding the Firebase SDK



](/azurgames/EN-Firebase-Google-Analytics-EP-2198b3b3dc36818e9de4fef8079d0b49?pvs=25#34c8b3b3dc3680d98900e1d05feeac6e)

[

2- Adding platforms to the project



](/azurgames/EN-Firebase-Google-Analytics-EP-2198b3b3dc36818e9de4fef8079d0b49?pvs=25#34c8b3b3dc3680319775e7f3884b0ff6)

[

3 - Configuring the ad\_impression event



](/azurgames/EN-Firebase-Google-Analytics-EP-2198b3b3dc36818e9de4fef8079d0b49?pvs=25#34c8b3b3dc368062ac63eb9a7ec60fa7)

[

4 - Configuring the in\_app\_purchase event



](/azurgames/EN-Firebase-Google-Analytics-EP-2198b3b3dc36818e9de4fef8079d0b49?pvs=25#34c8b3b3dc3680b1a190c15e8cc817b2)

[

5 - Configuring Firebase DebugView for iOS



](/azurgames/EN-Firebase-Google-Analytics-EP-2198b3b3dc36818e9de4fef8079d0b49?pvs=25#34c8b3b3dc368054ac94de5527274204)

[

Verifying the integration



](/azurgames/EN-Firebase-Google-Analytics-EP-2198b3b3dc36818e9de4fef8079d0b49?pvs=25#34c8b3b3dc3680c5890ec9f0235916ee)

### General Information

Firebase is an additional analytics tool that can be added to the project after the first tests.

Once the decision to integrate Firebase is made, the project will be created in the Firebase Console from our side and Google Analytics will be linked.

Firebase and Google Analytics are used to measure your game’s metrics and to track events.

At the initial stage of integration, only the

ad\_impression

event (ad event) needs to be added.

A/B testing is also performed using Firebase Remote Config.

Other custom events can be requested later if they are required.

![Callout icon](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F1df19947-f9b5-4c66-b085-0502ad867d6e%2F%25D1%2584%25D0%25BE%25D1%2580%25D0%25BC%25D1%258B_%25D0%25BF%25D0%25BE%25D0%25B8%25D0%25BD%25D1%2582%25D1%258B-02.png?table=custom_emoji&id=17c8b3b3-dc36-8040-bcb3-007ae14b4840&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=50&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

If Firebase is already in use in the project, you must grant the Owner access to the email address [analytics@azurgames.com](mailto:analytics@azurgames.com)

### Step 1. Preparing for integration

The Firebase project will be created from our end

If you need access, provide your email address and request access from your PM / Producer at Azur

A request for granting access to the Firebase project will be submitted via the Integrations & Release department [form](https://form.asana.com/?k=8hxzseI9fpdNNqVYM0KKGQ&d=713732988947687)

Once the project is created and access is granted, you will see your project in the Firebase Console on the dashboard.

Google Analytics and Google Cloud are created and linked automatically.

Your Integration Manager will handle the Google Play integration, but only if the release has been published

### Step 2. Integrating Firebase into the project

#### 1 - Adding the Firebase SDK

Alternative integration method:

In the Firebase console, download the two files google-services.json and GoogleService-Info.plist and add them to the Assets folder in your project .

Download the Unity Package from [![](https://www.gstatic.com/devrel-devsite/prod/v579073a50c63499824df5a68b8922367066583d283ef78fdade1028efdb4ceb5/developers/images/touchicon-180-new.png)Google for DevelopersDownload Google packages for Unity  |  Google for Developers](https://developers.google.com/unity/archive). You will definitely need the [Firebase Analytics](https://developers.google.com/unity/archive#google_analytics_for_firebase) and [Firebase App (Core)](https://developers.google.com/unity/archive#firebase_app_core) packages.

Initialize Firebase in your code; an example is provided below.

Example of Firebase initialization:

If you are using the [![](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F2427b2a7-e196-4e7b-8bb2-b6ec2dfe3fd9%2Fanalytics.png?table=custom_emoji&id=19e8b3b3-dc36-8046-88b8-007a4ad7d363&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=200&userId=&cache=v2&imgBuildSrc=renderMentionIcon)User Consent Manager (PP/ToU+CMP+ATT) \[EN\]](/User-Consent-Manager-PP-ToU-CMP-ATT-EN-2418b3b3dc3683eaa98c01468a384874?pvs=24) package, you need to initialize Firebase after UCM

If you are using the ApplicationIdentity package, you can obtain the UserId through this package. Example:

var result \= await ApplicationIdentity.RequestAsync(); if (result.IsSuccess) { Debug.Log($"\[ApplicationIdentityLoadingStep\] Application Id : {result.Result}"); \_appIdentityProvider.ApplicationId \= result.Result; await UniTask.NextFrame(); } else { Debug.LogError($"\[ApplicationIdentityLoadingStep\] Something went wrong : {result.Message}"); }

​

After that, you can apply the obtained UserId to Firebase during initialization:

private async UniTask<bool\> InitializeFirebase() { var resultId \= await ApplicationIdentity.RequestAsync(); try { var result \= await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task \=> { var dependencyStatus \= task.Result; if (dependencyStatus \== DependencyStatus.Available) { FirebaseAnalytics.SetAnalyticsCollectionEnabled(UserConsentManager.Instance.HasUserConsent); FirebaseAnalytics.SetUserId(resultId.Result); return true; } else { Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}"); return false; } }); return result; } catch (Exception e) { Debug.LogException(e); return false; } }

​

Also note that

FirebaseAnalytics.SetAnalyticsCollectionEnabled(UserConsentManager.Instance.HasUserConsent);

must be called only after initializing UserConsentManager. If you are certain that the Firebase initialization code is called after UserConsentManager, you can use the snippet from the previous example. An alternative option might look like this:

UserConsentManager.Instance.StartFlow(() \=> { FirebaseAnalytics.SetAnalyticsCollectionEnabled(hasUserConsent); })

​

For more details, see the [Init-Flow and Tech Funnel](/2d58b3b3dc368001bb8efcc5874283f8?pvs=25#2d58b3b3dc36805a9432eda8b0208aac) documentation

![Callout icon](/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fpublic.notion-static.com%2F1df19947-f9b5-4c66-b085-0502ad867d6e%2F%25D1%2584%25D0%25BE%25D1%2580%25D0%25BC%25D1%258B_%25D0%25BF%25D0%25BE%25D0%25B8%25D0%25BD%25D1%2582%25D1%258B-02.png?table=custom_emoji&id=17c8b3b3-dc36-8040-bcb3-007ae14b4840&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=50&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

Please use the same Firebase project for all platforms (Android and iOS).

Additional resources for clarification:

#### 2- Adding platforms to the project

Then specify the platforms for which the project is implemented

![](/image/attachment%3A2cfed87a-c8c7-47da-bc66-157c10e5e9b6%3Aimage.png?table=block&id=34c8b3b3-dc36-80b8-aa76-def4b7928154&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=740&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

Adding the Android platform:

![](/image/attachment%3Ac4d833fb-676c-4e6a-8926-d4c0501eb4a9%3Aimage.png?table=block&id=34c8b3b3-dc36-802c-8126-f8ff51ead710&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=740&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

Adding the iOS platform:

![](/image/attachment%3Acbd80d29-aaa3-438e-b405-cbd1e98fffbe%3Aimage.png?table=block&id=34c8b3b3-dc36-8074-825a-d1baef318d1f&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=740&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

After adding each platform, you will receive configuration files (

google-services.json

for Android and

GoogleService-Info.plist

for iOS), which you need to add to your Unity project.

#### 3 - Configuring the

ad\_impression

event

Be sure to add the

ad\_impression

event to track ad revenue (if ads are present) according to the official documentation:

[Tracking ad revenue via Firebase](https://firebase.google.com/docs/analytics/measure-ad-revenue)

An example of event integration is provided below. Please note that in this example, we are sending events from Applovin callbacks. If you are not using Applovin in your project, use similar callbacks from your ad mediation.

private void Start() { MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += SendFirebaseAdImpression; MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += SendFirebaseAdImpression; MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += SendFirebaseAdImpression; } private void SendFirebaseAdImpression(string adUnitId, MaxSdkBase.AdInfo adInfo) { var impressionParameters \= new Dictionary<string, object\> { {"ad\_platform", "AppLovin" }, {"ad\_source", adInfo.NetworkName }, {"ad\_unit\_name", adInfo.AdUnitIdentifier}, {"ad\_format", adInfo.AdFormat}, {"value", adInfo.Revenue}, {"currency", "USD"} }; SendEvent("ad\_impression", impressionParameters); } private void SendEvent(string eventName, Dictionary<string, object\> parameters, bool sendBuffer \= false) { Dictionary<string, object\> firebaseParameters \= parameters?.ToDictionary( pair \=> pair.Key, pair \=> (object)pair.Value); if (firebaseParameters != null && firebaseParameters.Count \> 0) { FirebaseAnalytics.LogEvent( eventName, parameters.Select(pair \=> { Parameter result; switch (pair.Value) { case int \_: case long \_: result \= new Parameter(pair.Key, (long)pair.Value); break; case float \_: result \= new Parameter(pair.Key, (float)pair.Value); break; case double \_: result \= new Parameter(pair.Key, (double)pair.Value); break; default: result \= new Parameter(pair.Key, (string)pair.Value); break; } return result; }).ToArray()); } else { FirebaseAnalytics.LogEvent(eventName); } }

​

#### 4 - Configuring the

in\_app\_purchase

event

For the Android platform:

This event is tracked automatically if Firebase is linked to Google Play.

To set up the integration:

Make sure the project is published on Google Play

Notify your Integration Manager that you need to link Firebase and Google Play for a specific project

Make sure your app is using Analytics SDK version 17.3.0+ (or Firebase Android BoM version 25.2.0+)

To verify the integration, go to

Project Settings → Integrations

.

![](/image/attachment%3Ae9e81075-2e09-42ad-9eb5-38c0c7c55636%3Aimage.png?table=block&id=34c8b3b3-dc36-804f-b610-f4a5b25ef8d6&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=1310&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

For the iOS platform:

Follow the official documentation - [![](https://www.gstatic.com/devrel-devsite/prod/v2f052e0cca7362dede225b85c12aee59eabee5b8fbb05d44fc345ffb54861aec/firebase/images/touchicon-180.png)FirebaseMeasure in-app purchases  |  Google Analytics for Firebase](https://firebase.google.com/docs/analytics/ios/measure-in-app-purchases)​

#### 5 - Configuring Firebase DebugView for iOS

[Documentation on enabling Firebase DebugView on iOS.](https://firebase.google.com/docs/analytics/debugview#ios+) You can also add a PostProcess script that will include the necessary parameter during iOS build.

#if UNITY\_EDITOR public class FirebaseDebugViewPostProcess { \[PostProcessBuild(int.MaxValue)\] public static void OnPostprocessBuild(BuildTarget target, string path) { #if FIREBASE\_ANALYTICS string schemePath \= path + "/Unity-iPhone.xcodeproj/xcshareddata/xcschemes/Unity-iPhone.xcscheme"; XcScheme xcscheme \= new XcScheme(); xcscheme.ReadFromFile(schemePath); xcscheme.AddArgumentPassedOnLaunch("-FIRDebugEnabled"); xcscheme.WriteToFile(schemePath); #endif } } #endif

​

Add the following code before initializing Firebase.

if (Debug.isDebugBuild) { PlayerPrefs.SetString("/google/firebase/debug\_mode", "true"); PlayerPrefs.SetString("/google/measurement/debug\_mode", "true"); }

​

### Verifying the integration

Once integration is complete, request [access](/2198b3b3dc36818e9de4fef8079d0b49?pvs=25#34c8b3b3dc3680c283dbc3691095ff5d) to the Firebase project for verification

If necessary, additional access to Google Analytics can also be provided to view event and revenue statistics.

![](/image/attachment%3A41768e91-eeaa-448d-bee8-f2dd2187e487%3A%D0%BF%D0%B8%D1%81%D1%8C%D0%BC%D0%BE.png?table=block&id=34c8b3b3-dc36-80e5-b699-cd7d1fccc1e3&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=50&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

If anything is unclear or you have ideas for improvement, leave your comments or contact your Integration Manager
