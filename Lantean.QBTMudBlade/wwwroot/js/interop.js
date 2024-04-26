const qbt = {};

qbt.triggerFileDownload = (url, fileName) => {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
}

qbt.getBoundingClientRect = (id) => {
    const element = document.getElementById(id);

    return element.getBoundingClientRect();
}

window.qbt = qbt;