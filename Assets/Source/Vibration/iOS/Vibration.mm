#import <Foundation/Foundation.h>
#import <AudioToolbox/AudioToolbox.h>
#import <UIKit/UIKit.h>

#import "Vibration.h"

#define USING_IPAD UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad

@interface Vibration ()

@end

@implementation Vibration


#pragma mark - Vibrate

+ (BOOL)    hasVibrator {
    return !(USING_IPAD);
}
+ (void)    vibrate {
    AudioServicesPlaySystemSoundWithCompletion(1352, NULL);
}
+ (void)    vibratePeek {
    AudioServicesPlaySystemSoundWithCompletion(1519, NULL); 
}
+ (void)    vibratePop {
    AudioServicesPlaySystemSoundWithCompletion(1520, NULL); 
}
+ (void)    vibrateNope {
    AudioServicesPlaySystemSoundWithCompletion(1521, NULL); 
}

@end

#pragma mark - "C"

extern "C" {
    
    bool    _HasVibrator () {
        return [Vibration hasVibrator];
    }
 
    void    _Vibrate () {
        [Vibration vibrate];
    }
    
    void    _VibratePeek () {
        [Vibration vibratePeek];
    }
    void    _VibratePop () {
        [Vibration vibratePop];
    }
    void    _VibrateNope () {
        [Vibration vibrateNope];
    }
    
}

