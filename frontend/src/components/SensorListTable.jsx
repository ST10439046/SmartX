import React from 'react';
import { Edit2, Trash2, Eye, CheckCircle, XCircle } from 'lucide-react';
import { Link } from 'react-router-dom';

const SensorListTable = ({ sensors, onEdit, onDelete }) => {
  const getStatusColor = (isActive) => {
    return isActive
      ? 'bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-400'
      : 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-400';
  };

  const getCategoryColor = (category) => {
    const colors = {
      Environmental: 'bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
      Power: 'bg-yellow-50 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400',
      Actuator: 'bg-purple-50 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400',
      Temperature: 'bg-orange-50 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
      Humidity: 'bg-cyan-50 text-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-400',
    };
    return colors[category] || 'bg-gray-50 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400';
  };

  return (
    <div className="overflow-hidden rounded-lg border border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b border-gray-200 bg-gray-50 dark:border-gray-700 dark:bg-gray-900/50">
              <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900 dark:text-white">
                MAC Address
              </th>
              <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900 dark:text-white">
                Node ID
              </th>
              <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900 dark:text-white">
                Category
              </th>
              <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900 dark:text-white">
                Location
              </th>
              <th className="px-6 py-3 text-left text-sm font-semibold text-gray-900 dark:text-white">
                Status
              </th>
              <th className="px-6 py-3 text-right text-sm font-semibold text-gray-900 dark:text-white">
                Actions
              </th>
            </tr>
          </thead>
          <tbody>
            {sensors.map((sensor) => (
              <tr
                key={sensor.macaddress}
                className="border-b border-gray-200 hover:bg-gray-50 dark:border-gray-700 dark:hover:bg-gray-700/50 transition-colors"
              >
                <td className="px-6 py-4 text-sm font-medium text-gray-900 dark:text-white">
                  {sensor.macaddress}
                </td>
                <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">
                  {sensor.nodeId || '-'}
                </td>
                <td className="px-6 py-4 text-sm">
                  <span
                    className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-medium ${getCategoryColor(
                      sensor.category
                    )}`}
                  >
                    {sensor.category || 'Unknown'}
                  </span>
                </td>
                <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">
                  <div className="space-y-1">
                    {sensor.zone && <div>{sensor.zone}</div>}
                    {sensor.room && <div className="text-xs">{sensor.room}</div>}
                  </div>
                </td>
                <td className="px-6 py-4">
                  <div className="flex items-center gap-2">
                    {sensor.isActive ? (
                      <>
                        <CheckCircle className="h-4 w-4 text-green-600" />
                        <span className="text-sm text-green-700 dark:text-green-400">
                          Active
                        </span>
                      </>
                    ) : (
                      <>
                        <XCircle className="h-4 w-4 text-red-600" />
                        <span className="text-sm text-red-700 dark:text-red-400">
                          Inactive
                        </span>
                      </>
                    )}
                  </div>
                </td>
                <td className="px-6 py-4 text-right">
                  <div className="flex items-center justify-end gap-2">
                    <Link
                      to={`/sensor/${sensor.macaddress}`}
                      title="View Details"
                      className="inline-flex items-center gap-1 rounded-lg border border-gray-300 p-2 text-gray-700 hover:bg-gray-100 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700 transition-colors"
                    >
                      <Eye className="h-4 w-4" />
                    </Link>
                    <button
                      onClick={() => onEdit(sensor)}
                      title="Edit Sensor"
                      className="inline-flex items-center gap-1 rounded-lg border border-gray-300 p-2 text-gray-700 hover:bg-gray-100 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700 transition-colors"
                    >
                      <Edit2 className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => onDelete(sensor)}
                      title="Delete Sensor"
                      className="inline-flex items-center gap-1 rounded-lg border border-red-300 p-2 text-red-600 hover:bg-red-50 dark:border-red-600/50 dark:text-red-400 dark:hover:bg-red-900/20 transition-colors"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default SensorListTable;
