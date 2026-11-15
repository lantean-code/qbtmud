class TorrentSplitElement extends HTMLElement {
    connectedCallback() {
        this.ratio = 40;
        this.pending = Promise.resolve();
        this.scrollRestored = false;
        this.scrollOffset = 0;
        try {
            this.scrollOffset = Number(sessionStorage.getItem("TorrentList.ScrollOffset")) || 0;
        } catch {
            this.scrollOffset = 0;
        }
        this.abort = new AbortController();
        const options = { signal: this.abort.signal };
        this.resize = new ResizeObserver(() => this.measure());
        this.resize.observe(this);
        this.mutations = new MutationObserver(() => this.layout());
        this.mutations.observe(this, { childList: true, subtree: true });
        window.addEventListener("resize", () => this.measure(), options);
        this.addEventListener("pointerdown", event => this.startDrag(event), options);
        this.addEventListener("pointermove", event => this.drag(event), options);
        this.addEventListener("pointerup", event => this.endDrag(event), options);
        this.addEventListener("pointercancel", event => this.endDrag(event, true), options);
        this.addEventListener("lostpointercapture", event => this.endDrag(event, true), options);
        this.addEventListener("keydown", event => this.keyResize(event), options);
        this.addEventListener("keyup", event => {
            if (event.target.closest(".torrent-split__grip")) {
                event.stopPropagation();
            }
        }, options);
        this.addEventListener("scroll", event => {
            if (event.target.matches(".torrent-split__list .mud-table-container")) {
                this.scrollOffset = event.target.scrollTop;
            }
        }, { ...options, capture: true });
    }

    disconnectedCallback() {
        this.abort.abort();
        this.resize.disconnect();
        this.mutations.disconnect();
        this.reference = null;
        try {
            sessionStorage.setItem("TorrentList.ScrollOffset", String(this.scrollOffset));
        } catch {
            this.scrollOffset = 0;
        }
    }

    notify(method, value) {
        this.pending = this.pending.then(() => {
            if (this.reference && this.isConnected) {
                return this.reference.invokeMethodAsync(method, value);
            }
        }).catch(error => console.error("Unable to update torrent split view.", error));
    }

    measure() {
        const available = window.innerWidth >= 1024 && window.innerHeight >= 768
            && this.clientWidth >= 720 && this.clientHeight >= 560;
        this.dataset.available = String(available);
        if (available !== this.available && this.reference) {
            this.available = available;
            this.notify("SetAvailability", available);
        }
        if (!available) {
            this.dragging = null;
        }
        this.layout();
    }

    layout() {
        const list = this.querySelector(".torrent-split__list .mud-table-container");
        if (!this.scrollRestored && list && list.scrollHeight > list.clientHeight) {
            list.scrollTop = this.scrollOffset;
            this.scrollRestored = true;
        }
        const space = this.clientHeight - (this.querySelector(".torrent-split__divider")?.getBoundingClientRect().height || 6);
        const height = Math.max(240, Math.min(space - 240, space * this.ratio / 100));
        this.style.setProperty("--torrent-details-height", `${height}px`);
        const grip = this.querySelector(".torrent-split__grip");
        if (grip) {
            grip.setAttribute("aria-valuenow", String(Math.round(this.ratio)));
        }
    }

    startDrag(event) {
        const grip = event.target.closest(".torrent-split__grip");
        if (!grip || event.button !== 0 || !this.available) {
            return;
        }
        event.preventDefault();
        grip.focus();
        this.dragging = { id: event.pointerId, y: event.clientY, ratio: this.ratio };
        grip.setPointerCapture(event.pointerId);
    }

    drag(event) {
        if (!this.dragging || event.pointerId !== this.dragging.id) {
            return;
        }
        const space = this.clientHeight - (this.querySelector(".torrent-split__divider")?.getBoundingClientRect().height || 6);
        this.ratio = Math.max(30, Math.min(60, this.dragging.ratio + (this.dragging.y - event.clientY) / space * 100));
        this.layout();
    }

    endDrag(event, cancelled = false) {
        if (!this.dragging || event.pointerId !== this.dragging.id) {
            return;
        }
        if (cancelled) {
            this.ratio = this.dragging.ratio;
            this.layout();
        } else {
            this.notify("ChangeRatio", Math.round(this.ratio));
        }
        this.dragging = null;
    }

    keyResize(event) {
        if (!event.target.closest(".torrent-split__grip")) {
            return;
        }
        let ratio;
        if (event.key === "ArrowUp") {
            ratio = this.ratio + 5;
        } else if (event.key === "ArrowDown") {
            ratio = this.ratio - 5;
        } else if (event.key === "Home") {
            ratio = 40;
        } else {
            return;
        }
        event.preventDefault();
        event.stopPropagation();
        this.ratio = Math.max(30, Math.min(60, ratio));
        this.layout();
        this.notify("ChangeRatio", Math.round(this.ratio));
    }
}

if (!customElements.get("qbt-torrent-split")) {
    customElements.define("qbt-torrent-split", TorrentSplitElement);
}

export function initialize(element, reference, ratio) {
    element.reference = reference;
    element.ratio = ratio;
    element.measure();
}

export function setRatio(element, ratio) {
    element.ratio = ratio;
    element.layout();
}
