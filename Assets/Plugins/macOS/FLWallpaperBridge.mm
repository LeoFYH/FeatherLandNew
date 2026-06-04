// FLWallpaperBridge.mm
// Native bridge for FeatherLand wallpaper mode on macOS.
//
// macOS equivalent of the Windows WorkerW trick:
//   * Lower the Unity NSWindow's level to just below the desktop-icon layer so
//     it appears in place of the wallpaper while system icons / menu bar stay
//     on top. (CGWindowLevelForKey(kCGDesktopIconWindowLevelKey) - 1)
//   * Mark the window as living on all Spaces / Stationary / IgnoresCycle so
//     it behaves like a wallpaper across desktops.
//   * Drop the title bar (NSWindowStyleMaskBorderless) and resize to fill the
//     main screen.
//   * On Exit, restore the saved level / collection behavior / style mask /
//     frame.
//   * Use CGEventTap to capture global mouse events and forward to Unity for
//     interaction support in wallpaper mode.

#import <Cocoa/Cocoa.h>
#import <CoreGraphics/CoreGraphics.h>

// Cached Unity window reference; survives losing main-window status after we
// drop our level (since we are no longer the front-most window).
static __strong NSWindow *g_unityWindow = nil;

// Saved state captured on first Enter, restored on Exit.
static NSInteger             g_savedLevel    = 0;
static NSWindowCollectionBehavior g_savedBehavior = 0;
static NSWindowStyleMask     g_savedStyle    = 0;
static NSRect                g_savedFrame    = {{0, 0}, {0, 0}};
static BOOL                  g_savedValid    = NO;
static BOOL                  g_wallpaperOn   = NO;

// Mouse event forwarding state
static CFMachPortRef         g_eventTap      = NULL;
static CGEventTapLocation    g_tapLocation   = kCGHIDEventTap;
static BOOL                  g_tapEnabled    = NO;

// Mouse state tracking for Unity
static volatile int          g_clickCount    = 0;
static volatile int          g_rightClickCount = 0;
static volatile float        g_wheelDelta    = 0.0f;
static volatile BOOL         g_isHorizontalWheel = NO;
static volatile double       g_mouseX        = 0.0;
static volatile double       g_mouseY        = 0.0;
static volatile BOOL         g_leftButtonDown = NO;
static volatile BOOL         g_rightButtonDown = NO;

// Keyboard state tracking
static volatile BOOL         g_shiftPressed  = NO;
static volatile BOOL         g_ctrlPressed   = NO;
static volatile BOOL         g_altPressed    = NO;
static volatile uint32_t     g_lastKeyCode   = 0;
static volatile BOOL         g_keyDown       = NO;

#pragma mark - Helpers

static NSWindow *FLLocateUnityWindow(void) {
    if (g_unityWindow != nil) {
        return g_unityWindow;
    }

    NSWindow *candidate = [NSApp mainWindow];
    if (candidate == nil) {
        candidate = [NSApp keyWindow];
    }
    if (candidate == nil) {
        for (NSWindow *w in [NSApp windows]) {
            if ([w isVisible] && [w contentView] != nil) {
                candidate = w;
                break;
            }
        }
    }
    g_unityWindow = candidate;
    return candidate;
}

static NSInteger FLDesiredWallpaperLevel(void) {
    // Just under the desktop-icon layer: above the wallpaper image, below the
    // icons / menu bar. This mirrors the Windows "WorkerW child" placement.
    return CGWindowLevelForKey(kCGDesktopIconWindowLevelKey) - 1;
}

static NSWindowCollectionBehavior FLDesiredWallpaperBehavior(void) {
    return NSWindowCollectionBehaviorCanJoinAllSpaces
         | NSWindowCollectionBehaviorStationary
         | NSWindowCollectionBehaviorIgnoresCycle;
}

static void FLRunOnMain(dispatch_block_t block) {
    if ([NSThread isMainThread]) {
        block();
    } else {
        dispatch_sync(dispatch_get_main_queue(), block);
    }
}

// Convert CGPoint from screen coordinates (bottom-left origin) to Unity coordinates (top-left origin)
static void FLConvertToUnityCoordinates(CGPoint screenPoint, double *outX, double *outY) {
    NSRect screenFrame = [[NSScreen mainScreen] frame];
    *outX = screenPoint.x;
    *outY = screenFrame.size.height - screenPoint.y;
}

#pragma mark - Wallpaper Window Management

// 等待全屏退出完成
static void FLWaitForFullScreenExit(NSWindow *window) {
    // 处理运行循环事件，等待全屏动画完成
    NSDate *timeout = [NSDate dateWithTimeIntervalSinceNow:2.0];
    while ([window styleMask] & NSWindowStyleMaskFullScreen) {
        if ([[NSDate date] compare:timeout] == NSOrderedDescending) {
            NSLog(@"[FLWallpaper] 等待全屏退出超时");
            break;
        }
        // 处理所有待处理的事件
        NSEvent *event;
        while ((event = [NSApp nextEventMatchingMask:NSEventMaskAny
                                           untilDate:[NSDate dateWithTimeIntervalSinceNow:0.1]
                                              inMode:NSDefaultRunLoopMode
                                             dequeue:YES])) {
            [NSApp sendEvent:event];
        }
    }
}

static void FLApplyWallpaper(void) {
    NSWindow *window = FLLocateUnityWindow();
    if (window == nil) {
        NSLog(@"[FLWallpaper] Apply skipped: no Unity NSWindow located.");
        return;
    }

    // 首先检查是否处于全屏模式，如果是，先退出全屏
    if ([window styleMask] & NSWindowStyleMaskFullScreen) {
        NSLog(@"[FLWallpaper] 检测到全屏模式，正在退出全屏...");
        // 保存全屏前的状态
        if (!g_savedValid) {
            g_savedLevel    = [window level];
            g_savedBehavior = [window collectionBehavior];
            g_savedStyle    = [window styleMask];
            g_savedFrame    = [window frame];
            g_savedValid    = YES;
        }
        
        [window toggleFullScreen:nil];
        FLWaitForFullScreenExit(window);
        NSLog(@"[FLWallpaper] 已退出全屏模式");
    }

    if (!g_savedValid) {
        g_savedLevel    = [window level];
        g_savedBehavior = [window collectionBehavior];
        g_savedStyle    = [window styleMask];
        g_savedFrame    = [window frame];
        g_savedValid    = YES;
    }

    [window setLevel:FLDesiredWallpaperLevel()];
    [window setCollectionBehavior:FLDesiredWallpaperBehavior()];

    NSWindowStyleMask desiredMask = NSWindowStyleMaskBorderless;
    if ([window styleMask] != desiredMask) {
        [window setStyleMask:desiredMask];
    }

    NSScreen *screen = [NSScreen mainScreen];
    if (screen != nil) {
        [window setFrame:[screen frame] display:YES];
    }

    // Enable mouse events for the window
    [window setAcceptsMouseMovedEvents:YES];
    
    // Don't make us key — we want to stay behind any real app.
    [window orderBack:nil];

    g_wallpaperOn = YES;
    NSLog(@"[FLWallpaper] Applied wallpaper layer: level=%ld frame=%@",
          (long)[window level], NSStringFromRect([window frame]));
}

static void FLRestoreWindow(void) {
    NSWindow *window = FLLocateUnityWindow();
    if (window == nil) {
        g_wallpaperOn = NO;
        return;
    }

    if (g_savedValid) {
        [window setLevel:g_savedLevel];
        [window setCollectionBehavior:g_savedBehavior];
        if ([window styleMask] != g_savedStyle) {
            [window setStyleMask:g_savedStyle];
        }
        [window setFrame:g_savedFrame display:YES];
    } else {
        [window setLevel:NSNormalWindowLevel];
        [window setCollectionBehavior:NSWindowCollectionBehaviorDefault];
    }

    g_wallpaperOn = NO;
    g_savedValid  = NO;
    NSLog(@"[FLWallpaper] Restored window: level=%ld frame=%@",
          (long)[window level], NSStringFromRect([window frame]));
}

#pragma mark - Event Tap Handling

// Check if the event is within Unity window bounds
static BOOL FLIsEventInUnityWindow(CGEventRef event) {
    if (!g_wallpaperOn) return NO;
    
    NSWindow *window = FLLocateUnityWindow();
    if (window == nil) return NO;
    
    CGPoint location = CGEventGetLocation(event);
    NSRect windowFrame = [window frame];
    
    // Convert screen point to window's coordinate system
    BOOL inWindow = NSPointInRect(NSPointFromCGPoint(location), windowFrame);
    
    // 调试日志 - 每次鼠标移动时打印
    static int logCounter = 0;
    if (logCounter++ % 60 == 0) { // 每60帧打印一次，避免刷屏
        NSLog(@"[FLWallpaper] Mouse at (%f, %f), window frame: (%f, %f, %f, %f), inWindow: %d",
              location.x, location.y,
              windowFrame.origin.x, windowFrame.origin.y,
              windowFrame.size.width, windowFrame.size.height,
              inWindow);
    }
    
    return inWindow;
}

// CGEventTap callback - captures global mouse events
static CGEventRef FLMouseEventCallback(CGEventTapProxy proxy, CGEventType type, 
                                       CGEventRef event, void *refcon) {
    // Only process events when wallpaper mode is active
    if (!g_wallpaperOn || !g_tapEnabled) {
        return event;
    }

    // Check if event is within Unity window bounds
    if (!FLIsEventInUnityWindow(event)) {
        return event;
    }

    CGPoint location = CGEventGetLocation(event);
    double unityX, unityY;
    FLConvertToUnityCoordinates(location, &unityX, &unityY);
    
    // Update mouse position
    g_mouseX = unityX;
    g_mouseY = unityY;

    switch (type) {
        case kCGEventLeftMouseDown:
            g_leftButtonDown = YES;
            g_clickCount++;
            NSLog(@"[FLWallpaper] Left mouse down at (%f, %f)", unityX, unityY);
            // 消费点击事件，防止macOS的"点按墙纸以显示桌面"功能拦截
            return NULL;
            
        case kCGEventLeftMouseUp:
            g_leftButtonDown = NO;
            NSLog(@"[FLWallpaper] Left mouse up at (%f, %f)", unityX, unityY);
            // 消费点击事件，防止macOS处理
            return NULL;
            
        case kCGEventRightMouseDown:
            g_rightButtonDown = YES;
            g_rightClickCount++;
            NSLog(@"[FLWallpaper] Right mouse down at (%f, %f)", unityX, unityY);
            // 消费右键事件
            return NULL;
            
        case kCGEventRightMouseUp:
            g_rightButtonDown = NO;
            NSLog(@"[FLWallpaper] Right mouse up at (%f, %f)", unityX, unityY);
            // 消费右键事件
            return NULL;
            
        case kCGEventScrollWheel: {
            // Get scroll wheel delta (z-axis is vertical on macOS)
            CGFloat deltaX = CGEventGetIntegerValueField(event, kCGScrollWheelEventDeltaAxis1);
            CGFloat deltaY = CGEventGetIntegerValueField(event, kCGScrollWheelEventDeltaAxis2);
            
            // macOS uses different coordinate system - normalize to Unity units
            if (deltaX != 0) {
                g_isHorizontalWheel = YES;
                g_wheelDelta = deltaX / 120.0f;
            } else {
                g_isHorizontalWheel = NO;
                g_wheelDelta = -deltaY / 120.0f; // Invert for Unity's coordinate system
            }
            
            NSLog(@"[FLWallpaper] Scroll wheel: delta=%f, horizontal=%d", 
                  g_wheelDelta, (int)g_isHorizontalWheel);
            // 消费滚轮事件
            return NULL;
        }
            
        case kCGEventMouseMoved:
            NSLog(@"[FLWallpaper] Mouse moved to (%f, %f)", unityX, unityY);
            // 消费鼠标移动事件
            return NULL;
            
        default:
            break;
    }

    // Allow the event to pass through to other applications
    return event;
}

// Keyboard event callback
static CGEventRef FLKeyboardEventCallback(CGEventTapProxy proxy, CGEventType type,
                                          CGEventRef event, void *refcon) {
    if (!g_wallpaperOn || !g_tapEnabled) {
        return event;
    }

    CGKeyCode keyCode = (CGKeyCode)CGEventGetIntegerValueField(event, kCGKeyboardEventKeycode);
    
    switch (type) {
        case kCGEventKeyDown:
            g_keyDown = YES;
            g_lastKeyCode = keyCode;
            
            // Track modifier keys
            if (keyCode == 56) g_shiftPressed = YES;   // Left Shift
            if (keyCode == 60) g_shiftPressed = YES;   // Right Shift
            if (keyCode == 59) g_ctrlPressed = YES;    // Left Ctrl
            if (keyCode == 62) g_ctrlPressed = YES;    // Right Ctrl
            if (keyCode == 58) g_altPressed = YES;     // Option/Alt
            break;
            
        case kCGEventKeyUp:
            g_keyDown = NO;
            
            // Track modifier keys
            if (keyCode == 56) g_shiftPressed = NO;
            if (keyCode == 60) g_shiftPressed = NO;
            if (keyCode == 59) g_ctrlPressed = NO;
            if (keyCode == 62) g_ctrlPressed = NO;
            if (keyCode == 58) g_altPressed = NO;
            break;
            
        default:
            break;
    }

    return event;
}

static BOOL FLCreateEventTap(void) {
    if (g_eventTap != NULL) {
        return YES;
    }

    // 检查是否有辅助功能权限
    NSDictionary *options = @{(__bridge id)kAXTrustedCheckOptionPrompt: @YES};
    BOOL isTrusted = AXIsProcessTrustedWithOptions((__bridge CFDictionaryRef)options);
    
    if (!isTrusted) {
        NSLog(@"[FLWallpaper] 警告: 没有辅助功能权限，事件捕获可能无法工作");
        NSLog(@"[FLWallpaper] 请在系统设置 > 隐私与安全性 > 辅助功能 中启用本应用");
    }

    // Create mouse event tap
    CGEventMask mouseMask = (1 << kCGEventLeftMouseDown) |
                            (1 << kCGEventLeftMouseUp) |
                            (1 << kCGEventRightMouseDown) |
                            (1 << kCGEventRightMouseUp) |
                            (1 << kCGEventMouseMoved) |
                            (1 << kCGEventScrollWheel);

    // Create keyboard event tap
    CGEventMask keyboardMask = (1 << kCGEventKeyDown) | 
                               (1 << kCGEventKeyUp);

    g_eventTap = CGEventTapCreate(
        g_tapLocation,
        kCGHeadInsertEventTap,
        kCGEventTapOptionDefault,
        mouseMask | keyboardMask,
        FLMouseEventCallback,
        NULL
    );

    if (g_eventTap == NULL) {
        NSLog(@"[FLWallpaper] Failed to create event tap - may need accessibility permissions");
        NSLog(@"[FLWallpaper] 请在系统设置 > 隐私与安全性 > 辅助功能 中启用本应用");
        return NO;
    }

    // Add the event tap to the run loop
    CFRunLoopSourceRef runLoopSource = CFMachPortCreateRunLoopSource(
        kCFAllocatorDefault, 
        g_eventTap, 
        0
    );
    
    if (runLoopSource == NULL) {
        NSLog(@"[FLWallpaper] Failed to create run loop source");
        CFRelease(g_eventTap);
        g_eventTap = NULL;
        return NO;
    }

    CFRunLoopAddSource(CFRunLoopGetCurrent(), runLoopSource, kCFRunLoopCommonModes);
    CFRelease(runLoopSource);

    // Enable the event tap
    CGEventTapEnable(g_eventTap, true);
    g_tapEnabled = YES;

    NSLog(@"[FLWallpaper] Event tap created successfully");
    return YES;
}

static void FLDestroyEventTap(void) {
    if (g_eventTap != NULL) {
        CGEventTapEnable(g_eventTap, false);
        CFRelease(g_eventTap);
        g_eventTap = NULL;
        g_tapEnabled = NO;
        NSLog(@"[FLWallpaper] Event tap destroyed");
    }
}

#pragma mark - Exported C API

extern "C" {

// Enter wallpaper mode.
__attribute__((visibility("default")))
void _FLWallpaperEnter(void) {
    FLRunOnMain(^{ 
        FLApplyWallpaper(); 
        FLCreateEventTap();
    });
}

// Restore the saved (pre-wallpaper) window state.
__attribute__((visibility("default")))
void _FLWallpaperExit(void) {
    FLRunOnMain(^{ 
        FLDestroyEventTap();
        FLRestoreWindow(); 
    });
}

// Re-assert level + collection behavior. Called periodically by C# to recover
// if something (Spaces switch, Mission Control, third-party tools) bumped us.
__attribute__((visibility("default")))
void _FLWallpaperRefresh(void) {
    FLRunOnMain(^{
        if (!g_wallpaperOn) return;
        NSWindow *window = FLLocateUnityWindow();
        if (window == nil) return;

        NSInteger desiredLevel = FLDesiredWallpaperLevel();
        if ([window level] != desiredLevel) {
            [window setLevel:desiredLevel];
        }
        NSWindowCollectionBehavior desiredBehavior = FLDesiredWallpaperBehavior();
        if ([window collectionBehavior] != desiredBehavior) {
            [window setCollectionBehavior:desiredBehavior];
        }
    });
}

// 1 if wallpaper mode is currently active, 0 otherwise.
__attribute__((visibility("default")))
int _FLWallpaperIsActive(void) {
    return g_wallpaperOn ? 1 : 0;
}

// Get the main screen's frame, in points.
//   fullFrame != 0  -> [NSScreen mainScreen].frame (includes menu-bar strip)
//   fullFrame == 0  -> [NSScreen mainScreen].visibleFrame (excludes dock/menu)
// Returns 1 on success.
__attribute__((visibility("default")))
int _FLWallpaperGetMainScreenFrame(double *outX, double *outY,
                                   double *outW, double *outH,
                                   int fullFrame) {
    __block NSRect r  = NSZeroRect;
    __block BOOL   ok = NO;
    FLRunOnMain(^{
        NSScreen *screen = [NSScreen mainScreen];
        if (screen == nil) return;
        r  = (fullFrame != 0) ? [screen frame] : [screen visibleFrame];
        ok = YES;
    });
    if (!ok) return 0;
    if (outX != NULL) *outX = (double)r.origin.x;
    if (outY != NULL) *outY = (double)r.origin.y;
    if (outW != NULL) *outW = (double)r.size.width;
    if (outH != NULL) *outH = (double)r.size.height;
    return 1;
}

// Move the Unity window to a centred, normal-chrome windowed size taking
// `fraction` (0..1] of the screen's visible frame. Used when transitioning
// back to Windowed mode from wallpaper mode where we forced borderless.
__attribute__((visibility("default")))
void _FLWallpaperWindowedReset(double fraction) {
    FLRunOnMain(^{
        NSWindow *window = FLLocateUnityWindow();
        if (window == nil) return;
        NSScreen *screen = [NSScreen mainScreen];
        if (screen == nil) return;

        double f = (fraction > 0.0 && fraction <= 1.0) ? fraction : 0.8;
        NSRect vr = [screen visibleFrame];
        double w = vr.size.width  * f;
        double h = vr.size.height * f;
        double x = vr.origin.x + (vr.size.width  - w) * 0.5;
        double y = vr.origin.y + (vr.size.height - h) * 0.5;

        // Restore standard chrome if we were left borderless after exit.
        NSWindowStyleMask titledMask =
              NSWindowStyleMaskTitled
            | NSWindowStyleMaskClosable
            | NSWindowStyleMaskMiniaturizable
            | NSWindowStyleMaskResizable;
        if (([window styleMask] & NSWindowStyleMaskTitled) == 0) {
            [window setStyleMask:titledMask];
        }

        [window setLevel:NSNormalWindowLevel];
        [window setCollectionBehavior:NSWindowCollectionBehaviorDefault];
        [window setFrame:NSMakeRect(x, y, w, h) display:YES];
    });
}

// Mouse event forwarding API - similar to Windows SimpleMouseForwarder

// Get left click count (for Unity to detect clicks)
__attribute__((visibility("default")))
int _FLMouseGetClickCount(void) {
    return g_clickCount;
}

// Get right click count
__attribute__((visibility("default")))
int _FLMouseGetRightClickCount(void) {
    return g_rightClickCount;
}

// Get current mouse position (Unity coordinates)
__attribute__((visibility("default")))
void _FLMouseGetPosition(double *outX, double *outY) {
    if (outX != NULL) *outX = g_mouseX;
    if (outY != NULL) *outY = g_mouseY;
}

// Get left button state
__attribute__((visibility("default")))
int _FLMouseGetLeftButtonDown(void) {
    return g_leftButtonDown ? 1 : 0;
}

// Get right button state
__attribute__((visibility("default")))
int _FLMouseGetRightButtonDown(void) {
    return g_rightButtonDown ? 1 : 0;
}

// Get wheel delta (clears after reading)
__attribute__((visibility("default")))
float _FLMouseGetWheelDelta(int *isHorizontal) {
    float delta = g_wheelDelta;
    if (isHorizontal != NULL) *isHorizontal = g_isHorizontalWheel ? 1 : 0;
    g_wheelDelta = 0.0f;
    return delta;
}

// Reset click counters
__attribute__((visibility("default")))
void _FLMouseResetCounters(void) {
    g_clickCount = 0;
    g_rightClickCount = 0;
}

// Keyboard state API

// Get shift key state
__attribute__((visibility("default")))
int _FLKeyboardGetShiftPressed(void) {
    return g_shiftPressed ? 1 : 0;
}

// Get control key state
__attribute__((visibility("default")))
int _FLKeyboardGetControlPressed(void) {
    return g_ctrlPressed ? 1 : 0;
}

// Get alt/option key state
__attribute__((visibility("default")))
int _FLKeyboardGetAltPressed(void) {
    return g_altPressed ? 1 : 0;
}

// Get last key code
__attribute__((visibility("default")))
uint32_t _FLKeyboardGetLastKeyCode(void) {
    return g_lastKeyCode;
}

// Get key down state
__attribute__((visibility("default")))
int _FLKeyboardGetKeyDown(void) {
    return g_keyDown ? 1 : 0;
}

// Clear keyboard state
__attribute__((visibility("default")))
void _FLKeyboardClearState(void) {
    g_shiftPressed = NO;
    g_ctrlPressed = NO;
    g_altPressed = NO;
    g_lastKeyCode = 0;
    g_keyDown = NO;
}

} // extern "C"
