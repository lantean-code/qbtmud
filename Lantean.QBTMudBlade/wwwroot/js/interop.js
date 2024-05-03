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

window.qbt.renderPiecesBar = (id, hash, pieces) => {
    const parentElement = document.getElementById(id);
    if (window.qbt.hash !== hash) {
        if (parentElement) {
            while (parentElement.lastElementChild) {
                parentElement.removeChild(parentElement.lastElementChild);
            }
        }
        window.qbt.hash = hash;
        window.qbt.piecesBar = new window.qbt.PiecesBar([], {
            height: 24
        });
        window.qbt.piecesBar.clear();
    }

    if (parentElement && !parentElement.hasChildNodes()) {
        const el = window.qbt.piecesBar.createElement();
        parentElement.appendChild(el);
    }
    
    window.qbt.piecesBar.setPieces(pieces);
}