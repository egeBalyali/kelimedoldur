#import <UIKit/UIKit.h>
#import <dispatch/dispatch.h>

extern "C" void WordGame_NotificationHaptic(int happy)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (@available(iOS 10.0, *)) {
            static UINotificationFeedbackGenerator *generator;
            if (generator == nil) generator = [[UINotificationFeedbackGenerator alloc] init];
            [generator prepare];
            [generator notificationOccurred:(happy ? UINotificationFeedbackTypeSuccess : UINotificationFeedbackTypeError)];
        }
    });
}
