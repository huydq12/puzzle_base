# Current Project State

Snapshot date:
- `2026-05-25`

## Config Toggles

From [AzurSdkConfig.asset](/Users/huy/Azur%20SDK%20SD/Assets/AZUR/Resources/AzurSdkConfig.asset:1):

- `enableAppLovinMax = false`
- `enableFirebase = true`
- `enableAppsFlyer = false`
- `enableAppMetrica = false`
- `enableFacebook = true`

This is intentional.

The project was reduced to a buildable baseline while production wrappers were being hardened.

## What Is Still Present In The Repo

- Firebase UPM archives exist locally in `ThirdParty/OfficialSDKs/Firebase`.
- AppMetrica package is still installed through UPM.
- AppsFlyer source files are still in `Assets/AppsFlyer`.
- Facebook SDK source files are still in `Assets/FacebookSDK`.
- AppLovin resource footprint still exists in `Assets/MaxSdk`, but active dependency wiring is disabled.
- Firebase config assets still exist:
  - [google-services.json](/Users/huy/Azur%20SDK%20SD/Assets/google-services.json:1)
  - [FirebaseApp.androidlib](/Users/huy/Azur%20SDK%20SD/Assets/Plugins/Android/FirebaseApp.androidlib/AndroidManifest.xml:1)

Firebase dependencies are now pointed at local `.tgz` archives in `Packages/manifest.json`, and Firebase runtime is re-enabled in `AzurSdkConfig`.

## What Was Hardened In Code

- AppLovin adapter:
  - mediation debugger hook
  - interstitial/rewarded retry logic
  - ad revenue forwarding
- AppsFlyer adapter:
  - base init flow
  - `af_purchase`
  - `af_ad_revenue`
  - iOS SCAN postprocess
- Firebase adapter:
  - dependency availability handling
  - delayed consent / user id application
- Remote Config wrapper:
  - defaults push before fetch
  - activate completion handling
- AppMetrica adapter:
  - critical event buffer flush
  - first-activation-as-update strategy
- Facebook adapter:
  - ad revenue buffer
  - iOS advertiser tracking hook

## Re-enable Order

1. Firebase
2. AppsFlyer
3. AppMetrica
4. AppLovin MAX

Do not re-enable all vendors at once.
