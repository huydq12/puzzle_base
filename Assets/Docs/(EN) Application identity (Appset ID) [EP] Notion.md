[

Introduction



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc36803dad69cb1d7dc0cb44)

[

Package Passport



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc3680b1a05bff8881df4a25)

[

Installation



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc3680719a77d5803e0dbd68)

[

Usage



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc3680ac8992ea1454577c2f)

[

Asynchronous (recommended)



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc3680558f19ed52243c365c)

[

Synchronous



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc3680749d21e9052f137a6b)

[

Additional settings



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc3680e0973ff1d78e40f34b)

[

Examples



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc368010b874d4076c8c9b49)

[

Important



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc3680e4a234f729ec5caa84)

[

Interaction with third-party solutions



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc36803c9e5af7c0e9592248)

[

Troubleshoot



](/p/azurgames/EN-Application-identity-Appset-ID-EP-21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25#21b8b3b3dc3680a88740c82bc9839bf6)

## Introduction

The library provides a way to easily obtain a unique vendor identifier for an application for iOS and Android platforms. The identifier is a unique value that is obtained by the user for all applications of a single publisher account. Using this identifier is necessary for more accurate data handling on the analytics side.

## Package Passport

Scope :

com.azur

Package Id :

com.azur.application-identity

Package Name : \[Azur\] - Application Identity

## Installation

Configure access to the NPM server

Configure the project by following the instructions [🤖(EN) Connecting Unity to AG NPM Server \[EP\]](/p/EN-Connecting-Unity-to-AG-NPM-Server-EP-2148b3b3dc3681ba8013f51b0fc2b495?pvs=24)

Open "Window → Package Manager"

Select the package "\[Azur\] - Application Identity".

Click Install

Open "Azur → Application Identity → Wizard".

Click Initialize

## Usage

The package has no explicit pre-initialization, so it can be used immediately at any point in the project code. However, there are certain [requirements](/p/21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25) that must be observed. If you encounter problems, it's worth double-checking that ALL [requirements](/p/21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25) are met and exploring [Troubleshoot](/p/21b8b3b3dc36803e8f36ccdcee9a15d0?pvs=25).

### Asynchronous (recommended)

public sealed class ApplicationIdentityAsyncSample : MonoBehaviour { private async void Start() { var result \= await ApplicationIdentity.RequestAsync(); if (result.IsSuccess) { UnityEngine.Debug.Log($"Application Id : {result.Result}"); } else { UnityEngine.Debug.LogError($"Something went wrong : {result.Message}"); } } }

​

### Synchronous

public sealed class ApplicationIdentitySample : MonoBehaviour { private void Start() { ApplicationIdentity.Completed += OnCompleted; ApplicationIdentity .RequestAsync() .ConfigureAwait(true); } private void OnCompleted(IOperationResult<string\> result) { if (result.IsSuccess) { UnityEngine.Debug.Log($"Application Id : {result.Result}"); } else { UnityEngine.Debug.LogError($"Something went wrong : {result.Message}"); } } }

​

## Additional settings

This package allows you to customize the ID request behavior by overriding the default options.

public sealed class ApplicationIdentityOptionsAsyncSample : MonoBehaviour { private async void Start() { var options \= new Options( timeout: TimeSpan.FromSeconds(3), retryInterval: TimeSpan.FromSeconds(1), retryAttempts: 5, token: destroyCancellationToken); var result \= await ApplicationIdentity.RequestAsync(options); if (result.IsSuccess) { UnityEngine.Debug.Log($"Application Id : {result.Result}"); } else { UnityEngine.Debug.LogError($"Something went wrong : {result.Message}"); } } }

​

## Examples

The package includes test cases available for installation via Package Manager from the package tab called Samples. Choose asynchronous examples for projects with asynchronous architecture and synchronous examples for projects that use a synchronous approach.

## Important

You should call

ApplicationIdentity.RequestAsync()

only after the application is fully initialized. For example, inside the

Start()

Unity method, not

Awake()

The application should request the identifier each time it starts up

CACHE the APP SET ID CATEGORICALLY NOT RECOMMENDED, as any of the following cases may reset the ID:

API app set ID has not been used by a group of apps with the same ID for more than 13 months

The last app in that app group has been uninstalled from the device

The user has done a full factory reset of the device

## Interaction with third-party solutions

AppsFlyer - ID should be passed to AppsFlyer at the time of installation tracking

[Setting the CUID](https://dev.appsflyer.com/hc/docs/basicintegration#set-customer-user-id)

[Setting CUID](https://support.appsflyer.com/hc/en-us/articles/207032016-Customer-User-ID-field-CUID#setting-the-cuid)

AppMetrica - identifier is set via the [withUserProfileID](https://yastatic.net/s3/doc-binary/src/dev/appmetrica/ru/javadoc-7.0.0/io/appmetrica/analytics/AppMetricaConfig.Builder.html#withUserProfileID(java.lang.String)) property during SDK initialization

AppLovin - the identifier should be passed each time an advertisement is displayed by setting it via [SetUserId](https://developers.applovin.com/en/max/reporting-apis/user-level-ad-revenue-api#setting-an-internal-user-id)

## Troubleshoot

If the identifier is not received - examine the information contained in

IOperationResult.Message

. It will have details of all failed attempts and a description of the errors. If the problem persists:

Make sure the latest version of the package is being used (the version appears in the Unity logs as

Application Identity v.2.X.X

)

ApplicationIdentity.RequestAsync()

is called only after the application is fully initialized

ApplicationIdentity.RequestAsync()

is not called from

Awake()

Asynchronous method contains the

await

keyword when

ApplicationIdentity.RequestAsync()

is called

No callbcks that interrupt the operation before it completes (e.g., callback without waiting (e.g., await for the asynchronous call) for the result)

ApplicationIdentity.RequestAsync()

is not called in the same frame as other resource-intensive operations

The value is not cached and is requested anew on each invocation

For synchronous API, theApplicationIdentity

.RequestAsync().ConfigureAwait(true)

construct is used instead of

ApplicationIdentity.RequestAsync()

as in the case of asynchronous invocation

![](/image/attachment%3A41768e91-eeaa-448d-bee8-f2dd2187e487%3A%D0%BF%D0%B8%D1%81%D1%8C%D0%BC%D0%BE.png?table=block&id=21b8b3b3-dc36-80b8-9575-e1746e953446&spaceId=a328c37c-a4cd-406c-a098-ab8c107b10c1&width=50&userId=&cache=v2&imgBuildSrc=requestProxiedImageUrl)

If something is unclear or you have ideas for improvement - leave your comments or write to your Integration Manager
