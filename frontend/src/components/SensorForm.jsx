import React, { useState } from 'react';
import { X, Loader2, AlertCircle } from 'lucide-react';
import { useForm } from '../hooks';

const SensorForm = ({ sensor, onSubmit, onCancel }) => {
  const [errors, setErrors] = useState({});

  const validate = (name, value) => {
    const newErrors = { ...errors };

    switch (name) {
      case 'macaddress':
        if (!value) {
          newErrors.macaddress = 'MAC Address is required';
        } else if (!/^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$/.test(value)) {
          newErrors.macaddress = 'Invalid MAC Address format (e.g., 00:1A:2B:3C:4D:5E)';
        } else {
          delete newErrors.macaddress;
        }
        break;
      case 'nodeId':
        if (!value) {
          newErrors.nodeId = 'Node ID is required';
        } else {
          delete newErrors.nodeId;
        }
        break;
      case 'category':
        if (!value) {
          newErrors.category = 'Category is required';
        } else {
          delete newErrors.category;
        }
        break;
      case 'zone':
        if (!value) {
          newErrors.zone = 'Zone is required';
        } else {
          delete newErrors.zone;
        }
        break;
      default:
        break;
    }

    setErrors(newErrors);
    return !newErrors[name];
  };

  const {
    values,
    touched,
    isSubmitting,
    handleChange,
    handleBlur,
    handleSubmit,
  } = useForm(
    {
      macaddress: sensor?.macaddress || '',
      nodeId: sensor?.nodeId || '',
      category: sensor?.category || '',
      zone: sensor?.zone || '',
      room: sensor?.room || '',
      isActive: sensor?.isActive !== undefined ? sensor.isActive : true,
    },
    async (formValues) => {
      // Validate all fields before submitting
      let isValid = true;
      const fieldNames = ['macaddress', 'nodeId', 'category', 'zone'];

      for (const field of fieldNames) {
        if (!validate(field, formValues[field])) {
          isValid = false;
        }
      }

      if (isValid) {
        await onSubmit(formValues);
      }
    }
  );

  const handleFieldChange = (e) => {
    const { name, value, type, checked } = e.target;
    handleChange(e);
    validate(name, type === 'checkbox' ? checked : value);
  };

  return (
    <div className="rounded-lg border border-gray-200 bg-white p-6 dark:border-gray-700 dark:bg-gray-800">
      <div className="mb-6 flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
          {sensor ? 'Edit Sensor' : 'Add New Sensor'}
        </h2>
        <button
          onClick={onCancel}
          className="rounded-lg p-2 hover:bg-gray-100 dark:hover:bg-gray-700"
          title="Close"
        >
          <X className="h-5 w-5 text-gray-500" />
        </button>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="grid gap-6 sm:grid-cols-2">
          {/* MAC Address */}
          <div>
            <label className="block text-sm font-medium text-gray-900 dark:text-white">
              MAC Address *
            </label>
            <input
              type="text"
              name="macaddress"
              value={values.macaddress}
              onChange={handleFieldChange}
              onBlur={handleBlur}
              placeholder="00:1A:2B:3C:4D:5E"
              disabled={!!sensor}
              className={`mt-2 w-full rounded-lg border px-4 py-2 text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 dark:bg-gray-700 dark:text-white dark:placeholder-gray-400 transition-all ${
                errors.macaddress && touched.macaddress
                  ? 'border-red-500 focus:ring-red-500'
                  : 'border-gray-300 focus:ring-blue-500 dark:border-gray-600'
              } ${sensor ? 'cursor-not-allowed opacity-50' : ''}`}
            />
            {errors.macaddress && touched.macaddress && (
              <p className="mt-1 flex items-center gap-1 text-sm text-red-600 dark:text-red-400">
                <AlertCircle className="h-4 w-4" />
                {errors.macaddress}
              </p>
            )}
          </div>

          {/* Node ID */}
          <div>
            <label className="block text-sm font-medium text-gray-900 dark:text-white">
              Node ID *
            </label>
            <input
              type="text"
              name="nodeId"
              value={values.nodeId}
              onChange={handleFieldChange}
              onBlur={handleBlur}
              placeholder="e.g., ENV-01, PWR-02"
              className={`mt-2 w-full rounded-lg border px-4 py-2 text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 dark:bg-gray-700 dark:text-white dark:placeholder-gray-400 transition-all ${
                errors.nodeId && touched.nodeId
                  ? 'border-red-500 focus:ring-red-500'
                  : 'border-gray-300 focus:ring-blue-500 dark:border-gray-600'
              }`}
            />
            {errors.nodeId && touched.nodeId && (
              <p className="mt-1 flex items-center gap-1 text-sm text-red-600 dark:text-red-400">
                <AlertCircle className="h-4 w-4" />
                {errors.nodeId}
              </p>
            )}
          </div>

          {/* Category */}
          <div>
            <label className="block text-sm font-medium text-gray-900 dark:text-white">
              Category *
            </label>
            <select
              name="category"
              value={values.category}
              onChange={handleFieldChange}
              onBlur={handleBlur}
              className={`mt-2 w-full rounded-lg border px-4 py-2 text-gray-900 focus:outline-none focus:ring-2 dark:bg-gray-700 dark:text-white transition-all ${
                errors.category && touched.category
                  ? 'border-red-500 focus:ring-red-500'
                  : 'border-gray-300 focus:ring-blue-500 dark:border-gray-600'
              }`}
            >
              <option value="">Select a category...</option>
              <option value="Environmental">Environmental</option>
              <option value="Power">Power</option>
              <option value="Actuator">Actuator</option>
              <option value="Temperature">Temperature</option>
              <option value="Humidity">Humidity</option>
              <option value="Pressure">Pressure</option>
              <option value="Light">Light</option>
              <option value="Motion">Motion</option>
              <option value="Other">Other</option>
            </select>
            {errors.category && touched.category && (
              <p className="mt-1 flex items-center gap-1 text-sm text-red-600 dark:text-red-400">
                <AlertCircle className="h-4 w-4" />
                {errors.category}
              </p>
            )}
          </div>

          {/* Zone */}
          <div>
            <label className="block text-sm font-medium text-gray-900 dark:text-white">
              Zone *
            </label>
            <input
              type="text"
              name="zone"
              value={values.zone}
              onChange={handleFieldChange}
              onBlur={handleBlur}
              placeholder="e.g., North Plot, Utility Wing"
              className={`mt-2 w-full rounded-lg border px-4 py-2 text-gray-900 placeholder-gray-500 focus:outline-none focus:ring-2 dark:bg-gray-700 dark:text-white dark:placeholder-gray-400 transition-all ${
                errors.zone && touched.zone
                  ? 'border-red-500 focus:ring-red-500'
                  : 'border-gray-300 focus:ring-blue-500 dark:border-gray-600'
              }`}
            />
            {errors.zone && touched.zone && (
              <p className="mt-1 flex items-center gap-1 text-sm text-red-600 dark:text-red-400">
                <AlertCircle className="h-4 w-4" />
                {errors.zone}
              </p>
            )}
          </div>

          {/* Room */}
          <div className="sm:col-span-2">
            <label className="block text-sm font-medium text-gray-900 dark:text-white">
              Room (Optional)
            </label>
            <input
              type="text"
              name="room"
              value={values.room}
              onChange={handleFieldChange}
              onBlur={handleBlur}
              placeholder="e.g., Greenhouse, Generator Room"
              className="mt-2 w-full rounded-lg border border-gray-300 px-4 py-2 text-gray-900 placeholder-gray-500 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700 dark:text-white dark:placeholder-gray-400"
            />
          </div>

          {/* Active Status */}
          <div className="sm:col-span-2">
            <label className="flex items-center gap-3">
              <input
                type="checkbox"
                name="isActive"
                checked={values.isActive}
                onChange={handleFieldChange}
                className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700"
              />
              <span className="text-sm font-medium text-gray-900 dark:text-white">
                Active
              </span>
            </label>
            <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
              {values.isActive
                ? 'This sensor is currently active and can receive data'
                : 'This sensor is inactive and will not receive data'}
            </p>
          </div>
        </div>

        {/* Form Actions */}
        <div className="flex gap-3 border-t border-gray-200 pt-6 dark:border-gray-700">
          <button
            type="button"
            onClick={onCancel}
            className="flex-1 rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50 transition-colors dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="flex-1 inline-flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2 font-medium text-white hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
            {sensor ? 'Update Sensor' : 'Create Sensor'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default SensorForm;
