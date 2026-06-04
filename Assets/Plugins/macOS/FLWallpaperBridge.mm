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
#import <objc/runtime.h>
#import <objc/message.h>

// ---------------------------------------------------------------------------
// Subclass plumbing — enables native click delivery in wallpaper mode.
//
// macOS only delivers mouseDown to a window if EITHER:
//   (a) the window is already key, OR
//   (b) the view under the cursor returns YES from `acceptsFirstMouse:`.
// A borderless NSWindow returns NO from `canBecomeKeyWindow` by default, so
// AppKit consumes the first click trying to make it key (which fails) and
// the click never reaches Unity. That's why hover (mouse position polling)
// works but clicks don't.
//
// Fix: swap the runtime class of the NSWindow to a subclass that overrides
// canBecomeKeyWindow / canBecomeMainWindow to return YES, and swap the
// contentView's class to a dynamic subclass that overrides acceptsFirstMouse:
// to return YES. Both swaps are reverted on exit.
// ---------------------------------------------------------------------------

// FLWallpaperPanel: 继承自 NSPanel (不是 NSWindow)。
// 这是 Plash / WallpaperEngine 等开源 Mac 壁纸应用的核心做法 —— 桌面层
// (kCGDesktopIconWindowLevel-1) 上的普通 NSWindow 拿不到 click 事件,
// macOS WindowServer 直接当作 "点击壁纸" 处理. 只有 NSPanel 加
// NSWindowStyleMaskNonactivatingPanel 才能在低层级被路由 mouseDown.
//
// 用 object_setClass 把 Unity 的 NSWindow 实例换成 FLWallpaperPanel:
//   - NSPanel 是 NSWindow 子类, 没有新 ivar, 替换安全
//   - canBecomeKeyWindow 重写返回 YES (NSPanel 也默认对 borderless 返回 NO)
//   - canBecomeMainWindow 返回 NO (panel 不该成 main)
//   - 切完类后才能 setStyleMask 加 NonactivatingPanel flag —— 普通 NSWindow
//     不接受这个 flag
@interface FLWallpaperPanel : NSPanel
@end
@implementation FLWallpaperPanel
- (BOOL)canBecomeKeyWindow {
    NSLog(@"[FLLOG][PANEL] canBecomeKeyWindow asked -> YES");
    return YES;
}
- (BOOL)canBecomeMainWindow {
    NSLog(@"[FLLOG][PANEL] canBecomeMainWindow asked -> NO (panel)");
    return NO;
}
- (BOOL)acceptsFirstResponder {
    return YES;
}
- (void)becomeKeyWindow {
    NSLog(@"[FLLOG][PANEL] >>> becomeKeyWindow");
    [super becomeKeyWindow];
    NSLog(@"[FLLOG][PANEL] <<< becomeKeyWindow done isKey=%d", [self isKeyWindow]);
}
- (void)resignKeyWindow {
    NSLog(@"[FLLOG][PANEL] >>> resignKeyWindow");
    [super resignKeyWindow];
}
- (void)makeKeyAndOrderFront:(id)sender {
    NSLog(@"[FLLOG][PANEL] makeKeyAndOrderFront: called by %@", sender);
    [super makeKeyAndOrderFront:sender];
}
- (void)sendEvent:(NSEvent *)event {
    NSEventType t = [event type];
    if (t == NSEventTypeLeftMouseDown  || t == NSEventTypeLeftMouseUp
     || t == NSEventTypeRightMouseDown || t == NSEventTypeRightMouseUp
     || t == NSEventTypeOtherMouseDown || t == NSEventTypeOtherMouseUp
     || t == NSEventTypeKeyDown        || t == NSEventTypeKeyUp
     || t == NSEventTypeScrollWheel)
    {
        NSLog(@"[FLLOG][PANEL] sendEvent type=%lu loc=%@ isKey=%d",
              (unsigned long)t,
              NSStringFromPoint([event locationInWindow]),
              [self isKeyWindow]);
    }
    [super sendEvent:event];
}
@end

static BOOL FLAcceptsFirstMouseImpl(id self, SEL _cmd, NSEvent *event) {
    NSLog(@"[FLLOG][VIEW] acceptsFirstMouse called self=%@ event.type=%lu -> YES",
          self, event ? (unsigned long)[event type] : 0);
    return YES;
}

// Hook mouseDown so we know if Cocoa is actually delivering clicks to the view.
// We just log and forward to super so Unity still handles it.
static void FLMouseDownImpl(id self, SEL _cmd, NSEvent *event) {
    NSLog(@"[FLLOG][VIEW] *** mouseDown DELIVERED *** self=%@ loc=%@",
          self, NSStringFromPoint([event locationInWindow]));
    struct objc_super sup = { self, class_getSuperclass(object_getClass(self)) };
    ((void (*)(struct objc_super *, SEL, NSEvent *))objc_msgSendSuper)(&sup, _cmd, event);
}
static void FLMouseUpImpl(id self, SEL _cmd, NSEvent *event) {
    NSLog(@"[FLLOG][VIEW] *** mouseUp DELIVERED *** self=%@", self);
    struct objc_super sup = { self, class_getSuperclass(object_getClass(self)) };
    ((void (*)(struct objc_super *, SEL, NSEvent *))objc_msgSendSuper)(&sup, _cmd, event);
}
static void FLRightMouseDownImpl(id self, SEL _cmd, NSEvent *event) {
    NSLog(@"[FLLOG][VIEW] *** rightMouseDown DELIVERED *** self=%@", self);
    struct objc_super sup = { self, class_getSuperclass(object_getClass(self)) };
    ((void (*)(struct objc_super *, SEL, NSEvent *))objc_msgSendSuper)(&sup, _cmd, event);
}

static __strong Class g_savedWindowClass      = nil;
static __strong Class g_savedContentViewClass = nil;
static Class          g_wallpaperViewSubclass = nil;

// NSEvent monitors so we can see EVERY mouse event the app receives
// (regardless of which window targets it).
static __strong id    g_localMonitor          = nil;
static __strong id    g_globalMonitor         = nil;

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

#pragma mark - Diagnostic Logging

static void FLLogWindow(NSString *tag, NSWindow *w) {
    if (w == nil) {
        NSLog(@"[FLLOG][%@] window=nil", tag);
        return;
    }
    NSLog(@"[FLLOG][%@] win=%p class=%s level=%ld style=0x%lx coll=0x%lx "
          @"isKey=%d isMain=%d isVisible=%d canBecomeKey=%d canBecomeMain=%d "
          @"ignoresMouse=%d acceptsMoved=%d frame=%@ contentView=%s",
          tag, w,
          class_getName([w class]),
          (long)[w level],
          (unsigned long)[w styleMask],
          (unsigned long)[w collectionBehavior],
          [w isKeyWindow], [w isMainWindow], [w isVisible],
          [w canBecomeKeyWindow], [w canBecomeMainWindow],
          [w ignoresMouseEvents], [w acceptsMouseMovedEvents],
          NSStringFromRect([w frame]),
          [w contentView] ? class_getName([[w contentView] class]) : "nil");
}

static void FLLogAllWindows(NSString *tag) {
    NSArray *windows = [NSApp windows];
    NSLog(@"[FLLOG][%@] ==== all NSWindows count=%lu keyWindow=%p mainWindow=%p ====",
          tag, (unsigned long)[windows count], [NSApp keyWindow], [NSApp mainWindow]);
    NSInteger i = 0;
    for (NSWindow *w in windows) {
        NSLog(@"[FLLOG][%@][#%ld] win=%p class=%s level=%ld style=0x%lx "
              @"isKey=%d isMain=%d isVisible=%d frame=%@",
              tag, (long)i++, w,
              class_getName([w class]),
              (long)[w level],
              (unsigned long)[w styleMask],
              [w isKeyWindow], [w isMainWindow], [w isVisible],
              NSStringFromRect([w frame]));
    }
    NSRunningApplication *front = [[NSWorkspace sharedWorkspace] frontmostApplication];
    NSLog(@"[FLLOG][%@] frontmostApp=%@ bundleID=%@ isActive=%d",
          tag, [front localizedName], [front bundleIdentifier], [front isActive]);
    NSLog(@"[FLLOG][%@] NSApp.isActive=%d activationPolicy=%ld",
          tag, [NSApp isActive], (long)[NSApp activationPolicy]);
}

static void FLInstallEventMonitors(void) {
    if (g_localMonitor != nil) return;

    NSEventMask mask = NSEventMaskLeftMouseDown  | NSEventMaskLeftMouseUp
                     | NSEventMaskRightMouseDown | NSEventMaskRightMouseUp
                     | NSEventMaskOtherMouseDown | NSEventMaskOtherMouseUp
                     | NSEventMaskScrollWheel
                     | NSEventMaskKeyDown        | NSEventMaskKeyUp;

    // Local monitor: events targeted at OUR app. We see them BEFORE the window
    // chain. Return event = pass through; nil = swallow.
    g_localMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:mask
                                                           handler:^NSEvent *(NSEvent *event) {
        NSWindow *w = [event window];
        NSLog(@"[FLLOG][LOCAL] type=%lu loc=%@ window=%p winClass=%s winLevel=%ld",
              (unsigned long)[event type],
              NSStringFromPoint([event locationInWindow]),
              w,
              w ? class_getName([w class]) : "nil",
              w ? (long)[w level] : 0);
        return event;
    }];

    // Global monitor: events targeted at OTHER apps. Tells us if clicks are
    // being delivered somewhere else when our app should be receiving them.
    g_globalMonitor = [NSEvent addGlobalMonitorForEventsMatchingMask:mask
                                                             handler:^(NSEvent *event) {
        NSLog(@"[FLLOG][GLOBAL] type=%lu mouseLoc=%@ — event went to ANOTHER app",
              (unsigned long)[event type],
              NSStringFromPoint([NSEvent mouseLocation]));
    }];

    NSLog(@"[FLLOG] installed local + global event monitors (local=%p global=%p)",
          g_localMonitor, g_globalMonitor);
}

static void FLRemoveEventMonitors(void) {
    if (g_localMonitor != nil) {
        [NSEvent removeMonitor:g_localMonitor];
        g_localMonitor = nil;
    }
    if (g_globalMonitor != nil) {
        [NSEvent removeMonitor:g_globalMonitor];
        g_globalMonitor = nil;
    }
    NSLog(@"[FLLOG] removed event monitors");
}

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

#pragma mark - Class Swap (enables click delivery to borderless window)

static void FLEnableNativeClickDelivery(NSWindow *window) {
    if (window == nil) {
        NSLog(@"[FLLOG][SWAP] window=nil — abort enable");
        return;
    }
    NSLog(@"[FLLOG][SWAP] === FLEnableNativeClickDelivery BEGIN ===");
    FLLogWindow(@"SWAP-BEFORE", window);

    // 1) Swap NSWindow class so canBecomeKeyWindow returns YES.
    Class wndClass = [window class];
    if (wndClass != [FLWallpaperPanel class]) {
        if (g_savedWindowClass == nil) {
            g_savedWindowClass = wndClass;
        }
        object_setClass(window, [FLWallpaperPanel class]);
        NSLog(@"[FLLOG][SWAP] Window class %s -> FLWallpaperPanel (NSPanel subclass)",
              class_getName(wndClass));
    } else {
        NSLog(@"[FLLOG][SWAP] Window already FLWallpaperPanel — skip");
    }

    // Sanity check after window swap.
    NSLog(@"[FLLOG][SWAP] post-window-swap canBecomeKey=%d canBecomeMain=%d",
          [window canBecomeKeyWindow], [window canBecomeMainWindow]);

    // 2) Swap contentView class so acceptsFirstMouse: returns YES.
    NSView *cv = [window contentView];
    if (cv == nil) {
        NSLog(@"[FLLOG][SWAP] contentView=nil — view swap skipped");
        return;
    }
    NSLog(@"[FLLOG][SWAP] contentView=%p class=%s superclass=%s",
          cv, class_getName([cv class]),
          class_getName(class_getSuperclass([cv class])));

    Class viewClass = [cv class];
    if (g_wallpaperViewSubclass != nil && viewClass == g_wallpaperViewSubclass) {
        NSLog(@"[FLLOG][SWAP] contentView already swapped — skip");
        return;
    }

    if (g_savedContentViewClass == nil) {
        g_savedContentViewClass = viewClass;
    }

    if (g_wallpaperViewSubclass == nil
        || class_getSuperclass(g_wallpaperViewSubclass) != viewClass)
    {
        NSString *name = [NSString stringWithFormat:@"FLWallpaperContentView_%s",
                          class_getName(viewClass)];
        const char *cName = [name UTF8String];
        Class existing = objc_getClass(cName);
        if (existing != nil) {
            g_wallpaperViewSubclass = existing;
            NSLog(@"[FLLOG][SWAP] reusing existing view subclass %s", cName);
        } else {
            Class sub = objc_allocateClassPair(viewClass, cName, 0);
            if (sub != nil) {
                // acceptsFirstMouse: → YES (lets clicks through when window isn't key)
                class_addMethod(sub, @selector(acceptsFirstMouse:),
                                (IMP)FLAcceptsFirstMouseImpl, "B@:@");
                // mouseDown:/Up:/rightMouseDown: → log + call super
                // Tells us if AppKit actually reaches the view with the click.
                class_addMethod(sub, @selector(mouseDown:),
                                (IMP)FLMouseDownImpl, "v@:@");
                class_addMethod(sub, @selector(mouseUp:),
                                (IMP)FLMouseUpImpl, "v@:@");
                class_addMethod(sub, @selector(rightMouseDown:),
                                (IMP)FLRightMouseDownImpl, "v@:@");
                objc_registerClassPair(sub);
                g_wallpaperViewSubclass = sub;
                NSLog(@"[FLLOG][SWAP] created dynamic view subclass %s "
                      @"(injected acceptsFirstMouse:, mouseDown:, mouseUp:, rightMouseDown:)",
                      cName);
            } else {
                NSLog(@"[FLLOG][SWAP] !!! objc_allocateClassPair FAILED for %s", cName);
            }
        }
    }

    if (g_wallpaperViewSubclass != nil) {
        object_setClass(cv, g_wallpaperViewSubclass);
        NSLog(@"[FLLOG][SWAP] ContentView class %s -> %s",
              class_getName(viewClass),
              class_getName(g_wallpaperViewSubclass));
    }

    FLLogWindow(@"SWAP-AFTER", window);
    NSLog(@"[FLLOG][SWAP] === FLEnableNativeClickDelivery END ===");
}

static void FLDisableNativeClickDelivery(NSWindow *window) {
    if (window == nil) return;

    NSView *cv = [window contentView];
    if (cv != nil && g_savedContentViewClass != nil
        && [cv class] == g_wallpaperViewSubclass)
    {
        object_setClass(cv, g_savedContentViewClass);
    }
    g_savedContentViewClass = nil;

    if (g_savedWindowClass != nil && [window class] == [FLWallpaperPanel class]) {
        object_setClass(window, g_savedWindowClass);
    }
    g_savedWindowClass = nil;
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
    NSLog(@"[FLLOG] ============================================");
    NSLog(@"[FLLOG] ====== FLApplyWallpaper ENTER ==============");
    NSLog(@"[FLLOG] ============================================");
    FLLogAllWindows(@"APPLY-IN");

    NSWindow *window = FLLocateUnityWindow();
    if (window == nil) {
        NSLog(@"[FLLOG][APPLY] !!! no Unity NSWindow located — abort");
        return;
    }
    FLLogWindow(@"APPLY-INITIAL", window);

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

    // *** ORDER MATTERS *** —— 三步:
    //   (a) 在原 PlayerWindow class 上先 setStyleMask(Borderless),让 AppKit 在
    //       正确的类身份下清理掉 titlebar 和 NSTitlebarView 的 KVO observer。
    //       如果先 swap class 再去 titled,unregister observer 时 KVO 因为类变了
    //       识别不到 observer,会抛 NSRangeException 导致 app 崩。
    //   (b) class swap 到 FLWallpaperPanel (NSPanel)
    //   (c) setStyleMask 再加上 NonactivatingPanel flag (NSPanel-only,普通
    //       NSWindow 不接受这个 flag,会被静默丢弃)

    // (a) 在原 class 上先去掉 titled,触发 AppKit 自己清理 titlebar 子视图
    NSWindowStyleMask preSwapMask = NSWindowStyleMaskBorderless;
    if ([window styleMask] != preSwapMask) {
        NSLog(@"[FLLOG][APPLY] (a) pre-swap setStyleMask 0x%lx -> 0x%lx (Borderless,在原 class 上)",
              (unsigned long)[window styleMask], (unsigned long)preSwapMask);
        [window setStyleMask:preSwapMask];
        NSLog(@"[FLLOG][APPLY] (a) after pre-swap styleMask=0x%lx",
              (unsigned long)[window styleMask]);
    }

    // (b) class swap 到 FLWallpaperPanel + 给 contentView 注入 acceptsFirstMouse:
    FLEnableNativeClickDelivery(window);

    // (c) NSPanel-only 的 NonactivatingPanel —— 这是让 macOS 在桌面层把
    //     点击路由到我们窗口的关键。
    NSWindowStyleMask finalMask = NSWindowStyleMaskBorderless | NSWindowStyleMaskNonactivatingPanel;
    if ([window styleMask] != finalMask) {
        NSLog(@"[FLLOG][APPLY] (c) post-swap setStyleMask 0x%lx -> 0x%lx (+NonactivatingPanel)",
              (unsigned long)[window styleMask], (unsigned long)finalMask);
        [window setStyleMask:finalMask];
        NSLog(@"[FLLOG][APPLY] (c) final styleMask = 0x%lx", (unsigned long)[window styleMask]);
    }

    // 3) NSPanel-only: becomesKeyOnlyIfNeeded 让窗口只在真的需要键盘输入时才抢 key,
    //    不抢 menubar / 应用焦点。
    if ([window respondsToSelector:@selector(setBecomesKeyOnlyIfNeeded:)]) {
        NSLog(@"[FLLOG][APPLY] setBecomesKeyOnlyIfNeeded:YES");
        [(NSPanel *)window setBecomesKeyOnlyIfNeeded:YES];
    }

    // 4) 现在才设 level / collection / frame —— 这些不影响 click routing,
    //    放后面避免和 class swap 互相干扰。
    NSLog(@"[FLLOG][APPLY] setLevel(%ld)", (long)FLDesiredWallpaperLevel());
    [window setLevel:FLDesiredWallpaperLevel()];
    NSLog(@"[FLLOG][APPLY] setCollectionBehavior(0x%lx)", (unsigned long)FLDesiredWallpaperBehavior());
    [window setCollectionBehavior:FLDesiredWallpaperBehavior()];

    NSScreen *screen = [NSScreen mainScreen];
    if (screen != nil) {
        NSLog(@"[FLLOG][APPLY] setFrame=%@", NSStringFromRect([screen frame]));
        [window setFrame:[screen frame] display:YES];
    }

    [window setAcceptsMouseMovedEvents:YES];
    [window setIgnoresMouseEvents:NO];  // 确保接收鼠标事件(默认就是 NO,显式写出来防意外)
    NSLog(@"[FLLOG][APPLY] setAcceptsMouseMovedEvents:YES setIgnoresMouseEvents:NO");

    // 5) 装事件监视器
    FLInstallEventMonitors();

    // 6) orderBack 不会改变 click routing(NonactivatingPanel 让我们仍然能收到 click),
    //    只是 z-order 上往后排,避免遮其他窗口。
    NSLog(@"[FLLOG][APPLY] orderBack:nil");
    [window orderBack:nil];
    FLLogWindow(@"APPLY-AFTER-ORDERBACK", window);

    g_wallpaperOn = YES;
    NSLog(@"[FLLOG][APPLY] g_wallpaperOn=YES");
    FLLogAllWindows(@"APPLY-DONE");
    NSLog(@"[FLLOG] ====== FLApplyWallpaper EXIT ===============");
}

static void FLRestoreWindow(void) {
    NSLog(@"[FLLOG] ====== FLRestoreWindow ENTER ===============");
    NSWindow *window = FLLocateUnityWindow();
    if (window == nil) {
        NSLog(@"[FLLOG][RESTORE] no window — abort");
        g_wallpaperOn = NO;
        return;
    }
    FLLogWindow(@"RESTORE-BEFORE", window);

    // Remove NSEvent monitors first so we stop logging on the way out.
    FLRemoveEventMonitors();

    // *** ORDER MATTERS (反过来) *** —— Apply 是 borderless -> swap -> +NonactivatingPanel
    // Restore 必须是 -NonactivatingPanel -> swap back -> 恢复原 styleMask
    // 不然如果直接在 NSPanel 上恢复成 Titled,AppKit 加 titlebar 时 KVO 又会和
    // FLWallpaperPanel 类 mismatch 崩.
    //
    // (c-reverse) 先在 NSPanel 类上 setStyleMask(Borderless),去掉 NonactivatingPanel
    NSWindowStyleMask plainBorderless = NSWindowStyleMaskBorderless;
    if ([window styleMask] != plainBorderless) {
        NSLog(@"[FLLOG][RESTORE] (c-rev) setStyleMask 0x%lx -> 0x%lx (Borderless,在 NSPanel 上去掉 NonactivatingPanel)",
              (unsigned long)[window styleMask], (unsigned long)plainBorderless);
        [window setStyleMask:plainBorderless];
    }

    // (b-reverse) swap class back to PlayerWindow + 还 contentView
    FLDisableNativeClickDelivery(window);

    if (g_savedValid) {
        [window setLevel:g_savedLevel];
        [window setCollectionBehavior:g_savedBehavior];

        // (a-reverse) 现在 class 已经回到原 PlayerWindow,可以安全地恢复 titled styleMask
        if ([window styleMask] != g_savedStyle) {
            NSLog(@"[FLLOG][RESTORE] (a-rev) setStyleMask 0x%lx -> 0x%lx (恢复原 styleMask)",
                  (unsigned long)[window styleMask], (unsigned long)g_savedStyle);
            [window setStyleMask:g_savedStyle];
        }
        [window setFrame:g_savedFrame display:YES];
    } else {
        [window setLevel:NSNormalWindowLevel];
        [window setCollectionBehavior:NSWindowCollectionBehaviorDefault];
    }

    g_wallpaperOn = NO;
    g_savedValid  = NO;
    FLLogWindow(@"RESTORE-AFTER", window);
    NSLog(@"[FLLOG] ====== FLRestoreWindow EXIT ================");
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

    // IMPORTANT: we return `event` (not NULL) for every case below. Consuming
    // the event at HID level was the original cause of clicks not reaching
    // Unity — without consumption the NSEvent flows normally, AppKit hands it
    // to FLWallpaperPanel (which can now become key + has NonactivatingPanel),
    // the click is delivered to the contentView (which now returns YES from
    // acceptsFirstMouse:), and Unity's Input.GetMouseButton* / EventSystem see
    // the click natively.
    //
    // We still update the counters so legacy code that reads
    // MouseForwarder.clickCount keeps working as a redundant signal.
    switch (type) {
        case kCGEventLeftMouseDown:
            g_leftButtonDown = YES;
            g_clickCount++;
            break;

        case kCGEventLeftMouseUp:
            g_leftButtonDown = NO;
            break;

        case kCGEventRightMouseDown:
            g_rightButtonDown = YES;
            g_rightClickCount++;
            break;

        case kCGEventRightMouseUp:
            g_rightButtonDown = NO;
            break;

        case kCGEventScrollWheel: {
            CGFloat deltaX = CGEventGetIntegerValueField(event, kCGScrollWheelEventDeltaAxis1);
            CGFloat deltaY = CGEventGetIntegerValueField(event, kCGScrollWheelEventDeltaAxis2);
            if (deltaX != 0) {
                g_isHorizontalWheel = YES;
                g_wheelDelta = deltaX / 120.0f;
            } else {
                g_isHorizontalWheel = NO;
                g_wheelDelta = -deltaY / 120.0f;
            }
            break;
        }

        case kCGEventMouseMoved:
        default:
            break;
    }

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
            NSLog(@"[FLLOG][REFRESH] level drifted %ld -> resetting to %ld",
                  (long)[window level], (long)desiredLevel);
            [window setLevel:desiredLevel];
        }
        NSWindowCollectionBehavior desiredBehavior = FLDesiredWallpaperBehavior();
        if ([window collectionBehavior] != desiredBehavior) {
            NSLog(@"[FLLOG][REFRESH] collectionBehavior drifted 0x%lx -> 0x%lx",
                  (unsigned long)[window collectionBehavior],
                  (unsigned long)desiredBehavior);
            [window setCollectionBehavior:desiredBehavior];
        }
        // Periodic heartbeat with current key/main + class.
        NSLog(@"[FLLOG][REFRESH] heartbeat winClass=%s level=%ld isKey=%d "
              @"isMain=%d frontApp=%@ activeApp=%d",
              class_getName([window class]),
              (long)[window level], [window isKeyWindow], [window isMainWindow],
              [[[NSWorkspace sharedWorkspace] frontmostApplication] localizedName],
              [NSApp isActive]);
    });
}

// Compile-time stamp so C# can verify the bundle is freshly built.
// If C# can't find this symbol the bundle is stale.
__attribute__((visibility("default")))
const char *_FLWallpaperBuildStamp(void) {
    return "FLWallpaperBridge rev=9-fix-titlebar-KVO-crash " __DATE__ " " __TIME__;
}

// On-demand full diagnostic dump. C# calls this when it wants a snapshot
// of the native state in Player.log.
__attribute__((visibility("default")))
void _FLDiagnose(void) {
    FLRunOnMain(^{
        NSLog(@"[FLLOG] @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
        NSLog(@"[FLLOG] @@@@@@@@ _FLDiagnose snapshot @@@@@@@@@@@@@@");
        NSLog(@"[FLLOG] @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
        NSLog(@"[FLLOG][DIAG] g_wallpaperOn=%d g_savedValid=%d g_savedLevel=%ld "
              @"savedWindowClass=%s savedContentViewClass=%s viewSubclass=%s",
              g_wallpaperOn, g_savedValid, (long)g_savedLevel,
              g_savedWindowClass ? class_getName(g_savedWindowClass) : "nil",
              g_savedContentViewClass ? class_getName(g_savedContentViewClass) : "nil",
              g_wallpaperViewSubclass ? class_getName(g_wallpaperViewSubclass) : "nil");
        NSLog(@"[FLLOG][DIAG] g_eventTap=%p g_tapEnabled=%d g_localMonitor=%p g_globalMonitor=%p",
              g_eventTap, g_tapEnabled, g_localMonitor, g_globalMonitor);
        NSLog(@"[FLLOG][DIAG] mouseLocation=%@",
              NSStringFromPoint([NSEvent mouseLocation]));
        FLLogAllWindows(@"DIAG");
        NSWindow *uw = FLLocateUnityWindow();
        if (uw != nil) {
            FLLogWindow(@"DIAG-UNITY-WIN", uw);
            NSView *cv = [uw contentView];
            if (cv != nil) {
                NSLog(@"[FLLOG][DIAG-VIEW] view=%p class=%s superclass=%s "
                      @"frame=%@ window=%p",
                      cv, class_getName([cv class]),
                      class_getName(class_getSuperclass([cv class])),
                      NSStringFromRect([cv frame]), [cv window]);
                // Probe acceptsFirstMouse: result without an event
                BOOL afm = [cv acceptsFirstMouse:nil];
                NSLog(@"[FLLOG][DIAG-VIEW] probe acceptsFirstMouse:nil -> %d", afm);
            }
        }
        NSLog(@"[FLLOG] @@@@@@@@ end snapshot @@@@@@@@@@@@@@@@@@@@@@");
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
