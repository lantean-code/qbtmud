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

window.qbt.getBoundingClientRect = (id) => {
    const element = document.getElementById(id);

    return element.getBoundingClientRect();
}

window.qbt.open = (url, target) => {
    window.open(url, target);
}

window.qbt.renderPiecesBar = (id, hash, pieces, downloadingColor, haveColor, borderColor) => {
    const parentElement = document.getElementById(id);
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

    if (parentElement && !parentElement.hasChildNodes()) {
        const el = window.qbt.piecesBar.createElement();
        parentElement.appendChild(el);
    }

    window.qbt.piecesBar.setPieces(pieces);
}

window.qbt.copyTextToClipboard = (text) => {
    if (!navigator.clipboard) {
        return fallbackCopyTextToClipboard(text);
    }
    return navigator.clipboard.writeText(text);
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
