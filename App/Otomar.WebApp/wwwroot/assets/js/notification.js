const NotificationManager = {
    connection: null,
    unreadCount: 0,
    currentPage: 1,
    isLoading: false,
    hasMore: true,
    activeDropdown: null,
    activeList: null,

    // NotificationType enum (C# tarafıyla senkron)
    NotificationType: {
        Info: 0,
        ListSearchCreated: 1,
        ListSearchAnswered: 2,
        OrderStatusChanged: 3
    },

    async init() {
        const bells = document.querySelectorAll('[data-notification-bell]');
        if (!bells.length) return;

        await this.startSignalR();
        await this.loadUnreadCount();

        bells.forEach(bell => {
            bell.addEventListener('click', (e) => {
                e.stopPropagation();
                const dropdown = bell.querySelector('.notification-dropdown');
                const list = bell.querySelector('.notification-dropdown__body');
                if (dropdown) {
                    this.toggleDropdown(dropdown, list);
                }
            });
        });

        document.addEventListener('click', () => {
            document.querySelectorAll('.notification-dropdown').forEach(d => {
                d.style.display = 'none';
            });
            this.activeDropdown = null;
            this.activeList = null;
        });

        document.querySelectorAll('.notification-dropdown__body').forEach(list => {
            list.addEventListener('scroll', () => {
                if (list.scrollTop + list.clientHeight >= list.scrollHeight - 10) {
                    this.loadMore();
                }
            });
        });

        // Dropdown içi tıklamalarının kapanmasını engelle
        document.querySelectorAll('.notification-dropdown').forEach(dropdown => {
            dropdown.addEventListener('click', (e) => e.stopPropagation());
        });
    },

    async startSignalR() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/notification')
                .withAutomaticReconnect()
                .build();

            this.connection.on('ReceiveNotification', (notification) => {
                this.unreadCount++;
                this.updateBadge();
                this.prependNotification(notification);
                this.showToast(notification);
            });

            await this.connection.start();
        } catch (err) {
            console.error('SignalR baglanti hatasi:', err);
        }
    },

    async loadUnreadCount() {
        try {
            const data = await makeRequest('/bildirimler/okunmamis-sayi', 'GET');
            if (data) {
                this.unreadCount = data.count ?? data.Count ?? 0;
                this.updateBadge();
            }
        } catch (_) { }
    },

    async loadNotifications(targetList) {
        if (this.isLoading) return;
        this.isLoading = true;
        this.currentPage = 1;
        this.hasMore = true;

        try {
            const data = await makeRequest('/bildirimler/listele?pageNumber=1&pageSize=20', 'GET');
            const list = targetList || this.activeList;
            if (!list) return;

            if (data && data.data && data.data.length > 0) {
                list.innerHTML = '';
                data.data.forEach(n => list.appendChild(this.createNotificationElement(n)));
                this.hasMore = data.hasNext ?? data.HasNext ?? false;
            } else {
                list.innerHTML = this.createEmptyState();
                this.hasMore = false;
            }
        } catch (_) { }
        finally {
            this.isLoading = false;
        }
    },

    async loadMore() {
        if (this.isLoading || !this.hasMore || !this.activeList) return;
        this.isLoading = true;
        this.currentPage++;

        try {
            const data = await makeRequest(`/bildirimler/listele?pageNumber=${this.currentPage}&pageSize=20`, 'GET');
            if (!data || !data.data) return;

            data.data.forEach(n => this.activeList.appendChild(this.createNotificationElement(n)));
            this.hasMore = data.hasNext ?? data.HasNext ?? false;
        } catch (_) {
            this.currentPage--;
        } finally {
            this.isLoading = false;
        }
    },

    createNotificationElement(notification) {
        const div = document.createElement('div');
        const isRead = notification.isRead ?? notification.IsRead ?? false;
        const type = notification.type ?? notification.Type ?? 0;
        div.className = `notification-item${isRead ? '' : ' notification-item--unread'}`;
        div.dataset.id = notification.id ?? notification.Id;

        const redirectUrl = notification.redirectUrl ?? notification.RedirectUrl ?? '';
        const title = notification.title ?? notification.Title ?? '';
        const message = notification.message ?? notification.Message ?? '';
        const createdAt = notification.createdAt ?? notification.CreatedAt ?? '';

        // Icon
        const iconHtml = this.getNotificationIcon(type);

        // Mark read button (sadece okunmamışlarda)
        const actionsHtml = !isRead
            ? `<div class="notification-item__actions">
                   <button class="notification-item__mark-read" title="Okundu olarak işaretle" data-mark-id="${div.dataset.id}">
                       ${this.getSvgIcon('check')}
                   </button>
               </div>`
            : '';

        div.innerHTML = `
            <div class="notification-item__icon ${this.getIconClass(type)}">
                ${iconHtml}
            </div>
            <div class="notification-item__content">
                <div class="notification-item__title">${this.escapeHtml(title)}</div>
                <div class="notification-item__message">${this.escapeHtml(message)}</div>
                <div class="notification-item__time">${this.formatTime(createdAt)}</div>
            </div>
            ${actionsHtml}
        `;

        // İçerik alanına tıklayınca redirect
        div.querySelector('.notification-item__content').addEventListener('click', (e) => {
            e.stopPropagation();
            this.handleClick(div.dataset.id, redirectUrl);
        });

        // İkon alanına tıklayınca da redirect
        div.querySelector('.notification-item__icon').addEventListener('click', (e) => {
            e.stopPropagation();
            this.handleClick(div.dataset.id, redirectUrl);
        });

        // Okundu butonu
        const markBtn = div.querySelector('.notification-item__mark-read');
        if (markBtn) {
            markBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.markAsRead(markBtn.dataset.markId);
            });
        }

        return div;
    },

    getIconClass(type) {
        switch (type) {
            case this.NotificationType.ListSearchCreated: return 'notification-item__icon--list-search-created';
            case this.NotificationType.ListSearchAnswered: return 'notification-item__icon--list-search-answered';
            case this.NotificationType.OrderStatusChanged: return 'notification-item__icon--order';
            default: return 'notification-item__icon--info';
        }
    },

    getNotificationIcon(type) {
        switch (type) {
            case this.NotificationType.ListSearchCreated:
                return this.getSvgIcon('file-plus');
            case this.NotificationType.ListSearchAnswered:
                return this.getSvgIcon('file-check');
            case this.NotificationType.OrderStatusChanged:
                return this.getSvgIcon('package');
            default:
                return this.getSvgIcon('bell');
        }
    },

    getSvgIcon(name) {
        const icons = {
            'file-plus': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="12" y1="18" x2="12" y2="12"/><line x1="9" y1="15" x2="15" y2="15"/></svg>',
            'file-check': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><polyline points="9 15 11 17 15 13"/></svg>',
            'package': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="16.5" y1="9.4" x2="7.5" y2="4.21"/><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/></svg>',
            'bell': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>',
            'check': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>',
            'check-double': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="18 6 9 17 4 12"/><polyline points="22 6 13 17"/></svg>',
            'bell-empty': '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/><line x1="1" y1="1" x2="23" y2="23"/></svg>'
        };
        return icons[name] || icons['bell'];
    },

    createEmptyState() {
        return `<div class="notification-empty">
            ${this.getSvgIcon('bell-empty')}
            <div>Bildirim bulunmuyor</div>
        </div>`;
    },

    async markAsRead(id) {
        try {
            await makeRequest(`/bildirimler/${id}/oku`, 'PUT');
            document.querySelectorAll(`.notification-item[data-id="${id}"]`).forEach(item => {
                item.classList.remove('notification-item--unread');
                // Okundu butonunu kaldır
                const actions = item.querySelector('.notification-item__actions');
                if (actions) actions.remove();
            });
            this.unreadCount = Math.max(0, this.unreadCount - 1);
            this.updateBadge();
        } catch (_) { }
    },

    async markAllAsRead() {
        try {
            await makeRequest('/bildirimler/tumu-oku', 'PUT');
            document.querySelectorAll('.notification-item--unread').forEach(el => {
                el.classList.remove('notification-item--unread');
                const actions = el.querySelector('.notification-item__actions');
                if (actions) actions.remove();
            });
            this.unreadCount = 0;
            this.updateBadge();
        } catch (_) { }
    },

    handleClick(id, redirectUrl) {
        this.markAsRead(id);
        if (redirectUrl) {
            window.location.href = redirectUrl;
        }
    },

    toggleDropdown(dropdown, list) {
        document.querySelectorAll('.notification-dropdown').forEach(d => {
            if (d !== dropdown) d.style.display = 'none';
        });

        const isVisible = dropdown.style.display !== 'none';
        dropdown.style.display = isVisible ? 'none' : 'flex';

        if (!isVisible) {
            this.activeDropdown = dropdown;
            this.activeList = list;
            this.loadNotifications(list);
        } else {
            this.activeDropdown = null;
            this.activeList = null;
        }
    },

    updateBadge() {
        document.querySelectorAll('.notification-badge').forEach(el => {
            el.textContent = this.unreadCount > 99 ? '99+' : this.unreadCount;
            el.style.display = this.unreadCount > 0 ? '' : 'none';
        });
    },

    prependNotification(notification) {
        document.querySelectorAll('.notification-dropdown__body').forEach(list => {
            const empty = list.querySelector('.notification-empty');
            if (empty) empty.remove();

            const el = this.createNotificationElement(notification);
            list.prepend(el);
        });
    },

    showToast(notification) {
        if (typeof toastr !== 'undefined') {
            const title = notification.title ?? notification.Title ?? 'Bildirim';
            const message = notification.message ?? notification.Message ?? '';
            toastr.info(message, title, {
                timeOut: 5000,
                closeButton: true,
                progressBar: true,
                onclick: () => {
                    const redirectUrl = notification.redirectUrl ?? notification.RedirectUrl;
                    if (redirectUrl) window.location.href = redirectUrl;
                }
            });
        }
    },

    formatTime(dateStr) {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        const now = new Date();
        const diff = Math.floor((now - date) / 1000);

        if (diff < 60) return 'Az önce';
        if (diff < 3600) return `${Math.floor(diff / 60)} dk önce`;
        if (diff < 86400) return `${Math.floor(diff / 3600)} saat önce`;
        if (diff < 604800) return `${Math.floor(diff / 86400)} gün önce`;

        return date.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    },

    escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
};

document.addEventListener('DOMContentLoaded', () => NotificationManager.init());
