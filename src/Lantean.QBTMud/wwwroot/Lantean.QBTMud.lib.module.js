export function beforeWebStart(options, extensions) {
    beforeStart(options, extensions);
}

export function afterWebStarted(blazor) {
    afterStarted(blazor)
}

export function beforeStart(options, extensions) {
    console.log("Injecting longpress.js");

    const element = document.createElement('script');
    element.src = "js/longpress.js";
    element.async = true;
    document.body.appendChild(element);
}

export function afterStarted(blazor) {
    console.log("Registering longpress.js");

    blazor.registerCustomEventType('longpress', {
        createEventArgs: event => {
            return {
                bubbles: event.bubbles,
                cancelable: event.cancelable,
                screenX: event.detail.screenX,
                screenY: event.detail.screenY,
                clientX: event.detail.clientX,
                clientY: event.detail.clientY,
                offsetX: event.detail.offsetX,
                offsetY: event.detail.offsetY,
                pageX: event.detail.pageX,
                pageY: event.detail.pageY,
                sourceElement: event.srcElement.localName,
                targetElement: event.target.localName,
                timeStamp: event.timeStamp,
                type: event.type,
            };
        }
    });
}
