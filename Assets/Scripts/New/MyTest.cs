using System;
using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using AOT;

/// <summary>
/// IME (Input Method Editor) detection utility
/// 
/// IMPORTANT: IME windows can exist in two forms:
/// 1. Child windows (with a parent) - typically attached to the input window
/// 2. Top-level windows (no parent) - independent floating candidate windows
/// 
/// This class comprehensively checks ALL windows by:
/// - Checking the foreground window and its parent/children
/// - Using ImmGetDefaultIMEWnd to find the default IME window
/// - Enumerating ALL top-level windows (via EnumWindows)
/// - Recursively checking ALL child windows of each top-level window
/// - Checking window class names to identify IME windows
/// - Checking for active IME composition status
/// 
/// Coverage: The script checks every window in the system:
/// - All top-level windows
/// - All child windows (recursively, at any nesting level)
/// - This ensures no IME window is missed, regardless of its parent/child relationship
/// </summary>
public class MyTest : MonoBehaviour
{
    // Windows API constants
    private const int CFS_DEFAULT = 0x0000;
    private const int CFS_RECT = 0x0001;
    private const int CFS_POINT = 0x0002;
    private const int CFS_FORCE_POSITION = 0x0020;
    private const int CFS_CANDIDATEPOS = 0x0040;
    private const int CFS_EXCLUDE = 0x0080;

    // Windows API structures
    [StructLayout(LayoutKind.Sequential)]
    public struct CANDIDATEFORM
    {
        public int dwIndex;
        public int dwStyle;
        public POINT ptCurrentPos;
        public RECT rcArea;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CANDIDATELIST
    {
        public int dwSize;
        public int dwStyle;
        public int dwCount;
        public int dwSelection;
        public int dwPageStart;
        public int dwPageSize;
        public int dwOffset; // Offset to first candidate string
    }

    // Windows API functions
    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetContext(IntPtr hWnd);

    [DllImport("imm32.dll")]
    private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

    [DllImport("imm32.dll")]
    private static extern bool ImmGetCandidateWindow(IntPtr hIMC, int dwIndex, ref CANDIDATEFORM lpCandidate);

    [DllImport("imm32.dll")]
    private static extern bool ImmGetOpenStatus(IntPtr hIMC);

    [DllImport("imm32.dll")]
    private static extern int ImmGetConversionStatus(IntPtr hIMC, ref int lpfdwConversion, ref int lpfdwSentence);

    [DllImport("imm32.dll")]
    private static extern int ImmGetCompositionString(IntPtr hIMC, int dwIndex, IntPtr lpBuf, int dwBufLen);

    [DllImport("imm32.dll")]
    private static extern int ImmGetCandidateList(IntPtr hIMC, int dwIndex, IntPtr lpCandList, int dwBufLen);

    [DllImport("imm32.dll")]
    private static extern int ImmGetCandidateListCount(IntPtr hIMC, out int lpdwListCount);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc enumProc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // Static fields for IL2CPP compatibility
    private static IntPtr foundWindowWithIME = IntPtr.Zero;
    private static IMEContent foundIMEContent = null;
    private static IntPtr targetCandidateWindow = IntPtr.Zero;
    private static IntPtr foundInputWindowForCandidate = IntPtr.Zero;

    // Common IME window class names (these can vary by IME)
    private static readonly string[] IMEWindowClassNames = new string[]
    {
        "IME",           // Standard IME window class
        "MSCTFIME UI",   // Microsoft Text Services Framework IME
        "CicMarshalWnd", // Microsoft IME
        "CicMarshalWndFrame", // Microsoft IME frame
    };

    // Composition string constants
    private const int GCS_COMPSTR = 0x0008;
    private const int GCS_COMPATTR = 0x0010;
    private const int GCS_COMPCLAUSE = 0x0020;
    private const int GCS_CURSORPOS = 0x0080;
    private const int GCS_DELTASTART = 0x0100;
    private const int GCS_RESULTREADSTR = 0x0200;
    private const int GCS_RESULTREADCLAUSE = 0x0400;
    private const int GCS_RESULTREADATTR = 0x0800;
    private const int GCS_RESULTSTR = 0x0800;
    private const int GCS_RESULTCLAUSE = 0x1000;

    // Candidate list constants
    private const int IMM_GWL_IMC = -16;
    private const int IME_CAND_UNKNOWN = 0x0000;
    private const int IME_CAND_READ = 0x0001;
    private const int IME_CAND_CODE = 0x0002;
    private const int IME_CAND_MEANING = 0x0003;
    private const int IME_CAND_RADICAL = 0x0004;
    private const int IME_CAND_STROKE = 0x0005;

    /// <summary>
    /// Checks if a window has an active IME composition
    /// </summary>
    private static bool HasActiveIMEComposition(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return false;

        IntPtr hIMC = ImmGetContext(hWnd);
        if (hIMC == IntPtr.Zero)
            return false;

        try
        {
            if (!ImmGetOpenStatus(hIMC))
                return false;

            int compositionLength = ImmGetCompositionString(hIMC, GCS_COMPSTR, IntPtr.Zero, 0);
            return compositionLength > 0;
        }
        finally
        {
            ImmReleaseContext(hWnd, hIMC);
        }
    }

    /// <summary>
    /// Checks if a window is an IME window by class name
    /// </summary>
    private static bool IsIMEWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return false;

        try
        {
            System.Text.StringBuilder className = new System.Text.StringBuilder(256);
            if (GetClassName(hWnd, className, className.Capacity) == 0)
                return false;

            string classNameStr = className.ToString();
            foreach (string imeClassName in IMEWindowClassNames)
            {
                if (classNameStr.Contains(imeClassName))
                    return true;
            }
        }
        catch
        {
            // Ignore errors in class name retrieval
        }

        return false;
    }

    /// <summary>
    /// Checks if a window is visible and might be an IME candidate window
    /// </summary>
    private static bool IsVisibleIMECandidateWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return false;

        // Check if window is visible
        if (!IsWindowVisible(hWnd))
            return false;

        // Check if it's an IME window by class name
        if (IsIMEWindow(hWnd))
            return true;

        // Check if it has active IME composition
        return HasActiveIMEComposition(hWnd);
    }

    /// <summary>
    /// Finds any window that has active IME composition
    /// This handles both parented and parentless IME windows
    /// </summary>
    private static IntPtr FindAnyWindowWithActiveIME()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        
        // First check foreground window (the input window itself)
        if (foregroundWindow != IntPtr.Zero)
        {
            if (HasActiveIMEComposition(foregroundWindow))
            {
                return foregroundWindow;
            }

            // Check if foreground window has a parent (it might be a child window)
            IntPtr parent = GetParent(foregroundWindow);
            if (parent != IntPtr.Zero && HasActiveIMEComposition(parent))
            {
                return parent;
            }

            // Check default IME window (this may or may not have a parent)
            IntPtr imeWindow = ImmGetDefaultIMEWnd(foregroundWindow);
            if (imeWindow != IntPtr.Zero)
            {
                // Check if the IME window itself has active composition
                if (HasActiveIMEComposition(imeWindow))
                {
                    return imeWindow;
                }

                // Check if IME window is visible (candidate window might be visible)
                if (IsVisibleIMECandidateWindow(imeWindow))
                {
                    return imeWindow;
                }

                // IME window might have a parent, check it
                IntPtr imeParent = GetParent(imeWindow);
                if (imeParent != IntPtr.Zero && IsVisibleIMECandidateWindow(imeParent))
                {
                    return imeParent;
                }

                // Also check child windows of the IME window
                foundWindowWithIME = IntPtr.Zero;
                EnumChildWindows(imeWindow, EnumWindowsForIME, IntPtr.Zero);
                if (foundWindowWithIME != IntPtr.Zero)
                    return foundWindowWithIME;
            }

            // Check child windows of foreground window (IME might be a child)
            foundWindowWithIME = IntPtr.Zero;
            EnumChildWindows(foregroundWindow, EnumWindowsForIME, IntPtr.Zero);
            if (foundWindowWithIME != IntPtr.Zero)
                return foundWindowWithIME;
        }

        // Enumerate all top-level windows to find parentless IME windows
        // This catches IME candidate windows that don't have a parent
        // NOTE: EnumWindowsForIME will also recursively check all child windows
        // of each top-level window, ensuring comprehensive coverage
        foundWindowWithIME = IntPtr.Zero;
        EnumWindows(EnumWindowsForIME, IntPtr.Zero);
        
        return foundWindowWithIME;
    }

    /// <summary>
    /// EnumWindows callback for finding window with active IME (IL2CPP compatible)
    /// Checks both IME composition status and IME window class names
    /// Also recursively checks all child windows of each top-level window
    /// </summary>
    [MonoPInvokeCallback(typeof(EnumWindowsProc))]
    private static bool EnumWindowsForIME(IntPtr hWnd, IntPtr lParam)
    {
        // Check if window has active IME composition
        if (HasActiveIMEComposition(hWnd))
        {
            foundWindowWithIME = hWnd;
            return false; // Stop enumeration
        }

        // Also check if it's a visible IME candidate window (by class name)
        // This catches parentless IME windows that might not show composition
        if (IsVisibleIMECandidateWindow(hWnd))
        {
            foundWindowWithIME = hWnd;
            return false; // Stop enumeration
        }

        // IMPORTANT: Also check all child windows recursively
        // This ensures we catch IME windows that are nested as children
        // EnumChildWindows recursively enumerates all descendant windows
        EnumChildWindows(hWnd, EnumWindowsForIME, IntPtr.Zero);
        
        // If we found a window in the children, stop enumeration
        if (foundWindowWithIME != IntPtr.Zero)
            return false;

        return true; // Continue enumeration
    }


    /// <summary>
    /// Gets the composition string (what user is currently typing) from IME
    /// </summary>
    private static string GetCompositionString(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return string.Empty;

        IntPtr hIMC = ImmGetContext(hWnd);
        if (hIMC == IntPtr.Zero)
        {
            // Debug: IME context not available for this window
            // This is normal for candidate windows - they don't have IME context
            return string.Empty;
        }

        try
        {
            // Get the length of the composition string
            int length = ImmGetCompositionString(hIMC, GCS_COMPSTR, IntPtr.Zero, 0);
            if (length <= 0)
                return string.Empty;

            // Allocate buffer and get the string
            IntPtr buffer = Marshal.AllocHGlobal(length);
            try
            {
                int actualLength = ImmGetCompositionString(hIMC, GCS_COMPSTR, buffer, length);
                if (actualLength > 0)
                {
                    // Convert from UTF-16 (Unicode) to string
                    return Marshal.PtrToStringUni(buffer, actualLength / 2);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting composition string: {ex.Message}");
        }
        finally
        {
            ImmReleaseContext(hWnd, hIMC);
        }

        return string.Empty;
    }

    /// <summary>
    /// Finds the input window that has the IME context (where user is typing)
    /// This is different from the candidate window which is just for display
    /// </summary>
    private static IntPtr FindInputWindowWithIMEContext()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            IntPtr hIMC = ImmGetContext(foregroundWindow);
            if (hIMC != IntPtr.Zero)
            {
                try
                {
                    // Check if IME is open and has active composition
                    if (ImmGetOpenStatus(hIMC))
                    {
                        int compositionLength = ImmGetCompositionString(hIMC, GCS_COMPSTR, IntPtr.Zero, 0);
                        if (compositionLength > 0)
                        {
                            return foregroundWindow;
                        }
                    }
                }
                finally
                {
                    ImmReleaseContext(foregroundWindow, hIMC);
                }
            }
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// Gets the candidate list from IME
    /// </summary>
    private static string[] GetCandidateList(IntPtr hWnd, int candidateIndex = 0)
    {
        if (hWnd == IntPtr.Zero)
            return new string[0];

        IntPtr hIMC = ImmGetContext(hWnd);
        if (hIMC == IntPtr.Zero)
        {
            // Debug: IME context not available for this window
            // This is normal for candidate windows - they don't have IME context
            return new string[0];
        }

        try
        {
            // Check if IME is open first
            if (!ImmGetOpenStatus(hIMC))
                return new string[0];

            // Try to get candidate list count first
            int listCount;
            if (ImmGetCandidateListCount(hIMC, out listCount) == 0 || listCount == 0)
            {
                // No candidate lists available
                return new string[0];
            }

            // First, get the size needed for the candidate list
            int sizeNeeded = ImmGetCandidateList(hIMC, candidateIndex, IntPtr.Zero, 0);
            if (sizeNeeded <= 0)
            {
                // Try index 0 if we were trying a different index
                if (candidateIndex != 0)
                {
                    sizeNeeded = ImmGetCandidateList(hIMC, 0, IntPtr.Zero, 0);
                    candidateIndex = 0;
                }
                
                if (sizeNeeded <= 0)
                {
                    // Debug: No candidate list available
                    return new string[0];
                }
            }

            // Allocate buffer and get the candidate list
            IntPtr buffer = Marshal.AllocHGlobal(sizeNeeded);
            try
            {
                int actualSize = ImmGetCandidateList(hIMC, candidateIndex, buffer, sizeNeeded);
                if (actualSize > 0)
                {
                    // Read the CANDIDATELIST structure
                    CANDIDATELIST candList = (CANDIDATELIST)Marshal.PtrToStructure(buffer, typeof(CANDIDATELIST));
                    
                    if (candList.dwCount == 0)
                        return new string[0];

                    string[] candidates = new string[candList.dwCount];
                    
                    // The candidate strings are stored starting at dwOffset bytes from the start of the buffer
                    // Each string is a null-terminated Unicode string
                    IntPtr stringsStart = new IntPtr(buffer.ToInt64() + candList.dwOffset);
                    IntPtr currentStringPtr = stringsStart;
                    
                    // Read each candidate string sequentially
                    for (int i = 0; i < candList.dwCount; i++)
                    {
                        // Read the null-terminated Unicode string
                        string candidate = Marshal.PtrToStringUni(currentStringPtr);
                        candidates[i] = candidate ?? string.Empty;
                        
                        // Move to the next string
                        // Each string is null-terminated, so we need to skip past the null terminator
                        if (candidate != null)
                        {
                            // Move pointer: (length + 1) * 2 bytes (Unicode is 2 bytes per char, +1 for null terminator)
                            currentStringPtr = new IntPtr(currentStringPtr.ToInt64() + (candidate.Length + 1) * 2);
                        }
                        else
                        {
                            // If candidate is null, skip at least 2 bytes (null terminator)
                            currentStringPtr = new IntPtr(currentStringPtr.ToInt64() + 2);
                        }
                    }
                    
                    return candidates;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting candidate list: {ex.Message}");
        }
        finally
        {
            ImmReleaseContext(hWnd, hIMC);
        }

        return new string[0];
    }

    /// <summary>
    /// Gets IME content from a specific window
    /// </summary>
    private static IMEContent GetIMEContentFromWindow(IntPtr hWnd)
    {
        IMEContent content = new IMEContent();
        
        if (hWnd == IntPtr.Zero)
            return content;

        try
        {
            // Get composition string from window
            content.CompositionString = GetCompositionString(hWnd);
            
            // Get candidate list from window
            // Try multiple candidate indices (some IMEs use different indices)
            for (int i = 0; i < 4; i++)
            {
                string[] candidates = GetCandidateList(hWnd, i);
                if (candidates != null && candidates.Length > 0)
                {
                    content.Candidates = candidates;
                    break; // Found candidates, stop trying other indices
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting IME content from window: {ex.Message}");
        }

        return content;
    }

    /// <summary>
    /// Gets the complete IME content including composition string and candidate list
    /// Checks foreground window first, then searches globally across all windows
    /// IMPORTANT: IME context is associated with the INPUT window,
    /// not the candidate window. The candidate window is just for display.
    /// </summary>
    public static IMEContent GetIMEContent()
    {
        IMEContent content = new IMEContent();
        
        try
        {
            // First, try the input window that has the IME context
            IntPtr inputWindow = FindInputWindowWithIMEContext();
            
            if (inputWindow != IntPtr.Zero)
            {
                content = GetIMEContentFromWindow(inputWindow);
                if (content.HasContent)
                {
                    return content;
                }
            }

            // Fallback: try the foreground window directly
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow != IntPtr.Zero && foregroundWindow != inputWindow)
            {
                content = GetIMEContentFromWindow(foregroundWindow);
                if (content.HasContent)
                {
                    return content;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting IME content: {ex.Message}\n{ex.StackTrace}");
        }

        return content;
    }

    /// <summary>
    /// Finds any window globally that has active IME composition
    /// Searches all windows in the system, not just foreground window
    /// </summary>
    private static IntPtr FindGlobalWindowWithIMEComposition()
    {
        foundInputWindowForCandidate = IntPtr.Zero;
        EnumWindows(EnumWindowsForInputWindowWithIME, IntPtr.Zero);
        return foundInputWindowForCandidate;
    }

    /// <summary>
    /// Gets IME content by searching globally across ALL windows in the system
    /// Does NOT check foreground window - searches all windows globally
    /// </summary>
    public static IMEContent GetIMEContentGlobal()
    {
        IMEContent content = new IMEContent();
        
        try
        {
            // Search globally across all windows for IME content
            foundIMEContent = null;
            EnumWindows(EnumWindowsForIMEContent, IntPtr.Zero);
            
            if (foundIMEContent != null && foundIMEContent.HasContent)
            {
                return foundIMEContent;
            }

            // If no content found, try to find any window with IME composition globally
            IntPtr windowWithComposition = FindGlobalWindowWithIMEComposition();
            if (windowWithComposition != IntPtr.Zero)
            {
                content = GetIMEContentFromWindow(windowWithComposition);
                if (content.HasContent)
                {
                    return content;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting global IME content: {ex.Message}\n{ex.StackTrace}");
        }

        return content;
    }

    /// <summary>
    /// EnumWindows callback for finding ANY window with active IME composition (IL2CPP compatible)
    /// This is used when we find a candidate window and need to find the input window
    /// </summary>
    [MonoPInvokeCallback(typeof(EnumWindowsProc))]
    private static bool EnumWindowsForInputWindowWithIME(IntPtr hWnd, IntPtr lParam)
    {
        // Skip candidate windows (they don't have IME context)
        if (IsIMEWindow(hWnd) || IsVisibleIMECandidateWindow(hWnd))
            return true;

        // Check if this window has IME context with active composition
        IntPtr hIMC = ImmGetContext(hWnd);
        if (hIMC != IntPtr.Zero)
        {
            try
            {
                if (ImmGetOpenStatus(hIMC))
                {
                    int compLen = ImmGetCompositionString(hIMC, GCS_COMPSTR, IntPtr.Zero, 0);
                    if (compLen > 0)
                    {
                        // Found a window with active IME composition - this is likely the input window
                        foundInputWindowForCandidate = hWnd;
                        return false; // Stop enumeration - found it!
                    }
                }
            }
            finally
            {
                ImmReleaseContext(hWnd, hIMC);
            }
        }

        // Also check child windows recursively
        EnumChildWindows(hWnd, EnumWindowsForInputWindowWithIME, IntPtr.Zero);
        
        // If we found a window in children, stop enumeration
        if (foundInputWindowForCandidate != IntPtr.Zero)
            return false;

        return true; // Continue enumeration
    }

    /// <summary>
    /// EnumWindows callback for finding input window associated with candidate window (IL2CPP compatible)
    /// </summary>
    [MonoPInvokeCallback(typeof(EnumWindowsProc))]
    private static bool EnumWindowsForInputWindow(IntPtr hWnd, IntPtr lParam)
    {
        // Skip the candidate window itself
        if (hWnd == targetCandidateWindow)
            return true;

        // Skip candidate windows
        if (IsIMEWindow(hWnd) || IsVisibleIMECandidateWindow(hWnd))
            return true;

        // Check if this window has IME context
        IntPtr hIMC = ImmGetContext(hWnd);
        if (hIMC != IntPtr.Zero)
        {
            try
            {
                if (ImmGetOpenStatus(hIMC))
                {
                    // Check if this window's IME window matches the candidate window
                    IntPtr imeWnd = ImmGetDefaultIMEWnd(hWnd);
                    if (imeWnd == targetCandidateWindow || GetParent(imeWnd) == targetCandidateWindow)
                    {
                        foundInputWindowForCandidate = hWnd;
                        return false; // Stop enumeration
                    }

                    // Also check if this window has active composition
                    int compLen = ImmGetCompositionString(hIMC, GCS_COMPSTR, IntPtr.Zero, 0);
                    if (compLen > 0)
                    {
                        // This might be the input window
                        if (foundInputWindowForCandidate == IntPtr.Zero)
                            foundInputWindowForCandidate = hWnd;
                    }
                }
            }
            finally
            {
                ImmReleaseContext(hWnd, hIMC);
            }
        }
        return true; // Continue enumeration
    }

    /// <summary>
    /// EnumWindows callback for finding candidate window (IL2CPP compatible)
    /// </summary>
    [MonoPInvokeCallback(typeof(EnumWindowsProc))]
    private static bool EnumWindowsForCandidateWindow(IntPtr hWnd, IntPtr lParam)
    {
        if (IsVisibleIMECandidateWindow(hWnd))
        {
            foundWindowWithIME = hWnd;
            return false; // Stop enumeration
        }
        return true; // Continue enumeration
    }

    /// <summary>
    /// EnumWindows callback for finding IME content globally (IL2CPP compatible)
    /// Checks each window for IME content and stops when found
    /// Also handles candidate windows by finding their associated input windows
    /// </summary>
    [MonoPInvokeCallback(typeof(EnumWindowsProc))]
    private static bool EnumWindowsForIMEContent(IntPtr hWnd, IntPtr lParam)
    {
        // Check if this window has IME content
        IMEContent content = GetIMEContentFromWindow(hWnd);
        if (content.HasContent)
        {
            foundIMEContent = content;
            return false; // Stop enumeration - found content
        }

        // Check if this is a candidate window (display window)
        // If so, try to get content from it first, then find the associated input window
        if (IsVisibleIMECandidateWindow(hWnd) || IsIMEWindow(hWnd))
        {
            // First try to get content directly from the candidate window
            IMEContent candidateContent = GetIMEContentFromWindow(hWnd);
            if (candidateContent.HasContent)
            {
                foundIMEContent = candidateContent;
                return false; // Stop enumeration - found content
            }

            // If candidate window doesn't have content, search for a window with IME composition
            // The candidate window itself doesn't have IME context, so we need to find the input window
            // We'll continue enumeration to find a window with active IME composition
        }

        // Also check all child windows recursively
        EnumChildWindows(hWnd, EnumWindowsForIMEContent, IntPtr.Zero);
        
        // If we found content in children, stop enumeration
        if (foundIMEContent != null && foundIMEContent.HasContent)
            return false;

        return true; // Continue enumeration
    }

    /// <summary>
    /// Data structure to hold IME content
    /// </summary>
    public class IMEContent
    {
        public string CompositionString { get; set; } = string.Empty;
        public string[] Candidates { get; set; } = new string[0];
        
        public bool HasContent => !string.IsNullOrEmpty(CompositionString) || (Candidates != null && Candidates.Length > 0);
        
        public override string ToString()
        {
            if (!HasContent)
                return "No IME content";
            
            string result = $"Composition: {CompositionString}";
            if (Candidates != null && Candidates.Length > 0)
            {
                result += $"\nCandidates ({Candidates.Length}): {string.Join(", ", Candidates)}";
            }
            return result;
        }
    }

    /// <summary>
    /// Checks if the IME candidate window is currently open globally and gets its content
    /// Searches ALL windows globally, does NOT check foreground window
    /// </summary>
    /// <param name="content">Output parameter that receives the IME content if found</param>
    /// <returns>True if IME candidate window is open, false otherwise</returns>
    public static bool IsIMECandidateWindowOpen(out IMEContent content)
    {
        content = null;
        
        try
        {
            // Search globally for any window with active IME composition (NOT checking foreground window)
            IntPtr windowWithComposition = FindGlobalWindowWithIMEComposition();
            if (windowWithComposition != IntPtr.Zero)
            {
                // Get content from the window that has IME composition
                content = GetIMEContentFromWindow(windowWithComposition);
                if (content != null && content.HasContent)
                {
                    return true;
                }
            }

            // Also search globally for candidate windows and get content from them
            foundIMEContent = null;
            EnumWindows(EnumWindowsForIMEContent, IntPtr.Zero);
            
            if (foundIMEContent != null && foundIMEContent.HasContent)
            {
                content = foundIMEContent;
                return true;
            }

            // Last resort: global search for any IME content
            content = GetIMEContentGlobal();
            return content != null && content.HasContent;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking IME candidate window: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if the IME candidate window is currently open globally
    /// </summary>
    /// <returns>True if IME candidate window is open, false otherwise</returns>
    public static bool IsIMECandidateWindowOpen()
    {
        IMEContent content;
        return IsIMECandidateWindowOpen(out content);
    }

    // Example usage in Unity
    void Update()
    {
        IMEContent content;
        bool isOpen = IsIMECandidateWindowOpen(out content);
        var textComponent = this.gameObject.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            if (isOpen)
            {
                // Debug information
                IntPtr fgWindow = GetForegroundWindow();
                IntPtr inputWindow = FindInputWindowWithIMEContext();
                
                string debugInfo = $"IME Candidate Window is OPEN\n";
                debugInfo += $"Foreground Window: 0x{fgWindow.ToInt64():X}\n";
                debugInfo += $"Input Window: 0x{inputWindow.ToInt64():X}\n";
                
                if (content != null && content.HasContent)
                {
                    debugInfo += $"{content}";
                }
                else
                {
                    debugInfo += "No IME content found";
                    
                    // If no content, add debug info
                    debugInfo += "\n[DEBUG: No content found - checking IME context...]";
                    if (inputWindow != IntPtr.Zero)
                    {
                        IntPtr hIMC = ImmGetContext(inputWindow);
                        if (hIMC != IntPtr.Zero)
                        {
                            try
                            {
                                bool isOpen2 = ImmGetOpenStatus(hIMC);
                                int compLen = ImmGetCompositionString(hIMC, GCS_COMPSTR, IntPtr.Zero, 0);
                                int listCount;
                                int countResult = ImmGetCandidateListCount(hIMC, out listCount);
                                
                                debugInfo += $"\nIME Open: {isOpen2}, Comp Length: {compLen}, Candidate Lists: {listCount}";
                            }
                            finally
                            {
                                ImmReleaseContext(inputWindow, hIMC);
                            }
                        }
                    }
                }
                
                textComponent.text = debugInfo;
            }
            else
            {
                textComponent.text = "IME Candidate Window is CLOSED";
            }
        }
    }
}

