//
//  SoundSwitch.m
//  SoundSwitch
//
//  Created by Nguyen Hoai Phuong on 1/5/20.
//  Copyright © 2020 Moshe Gottlieb. All rights reserved.
//

#import "SharkfoodMuteSwitchDetector.h"

extern "C" {
    void _checkStatus() {
        [[SharkfoodMuteSwitchDetector shared] checkStatus];
    }
    
    bool _isMuted() {
        return [SharkfoodMuteSwitchDetector shared].isMute;
    }
    
    bool _isChecking() {
        return [SharkfoodMuteSwitchDetector shared].isChecking;
    }
}
