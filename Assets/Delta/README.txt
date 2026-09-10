---------------------------------------------------Build Firebase

 - GameOps 2.0.6f
 - Firebase 11.9.0
 - Firebase -- ENABLE_FIREBASE -- Change package name same Firebase config -- app_configs.bytes android_package_name -- Copy 2 file Firebase from website
 - Login -- USE_FACEBOOK / USE_GOOGLE_SIGN_IN -- ios USE_APPLE_AUTHENTICATION

============1.1
Orgin Folder :
 - AthenaGame
 - Atom
 - TextMesh Pro
 - Plugins
   - Demigiant
 - Resources
   - (BillingMode)
Remove others folder when update SDK

============1.2 GameOps Tool

 - Athena
 - OneSDKModules

============1.3 Playfab

 - PlayFabEditorExtensions
 - PlayFabSDK

============1.4 Firebase

 - Editor Default Resources
 - ExternalDependencyManager
 - Firebase
 - GeneratedLocalRepo
 - (google-services)
 - (GoogleService-Info)
 - Plugins
   - Android
     - FirebaseApp.androidlib
     - (gradleTemplate)
     - (mainTemplate)
   - iOS
     - Firebase
   - tvOS
     - Firebase
 - StreamingAssets
   - (google-services-desktop)

-----
(brew install git-lfs)
git lfs track "Assets/Firebase/Plugins/x86_64/FirebaseCppApp-11_9_0.bundle"
git lfs track "Assets/Firebase/Plugins/x86_64/FirebaseCppApp-11_9_0.so"

============1.5 Adjust

 - Adjust

============1.6 Google Mobile Ads v8.7.0

 - GoogleMobileAds
 - Plugins/Android/googlemobile-ads-unity.aar
 - Plugins/Android/GoogleMobileAdsPlugin.androidlib
 - Plugins/iOS/GADUAAdNexworksExtras.h
 - Plugins/iOS/unity-plugin-library.a

============1.7 AdjustSignatureV3

 - Android/adjust-android-signature-3.7.0.aar
 - iOs/AdjustSigSdk.a

============1.8 App Tracking Transparency

- AppTrackingTransparency

---------------------------------------------------Build Login

============
Login :
 + Install package sdk-full
 + Add Scripting Define Symbols USE_FACEBOOK / USE_APPLE_SIGN
 + Use keystore 8865564795139889467.keystore release
 + Google : survival.survive.action.fun.strategy.games
 + Playfab Title Id : F7E64 / 5PUXA9ESNWW7HCT7B1A7D1YIOEEGS64QMPS4S9MJF4QKES6AKR
 + Facebook Setting : Pixel Isle / 588474805665147

============1.2 -> add login sdk
SDK folder

 - AppleAuth
 - AppleAuthSample
 - ExternalDependencyManager
 - FacebookSDK
 - GeneratedLocalRepo
 - GoogleSignIn
 - OneSDKModules
 - PlayFabEditorExtensions
 - PlayFabSDK
 - Plugins
   - Demigiant
   - Android
     - (AndroidManifest)
     - (gradleTemplate)
     - (mainTemplate)
   - iOS
     - GoogleSignIn
 - Resources
   - (BillingMode)
   - (DOTweenSettings)
   - (plugin-manifest-local)
 - SignInSample

============1.3 -> add Game Ops
Game Ops Folder
 - Athena

============1.4 --> add Firebase
Firebase Folder
 - Editor Default Resources
 - Firebase
 - Parse
 - Plugins
 - (google-services.json)
 - (GoogleService-Info.json)

   LFS - Assets/Firebase/Plugins/x86_64/FirebaseCppApp-9_1_0.bundle


------------------------------------------------------Build Android Studio
1. Android==============
1.1. Unity Build
Unity -> Settings -> External Tools -> /Users/khaccanh/Desktop/Data/Soft/gradle-6.7.1 (download folder)
check :
      - Main Manifest
      - mainTemplate.gradle  -> lib dependence of Firebase
      - gradleTemplate.properties
1.2 Gradle Build
Unity -> Export
Android Studio -> File -> Project Structure -> Project -> 4.2.1 / 7.6.4
Android Studio -> Settings -> Build Tools -> Gradle -> JDK 11
2. IOS==================
Use 'brew' , not 'gem'
- brew install cocoapods
- brew link cocoapods


--------------------------------------------------- Switch Enviroment ---------------------------------------------------
  Dev: Delta Tools -> Enviroment -> Switch to DEV
  Prod: Delta Tools -> Enviroment -> Switch to PROD
