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

        Blazor.start({
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
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startBlazor);
    } else {
        startBlazor();
    }
}());
