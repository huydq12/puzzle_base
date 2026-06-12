# SDK Version Matrix

Target editor baseline:
- Unity `2022.3.62f3`
- Android Gradle Plugin `7.4.2`

## Goal

Keep vendor SDKs within a range that still builds on Unity 2022 without D8 / Kotlin metadata failures.

## Current Safe Pins In Repo

Android-side pins currently present in repo state:

- `io.appmetrica.analytics` Unity plugin: `6.6.0`
- `io.appmetrica.analytics:analytics`: `7.11.0`
- `com.appsflyer:af-android-sdk`: `6.16.2`
- `com.appsflyer:unity-wrapper`: `6.16.2`
- `com.android.installreferrer:installreferrer`: `2.1`
- Facebook Android SDK range:
  - `facebook-applinks:[18.0.0,19)`
  - `facebook-core:[18.0.0,19)`
  - `facebook-gamingservices:[18.0.0,19)`
  - `facebook-login:[18.0.0,19)`
  - `facebook-share:[18.0.0,19)`

Gradle safety pins:

- Kotlin stdlib family: `1.8.22`
- Kotlin coroutines family: `1.7.3`

## Temporarily Disabled

- AppLovin MAX
  - reason: recent AppLovin Android SDK versions triggered D8 failure on Unity `2022.3.62f3`
- Firebase
  - reason: correct Unity installation channel still needs to be restored; direct UPM pinning was invalid for this project setup

## Known Risk Versions

- AppLovin Android SDK `13.x`
  - observed D8 failure on this project
- AppsFlyer `6.17.x` with `purchase-connector:2.2.0`
  - part of the incompatible dependency chain previously observed
- AppMetrica `8.x`
  - too new for this Unity 2022 toolchain
- Firebase Unity `13.x`
  - too aggressive for this toolchain during earlier attempts

## Rules

1. Do not upgrade vendor SDKs and resolver output together in one step without a clean build after each vendor.
2. After every dependency change:
   - let Package Manager refresh
   - run `Assets > External Dependency Manager > Android Resolver > Force Resolve`
   - make one Android build before touching another SDK
3. If D8 starts failing again, compare against:
   - [Assets/Plugins/Android/mainTemplate.gradle](/Users/huy/Azur%20SDK%20SD/Assets/Plugins/Android/mainTemplate.gradle:1)
   - [Assets/Plugins/Android/baseProjectTemplate.gradle](/Users/huy/Azur%20SDK%20SD/Assets/Plugins/Android/baseProjectTemplate.gradle:1)
   - [ProjectSettings/AndroidResolverDependencies.xml](/Users/huy/Azur%20SDK%20SD/ProjectSettings/AndroidResolverDependencies.xml:1)
