const supportsMediaQuery = (query) => {
    if (typeof window.matchMedia !== "function") {
        return false;
    }

    return window.matchMedia(query).matches;
}

export function supportsHoverPointer() {
    const primaryPointerQuery = "(hover: hover) and (pointer: fine)";
    return supportsMediaQuery(primaryPointerQuery);
}

export function getCapabilities() {
    const browserNavigator = window.navigator ?? {};
    const maxTouchPoints = Number(browserNavigator.maxTouchPoints ?? 0);
    const hasLegacyTouchPoints = Number(browserNavigator.msMaxTouchPoints ?? 0) > 0;
    const supportsPointerEvents = "PointerEvent" in window;
    const hasTouchInput = maxTouchPoints > 0 || hasLegacyTouchPoints || ("ontouchstart" in window);

    const hasHoverPointer = supportsHoverPointer();
    const supportsHover = supportsMediaQuery("(hover: hover)") || supportsMediaQuery("(any-hover: hover)");
    const supportsFinePointer = supportsMediaQuery("(pointer: fine)") || supportsMediaQuery("(any-pointer: fine)");
    const supportsCoarsePointer = supportsMediaQuery("(pointer: coarse)") || supportsMediaQuery("(any-pointer: coarse)");
    const prefersReducedMotion = supportsMediaQuery("(prefers-reduced-motion: reduce)");
    const prefersReducedData = supportsMediaQuery("(prefers-reduced-data: reduce)");
    const prefersDarkColorScheme = supportsMediaQuery("(prefers-color-scheme: dark)");
    const forcedColorsActive = supportsMediaQuery("(forced-colors: active)");
    const prefersHighContrast = supportsMediaQuery("(prefers-contrast: more)");

    const supportsClipboardRead = typeof browserNavigator.clipboard?.readText === "function";
    const supportsClipboardWrite = typeof browserNavigator.clipboard?.writeText === "function";
    const supportsShareApi = typeof browserNavigator.share === "function";
    const supportsInstallPrompt = "onbeforeinstallprompt" in window;

    const userAgent = browserNavigator.userAgent ?? "";
    const isAppleMobileUserAgent = /iPad|iPhone|iPod/.test(userAgent);
    const isIpadOsDesktopMode = browserNavigator.platform === "MacIntel" && maxTouchPoints > 1;
    const isAppleMobilePlatform = isAppleMobileUserAgent || isIpadOsDesktopMode;

    const isStandaloneDisplayMode = supportsMediaQuery("(display-mode: standalone)") || browserNavigator.standalone === true;

    return {
        supportsHoverPointer: hasHoverPointer,
        supportsHover: supportsHover,
        supportsFinePointer: supportsFinePointer,
        supportsCoarsePointer: supportsCoarsePointer,
        supportsPointerEvents: supportsPointerEvents,
        hasTouchInput: hasTouchInput,
        maxTouchPoints: maxTouchPoints,
        prefersReducedMotion: prefersReducedMotion,
        prefersReducedData: prefersReducedData,
        prefersDarkColorScheme: prefersDarkColorScheme,
        forcedColorsActive: forcedColorsActive,
        prefersHighContrast: prefersHighContrast,
        supportsClipboardRead: supportsClipboardRead,
        supportsClipboardWrite: supportsClipboardWrite,
        supportsShareApi: supportsShareApi,
        supportsInstallPrompt: supportsInstallPrompt,
        isAppleMobilePlatform: isAppleMobilePlatform,
        isStandaloneDisplayMode: isStandaloneDisplayMode,
    };
}
