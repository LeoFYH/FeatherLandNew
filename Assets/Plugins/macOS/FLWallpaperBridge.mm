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
//
// Build: this file is auto-compiled by Unity when building for macOS because
// it sits under Assets/Plugins/macOS. C# calls the exported `_FL...` functions
// through [DllImport("__Internal")].

#import <Cocoa/Cocoa.h>

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

static void FLApplyWallpaper(void) {
    NSWindow *window = FLLocateUnityWindow();
    if (window == nil) {
        NSLog(@"[FLWallpaper] Apply skipped: no Unity NSWindow located.");
        return;
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
        // Use full frame (including menu-bar strip) so Unity covers the entire
        // visible wallpaper area; the menu bar lives on a higher window level
        // and will render above us regardless.
        [window setFrame:[screen frame] display:YES];
    }

    // Don't make us key — we want to stay behind any real app. Just ensure we
    // remain ordered correctly relative to other wallpaper-level windows.
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
        // Defensive fallback if exit is called without enter having saved state.
        [window setLevel:NSNormalWindowLevel];
        [window setCollectionBehavior:NSWindowCollectionBehaviorDefault];
    }

    g_wallpaperOn = NO;
    g_savedValid  = NO;
    NSLog(@"[FLWallpaper] Restored window: level=%ld frame=%@",
          (long)[window level], NSStringFromRect([window frame]));
}

#pragma mark - Exported C API

extern "C" {

// Enter wallpaper mode.
void _FLWallpaperEnter(void) {
    FLRunOnMain(^{ FLApplyWallpaper(); });
}

// Restore the saved (pre-wallpaper) window state.
void _FLWallpaperExit(void) {
    FLRunOnMain(^{ FLRestoreWindow(); });
}

// Re-assert level + collection behavior. Called periodically by C# to recover
// if something (Spaces switch, Mission Control, third-party tools) bumped us.
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
int _FLWallpaperIsActive(void) {
    return g_wallpaperOn ? 1 : 0;
}

// Get the main screen's frame, in points.
//   fullFrame != 0  -> [NSScreen mainScreen].frame (includes menu-bar strip)
//   fullFrame == 0  -> [NSScreen mainScreen].visibleFrame (excludes dock/menu)
// Returns 1 on success.
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

} // extern "C"
