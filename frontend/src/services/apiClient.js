import axios from 'axios';

const API_BASE_URL = 'http://localhost:5231';

// Create axios instance
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor
apiClient.interceptors.request.use(
  (config) => {
    // Add any auth tokens if needed
    console.log('API Request:', config.method?.toUpperCase(), config.url);

    // For FormData, don't set Content-Type - let the browser set it with the boundary
    if (config.data instanceof FormData) {
      delete config.headers['Content-Type'];
      console.log('FormData detected - removing Content-Type header to allow browser to set it');
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor
apiClient.interceptors.response.use(
  (response) => {
    console.log('API Response:', response.status, response.data);
    return response;
  },
  (error) => {
    console.error('API Error:', error.response?.status, error.response?.data || error.message);
    return Promise.reject(error);
  }
);

// ============ SENSOR API ============
export const sensorApi = {
  // Get all sensors
  getAllSensors: async () => {
    return apiClient.get('/api/sensor');
  },

  // Get sensor by MAC address
  getSensorByMac: async (macAddress) => {
    return apiClient.get(`/api/sensor/${macAddress}`);
  },

  // Create new sensor
  createSensor: async (sensorData) => {
    return apiClient.post('/api/sensor', sensorData);
  },

  // Update sensor
  updateSensor: async (macAddress, sensorData) => {
    return apiClient.put(`/api/sensor/${macAddress}`, sensorData);
  },

  // Delete sensor
  deleteSensor: async (macAddress) => {
    return apiClient.delete(`/api/sensor/${macAddress}`);
  },
};

// ============ TELEMETRY DATA API ============
export const telemetryApi = {
  // Get all telemetry data
  getAllTelemetry: async () => {
    return apiClient.get('/api/telemetrydata');
  },

  // Get telemetry by ID
  getTelemetryById: async (id) => {
    return apiClient.get(`/api/telemetrydata/${id}`);
  },

  // Create new telemetry record
  createTelemetry: async (telemetryData) => {
    return apiClient.post('/api/telemetrydata', telemetryData);
  },

  // Update telemetry record
  updateTelemetry: async (id, telemetryData) => {
    return apiClient.put(`/api/telemetrydata/${id}`, telemetryData);
  },

  // Delete telemetry record
  deleteTelemetry: async (id) => {
    return apiClient.delete(`/api/telemetrydata/${id}`);
  },
};

// ============ ACTIVE ALERT API ============
export const alertApi = {
  // Get all active alerts
  getAllAlerts: async () => {
    return apiClient.get('/api/activealert');
  },

  // Get alert by ID
  getAlertById: async (id) => {
    return apiClient.get(`/api/activealert/${id}`);
  },

  // Create new alert
  createAlert: async (alertData) => {
    return apiClient.post('/api/activealert', alertData);
  },

  // Update alert
  updateAlert: async (id, alertData) => {
    return apiClient.put(`/api/activealert/${id}`, alertData);
  },

  // Delete alert
  deleteAlert: async (id) => {
    return apiClient.delete(`/api/activealert/${id}`);
  },
};

// ============ ATTACHMENT API ============
export const attachmentApi = {
  // Get all attachments
  getAllAttachments: async () => {
    return apiClient.get('/api/attachment');
  },

  // Get attachment by ID
  getAttachmentById: async (id) => {
    return apiClient.get(`/api/attachment/${id}`);
  },

  // Get attachments for a specific sensor
  getAttachmentsBySensor: async (sensorMac) => {
    return apiClient.get(`/api/attachment/sensor/${sensorMac}`);
  },

  // Upload file as attachment (correct endpoint with correct field names)
  uploadFile: async (file, sensorMac, fileType = 'document') => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('sensorMac', sensorMac); // Backend expects 'sensorMac' NOT 'sensorMacAddress'
    formData.append('fileType', fileType);

    console.log('AttachmentApi.uploadFile called with:', {
      fileName: file.name,
      fileSize: file.size,
      sensorMac,
      fileType,
    });

    // Do NOT set Content-Type header for FormData - let axios/browser set it with boundary
    return apiClient.post('/api/attachment/upload', formData);
  },

  // Create new attachment record (without file upload)
  createAttachment: async (attachmentData) => {
    return apiClient.post('/api/attachment', attachmentData);
  },

  // Update attachment
  updateAttachment: async (id, attachmentData) => {
    return apiClient.put(`/api/attachment/${id}`, attachmentData);
  },

  // Delete attachment
  deleteAttachment: async (id) => {
    return apiClient.delete(`/api/attachment/${id}`);
  },

  // Download attachment
  downloadAttachment: async (id) => {
    return apiClient.get(`/api/attachment/download/${id}`, {
      responseType: 'blob',
    });
  },

  // View attachment (text-based files only)
  viewAttachment: async (id) => {
    return apiClient.get(`/api/attachment/view/${id}`);
  },
};

// ============ COMMAND HISTORY API ============
export const commandApi = {
  // Get all command history
  getAllCommands: async () => {
    return apiClient.get('/api/commandhistory');
  },

  // Get command by ID
  getCommandById: async (id) => {
    return apiClient.get(`/api/commandhistory/${id}`);
  },

  // Create new command
  createCommand: async (commandData) => {
    return apiClient.post('/api/commandhistory', commandData);
  },

  // Update command
  updateCommand: async (id, commandData) => {
    return apiClient.put(`/api/commandhistory/${id}`, commandData);
  },

  // Delete command
  deleteCommand: async (id) => {
    return apiClient.delete(`/api/commandhistory/${id}`);
  },
};

// ============ NETWORK TOPOLOGY API ============
export const topologyApi = {
  // Get all topologies
  getAllTopologies: async () => {
    return apiClient.get('/api/networktopology');
  },

  // Get topology by ID
  getTopologyById: async (id) => {
    return apiClient.get(`/api/networktopology/${id}`);
  },

  // Create new topology
  createTopology: async (topologyData) => {
    return apiClient.post('/api/networktopology', topologyData);
  },

  // Update topology
  updateTopology: async (id, topologyData) => {
    return apiClient.put(`/api/networktopology/${id}`, topologyData);
  },

  // Delete topology
  deleteTopology: async (id) => {
    return apiClient.delete(`/api/networktopology/${id}`);
  },
};

// ============ HEALTH CHECK ============
export const healthCheck = async () => {
  try {
    return await apiClient.get('/health');
  } catch (error) {
    console.error('Backend health check failed:', error);
    return null;
  }
};

export default apiClient;
