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

    // ---- Reminders (local notifications, shown while the app is open) ----
    notificationPermission: () => ("Notification" in window) ? Notification.permission : "unsupported",
    requestNotifications: async () => {
        if (!("Notification" in window)) { return "unsupported"; }
        if (Notification.permission !== "default") { return Notification.permission; }
        return await Notification.requestPermission();
    },
    notify: async (title, body) => {
        if (!("Notification" in window) || Notification.permission !== "granted") { return; }
        const options = { body, icon: "icon-192.png", badge: "icon-192.png", tag: body };
        // Mobile browsers only allow notifications through the service worker.
        const registration = navigator.serviceWorker && await navigator.serviceWorker.getRegistration();
        if (registration) {
            await registration.showNotification(title, options);
        } else {
            new Notification(title, options);
        }
    },
};

// Ask the browser not to evict our local data under storage pressure.
if (navigator.storage && navigator.storage.persist) {
    navigator.storage.persist().catch(() => { });
}
