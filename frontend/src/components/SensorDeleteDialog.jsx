import React from 'react';
import { Trash2, AlertCircle, Loader2 } from 'lucide-react';

const SensorDeleteDialog = ({ isOpen, sensor, onConfirm, onCancel, isDeleting = false }) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-md rounded-lg bg-white p-6 dark:bg-gray-800 shadow-xl">
        {/* Icon */}
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-red-100 dark:bg-red-900/30">
          <Trash2 className="h-6 w-6 text-red-600 dark:text-red-400" />
        </div>

        {/* Title */}
        <h3 className="mt-4 text-center text-lg font-semibold text-gray-900 dark:text-white">
          Delete Sensor
        </h3>

        {/* Message */}
        <div className="mt-4 space-y-3">
          <p className="text-center text-sm text-gray-600 dark:text-gray-400">
            Are you sure you want to delete this sensor? This action cannot be undone.
          </p>

          {sensor && (
            <div className="rounded-lg bg-gray-50 p-4 dark:bg-gray-700/50">
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="font-medium text-gray-700 dark:text-gray-300">
                    MAC Address:
                  </span>
                  <span className="font-mono text-gray-900 dark:text-white">
                    {sensor.macaddress}
                  </span>
                </div>
                {sensor.nodeId && (
                  <div className="flex justify-between">
                    <span className="font-medium text-gray-700 dark:text-gray-300">
                      Node ID:
                    </span>
                    <span className="text-gray-900 dark:text-white">
                      {sensor.nodeId}
                    </span>
                  </div>
                )}
                {sensor.category && (
                  <div className="flex justify-between">
                    <span className="font-medium text-gray-700 dark:text-gray-300">
                      Category:
                    </span>
                    <span className="text-gray-900 dark:text-white">
                      {sensor.category}
                    </span>
                  </div>
                )}
                {sensor.zone && (
                  <div className="flex justify-between">
                    <span className="font-medium text-gray-700 dark:text-gray-300">
                      Zone:
                    </span>
                    <span className="text-gray-900 dark:text-white">
                      {sensor.zone}
                    </span>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Warning */}
          <div className="flex items-start gap-3 rounded-lg bg-red-50 p-3 dark:bg-red-900/20">
            <AlertCircle className="h-5 w-5 flex-shrink-0 text-red-600 dark:text-red-400 mt-0.5" />
            <p className="text-sm text-red-800 dark:text-red-300">
              All associated telemetry data, alerts, and commands related to this sensor will also be removed.
            </p>
          </div>
        </div>

        {/* Buttons */}
        <div className="mt-6 flex gap-3">
          <button
            onClick={onCancel}
            disabled={isDeleting}
            className="flex-1 rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700"
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            disabled={isDeleting}
            className="flex-1 inline-flex items-center justify-center gap-2 rounded-lg bg-red-600 px-4 py-2 font-medium text-white hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isDeleting && <Loader2 className="h-4 w-4 animate-spin" />}
            Delete Sensor
          </button>
        </div>
      </div>
    </div>
  );
};

export default SensorDeleteDialog;
