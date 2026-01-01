import React, { useState, useEffect, useRef } from 'react';
import { getNotifications, markNotificationAsRead, Notification } from '../services/notificationService';

interface NotificationBellProps {
  className?: string;
}

const NotificationBell: React.FC<NotificationBellProps> = ({ className = '' }) => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [showDropdown, setShowDropdown] = useState(false);
  const [loading, setLoading] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Bildirimleri yükle
  const loadNotifications = async () => {
    setLoading(true);
    try {
      const data = await getNotifications();
      setNotifications(data);
    } catch (error) {
      console.error('Bildirimler yüklenirken hata:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadNotifications();
    // Her 30 saniyede bir bildirimleri yenile
    const interval = setInterval(loadNotifications, 30000);
    return () => clearInterval(interval);
  }, []);

  // Dışarı tıklanınca dropdown'ı kapat
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setShowDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  // Okunmamış bildirim sayısı
  const unreadCount = notifications.filter(n => !n.isRead).length;

  // Bildirimi okundu olarak işaretle
  const handleMarkAsRead = async (notificationId: number) => {
    try {
      await markNotificationAsRead(notificationId);
      setNotifications(prev =>
        prev.map(n => n.id === notificationId ? { ...n, isRead: true } : n)
      );
    } catch (error) {
      console.error('Bildirim okundu olarak işaretlenirken hata:', error);
    }
  };

  // Tüm bildirimleri okundu olarak işaretle
  const handleMarkAllAsRead = async () => {
    const unreadNotifications = notifications.filter(n => !n.isRead);
    try {
      await Promise.all(unreadNotifications.map(n => markNotificationAsRead(n.id)));
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    } catch (error) {
      console.error('Bildirimler okundu olarak işaretlenirken hata:', error);
    }
  };

  return (
    <div className={`relative ${className}`} ref={dropdownRef}>
      {/* Zil İkonu */}
      <button
        onClick={() => setShowDropdown(!showDropdown)}
        className="relative p-2 rounded-full hover:bg-white/20 transition-colors"
        aria-label="Bildirimler"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          className="h-6 w-6"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
          />
        </svg>
        {/* Okunmamış bildirim badge */}
        {unreadCount > 0 && (
          <span className="absolute top-0 right-0 bg-red-500 text-white text-xs rounded-full h-5 w-5 flex items-center justify-center">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown */}
      {showDropdown && (
        <div className="absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg border border-slate-200 z-50 max-h-96 overflow-hidden flex flex-col">
          {/* Header */}
          <div className="p-4 border-b border-slate-200 flex items-center justify-between">
            <h3 className="font-semibold text-slate-900">Bildirimler</h3>
            {unreadCount > 0 && (
              <button
                onClick={handleMarkAllAsRead}
                className="text-sm text-blue-600 hover:text-blue-800"
              >
                Tümünü okundu işaretle
              </button>
            )}
          </div>

          {/* Bildirimler Listesi */}
          <div className="overflow-y-auto flex-1">
            {loading ? (
              <div className="p-4 text-center text-slate-500">Yükleniyor...</div>
            ) : notifications.length === 0 ? (
              <div className="p-4 text-center text-slate-500">Bildirim yok</div>
            ) : (
              <div className="divide-y divide-slate-200">
                {notifications.map((notification) => (
                  <div
                    key={notification.id}
                    className={`p-4 hover:bg-slate-50 cursor-pointer ${
                      !notification.isRead ? 'bg-blue-50' : ''
                    }`}
                    onClick={() => !notification.isRead && handleMarkAsRead(notification.id)}
                  >
                    <div className="flex items-start gap-3">
                      <div className="flex-1">
                        <p className="font-semibold text-sm text-slate-900">
                          {notification.title}
                        </p>
                        <p className="text-sm text-slate-600 mt-1">
                          {notification.message}
                        </p>
                        <p className="text-xs text-slate-400 mt-2">
                          {new Date(notification.createdAt).toLocaleString('tr-TR')}
                        </p>
                      </div>
                      {!notification.isRead && (
                        <div className="h-2 w-2 bg-blue-600 rounded-full mt-1"></div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default NotificationBell;

