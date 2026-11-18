// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });

// Clicking a reminder brings the app to the front (or opens it).
self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(clients => {
        const client = clients.find(c => 'focus' in c);
        return client ? client.focus() : self.clients.openWindow(self.registration.scope);
    }));
});
