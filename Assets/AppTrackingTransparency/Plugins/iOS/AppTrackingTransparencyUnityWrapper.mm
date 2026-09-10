
#import "AppTrackingTransparencyUnityWrapper.h"

@implementation AppTrackingTransparencyUnityWrapper

extern "C"
{
    typedef void (*RequestResultCallbackType)(int);

    void _NATIVE_RequestTrackingAuthorization(RequestResultCallbackType func) {
        #ifdef __IPHONE_14_0
        if (@available(iOS 14, *)) {
            NSLog(@"Is iOS 14");
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler: ^(ATTrackingManagerAuthorizationStatus status){
                if (status == ATTrackingManagerAuthorizationStatusAuthorized) {
                    NSLog(@"Result Authorized");
                    func(0);
                    return;
                }
                if (status == ATTrackingManagerAuthorizationStatusDenied) {
                    NSLog(@"Result Denied");
                    func(1);
                    return;
                }
                if (status == ATTrackingManagerAuthorizationStatusNotDetermined) {
                    NSLog(@"Result NotDetermined");
                    func(2);
                    return;
                }
                if (status == ATTrackingManagerAuthorizationStatusRestricted) {
                    NSLog(@"Result Restricted");
                    func(3);
                    return;
                }
            }];
        } else {
            NSLog(@"Not iOS 14");
            // Fallback on earlier versions
        }
        #endif
    }

}
@end
