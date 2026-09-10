//
//  FullScreenAdClosedObserver.m
//  Unity-iPhone
//
//  Created by Nguyen Hoai Phuong on 4/1/20.
//

#import <Foundation/Foundation.h>
#import <AudioToolbox/AudioToolbox.h>
#import <AVFoundation/AVFoundation.h>

#define DEFINE_NOTIFICATION(name) extern "C" __attribute__((visibility ("default"))) NSString* const name;

DEFINE_NOTIFICATION(kUnityViewDidAppear);

@interface FullScreenAdClosedObserver : NSObject
@property (nonatomic, readonly) BOOL isPlayingAd;
@property (nonatomic, readonly) BOOL pausedByInterruption;
+ (instancetype)sharedManager;
- (void)startPlayingAd;
@end

@implementation FullScreenAdClosedObserver
#pragma mark Singelton
      
+ (instancetype)sharedManager {
    static FullScreenAdClosedObserver *sharedManager;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedManager = [[FullScreenAdClosedObserver alloc] init];
    });
    return sharedManager;
}

#pragma mark Initialization
      
- (id)init {
    self = [super init];
    if (self) {
        _isPlayingAd = YES;
        
        [[NSNotificationCenter defaultCenter] addObserver: self selector: @selector(unityViewDidAppear:) name: kUnityViewDidAppear object: nil];
        
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(handleInterruptionChangeToState:) name:AVAudioSessionInterruptionNotification object:nil];
    }
    return self;
}

- (void)dealloc {
    [[NSNotificationCenter defaultCenter] removeObserver:self name:kUnityViewDidAppear object:nil];
    
    [[NSNotificationCenter defaultCenter] removeObserver:self name:AVAudioSessionInterruptionNotification object:nil];
}

- (void)unityViewDidAppear:(NSNotification*)notification {
    _isPlayingAd = NO;

    if (UnityIsPaused()) {
        UnityPause(NO);
    }
}

- (void)startPlayingAd {
    _isPlayingAd = YES;
}

- (void)stopPlayingAd {
    _isPlayingAd = NO;
}

- (void)handleInterruptionChangeToState:(NSNotification *)notification {
    if (notification.name != AVAudioSessionInterruptionNotification ||
        notification.userInfo == nil)
        return;
        
    NSDictionary *interuptionDict = notification.userInfo;
    NSInteger interuptionType = [[interuptionDict valueForKey:AVAudioSessionInterruptionTypeKey] integerValue];
    
    if (interuptionType == AVAudioSessionInterruptionTypeBegan){
        NSLog(@"AVAudioSessionInterruptionTypeBegan");
        if(!_pausedByInterruption)
        {
            UnitySetAudioSessionActive(false);
            [[AVAudioSession sharedInstance] setActive:NO error:nil];
            _pausedByInterruption = YES;
        }
    }
    else if (interuptionType == AVAudioSessionInterruptionTypeEnded){
        NSLog(@"AVAudioSessionInterruptionTypeEnded");
        if (_pausedByInterruption)
        {
            [[AVAudioSession sharedInstance] setActive:YES error:nil];
            UnitySetAudioSessionActive(true);
            _pausedByInterruption = NO;
        }
    }
}
@end

extern "C" {
    void _initAdClosedObserver() {
        [FullScreenAdClosedObserver sharedManager];
    }

    bool _isPlayingAd() {
        return [[FullScreenAdClosedObserver sharedManager] isPlayingAd];
    }

    void _startPlayingAd() {
        [[FullScreenAdClosedObserver sharedManager] startPlayingAd];
    }

    void _stopPlayingAd() {
        [[FullScreenAdClosedObserver sharedManager] stopPlayingAd];
    }
}
