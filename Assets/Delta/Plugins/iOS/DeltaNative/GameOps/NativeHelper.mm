#import "StringHelper.h"
#import <AdSupport/ASIdentifierManager.h>

extern "C" 
{
    char* _getCountryCode()
    {
        NSLocale *currentLocale = [NSLocale currentLocale];  // get the current locale.
        NSString *countryCode = [currentLocale objectForKey:NSLocaleCountryCode];
        return cStringCopy([countryCode UTF8String]);
    }

    char* _getCountry()
    {
        NSLocale *locale = [NSLocale currentLocale];
        NSString *countryCode = [locale objectForKey: NSLocaleCountryCode];
        NSLocale *usLocale = [[NSLocale alloc] initWithLocaleIdentifier:@"en_US"];
        NSString *country = [usLocale displayNameForKey: NSLocaleCountryCode value: countryCode];
        return cStringCopy([country UTF8String]);
    }

    float _getDeviceNativeScale() {
        return UIScreen.mainScreen.nativeScale;
    }

    float _getDeviceScreenSizeHorizontal() {
        return UIScreen.mainScreen.bounds.size.width;
    }

    float _getDeviceScreenSizeVertical() {
        return UIScreen.mainScreen.bounds.size.height;
    }

    char * _NATIVE_Get_IDFV()
    {
        return cStringCopy([[[[UIDevice currentDevice] identifierForVendor] UUIDString] UTF8String]);
    }
    
    char * _NATIVE_Get_IDFA()
    {
        return cStringCopy([[[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString] UTF8String]);
    }
    int _NATIVE_IsTestFlightBuild()
    {
        BOOL isRunningTestFlightBeta = [[[[NSBundle mainBundle] appStoreReceiptURL] lastPathComponent] isEqualToString:@"sandboxReceipt"];
        if (isRunningTestFlightBeta)
            return 1;
        return 0;
    }
}
