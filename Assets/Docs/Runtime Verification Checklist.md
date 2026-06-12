# Runtime Verification Checklist

Use this after each vendor is re-enabled.

## Global

- [ ] Unity editor compiles cleanly after refresh
- [ ] `AZUR/Validate SDK Setup` reports no package/define mismatch for the enabled vendor
- [ ] Android build completes
- [ ] If testing iOS, exported Xcode project completes

## Firebase

- [ ] Dependency check finishes without warning in logs
- [ ] Analytics collection follows consent state
- [ ] `SetUserId` is applied
- [ ] `ad_impression` is emitted after a paid ad event
- [ ] Remote Config fetch completes successfully
- [ ] Remote Config activate completes successfully
- [ ] Fallback defaults still work when fetch fails

## AppsFlyer

- [ ] SDK initializes and `getAppsFlyerId()` returns a value
- [ ] `af_purchase` appears for non-subscription purchases
- [ ] `af_ad_revenue` appears for paid ad callbacks
- [ ] iOS export contains `NSAdvertisingAttributionReportEndpoint`
- [ ] If subscriptions are required, verify Purchase Connector separately before production

## AppMetrica

- [ ] SDK activates successfully
- [ ] `SetDataSendingEnabled` follows consent state
- [ ] `level_start` flushes buffer
- [ ] `level_complete` flushes buffer
- [ ] purchase event appears
- [ ] ad revenue event appears
- [ ] first activation as update behaves correctly on an already-installed app

## Facebook

- [ ] SDK initializes and activates app
- [ ] purchase event is sent
- [ ] ad revenue buffer persists between launches
- [ ] buffered revenue flushes at configured threshold
- [ ] iOS advertiser tracking hook executes on device build

## AppLovin MAX

- [ ] SDK initializes successfully
- [ ] Mediation Debugger opens in debug build
- [ ] Creative Debugger toggle works
- [ ] interstitial load retry works after a forced load failure
- [ ] rewarded load retry works after a forced load failure
- [ ] interstitial revenue is forwarded
- [ ] rewarded revenue is forwarded
- [ ] banner behavior is stable
