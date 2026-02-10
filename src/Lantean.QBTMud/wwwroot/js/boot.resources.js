(function () {
    const logPrefix = "[qbtmud]";
    const cdnAssetTypes = new Set([
        "dotnetwasm",
        "assembly",
        "pdb",
        "icu",
        "symbols",
        "js-module-native",
        "js-module-runtime"
    ]);

    const requested = Boolean(window.__useCdnAot);
    const rawBase = typeof window.__cdnBase === "string" ? window.__cdnBase.trim() : "";
    const normalizedBase = ensureTrailingSlash(rawBase);
    const baseIsValid = hasValidHttpScheme(normalizedBase);

    if (requested && !baseIsValid) {
        console.warn(`${logPrefix} ReleaseAOT requested CDN mode but AotCdnBaseUrl "${rawBase}" is invalid. Falling back to local assets.`);
    }

    const state = {
        enabled: requested && baseIsValid,
        baseUrl: normalizedBase,
        cdnFailed: false,
        warnedFailure: false
    };

    function applyBootstrapFont() {
        try {
            const rawFontFamily = localStorage.getItem("ThemeManager.BootstrapFontFamily");
            const fontFamily = normalizeStorageString(rawFontFamily);
            if (!isValidFontFamily(fontFamily)) {
                return;
            }

            const fontId = buildFontId(fontFamily);
            if (fontId && document.getElementById(fontId)) {
                return;
            }

            const url = buildGoogleFontUrl(fontFamily);
            const link = document.createElement("link");
            link.rel = "stylesheet";
            link.href = url;
            if (fontId) {
                link.id = fontId;
            }

            document.head.appendChild(link);
        } catch {
        }
    }

    function applyBootstrapTheme() {
        try {
            const rawBootstrapMode = localStorage.getItem("ThemeManager.BootstrapIsDark");
            const rawIsDark = localStorage.getItem("MainLayout.IsDarkMode");
            const isDark = parseBoolean(rawBootstrapMode ?? rawIsDark);
            const css = localStorage.getItem(isDark ? "ThemeManager.BootstrapCss.Dark" : "ThemeManager.BootstrapCss.Light")
                ?? localStorage.getItem("ThemeManager.BootstrapCss");
            if (!css) {
                return;
            }

            const existing = document.getElementById("qbt-bootstrap-theme");
            if (existing) {
                existing.textContent = css;
                return;
            }

            const style = document.createElement("style");
            style.id = "qbt-bootstrap-theme";
            style.textContent = css;
            document.body.appendChild(style);
        } catch {
        }
    }

    function parseBoolean(value) {
        if (!value) {
            return false;
        }

        const normalized = value.trim().toLowerCase();
        return normalized === "true";
    }

    function normalizeStorageString(value) {
        if (!value) {
            return "";
        }

        // BrowserStorageService stores JSON for SetItemAsync, while SetItemAsStringAsync stores raw strings.
        // This normalizes either representation to a plain string value.
        const trimmed = value.trim();
        if (trimmed.startsWith("\"") && trimmed.endsWith("\"")) {
            try {
                const parsed = JSON.parse(trimmed);
                return typeof parsed === "string" ? parsed.trim() : "";
            } catch {
                return trimmed.substring(1, trimmed.length - 1).trim();
            }
        }

        return trimmed;
    }

    function isValidFontFamily(value) {
        if (!value) {
            return false;
        }

        // Matches ThemeFontCatalog's font family validation to prevent injection.
        return /^[a-zA-Z0-9][a-zA-Z0-9\\s\\-]*$/.test(value);
    }

    function buildFontId(fontFamily) {
        if (!fontFamily) {
            return "qbt-font-default";
        }

        const normalized = fontFamily.trim().toLowerCase().split("").map(ch => {
            return /[a-z0-9]/.test(ch) ? ch : "-";
        }).join("");

        return `qbt-font-${normalized}`;
    }

    function buildGoogleFontUrl(fontFamily) {
        const encoded = encodeURIComponent(fontFamily).replace(/%20/g, "+");
        return `https://fonts.googleapis.com/css2?family=${encoded}&display=swap`;
    }

    function ensureTrailingSlash(value) {
        if (!value) {
            return "";
        }

        return value.endsWith("/") ? value : `${value}/`;
    }

    function hasValidHttpScheme(value) {
        if (!value) {
            return false;
        }

        try {
            const parsed = new URL(value);
            return parsed.protocol === "https:" || parsed.protocol === "http:";
        } catch {
            return false;
        }
    }

    function buildCdnUrl(name) {
        const relative = name.startsWith("_framework/") ? name : `_framework/${name}`;
        return `${state.baseUrl}${relative}`;
    }

    function getFetchOptions(integrity) {
        if (!integrity) {
            return undefined;
        }

        return { integrity };
    }

    function logCdnFailure(message, error) {
        if (!state.warnedFailure) {
            console.error(`${logPrefix} ${message}`, error);
            state.warnedFailure = true;
        } else {
            console.debug(`${logPrefix} ${message}`, error);
        }
    }

    function shouldUseCdn(type) {
        return state.enabled && !state.cdnFailed && cdnAssetTypes.has(type);
    }

    function startBlazor() {
        if (!window.Blazor) {
            console.error(`${logPrefix} Blazor runtime script did not load.`);
            return;
        }

        const startPromise = Blazor.start({
            loadBootResource: (type, name, defaultUri, integrity) => {
                if (!shouldUseCdn(type)) {
                    return defaultUri;
                }

                const cdnUrl = buildCdnUrl(name);
                return fetch(cdnUrl, getFetchOptions(integrity))
                    .then(response => {
                        if (response.ok) {
                            return response;
                        }

                        throw new Error(`HTTP ${response.status} ${response.statusText}`.trim());
                    })
                    .catch(error => {
                        logCdnFailure(`CDN fetch failed for ${cdnUrl}. Falling back to local assets.`, error);
                        state.cdnFailed = true;
                        return defaultUri;
                    });
            }
        });

        setupLoadingProgress(startPromise);
    }

    function setupLoadingProgress(startPromise) {
        const root = document.documentElement;
        let intervalId = 0;

        function updateProgress() {
            const computed = getComputedStyle(root);
            const rawPercent = computed.getPropertyValue("--blazor-load-percentage");
            const rawText = computed.getPropertyValue("--blazor-load-percentage-text");

            const startingText = typeof rawText === "string" && rawText.toLowerCase().includes("starting");

            root.style.setProperty("--qbt-load-percentage", `${rawPercent}`);
            root.style.setProperty("--qbt-load-percentage-text", startingText ? "\"Starting...\"" : (rawText || ""));
        }

        intervalId = window.setInterval(updateProgress, 5);
        updateProgress();

        startPromise
            .then(() => {
                if (intervalId) {
                    window.clearInterval(intervalId);
                }

                root.style.setProperty("--qbt-load-percentage", "100%");
                root.style.setProperty("--qbt-load-percentage-text", "\"Starting...\"");
            })
            .catch(() => {
                if (intervalId) {
                    window.clearInterval(intervalId);
                }
            });
    }

    applyBootstrapFont();
    applyBootstrapTheme();

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startBlazor);
    } else {
        startBlazor();
    }
}());
