# AZUR Production Readiness

Target editor baseline:
- Unity `2022.3.62f3`

Current priority order:
1. Keep dependency versions pinned to a buildable set for Unity 2022.
2. Finish AppLovin MAX production runtime behavior.
3. Finish AppsFlyer production attribution/revenue flow.
4. Reintroduce Firebase using the correct Unity installation channel.
5. Finish AppMetrica production-specific behaviors.
6. Finish Facebook production-specific behaviors.

## P0 Dependency Gate

- Android build must pass with the selected SDK set enabled.
- Do not upgrade vendor SDKs without updating the compatibility matrix.
- Re-run `Assets > External Dependency Manager > Android Resolver > Force Resolve` after any SDK change.

## P1 AppLovin MAX

- [x] SDK init through the AZUR adapter
- [x] Consent propagation
- [x] User ID propagation
- [x] Creative Debugger toggle in debug builds
- [x] Mediation Debugger entry point in debug builds
- [x] Ad revenue forwarding into the shared analytics layer
- [x] Exponential retry for interstitial load failures
- [x] Exponential retry for rewarded load failures
- [ ] Validate banner behavior on device
- [ ] Validate interstitial lifecycle on device
- [ ] Validate rewarded lifecycle on device
- [ ] Re-enable a Unity-2022-safe AppLovin version

## P2 AppsFlyer

- [x] Core SDK init wrapper
- [x] Customer user ID propagation
- [x] `af_purchase` event wrapper
- [x] `af_ad_revenue` event wrapper
- [x] SCAN postback configuration for iOS
- [ ] Decide whether subscription support is required
- [ ] If subscriptions are required, add a compatible Purchase Connector flow

## P3 Firebase

- [x] AZUR adapter abstraction exists
- [x] Remote Config wrapper exists
- [x] Dependency check result is now handled in the adapter
- [x] Consent and user ID are now applied after dependency availability
- [x] Remote Config defaults are now pushed before fetch
- [x] Remote Config fetch now waits for activate completion
- [ ] Reinstall Firebase through a Unity-2022-compatible source
- [ ] Confirm dependency resolution and runtime init sequence
- [ ] Validate `ad_impression`
- [ ] Validate Remote Config fetch / activate / defaults

## P4 AppMetrica

- [x] AZUR adapter abstraction exists
- [x] Consent-controlled data sending exists
- [x] Add explicit event buffer flush flow for critical events
- [x] Add first-session/update handling strategy
- [ ] Re-verify no AppLovin bridge is auto-enabled unless MAX is actually active

## P5 Facebook

- [x] Core SDK init wrapper
- [x] Purchase wrapper
- [x] Ad revenue buffer wrapper
- [x] Add advertiser tracking handling on iOS
- [ ] Validate production event delivery on device
