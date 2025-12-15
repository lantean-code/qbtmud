if (window.qbt === undefined) {
    window.qbt = {};
}

window.qbt.triggerFileDownload = (url, fileName) => {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
}

window.qbt.getBoundingClientRect = (selector) => {
    const element = getElementBySelector(selector);

    if (!element) {
        return null;
    }

    const rect = element.getBoundingClientRect();
    return rect;
}

window.qbt.getInnerDimensions = (selector) => {
    const element = getElementBySelector(selector);

    if (!element) {
        return null;
    }

    const computedStyle = getComputedStyle(element);

    const paddingX = parseFloat(computedStyle.paddingLeft) + parseFloat(computedStyle.paddingRight);
    const paddingY = parseFloat(computedStyle.paddingTop) + parseFloat(computedStyle.paddingBottom);

    const borderX = parseFloat(computedStyle.borderLeftWidth) + parseFloat(computedStyle.borderRightWidth);
    const borderY = parseFloat(computedStyle.borderTopWidth) + parseFloat(computedStyle.borderBottomWidth);

    // Element width and height minus padding and border

    return {
        height: element.offsetHeight - paddingY - borderY,
        width: element.offsetWidth - paddingX - borderX,
    };
}

window.qbt.getWindowSize = () => {
    return {
        height: window.innerHeight,
        width: window.innerWidth,
    };
}

window.qbt.open = (url, target) => {
    window.open(url, target);
}

window.qbt.registerMagnetHandler = (templateUrl) => {
    if (typeof navigator.registerProtocolHandler !== "function") {
        if (window.location.protocol !== "https:") {
            return { status: "insecure" };
        }

        return { status: "unsupported" };
    }

    try {
        navigator.registerProtocolHandler("magnet", templateUrl, "qBittorrent WebUI magnet handler");
        return { status: "success" };
    } catch (error) {
        if (window.location.protocol !== "https:") {
            return { status: "insecure" };
        }

        return { status: "error", message: error?.message ?? null };
    }
};

window.qbt.renderPiecesBar = (id, hash, pieces, downloadingColor, haveColor, borderColor) => {
    const parentElement = document.getElementById(id);
    if (!parentElement) {
        return;
    }
    if (window.qbt.hash !== hash) {
        if (parentElement) {
            while (parentElement.lastElementChild) {
                parentElement.removeChild(parentElement.lastElementChild);
            }
        }
        window.qbt.hash = hash;
        const options = {
            height: 24
        };
        if (downloadingColor) {
            options.downloadingColor = downloadingColor;
        }
        if (haveColor) {
            options.haveColor = haveColor;
        }
        if (borderColor) {
            options.borderColor = borderColor;
        }
        window.qbt.piecesBar = new window.qbt.PiecesBar([], options);
        window.qbt.piecesBar.clear();
    }

    if (!parentElement.hasChildNodes()) {
        const el = window.qbt.piecesBar.createElement();
        parentElement.appendChild(el);
    }

    window.qbt.piecesBar.setPieces(pieces);
}

window.qbt.renderPiecesCanvas = (canvasId, width, height, columns, cellSize, pieces, downloadedColor, downloadingColor, pendingColor) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        return;
    }

    const ctx = canvas.getContext('2d');
    if (!ctx) {
        return;
    }

    const dpr = window.devicePixelRatio || 1;
    const w = Math.max(1, Math.round(width));
    const h = Math.max(1, Math.round(height));
    canvas.width = w * dpr;
    canvas.height = h * dpr;
    canvas.style.width = `${width}px`;
    canvas.style.height = `${height}px`;

    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.scale(dpr, dpr);

    const gap = Math.max(0, Math.floor(Math.max(1, cellSize) * 0.08));
    const drawSize = Math.max(1, cellSize - gap);
    const offset = gap > 0 ? gap / 2 : 0;

    for (let index = 0; index < pieces.length; index++) {
        const state = pieces[index];
        let fillStyle;
        switch (state) {
            case 2:
                fillStyle = downloadedColor;
                break;
            case 1:
                fillStyle = downloadingColor;
                break;
            default:
                fillStyle = pendingColor;
                break;
        }

        const column = index % columns;
        const row = Math.floor(index / columns);
        const x = (column * cellSize) + offset;
        const y = (row * cellSize) + offset;

        ctx.fillStyle = fillStyle;
        ctx.fillRect(x, y, drawSize, drawSize);
    }
}

window.qbt.copyTextToClipboard = (text) => {
    if (!navigator.clipboard) {
        return fallbackCopyTextToClipboard(text);
    }
    return navigator.clipboard.writeText(text);
}

window.qbt.clearSelection = () => {
    if (window.getSelection) {
        if (window.getSelection().empty) {
            // Chrome
            window.getSelection().empty();
        } else if (window.getSelection().removeAllRanges) {
            // Firefox
            window.getSelection().removeAllRanges();
        }
    } else if (document.selection) {
        // IE
        document.selection.empty();
    }
}

let supportedEvents = new Map();
let focusInstance = null;

document.addEventListener('keyup', event => {
    const key = getKey(event);

    const references = supportedEvents.get(key);
    if (!references) {
        return;
    }

    references.forEach(dotNetObjectReference => {
        if (focusInstance && dotNetObjectReference._id != focusInstance._id) {
            return;
        }
        dotNetObjectReference.invokeMethodAsync('HandleKeyPressEvent', {
            key: event.key,
            code: event.code,
            altKey: event.altKey,
            ctrlKey: event.ctrlKey,
            metaKey: event.metaKey,
            shiftKey: event.shiftKey,
        }).catch(error => {
            console.error("Error handling key press:", error);
        });
    });
});

window.qbt.registerKeypressEvent = (keyboardEventArgs, dotNetObjectReference) => {
    const key = getKey(keyboardEventArgs);

    const references = supportedEvents.get(key);
    if (references) {
        references.set(dotNetObjectReference._id, dotNetObjectReference);
    }
    else {
        const references = new Map();
        references.set(dotNetObjectReference._id, dotNetObjectReference);
        supportedEvents.set(key, references);
    }
}

window.qbt.unregisterKeypressEvent = (keyboardEventArgs, dotNetObjectReference) => {
    const key = getKey(keyboardEventArgs);

    const references = supportedEvents.get(key);
    if (!references) {
        return;
    }

    references.delete(dotNetObjectReference._id);
}

window.qbt.keyPressFocusInstance = dotNetObjectReference => {
    focusInstance = dotNetObjectReference;
}

window.qbt.keyPressUnFocusInstance = dotNetObjectReference => {
    focusInstance = null;
}

function getKey(keyboardEvent) {
    return keyboardEvent.key + (keyboardEvent.ctrlKey ? '1' : '0') + (keyboardEvent.shiftKey ? '1' : '0') + (keyboardEvent.altKey ? '1' : '0') + (keyboardEvent.metaKey ? '1' : '0') + (keyboardEvent.repeat ? '1' : '0');
}

function fallbackCopyTextToClipboard(text) {
    const textArea = document.createElement("textarea");
    textArea.value = text;

    // Avoid scrolling to bottom
    textArea.style.top = "0";
    textArea.style.left = "0";
    textArea.style.position = "fixed";

    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();

    let error;
    try {
        document.execCommand('copy');
    } catch (err) {
        error = err;
    }

    document.body.removeChild(textArea);

    return new Promise((resolve, reject) => {
        if (error) {
            reject(error);
        } else {
            resolve();
        }
    })
}

function getElementBySelector(selector) {
    const identifier = selector[0];
    let element;

    if (identifier == '#') {
        element = document.getElementById(selector.substring(1));
    } else if (identifier == '.') {
        element = document.getElementsByClassName(selector.substring(1))[0];
    }

    return element;
}
