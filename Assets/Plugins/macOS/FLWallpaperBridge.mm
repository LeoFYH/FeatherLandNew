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
#include <unistd.h>   // getpid(窗口覆盖检测)

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

// FLBorderlessWindow: 无边框全屏模式用的 NSWindow 子类(不是 NSPanel!)。
//
// rev=14/15 实测日志证明: 壁纸往返之后再走 Unity 的 Screen.fullScreen=true
// (toggleFullScreen 建原生全屏 Space), 新 Space 不给窗口路由任何 mouseDown ——
// 窗口 isKey=1/isMain=1/app active/位置在更新, 点击就是进不来, 连翻转
// ignoresMouseEvents 强制重建路由都救不回。所以全屏模式改为无边框全屏窗
// (等价 Win 端 WS_POPUP + HWND_TOP), 彻底绕开 fullscreen Space。
//
// 为什么必须换类: PlayerWindow 变 Borderless 后 canBecomeKeyWindow 返回 NO
// (rev=14 日志 SWAP-BEFORE 实测 canBecomeKey=0), 键盘/点击都会失效。
// 用 NSWindow 子类而不是复用 FLWallpaperPanel, 是避免 NSPanel 的 onClick
// 漏发怪癖(见 FLClickProbe 的 PhotoPopup 兜底)污染全屏模式。
@interface FLBorderlessWindow : NSWindow
@end
@implementation FLBorderlessWindow
- (BOOL)canBecomeKeyWindow  { return YES; }
- (BOOL)canBecomeMainWindow { return YES; }
// 不让 AppKit 把全屏 frame 往菜单栏下面压
- (NSRect)constrainFrameRect:(NSRect)frameRect toScreen:(NSScreen *)screen {
    return frameRect;
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

// 无边框全屏(rev=16)状态: 进入时保存原窗口类(PlayerWindow), 退出时换回。
static __strong Class        g_fsSavedWindowClass = nil;
static BOOL                  g_borderlessFsOn     = NO;

// Mouse event forwarding state
static CFMachPortRef         g_eventTap      = NULL;   // 鼠标 tap(点击计数/滚轮)
static CFMachPortRef         g_keyTap        = NULL;   // 键盘 tap(rev=23 起独立,无辅助功能权限时不建)
static CGEventTapLocation    g_tapLocation   = kCGHIDEventTap;
static BOOL                  g_tapEnabled    = NO;

// Mouse state tracking for Unity
static volatile int          g_clickCount    = 0;
static volatile int          g_rightClickCount = 0;
static volatile float        g_wheelDelta    = 0.0f;
static volatile BOOL         g_isHorizontalWheel = NO;
static volatile double       g_mouseX        = 0.0;
static volatile double       g_mouseY        = 0.0;
// 原始 CG 屏幕坐标(左上原点,点单位),供窗口覆盖检测用 —— 与 CGWindowList 的
// kCGWindowBounds 同一坐标系,避免 C# 侧来回换算
static volatile double       g_rawMouseX     = 0.0;
static volatile double       g_rawMouseY     = 0.0;
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
    // *** 之前用 kCGDesktopIconWindowLevel - 1 (桌面图标下方),希望保留 Mac
    //     桌面图标可见。 实测 macOS 在这一层不路由 click:
    //     Finder 在 kCGDesktopIconWindowLevel 有一个覆盖全屏的桌面窗口,
    //     WindowServer 优先把 click 给 Finder, 我们永远收不到 mouseDown,
    //     log 里全是 [FLLOG][GLOBAL] event went to ANOTHER app.
    //
    // *** 现在用 kCGDesktopIconWindowLevel + 1 —— 把窗口放到 Finder 桌面窗口
    //     之上, click 直接路由进来, 不需要 Accessibility 权限。代价是
    //     Mac 桌面图标会被游戏窗口遮住 (用户在产品决策里选择了这条路)。
    //
    // *** 仍然在 kCGNormalWindowLevel (= 0) 下方很多, 所以正常 app 窗口
    //     一开就在我们上面, 仍然有 "壁纸" 的视觉效果。
    return CGWindowLevelForKey(kCGDesktopIconWindowLevelKey) + 1;
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

// CGEvent 屏幕坐标(左上原点,单位:点 pt)-> 左下原点"点"坐标(只翻转 Y,不缩放)。
// rev=22:不再在原生侧乘 backingScaleFactor —— rev=21 盲乘 scale 押注 Unity
// 后备缓冲一定是 Retina 原生像素,一旦实际不是(窗口背板未挂 Retina/缩放模式差异),
// 所有坐标偏 2 倍,FindDragTarget/ForwardWheelToUI 的 raycast 全体脱靶,拖拽直接
// 全废,还会经 isDraggingFromHook 压制原本可用的原生 EventSystem 拖拽。
// 现在原生只报"点",C# 侧(SimpleMouseForwarderMac)用 Screen.width/height ÷
// _FLWallpaperGetMainScreenFrame 点尺寸的实测比值换算成 Unity 像素 —— 与真实
// 后备缓冲严格一致,Retina 开/关/任意缩放模式都正确。C# 按 BuildStamp 的 rev
// 区分新旧 bundle:rev>=22 走点坐标换算,旧 bundle 维持原样(ABI 兼容)。
// (点击不受影响:点击走原生 NSEvent 流,由 Unity 自己换算坐标。)
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

#pragma mark - Borderless Fullscreen (rev=16, 绕开 macOS 原生全屏 Space)

// 退出无边框全屏: 恢复菜单栏/Dock + 把窗口类换回 PlayerWindow。
// 幂等; 不动 styleMask/frame(交给后续的 windowedReset / wallpaperApply)。
// 必须在任何 "titled styleMask 恢复" 之前调用 —— titled 位只能在原始
// PlayerWindow 类上动, 否则 titlebar 的 KVO 会因类不匹配崩(同壁纸路径的教训)。
static void FLExitBorderlessFullscreenInternal(void) {
    if (!g_borderlessFsOn) return;

    [NSApp setPresentationOptions:NSApplicationPresentationDefault];

    NSWindow *window = FLLocateUnityWindow();
    if (window != nil && [window class] == [FLBorderlessWindow class]
        && g_fsSavedWindowClass != nil)
    {
        object_setClass(window, g_fsSavedWindowClass);
        NSLog(@"[FLLOG][BFS-EXIT] window class FLBorderlessWindow -> %s",
              class_getName(g_fsSavedWindowClass));
    }
    g_fsSavedWindowClass = nil;
    g_borderlessFsOn = NO;

    FLRemoveEventMonitors();
    NSLog(@"[FLLOG][BFS-EXIT] borderless fullscreen OFF, presentationOptions restored");
}

// 进入无边框全屏: Borderless + 撑满 [NSScreen frame] + 普通层级 +
// 自动隐藏菜单栏/Dock。等价 Windows 端 FullscreenMode 的 WS_POPUP + HWND_TOP。
// 不创建 fullscreen Space, 所以不受 "壁纸往返后 Space 不路由点击" 影响。
static void FLEnterBorderlessFullscreenInternal(void) {
    if (g_wallpaperOn) {
        NSLog(@"[FLLOG][BFS] skipped — wallpaper is ON (safe guard)");
        return;
    }
    NSWindow *window = FLLocateUnityWindow();
    if (window == nil) {
        NSLog(@"[FLLOG][BFS] no Unity window — abort");
        return;
    }
    FLLogWindow(@"BFS-BEFORE", window);

    // 还在原生全屏 Space 里(比如启动即全屏)就先退出来, 复用壁纸入口的等待逻辑
    if ([window styleMask] & NSWindowStyleMaskFullScreen) {
        NSLog(@"[FLLOG][BFS] window in native fullscreen — toggling out first");
        [window toggleFullScreen:nil];
        FLWaitForFullScreenExit(window);
    }

    // *** 超时守卫(评审确认的 critical) ***: 全屏过渡进行中时 toggleFullScreen
    // 会被 AppKit 忽略, FLWaitForFullScreenExit 2s 超时后 FullScreen 位仍置位。
    // 此时绝不能 setStyleMask —— 在全屏过渡之外增删 FullScreen 位会抛
    // NSGenericException(FLRestoreWindow 注释里就是这个异常), ObjC 异常穿过
    // IL2CPP 边界没人接, 直接崩 app。放弃本次(不设任何状态), 用户再切一次即可。
    if ([window styleMask] & NSWindowStyleMaskFullScreen) {
        NSLog(@"[FLLOG][BFS] !!! still in native fullscreen after wait — abort, retry later");
        return;
    }

    if (!g_borderlessFsOn) {
        // titled -> borderless 必须在原始类上做(KVO titlebar 教训), 再换类
        if ([window styleMask] != NSWindowStyleMaskBorderless) {
            NSLog(@"[FLLOG][BFS] setStyleMask 0x%lx -> Borderless (在原 class 上)",
                  (unsigned long)[window styleMask]);
            [window setStyleMask:NSWindowStyleMaskBorderless];
        }
        Class c = [window class];
        if (c != [FLBorderlessWindow class]) {
            g_fsSavedWindowClass = c;
            object_setClass(window, [FLBorderlessWindow class]);
            NSLog(@"[FLLOG][BFS] window class %s -> FLBorderlessWindow",
                  class_getName(c));
        }
    }

    // 自动隐藏菜单栏 + Dock(macOS 要求两个 flag 必须成对)
    [NSApp setPresentationOptions:
        (NSApplicationPresentationAutoHideMenuBar | NSApplicationPresentationAutoHideDock)];

    [window setLevel:NSNormalWindowLevel];
    [window setCollectionBehavior:NSWindowCollectionBehaviorDefault];

    NSScreen *screen = [NSScreen mainScreen];
    if (screen != nil) {
        [window setFrame:[screen frame] display:YES];
    }

    [window setAcceptsMouseMovedEvents:YES];
    [window setIgnoresMouseEvents:NO];

    // 诊断监视器留着: 如果还有点击问题, 下一份日志能直接看到每次点击去了哪
    FLInstallEventMonitors();

    [NSApp activateIgnoringOtherApps:YES];
    [window makeKeyAndOrderFront:nil];

    g_borderlessFsOn = YES;
    FLLogWindow(@"BFS-AFTER", window);
    NSLog(@"[FLLOG][BFS] borderless fullscreen ON");
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

    // rev=16: 如果当前在无边框全屏, 先退出来(类换回 PlayerWindow + 恢复菜单栏),
    // 保证壁纸机器永远从原始 PlayerWindow 状态出发, class swap 保存/恢复不会串。
    FLExitBorderlessFullscreenInternal();

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

    // *** 超时守卫(同 BFS, 评审确认的 critical) ***: 等待超时后 FullScreen 位
    // 仍置位的话, 下面的 setStyleMask(Borderless) 会在全屏过渡外清 FullScreen 位
    // → NSGenericException → 崩 app。放弃本次进入(g_wallpaperOn 保持 NO,
    // C# 侧会据此回退到全屏模式)。
    if ([window styleMask] & NSWindowStyleMaskFullScreen) {
        NSLog(@"[FLLOG][APPLY] !!! still in native fullscreen after wait — abort wallpaper enter");
        return;
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

    // NSPanel-only 残留清理: 进壁纸时设了 becomesKeyOnlyIfNeeded:YES,
    // 这个 flag 存在 window 实例上,换回 PlayerWindow 类之前拨回 NO,
    // 避免残留影响退出壁纸后点击/成 key 的行为。
    if ([window respondsToSelector:@selector(setBecomesKeyOnlyIfNeeded:)]) {
        [(NSPanel *)window setBecomesKeyOnlyIfNeeded:NO];
        NSLog(@"[FLLOG][RESTORE] setBecomesKeyOnlyIfNeeded:NO (清 NSPanel 残留)");
    }

    // (b-reverse) swap class back to PlayerWindow + 还 contentView
    FLDisableNativeClickDelivery(window);

    if (g_savedValid) {
        [window setLevel:g_savedLevel];
        [window setCollectionBehavior:g_savedBehavior];

        // (a-reverse) 现在 class 已经回到原 PlayerWindow,可以恢复 styleMask。
        // 注意:必须 *去掉* NSWindowStyleMaskFullScreen 这位。macOS 不允许
        // 直接 setStyleMask 把这一位写回去 ("set on a window outside of a
        // full screen transition")。如果之前是 fullscreen,C# 层调
        // _FLWallpaperExit() 之后会立即 Screen.fullScreenMode =
        // FullScreenWindow / Screen.fullScreen=true,Unity 会通过正常的
        // toggleFullScreen: 路径重新进 fullscreen。
        NSWindowStyleMask safeStyle = g_savedStyle & ~NSWindowStyleMaskFullScreen;
        if ([window styleMask] != safeStyle) {
            NSLog(@"[FLLOG][RESTORE] (a-rev) setStyleMask 0x%lx -> 0x%lx "
                  @"(savedStyle=0x%lx, 已去掉 FullScreen flag 防 NSGenericException)",
                  (unsigned long)[window styleMask],
                  (unsigned long)safeStyle,
                  (unsigned long)g_savedStyle);
            [window setStyleMask:safeStyle];
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

// 前置声明:键盘事件与鼠标共用一个 tap,由本回调分发过去
static CGEventRef FLKeyboardEventCallback(CGEventTapProxy proxy, CGEventType type,
                                          CGEventRef event, void *refcon);

// CGEventTap callback - captures global mouse events
static CGEventRef FLMouseEventCallback(CGEventTapProxy proxy, CGEventType type,
                                       CGEventRef event, void *refcon) {
    // rev=21 自愈:Unity 主线程卡顿(换图/GC)超阈值时系统会禁用 tap 且不再回调,
    // 不重启的话鼠标状态全冻结,壁纸输入整体死掉直到重进壁纸("时好时坏"元凶之一)。
    if (type == kCGEventTapDisabledByTimeout || type == kCGEventTapDisabledByUserInput) {
        if (g_eventTap != NULL) {
            CGEventTapEnable(g_eventTap, true);
            NSLog(@"[FLWallpaper] event tap 被系统禁用(type=%d),已自动重启", (int)type);
        }
        return event;
    }

    // Only process events when wallpaper mode is active
    if (!g_wallpaperOn || !g_tapEnabled) {
        return event;
    }

    // rev=23: 键盘事件不再进本 tap(独立成 g_keyTap)。无辅助功能权限时,
    // 掩码里混着键盘会让系统把整个 tap 反复禁用(实测日志 type=-1 循环),
    // 连带鼠标事件一起死 —— 拆开后键盘权限问题不再殃及鼠标。

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
    g_rawMouseX = location.x;
    g_rawMouseY = location.y;

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
            // rev=18 修复三处(此前壁纸模式滚轮/触控板滚动完全不可用):
            // 1) 轴向写反 —— CG 滚轮事件 Axis1 是【垂直】、Axis2 才是水平,
            //    旧代码把垂直滚动当水平转发;
            // 2) 触控板两指滑动是 continuous 事件,整数行增量(IntegerField)
            //    绝大多数为 0 —— 必须按 IsContinuous 区分:触控板读像素增量,
            //    传统滚轮读 FixedPt 行增量;
            // 3) 旧代码 /120 把 ±1~3/格 的行增量缩到近乎 0(120 是 Windows 的
            //    WHEEL_DELTA,CG 没有这个概念),且 = 赋值会丢同帧内的多个事件
            //    —— 改为累加,读取端 _FLMouseGetWheelDelta 本就是取后清零。
            int64_t isContinuous = CGEventGetIntegerValueField(event, kCGScrollWheelEventIsContinuous);
            double dy, dx;
            if (isContinuous) {
                // 触控板/妙控鼠标:像素增量,约 50px 折算 1 行,手感与滚轮一格相近
                dy = CGEventGetDoubleValueField(event, kCGScrollWheelEventPointDeltaAxis1) / 50.0;
                dx = CGEventGetDoubleValueField(event, kCGScrollWheelEventPointDeltaAxis2) / 50.0;
            } else {
                // 传统滚轮:FixedPt 行增量,一格约 ±1
                dy = CGEventGetDoubleValueField(event, kCGScrollWheelEventFixedPtDeltaAxis1);
                dx = CGEventGetDoubleValueField(event, kCGScrollWheelEventFixedPtDeltaAxis2);
            }
            // 单增量+方向标志的 API 保持不变(C# ABI 不动):取主导轴,方向跟随最近事件。
            // 增量符号原样透传 —— CGEventTap 拿到的已是用户"自然滚动"偏好换算后的值,
            // 若实测方向反了,把下面两处 += 改成 -= 即可。
            if (fabs(dx) > fabs(dy)) {
                g_isHorizontalWheel = YES;
                g_wheelDelta += (float)dx;
            } else if (dy != 0.0) {
                g_isHorizontalWheel = NO;
                g_wheelDelta += (float)dy;
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
    // 自愈同鼠标 tap:被系统禁用时重启(权限被撤销时会反复触发,只影响本 tap)
    if (type == kCGEventTapDisabledByTimeout || type == kCGEventTapDisabledByUserInput) {
        if (g_keyTap != NULL) {
            CGEventTapEnable(g_keyTap, true);
        }
        return event;
    }

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

    // rev=23: 鼠标 tap 只负责点击计数 + 滚轮增量。位置/按键状态已改为
    // NSEvent 轮询(无需权限),不再需要 Moved/Dragged 事件流 —— 掩码里去掉
    // 它们能省掉绝大多数回调开销(旁听 tap 每个事件都会唤醒本进程)。
    CGEventMask mouseMask = (1 << kCGEventLeftMouseDown) |
                            (1 << kCGEventLeftMouseUp) |
                            (1 << kCGEventRightMouseDown) |
                            (1 << kCGEventRightMouseUp) |
                            (1 << kCGEventScrollWheel);

    CGEventMask keyboardMask = (1 << kCGEventKeyDown) |
                               (1 << kCGEventKeyUp);

    // rev=21: Default(同步过滤)→ ListenOnly(旁听)。回调从来都原样 return event,
    // 却让全系统每个鼠标/键盘事件同步等待 Unity 主循环,帧率一抖整机输入跟着卡,
    // 还大幅提高被系统按超时禁用的概率。
    // rev=23: 鼠标/键盘拆成两个 tap。实测日志(2026-07-13):无辅助功能权限时,
    // 混合掩码的 tap 被系统以 kCGEventTapDisabledByUserInput 反复禁用,自愈
    // 重启也只是循环拉锯,鼠标事件跟着一起死。拆开后键盘权限问题只影响键盘。
    g_eventTap = CGEventTapCreate(
        g_tapLocation,
        kCGHeadInsertEventTap,
        kCGEventTapOptionListenOnly,
        mouseMask,
        FLMouseEventCallback,
        NULL
    );

    if (g_eventTap == NULL) {
        NSLog(@"[FLWallpaper] Failed to create mouse event tap - may need accessibility permissions");
        NSLog(@"[FLWallpaper] 请在系统设置 > 隐私与安全性 > 辅助功能 中启用本应用");
        NSLog(@"[FLWallpaper] (拖拽/点击不受影响 —— 位置/按键走 NSEvent 轮询;受影响的是滚轮转发)");
        return NO;
    }

    // Add the mouse tap to the run loop
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

    // Enable the mouse tap
    CGEventTapEnable(g_eventTap, true);
    g_tapEnabled = YES;
    NSLog(@"[FLWallpaper] Mouse event tap created successfully");

    // 键盘 tap:没权限时系统会立刻禁用它并反复拉锯,干脆不建(键盘状态 API 读 0,
    // 与历史行为一致);有权限则正常工作。
    if (isTrusted) {
        g_keyTap = CGEventTapCreate(
            g_tapLocation,
            kCGHeadInsertEventTap,
            kCGEventTapOptionListenOnly,
            keyboardMask,
            FLKeyboardEventCallback,
            NULL
        );
        if (g_keyTap != NULL) {
            CFRunLoopSourceRef keySource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, g_keyTap, 0);
            if (keySource != NULL) {
                CFRunLoopAddSource(CFRunLoopGetCurrent(), keySource, kCFRunLoopCommonModes);
                CFRelease(keySource);
                CGEventTapEnable(g_keyTap, true);
                NSLog(@"[FLWallpaper] Keyboard event tap created successfully");
            } else {
                CFRelease(g_keyTap);
                g_keyTap = NULL;
                NSLog(@"[FLWallpaper] Keyboard tap run loop source failed - keyboard state disabled");
            }
        } else {
            NSLog(@"[FLWallpaper] Keyboard tap create failed - keyboard state disabled");
        }
    } else {
        NSLog(@"[FLWallpaper] 无辅助功能权限 —— 跳过键盘 tap(键盘状态 API 恒 0)");
    }

    return YES;
}

static void FLDestroyEventTap(void) {
    if (g_eventTap != NULL) {
        CGEventTapEnable(g_eventTap, false);
        CFRelease(g_eventTap);
        g_eventTap = NULL;
        NSLog(@"[FLWallpaper] Mouse event tap destroyed");
    }
    if (g_keyTap != NULL) {
        CGEventTapEnable(g_keyTap, false);
        CFRelease(g_keyTap);
        g_keyTap = NULL;
        NSLog(@"[FLWallpaper] Keyboard event tap destroyed");
    }
    g_tapEnabled = NO;
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
    return "FLWallpaperBridge rev=23-poll-input " __DATE__ " " __TIME__;
}

// On-demand full diagnostic dump. C# calls this when it wants a snapshot
// of the native state in Player.log.
__attribute__((visibility("default")))
void _FLDiagnose(void) {
    FLRunOnMain(^{
        NSLog(@"[FLLOG] @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
        NSLog(@"[FLLOG] @@@@@@@@ _FLDiagnose snapshot @@@@@@@@@@@@@@");
        NSLog(@"[FLLOG] @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
        NSLog(@"[FLLOG][DIAG] g_wallpaperOn=%d g_borderlessFsOn=%d g_savedValid=%d g_savedLevel=%ld "
              @"savedWindowClass=%s savedContentViewClass=%s viewSubclass=%s",
              g_wallpaperOn, g_borderlessFsOn, g_savedValid, (long)g_savedLevel,
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

// 切回全屏/窗口模式时调用：把窗口重新激活成 key window，让 Unity 重新收到
// 原生 mouseDown（否则退出壁纸后 Input.GetMouseButtonDown(0) 不触发，撒食物失效）。
//
// *** 安全铁闸 ***：block 里第一件事就是检查 g_wallpaperOn —— 壁纸开着时
// 直接 return，绝不调用 activateIgnoringOtherApps / makeKeyAndOrderFront。
// 这两个会强抢焦点把窗口拉到前台，是壁纸模式(NonactivatingPanel,绝不抢焦点)
// 绝对不能做的。FLApplyWallpaper / FLRestoreWindow 都在主线程改 g_wallpaperOn，
// 这个 block 也在主线程跑，串行执行，所以守卫是原子的：不可能在壁纸激活期间触发。
// rev=16: 进入无边框全屏(替代 Screen.fullScreen=true 的原生 Space 全屏)。
// C# FullscreenMode 调用。带 g_wallpaperOn 铁闸, 壁纸开着时绝不执行。
__attribute__((visibility("default")))
void _FLEnterBorderlessFullscreen(void) {
    FLRunOnMain(^{ FLEnterBorderlessFullscreenInternal(); });
}

// rev=16: 退出无边框全屏(恢复窗口类 + 菜单栏/Dock)。C# WindowedMode 调用;
// 进壁纸时 FLApplyWallpaper 内部会自动调, C# 无需管。幂等。
__attribute__((visibility("default")))
void _FLExitBorderlessFullscreen(void) {
    FLRunOnMain(^{ FLExitBorderlessFullscreenInternal(); });
}

__attribute__((visibility("default")))
void _FLWallpaperActivateWindow(void) {
    dispatch_block_t activate = ^{
        if (g_wallpaperOn) {
            NSLog(@"[FLLOG][ACTIVATE] skipped — wallpaper is ON (safe guard)");
            return;
        }
        NSWindow *window = FLLocateUnityWindow();
        [NSApp activateIgnoringOtherApps:YES];
        if (window != nil) {
            [window makeKeyAndOrderFront:nil];
            NSLog(@"[FLLOG][ACTIVATE] activated isKey=%d isMain=%d",
                  [window isKeyWindow], [window isMainWindow]);
        } else {
            NSLog(@"[FLLOG][ACTIVATE] no Unity window to activate");
        }
    };

    // 鼠标路由 nudge —— rev=14 实测日志证明: 退出壁纸重进全屏后窗口
    // isKey=1 / isMain=1 / app active, Input.mousePosition 也在更新,
    // 但 mouseDown 一律收不到(用户看到系统光标)。窗口自身状态全部正常,
    // 说明是 WindowServer 侧该窗口的鼠标事件路由记录没跟上(此窗口经历了
    // NSPanel class swap + NonactivatingPanel + toggleFullScreen 往返)。
    // 翻转 ignoresMouseEvents 强制 WindowServer 重建这个窗口的鼠标路由,
    // 再补 makeKey + 光标区域刷新。幂等,带壁纸铁闸。
    dispatch_block_t nudge = ^{
        if (g_wallpaperOn) {
            NSLog(@"[FLLOG][NUDGE] skipped — wallpaper is ON (safe guard)");
            return;
        }
        NSWindow *window = FLLocateUnityWindow();
        if (window == nil) return;
        [window setIgnoresMouseEvents:YES];
        [window setIgnoresMouseEvents:NO];
        [window setAcceptsMouseMovedEvents:YES];
        if (![window isKeyWindow]) {
            [NSApp activateIgnoringOtherApps:YES];
            [window makeKeyAndOrderFront:nil];
        }
        NSView *cv = [window contentView];
        if (cv != nil) {
            [window invalidateCursorRectsForView:cv];
        }
        NSLog(@"[FLLOG][NUDGE] mouse-routing nudge done: class=%s style=0x%lx "
              @"level=%ld isKey=%d isMain=%d ignoresMouse=%d",
              class_getName([window class]),
              (unsigned long)[window styleMask], (long)[window level],
              [window isKeyWindow], [window isMainWindow],
              [window ignoresMouseEvents]);
    };

    FLRunOnMain(activate);
    // 不再在 0.3s(全屏过渡动画中途)抢 makeKeyAndOrderFront —— 过渡中抢 key
    // 有干扰 fullscreen Space 建立的风险。改为过渡结束后(动画约 1s)的
    // 1.2s / 2.8s 两拍 nudge,覆盖慢机器,幂等且带铁闸。
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(1.2 * NSEC_PER_SEC)),
                   dispatch_get_main_queue(), nudge);
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(2.8 * NSEC_PER_SEC)),
                   dispatch_get_main_queue(), nudge);
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

// Get current mouse position。rev>=22 协议:左下原点"点"(pt)坐标,由 C# 侧按
// Screen/主屏点尺寸比值换算成 Unity 像素。
// rev=23:改为实时轮询 [NSEvent mouseLocation](本就是左下原点点坐标,无需翻转)
// —— 不再依赖 event tap 缓存。实测日志(2026-07-13)证明无辅助功能权限时系统会
// 反复禁用 tap(kCGEventTapDisabledByUserInput),缓存位置冻结=拖拽必死;
// mouseLocation 轮询不需要任何权限,按住期间照样实时更新,tap 死活不影响拖拽。
__attribute__((visibility("default")))
void _FLMouseGetPosition(double *outX, double *outY) {
    NSPoint loc = [NSEvent mouseLocation];
    if (outX != NULL) *outX = loc.x;
    if (outY != NULL) *outY = loc.y;
}

// Get left button state。rev=23:改读 [NSEvent pressedMouseButtons](实时全局
// 按键位图,bit0=左键,无需权限),同上不再依赖 tap 缓存。
__attribute__((visibility("default")))
int _FLMouseGetLeftButtonDown(void) {
    return ([NSEvent pressedMouseButtons] & 1) ? 1 : 0;
}

// Get right button state(bit1=右键)
__attribute__((visibility("default")))
int _FLMouseGetRightButtonDown(void) {
    return ([NSEvent pressedMouseButtons] & 2) ? 1 : 0;
}

// rev=20: 光标上方是否被其他应用的【普通应用窗口】覆盖。现只用于滚轮转发闸门
// (拖拽闸门已改用 C# 侧 Unity Input 归属判定 —— 系统把 mouseDown 路由给谁,
//  谁就是点击的主人,不做几何猜测)。
//
// 历史教训:rev=18 用 CGWindowListCopyWindowInfo 几何+alpha 判定,被 macOS 录屏/
// 共享的全屏点击穿透指示窗误伤成"全屏被覆盖",拖拽滚轮全废;rev=19 的
// windowNumberAtPoint + layer∈[0,101] 带仍可能把 25 层(状态层)的录屏工具窗
// 算成覆盖。rev=20 收窄:只有 layer==0(真实普通应用窗口:浏览器/Finder 等)
// 才算覆盖;桌面层(<0)、Dock(20)/菜单栏(24)/状态层(25)/更高的被动叠层一律放行。
// 代价:光标悬在 Dock/菜单栏上滚动会漏给壁纸,属边缘小瑕疵,远好于滚动被闸死。
__attribute__((visibility("default")))
int _FLIsCursorCoveredByOtherWindow(void) {
    __block int covered = 0;
    FLRunOnMain(^{
        // NSEvent.mouseLocation 与 windowNumberAtPoint 同为左下原点屏幕坐标
        NSPoint p = [NSEvent mouseLocation];
        NSInteger num = [NSWindow windowNumberAtPoint:p belowWindowWithWindowNumber:0];
        if (num <= 0) return;                                   // 未命中任何窗口

        if ([NSApp windowWithWindowNumber:num] != nil) return;  // 本进程窗口(壁纸自身)

        CFArrayRef arr = CGWindowListCopyWindowInfo(kCGWindowListOptionIncludingWindow,
                                                    (CGWindowID)num);
        if (arr == NULL) return;
        int layer = 0;
        if (CFArrayGetCount(arr) > 0) {
            CFDictionaryRef info = (CFDictionaryRef)CFArrayGetValueAtIndex(arr, 0);
            CFNumberRef layerRef = (CFNumberRef)CFDictionaryGetValue(info, kCGWindowLayer);
            if (layerRef != NULL) CFNumberGetValue(layerRef, kCFNumberIntType, &layer);
        }
        CFRelease(arr);

        covered = (layer == 0) ? 1 : 0;
    });
    return covered;
}

// rev=19: 第二信号 —— 本进程窗口最近一次真实收到 NSEvent 左键按下的时间。
// macOS 窗口服务器把物理 mouseDown 路由给了我们的 FLWallpaperPanel,就是
// "这次点击属于壁纸"的铁证(比任何几何推断都准)。C# 拖拽闸门用它兜底:
// 命中测试误报"被覆盖"、但我们确实收到了按下 -> 照样允许拖拽。
static double g_lastLocalMouseDownAt = -1.0e9;
static id     g_localDownMonitor     = nil;

static void FLEnsureLocalDownMonitor(void) {
    if (g_localDownMonitor != nil) return;
    // rev=21: 同时监听 Up —— 一次按下结束后立刻使时间戳失效,防止
    // "点完游戏 0.3s 内又按在浏览器上"被旧时间戳误判为属于游戏。
    g_localDownMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:
                              (NSEventMaskLeftMouseDown | NSEventMaskLeftMouseUp)
                                                               handler:^NSEvent *(NSEvent *e) {
        if (e.type == NSEventTypeLeftMouseDown)
            g_lastLocalMouseDownAt = CFAbsoluteTimeGetCurrent();
        else
            g_lastLocalMouseDownAt = -1.0e9;   // up:本次按下归属判定已结束
        return e;   // 只观察,不拦截
    }];
}

__attribute__((visibility("default")))
double _FLSecondsSinceNativeMouseDown(void) {
    __block double since = 1.0e9;
    FLRunOnMain(^{
        FLEnsureLocalDownMonitor();
        since = CFAbsoluteTimeGetCurrent() - g_lastLocalMouseDownAt;
    });
    return since;
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
    // rev=21: 同时清按钮/滚轮态。退出壁纸会先销毁 tap,若当时左键正按着,
    // mouseUp 永远收不到,g_leftButtonDown 卡 YES —— 重进壁纸首帧就会出现
    // "幻影按下沿",拖拽状态机被陈旧起点污染。C# OnEnable/OnDisable 都调本函数。
    g_leftButtonDown = NO;
    g_rightButtonDown = NO;
    g_wheelDelta = 0.0f;
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
