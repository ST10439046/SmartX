import React from 'react';
import { X, Check, AlertCircle, Info } from 'lucide-react';

const Toast = ({ notification, onDismiss }) => {
  const { id, message, type } = notification;

  const typeConfig = {
    success: {
      bgColor: 'bg-success-50 dark:bg-success-900/20',
      borderColor: 'border-success-300 dark:border-success-700',
      textColor: 'text-success-800 dark:text-success-200',
      icon: Check,
    },
    error: {
      bgColor: 'bg-danger-50 dark:bg-danger-900/20',
      borderColor: 'border-danger-300 dark:border-danger-700',
      textColor: 'text-danger-800 dark:text-danger-200',
      icon: AlertCircle,
    },
    warning: {
      bgColor: 'bg-warning-50 dark:bg-warning-900/20',
      borderColor: 'border-warning-300 dark:border-warning-700',
      textColor: 'text-warning-800 dark:text-warning-200',
      icon: AlertCircle,
    },
    info: {
      bgColor: 'bg-primary-50 dark:bg-primary-900/20',
      borderColor: 'border-primary-300 dark:border-primary-700',
      textColor: 'text-primary-800 dark:text-primary-200',
      icon: Info,
    },
  };

  const config = typeConfig[type] || typeConfig.info;
  const Icon = config.icon;

  return (
    <div
      className={`toast-enter ${config.bgColor} border ${config.borderColor} ${config.textColor} rounded-lg px-4 py-3 flex items-center gap-3 shadow-lg max-w-sm`}
      role="alert"
    >
      <Icon size={20} className="flex-shrink-0" />
      <span className="flex-1 text-sm">{message}</span>
      <button
        onClick={() => onDismiss(id)}
        className="flex-shrink-0 p-1 hover:bg-white/20 rounded transition-colors"
        aria-label="Dismiss"
      >
        <X size={18} />
      </button>
    </div>
  );
};

export const ToastContainer = ({ notifications, onDismiss }) => {
  return (
    <div className="fixed top-4 right-4 z-50 flex flex-col gap-2 pointer-events-auto">
      {notifications.map((notif) => (
        <Toast
          key={notif.id}
          notification={notif}
          onDismiss={onDismiss}
        />
      ))}
    </div>
  );
};

export default Toast;
