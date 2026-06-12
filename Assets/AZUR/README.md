# AZUR SDK

Unified Unity SDK layer for:

- AppLovin MAX
- Firebase Analytics
- AppsFlyer
- AppMetrica
- Facebook SDK
- Firebase Remote Config wrapper

## Setup

1. Install vendor SDK packages:
   - Firebase and AppMetrica are already wired through `Packages/manifest.json`.
   - AppLovin MAX, AppsFlyer, and Facebook SDK can be installed from the official archives already downloaded in `ThirdParty/OfficialSDKs`.
   - To extract those archives into the project, run:
     - `python3 Tools/install_vendor_sdks.py`
2. Add the corresponding scripting define symbols:
   - `AZUR_APPLOVIN_MAX`
   - `AZUR_FIREBASE`
   - `AZUR_APPSFLYER`
   - `AZUR_APPMETRICA`
   - `AZUR_FACEBOOK`
3. Reopen Unity and let Package Manager resolve dependencies from `manifest.json`.
4. In Unity, run `AZUR/Create SDK Config`.
5. Fill the generated `AzurSdkConfig` asset.
6. Run `AZUR/Create Bootstrap In Scene`.
7. Optionally run:
   - `AZUR/Enable All Define Symbols`
   - `AZUR/Validate SDK Setup`
   - `AZUR/Create Sample Behaviour In Scene`

## Runtime API

```csharp
using AZUR;
using System.Collections.Generic;

AzurSdk.SetConsent(true);
AzurSdk.SetUserId("player-123");

AzurSdk.TrackEvent("level_start", new Dictionary<string, object>
{
    ["level"] = 1,
    ["source"] = "main_menu"
});

AzurSdk.TrackPurchase(new AzurPurchaseEvent(
    productId: "coins_pack_1",
    currency: "USD",
    revenue: 4.99,
    transactionId: "txn-001"));

AzurAds.LoadInterstitial();
AzurAds.ShowInterstitial("game_over");

AzurAnalytics.TrackLevelStart(1, "Level_1");
AzurCommerce.TrackPurchase("coins_pack_1", "USD", 4.99, "txn-001");

AzurRemoteConfig.Fetch(success =>
{
    var welcome = AzurRemoteConfig.GetString("welcome_message", "hello");
    var offerPrice = AzurRemoteConfig.GetInt("offer_price", 0);
});
```

## Notes

- The SDK is safe to import before vendor packages are installed because vendor bindings are wrapped with define symbols.
- AppLovin ad revenue is forwarded into Firebase, AppsFlyer, AppMetrica, and Facebook through `AzurSdk.TrackAdRevenue`.
- Facebook ad revenue buffering is persisted in `Application.persistentDataPath/azur_fb_revenue.json`.
- `AZUR/Validate SDK Setup` now checks define symbols, required config values, and whether package footprints are present in the project.
- `AzurRemoteConfig` uses Firebase Remote Config when `AZUR_FIREBASE` is defined and `enableRemoteConfig` is enabled; otherwise it falls back to local defaults from config.
