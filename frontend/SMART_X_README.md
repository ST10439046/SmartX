# Smart-X IoT Frontend

A professional, responsive React frontend for the Smart-X IoT platform with real-time telemetry visualization, sensor registration, and network management capabilities.

## 🚀 Features

### Architectural Pillars

1. **Sensor Data Ingestion and Telemetry** (Active/Enabled)
   - Real-time sensor monitoring dashboard
   - Live telemetry data visualization
   - Sensor registration and management
   - File upload for configuration and logs
   - Alert and notification system

2. **Real-Time Command Stream and History** (Coming Soon)
   - Send commands to sensors
   - Track command execution history
   - Command scheduling capabilities

3. **Network Topology and Mesh Routing** (Coming Soon)
   - Visualize sensor network topology
   - Monitor mesh routing
   - Network performance analytics

## 📋 Prerequisites

- Node.js 16+ and npm/yarn
- .NET 10 Backend API running on `http://localhost:5231`

## 🛠️ Installation

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Ensure your backend API is running:**
   ```
   The application expects the API to be available at http://localhost:5231
   ```

3. **Start the development server:**
   ```bash
   npm run dev
   ```

4. **Build for production:**
   ```bash
   npm run build
   ```

## 📁 Project Structure

```
src/
├── components/          # Reusable UI components
│   ├── FileUpload.jsx
│   ├── SensorRegistrationForm.jsx
│   ├── Sidebar.jsx
│   ├── Tabs.jsx
│   ├── TelemetryDashboard.jsx
│   ├── Toast.jsx
│   └── index.js
├── hooks/              # Custom React hooks
│   └── index.js        # useNotification, useForm, useAsync, useFileUpload
├── pages/              # Page components
│   ├── DashboardPage.jsx
│   └── ComingSoonPage.jsx
├── services/           # API and external services
│   └── apiClient.js    # Axios configuration and API methods
├── App.jsx             # Main app component
├── index.css           # Tailwind CSS with custom styles
└── main.jsx            # React entry point
```

## 🎨 UI/UX Features

- **Tailwind CSS**: Professional, responsive design
- **Dark Mode Support**: Automatic dark mode based on system preferences
- **Real-time Updates**: Live data refresh for sensor telemetry
- **Toast Notifications**: Success, error, warning, and info messages
- **Responsive Layout**: Mobile, tablet, and desktop optimized
- **Accessibility**: ARIA labels and semantic HTML

## 🔌 API Integration

The application uses Axios to communicate with the .NET 10 backend API at `http://localhost:5231`.

### TelemetryPacket Structure

All API requests follow this generic structure matching the C# backend:

```typescript
interface TelemetryPacket<T> {
  timestamp: string;           // ISO 8601 timestamp
  data: T;                     // Generic payload
  metadata: {
	version: string;           // API version
	source: string;            // "web-frontend"
	action?: string;           // Optional action identifier
	sensorId?: string;         // Optional sensor ID
  };
}
```

### Available API Endpoints

#### Sensor Management
- `POST /api/sensors/register` - Register new sensor
- `GET /api/sensors` - Get all sensors
- `GET /api/sensors/{id}` - Get sensor by ID
- `PUT /api/sensors/{id}` - Update sensor
- `POST /api/sensors/upload` - Upload sensor files

#### Telemetry
- `GET /api/telemetry/{sensorId}` - Get real-time telemetry
- `GET /api/telemetry/{sensorId}/history` - Get telemetry history
- `POST /api/telemetry/submit` - Submit telemetry data

#### Health
- `GET /health` - API health check

## 📝 Sensor Registration Form

The registration form includes:

- **Sensor Name** - Display name for the sensor
- **MAC Address** - Unique identifier (validated format: XX:XX:XX:XX:XX:XX)
- **Deployment Location** - Room/Location, Zone, and Node ID
- **Category** - Environmental, Power Consumption, or Actuator
- **Description** - Optional notes
- **File Upload** - Attach configuration files, photos, or logs

All form fields are validated client-side before submission.

## 📊 Real-time Telemetry Dashboard

The dashboard provides:

- **Sensor Status Cards** - Current values, min/max/avg metrics, and trend data
- **Active Alerts Panel** - Critical alerts, warnings, and notifications
- **Network Status** - API connectivity, data throughput, and uptime
- **Live Updates** - Real-time data refresh every 5 seconds

## 🔔 Notification System

Toast notifications provide crisp feedback for:

- Successful form submissions
- API errors with descriptive messages
- File upload completion
- Sensor connection status changes
- System alerts

Access via the `useNotification()` hook:

```javascript
const { show, success, error, warning, info } = useNotification();

// Show different notification types
success('Operation completed!');
error('Something went wrong');
warning('Please review this');
info('Here is some information');
```

## 🪝 Custom Hooks

### useNotification()
Manages toast notifications across the application.

### useForm(initialValues, onSubmit, validate)
Handles form state, validation, and submission with built-in error handling.

### useAsync(asyncFunction, immediate)
Manages API loading states and error handling.

### useFileUpload()
Handles file uploads with progress tracking and error management.

## 🎯 File Upload Features

- **Drag & Drop Support** - Drop files directly onto the upload area
- **Progress Tracking** - Visual feedback during uploads
- **File Validation** - Size and type checking
- **Multiple Files** - Upload multiple files simultaneously
- **Error Handling** - Clear error messages for validation failures

Supported file types:
- Configuration: `.json`, `.xml`, `.csv`
- Media: `.jpg`, `.jpeg`, `.png`, `.pdf`
- Logs: `.log`

Max file size: 50MB per file

## 🔐 Security Considerations

- MAC address validation to prevent malformed entries
- File type and size validation
- Input sanitization for form fields
- CORS configuration for backend API
- No sensitive data stored in localStorage

## 🌐 Responsive Breakpoints

- **Mobile**: < 640px
- **Tablet**: 640px - 1024px
- **Desktop**: > 1024px

## 🚀 Performance Optimizations

- Code splitting with React Router
- Lazy component loading
- Optimized re-renders with React hooks
- Efficient file upload handling with progress tracking
- Debounced form validation

## 📦 Dependencies

- **react** (^19.2.8) - UI library
- **react-dom** (^19.2.8) - DOM rendering
- **react-router-dom** (^6.20.0) - Client-side routing
- **axios** (^1.6.2) - HTTP client
- **lucide-react** (^0.263.1) - Icon library
- **tailwindcss** (^3.3.6) - CSS framework
- **postcss** (^8.4.31) - CSS processing
- **autoprefixer** (^10.4.16) - CSS vendor prefixing

## 🎮 Development

### ESLint Configuration

The project includes ESLint with React-specific rules. Run:

```bash
npm run lint
```

### Hot Module Replacement (HMR)

Changes to files are automatically reflected in the browser during development.

### Browser DevTools

React DevTools extension is recommended for debugging component state and props.

## 📖 Usage Examples

### Registering a Sensor

1. Navigate to the Dashboard
2. Click on the "Register Sensor" tab
3. Fill in all required fields
4. Optionally upload configuration files
5. Click "Register Sensor"
6. Receive success notification and view in telemetry dashboard

### Monitoring Real-time Telemetry

1. Navigate to the "Telemetry Overview" tab
2. View sensor status cards with live data
3. Check alerts panel for any anomalies
4. Monitor network status indicators

## 🐛 Troubleshooting

### Backend Connection Issues
- Verify .NET API is running on `http://localhost:5231`
- Check CORS configuration on backend
- Inspect browser console for network errors

### Form Validation Errors
- Ensure MAC address follows format: XX:XX:XX:XX:XX:XX
- All required fields must be filled
- File sizes must not exceed 50MB

### File Upload Failures
- Check browser console for error details
- Verify file type is supported
- Ensure file size is within limits

## 📄 License

[Add your license information here]

## 🤝 Contributing

[Add contribution guidelines here]

## 📧 Support

For issues and feature requests, please contact the development team.

---

**Built with React, Tailwind CSS, and Axios** ⚡
