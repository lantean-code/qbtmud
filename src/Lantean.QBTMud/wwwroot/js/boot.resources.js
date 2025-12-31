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
        let lastPercent = 0;
        let displayedPercent = 0;
        let lastUpdate = performance.now();
        let startCompleted = false;
        let startingMode = false;

        function normalizeText(value, fallback) {
            if (!value) {
                return fallback;
            }

            const trimmed = value.trim();
            return trimmed.length > 0 ? trimmed : fallback;
        }

        function updateProgress() {
            const computed = getComputedStyle(root);
            const rawPercent = computed.getPropertyValue("--blazor-load-percentage");
            const rawText = computed.getPropertyValue("--blazor-load-percentage-text");
            const percentValue = Number.parseFloat(rawPercent);
            const now = performance.now();

            if (Number.isFinite(percentValue) && percentValue > lastPercent) {
                lastPercent = Math.min(percentValue, 100);
                displayedPercent = Math.min(lastPercent, 99);
                lastUpdate = now;
                startingMode = false;
            } else if (!startCompleted && !startingMode && now - lastUpdate > 700) {
                displayedPercent = 100;
                startingMode = true;
            } else if (!startCompleted && displayedPercent < 99 && now - lastUpdate > 400) {
                displayedPercent = Math.min(displayedPercent + 1, 99);
            }

            if (displayedPercent > 0 || Number.isFinite(percentValue)) {
                root.style.setProperty("--qbt-load-percentage", `${displayedPercent}%`);
                if (startCompleted || startingMode || lastPercent >= 100 || displayedPercent >= 99) {
                    root.style.setProperty("--qbt-load-percentage-text", "\"Starting...\"");
                } else {
                    root.style.setProperty("--qbt-load-percentage-text", normalizeText(rawText, "\"Loading\""));
                }
            }
        }

        intervalId = window.setInterval(updateProgress, 120);
        updateProgress();

        startPromise
            .then(() => {
                startCompleted = true;
                displayedPercent = 100;
                startingMode = true;
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

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startBlazor);
    } else {
        startBlazor();
    }
}());
