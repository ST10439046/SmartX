import { useState } from 'react';
import { Loader2, CheckCircle } from 'lucide-react';
import { useForm, useNotification } from '../hooks';
import FileUpload from './FileUpload';
import { sensorApi, attachmentApi } from '../services/apiClient';

const getFileType = (filename) => {
  const ext = filename.split('.').pop().toLowerCase();
  if (['jpg', 'jpeg', 'png', 'gif'].includes(ext)) return 'photo';
  if (['json', 'xml', 'csv', 'conf'].includes(ext)) return 'config';
  if (['log', 'txt'].includes(ext)) return 'log';
  if (['pdf'].includes(ext)) return 'document';
  return 'other';
};

const SensorRegistrationForm = ({ onSubmitSuccess }) => {
  const { show } = useNotification();
  const [attachedFiles, setAttachedFiles] = useState([]);

  const validate = (name, value) => {
    switch (name) {
      case 'macAddress':
        if (!value) return 'MAC Address is required';
        if (!/^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$/.test(value)) {
          return 'Invalid MAC Address format (e.g., 00:1A:2B:3C:4D:5E)';
        }
        return null;
      case 'sensorName':
        return !value ? 'Sensor Name is required' : null;
      case 'location':
        return !value ? 'Location is required' : null;
      case 'zone':
        return !value ? 'Zone is required' : null;
      case 'nodeId':
        return !value ? 'Node ID is required' : null;
      case 'category':
        return !value ? 'Category is required' : null;
      default:
        return null;
    }
  };

  const {
    values,
    errors,
    touched,
    isSubmitting,
    handleChange,
    handleBlur,
    handleSubmit,
    reset,
  } = useForm(
    {
      sensorName: '',
      macAddress: '',
      location: '',
      zone: '',
      nodeId: '',
      category: '',
      description: '',
    },
    async (formValues) => {
      try {
        // Prepare sensor data according to backend structure
        const sensorData = {
          macAddress: formValues.macAddress.toUpperCase(),
          sensorName: formValues.sensorName,
          category: formValues.category,
          location: formValues.location,
          zone: formValues.zone,
          nodeId: formValues.nodeId,
          description: formValues.description || '',
          isActive: true,
          createdDate: new Date().toISOString(),
        };

        // Create sensor
        const response = await sensorApi.createSensor(sensorData);
        const createdSensor = response.data;

        show(`Sensor "${sensorData.sensorName}" registered successfully!`, 'success');

        // Upload attached files if any
        if (attachedFiles.length > 0 && createdSensor?.macAddress) {
          for (const file of attachedFiles) {
            try {
              const attachmentData = new FormData();
              attachmentData.append('file', file);
              attachmentData.append('sensorMacAddress', createdSensor.macAddress);
              attachmentData.append('fileType', getFileType(file.name));
              attachmentData.append('uploadDate', new Date().toISOString());

              await attachmentApi.createAttachment(attachmentData);
              show(`File "${file.name}" uploaded successfully`, 'success');
            } catch (uploadErr) {
              console.error('File upload error:', uploadErr);
              show(`Failed to upload "${file.name}"`, 'warning');
            }
          }
        }

        reset();
        setAttachedFiles([]);
        onSubmitSuccess?.();
      } catch (error) {
        const errorMsg =
          error.response?.data?.message ||
          error.response?.data?.error ||
          error.message ||
          'Failed to register sensor. Please check the form and try again.';
        show(errorMsg, 'error');
        console.error('Registration error:', error);
      }
    },
    validate
  );

  const handleFileSelect = (files) => {
    setAttachedFiles(files);
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-8">
      <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
        Register New Sensor
      </h2>
      <p className="text-gray-600 dark:text-gray-400 mb-6">
        Register a new IoT sensor to the Smart-X platform with deployment
        location and category information.
      </p>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Sensor Name */}
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Sensor Name *
          </label>
          <input
            type="text"
            name="sensorName"
            value={values.sensorName}
            onChange={handleChange}
            onBlur={handleBlur}
            placeholder="e.g., Kitchen Temperature Sensor"
            className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:ring-2 focus:ring-primary-500"
          />
          {touched.sensorName && errors.sensorName && (
            <p className="text-danger-500 text-sm mt-1">{errors.sensorName}</p>
          )}
        </div>

        {/* MAC Address */}
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            MAC Address (Unique Identifier) *
          </label>
          <input
            type="text"
            name="macAddress"
            value={values.macAddress}
            onChange={handleChange}
            onBlur={handleBlur}
            placeholder="e.g., 00:1A:2B:3C:4D:5E"
            className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:ring-2 focus:ring-primary-500"
          />
          {touched.macAddress && errors.macAddress && (
            <p className="text-danger-500 text-sm mt-1">{errors.macAddress}</p>
          )}
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
            Format: XX:XX:XX:XX:XX:XX (will be converted to uppercase)
          </p>
        </div>

        {/* Deployment Location Section */}
        <div className="border-t pt-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
            Deployment Location
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {/* Room/Location */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Room / Location *
              </label>
              <input
                type="text"
                name="location"
                value={values.location}
                onChange={handleChange}
                onBlur={handleBlur}
                placeholder="e.g., Kitchen"
                className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:ring-2 focus:ring-primary-500"
              />
              {touched.location && errors.location && (
                <p className="text-danger-500 text-sm mt-1">{errors.location}</p>
              )}
            </div>

            {/* Zone */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Zone *
              </label>
              <input
                type="text"
                name="zone"
                value={values.zone}
                onChange={handleChange}
                onBlur={handleBlur}
                placeholder="e.g., Zone A"
                className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:ring-2 focus:ring-primary-500"
              />
              {touched.zone && errors.zone && (
                <p className="text-danger-500 text-sm mt-1">{errors.zone}</p>
              )}
            </div>

            {/* Node ID */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Node ID *
              </label>
              <input
                type="text"
                name="nodeId"
                value={values.nodeId}
                onChange={handleChange}
                onBlur={handleBlur}
                placeholder="e.g., NODE-001"
                className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:ring-2 focus:ring-primary-500"
              />
              {touched.nodeId && errors.nodeId && (
                <p className="text-danger-500 text-sm mt-1">{errors.nodeId}</p>
              )}
            </div>
          </div>
        </div>

        {/* Category Selection */}
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Category *
          </label>
          <select
            name="category"
            value={values.category}
            onChange={handleChange}
            onBlur={handleBlur}
            className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500"
          >
            <option value="">Select a category</option>
            <option value="Environmental">Environmental</option>
            <option value="Power Consumption">Power Consumption</option>
            <option value="Actuator">Actuator</option>
          </select>
          {touched.category && errors.category && (
            <p className="text-danger-500 text-sm mt-1">{errors.category}</p>
          )}
        </div>

        {/* Description */}
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Description (Optional)
          </label>
          <textarea
            name="description"
            value={values.description}
            onChange={handleChange}
            placeholder="Add any additional notes about this sensor..."
            rows="4"
            className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:ring-2 focus:ring-primary-500 resize-none"
          />
        </div>

        {/* File Upload */}
        <div className="border-t pt-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
            Attach Configuration Files (Optional)
          </h3>
          <FileUpload
            onFileSelect={handleFileSelect}
            maxSize={50 * 1024 * 1024}
            accept=".json,.xml,.csv,.log,.jpg,.jpeg,.png,.pdf"
          />
          {attachedFiles.length > 0 && (
            <p className="text-sm text-success-600 dark:text-success-400 mt-2 flex items-center gap-2">
              <CheckCircle size={16} />
              {attachedFiles.length} file(s) ready to upload
            </p>
          )}
        </div>

        {/* Submit Button */}
        <div className="flex gap-3 pt-4">
          <button
            type="submit"
            disabled={isSubmitting}
            className="flex-1 px-6 py-3 bg-primary-600 hover:bg-primary-700 disabled:bg-primary-400 text-white font-medium rounded-lg transition-colors flex items-center justify-center gap-2"
          >
            {isSubmitting ? (
              <>
                <Loader2 size={18} className="animate-spin" />
                Registering...
              </>
            ) : (
              <>
                <CheckCircle size={18} />
                Register Sensor
              </>
            )}
          </button>
          <button
            type="reset"
            onClick={() => {
              reset();
              setAttachedFiles([]);
            }}
            className="px-6 py-3 bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 text-gray-900 dark:text-white font-medium rounded-lg transition-colors"
          >
            Clear
          </button>
        </div>
      </form>
    </div>
  );
};

export default SensorRegistrationForm;
