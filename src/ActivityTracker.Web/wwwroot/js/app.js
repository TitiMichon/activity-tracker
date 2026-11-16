// Small JS helpers called from Blazor via IJSRuntime.
window.tracker = {
    storageGet: (key) => localStorage.getItem(key),
    storageSet: (key, value) => localStorage.setItem(key, value),
    download: (fileName, text) => {
        const url = URL.createObjectURL(new Blob([text], { type: "application/json" }));
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    },
    scrollToEnd: (el) => { if (el) { el.scrollLeft = el.scrollWidth; } },
};

// Ask the browser not to evict our local data under storage pressure.
if (navigator.storage && navigator.storage.persist) {
    navigator.storage.persist().catch(() => { });
}
