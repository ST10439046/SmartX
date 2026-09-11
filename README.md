# Smart-X IoT Platform

**PROG7312 POE 2026**
**Student Number:** ST10439046
**Owner and Contributor:** ST10439046

Smart-X is an IoT monitoring and management platform developed as part of the PROG7312 Programming 3 POE assignment. The system combines a React-based web frontend with an ASP.NET Core .NET 10 backend API to provide sensor registration, telemetry monitoring, alert management, file attachments, and the foundation for future command and network-management functionality.

The platform is designed around a smart-building and smart-facility environment where connected sensors can be registered, monitored, and managed through a central web interface.

---

## Table of Contents

* [Project Overview](#project-overview)
* [System Architecture](#system-architecture)
* [Features](#features)
* [Technology Stack](#technology-stack)
* [Project Structure](#project-structure)
* [Prerequisites](#prerequisites)
* [Installation](#installation)
* [Running the Application](#running-the-application)
* [Frontend](#frontend)
* [Backend API](#backend-api)
* [Sensor Management](#sensor-management)
* [Telemetry](#telemetry)
* [Alerts](#alerts)
* [File Attachments](#file-attachments)
* [Notifications](#notifications)
* [Application Navigation](#application-navigation)
* [API Reference](#api-reference)
* [Development](#development)
* [Production Build](#production-build)
* [Validation and Error Handling](#validation-and-error-handling)
* [Security Considerations](#security-considerations)
* [Responsive Design](#responsive-design)
* [Troubleshooting](#troubleshooting)
* [Future Development](#future-development)
* [Project Information](#project-information)

---

# Project Overview

Smart-X provides a central interface for monitoring and managing IoT sensors.

The active part of the platform focuses on **Sensor Data Ingestion and Telemetry**. Users can register sensors, view their current status, monitor telemetry readings, review alerts, and attach files to sensors.

The application has been structured so that additional IoT functionality can be introduced without requiring a complete redesign of the existing system.

The platform is divided into two primary applications:

```text
Smart-X
│
├── Backend API
│   └── ASP.NET Core .NET 10
│
└── Frontend
    └── React + Vite
```

The frontend communicates with the backend through HTTP requests using Axios.

---

# System Architecture

Smart-X follows a client-server architecture.

```text
┌─────────────────────────────────────────────┐
│              Smart-X Frontend               │
│                                             │
│  React                                      │
│  React Router                               │
│  Tailwind CSS                               │
│  Axios                                      │
│  Lucide React                               │
└──────────────────┬──────────────────────────┘
                   │
                   │ HTTP / JSON
                   │
                   ▼
┌─────────────────────────────────────────────┐
│             Smart-X Backend API             │
│                                             │
│  ASP.NET Core .NET 10                       │
│  REST API                                   │
│  Entity Framework Core                      │
│  Sensor Management                          │
│  Telemetry                                  │
│  Alerts                                     │
│  Attachments                                │
│  Command History                             │
│  Network Topology                            │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│                 Database                    │
│                                             │
│  Sensor data                                │
│  Telemetry data                             │
│  Alerts                                     │
│  Attachments                                │
│  Commands                                   │
│  Network information                        │
└─────────────────────────────────────────────┘
```

The frontend does not directly access the database. All application data is accessed through the backend API.

---

# Features

## 1. Sensor Data Ingestion and Telemetry

The primary implemented architectural pillar provides:

* Sensor registration
* Sensor status monitoring
* Sensor details
* Telemetry collection
* Telemetry simulation
* Active alert monitoring
* Sensor file attachments
* Automatic dashboard refresh
* API health checking
* Responsive dashboard interface
* Toast notifications
* Dark-mode compatible interface

## 2. Real-Time Command Stream and History

This architectural pillar has been reserved for future implementation.

Planned functionality includes:

* Sending commands to sensors
* Command execution tracking
* Command history
* Command scheduling
* Command status monitoring

The application currently provides a dedicated navigation area for this functionality.

## 3. Network Topology and Mesh Routing

This architectural pillar has also been reserved for future implementation.

Planned functionality includes:

* Sensor network visualization
* Network topology management
* Mesh routing information
* Network performance monitoring
* Deployment-node relationships

---

# Technology Stack

## Frontend

| Technology       | Purpose                              |
| ---------------- | ------------------------------------ |
| React            | User interface                       |
| Vite             | Development server and build tooling |
| React Router     | Client-side navigation               |
| Axios            | HTTP communication                   |
| Tailwind CSS     | Styling and responsive design        |
| Lucide React     | Interface icons                      |
| JavaScript / JSX | Application development              |

## Backend

| Technology            | Purpose             |
| --------------------- | ------------------- |
| ASP.NET Core          | Web API framework   |
| .NET 10               | Backend runtime     |
| Entity Framework Core | Data access         |
| C#                    | Backend development |
| REST                  | API communication   |

---

# Project Structure

The repository contains both the backend API and frontend application.

```text
Smart-X/
│
├── backend/
│   └── SmartXApi/
│       ├── Controllers/
│       ├── Models/
│       ├── Data/
│       ├── Services/
│       ├── Program.cs
│       ├── SmartXApi.csproj
│       └── ...
│
├── frontend/
│   ├── public/
│   ├── src/
│   │   ├── assets/
│   │   ├── components/
│   │   │   ├── FileUpload.jsx
│   │   │   ├── SensorRegistrationForm.jsx
│   │   │   ├── SensorDetailsPage.jsx
│   │   │   ├── Sidebar.jsx
│   │   │   ├── Tabs.jsx
│   │   │   ├── TelemetryDashboard.jsx
│   │   │   ├── Toast.jsx
│   │   │   └── index.js
│   │   │
│   │   ├── hooks/
│   │   │   └── index.js
│   │   │
│   │   ├── pages/
│   │   │   ├── DashboardPage.jsx
│   │   │   ├── SensorCRUDPage.jsx
│   │   │   ├── SensorDetailsPage.jsx
│   │   │   └── ComingSoonPage.jsx
│   │   │
│   │   ├── services/
│   │   │   └── apiClient.js
│   │   │
│   │   ├── App.jsx
│   │   ├── index.css
│   │   └── main.jsx
│   │
│   ├── package.json
│   ├── vite.config.js
│   ├── tailwind.config.js
│   ├── postcss.config.js
│   ├── eslint.config.js
│   └── index.html
│
├── SmartX.slnx
├── .gitignore
└── README.md
```

The exact contents of the backend directory depend on the API implementation, while the frontend contains the React application and its reusable components.

---

# Prerequisites

The following software is required to run Smart-X locally.

### Backend

* .NET 10 SDK
* Database server configured for the Smart-X API
* Visual Studio 2022/2026, Visual Studio Code, or another compatible .NET IDE

### Frontend

* Node.js 16 or later
* npm
* A modern web browser

### Recommended

* Git
* GitHub account
* Postman or another HTTP API testing tool

---

# Installation

## 1. Clone the repository

```bash
git clone https://github.com/EMKNDN/emkndn-prog7312-g2-2026-prog7312-poe-st10439046.git
```

Navigate into the repository:

```bash
cd emkndn-prog7312-g2-2026-prog7312-poe-st10439046
```

---

## 2. Install frontend dependencies

Navigate to the frontend:

```bash
cd frontend
```

Install the required packages:

```bash
npm install
```

---

## 3. Configure the backend

Navigate to:

```text
backend/
```

Configure the database connection according to the backend configuration.

The API is configured to run locally on:

```text
http://localhost:5231
```

---

# Running the Application

Smart-X consists of two applications that should be running simultaneously.

## Start the Backend

From the backend directory:

```bash
dotnet run
```

The API should become available at:

```text
http://localhost:5231
```

The health endpoint can be tested using:

```text
http://localhost:5231/health
```

---

## Start the Frontend

From the frontend directory:

```bash
npm run dev
```

Vite will display the local development address in the terminal.

The frontend communicates with:

```text
http://localhost:5231
```

for backend API requests.

---

# Frontend

The frontend is built using React and provides the primary user interface for Smart-X.

## Main Application Components

### Sidebar

Provides navigation between the major Smart-X functionality areas.

### Telemetry Dashboard

Provides an overview of registered sensors, their active/inactive state, current telemetry readings, and active alerts.

### Sensor Registration

Allows new sensors to be registered with:

* MAC address
* Room
* Zone
* Node ID
* Category
* Additional sensor information
* Supporting files

### Sensor Details

Individual sensors can be selected from the dashboard and opened through a dedicated route.

Example:

```text
/sensor/00:1A:2B:3C:4D:5E
```

### Sensor CRUD

Provides sensor management functionality through the backend API.

---

# Sensor Management

Sensors are identified using their MAC address.

A typical sensor contains information such as:

```json
{
  "macaddress": "00:1A:2B:3C:4D:5E",
  "room": "Greenhouse",
  "zone": "North Plot",
  "nodeId": "ENV-01",
  "category": "Environmental",
  "isActive": true
}
```

## Sensor Categories

Smart-X supports categories including:

* Environmental
* Power
* Actuator

The architecture also allows additional categories to be introduced.

## Sensor Status

The dashboard distinguishes between:

```text
Active
Inactive
```

Active status represents the sensor's configured application state.

Telemetry availability is handled separately from the sensor's active/inactive state.

---

# Telemetry

The telemetry dashboard retrieves sensor telemetry through the backend API.

Telemetry information can include:

* Sensor MAC address
* Data type
* Data value
* Timestamp

Examples include:

```text
Temperature
Power wattage
Valve state
Generic sensor readings
```

The dashboard periodically refreshes data from the API.

## Telemetry Simulation

The dashboard contains a telemetry simulation function for development and demonstration.

When simulation is enabled, the frontend periodically generates sample telemetry values and submits them through the API.

The simulation can be started and stopped directly from the dashboard.

---

# Alerts

Smart-X provides an Active Alerts section on the telemetry dashboard.

Alerts can contain:

* Error code
* Description
* Sensor MAC address
* Severity

Supported severity levels include:

```text
Info
Warning
Critical
```

Alerts are retrieved through the backend API and displayed using contextual styling.

---

# File Attachments

Smart-X supports attaching files to sensors.

The frontend provides a file upload interface supporting drag-and-drop interaction.

Files can include:

* Configuration files
* CSV files
* JSON files
* XML files
* Images
* PDF documents
* Log files

The maximum supported file size is:

```text
50 MB
```

The frontend uses `FormData` when uploading files so that the browser can correctly construct the multipart request boundary.

The backend stores the sensor MAC address in its original format while using a filesystem-safe representation when creating attachment directories.

For example:

```text
MAC address:
00:1A:2B:3C:4D:5E

Filesystem-safe directory:
00-1A-2B-3C-4D-5E
```

---

# Notifications

Smart-X includes a reusable notification system.

Notifications can communicate:

* Successful operations
* Errors
* Warnings
* Informational messages

The application exposes notifications through the `useNotification()` hook.

Example:

```javascript
const { show } = useNotification();

show('Sensor registered successfully', 'success');
```

---

# Application Navigation

The frontend uses React Router.

Current routes include:

| Route                 | Purpose                        |
| --------------------- | ------------------------------ |
| `/`                   | Dashboard                      |
| `/dashboard`          | Dashboard                      |
| `/sensors`            | Sensor management              |
| `/sensor/:macaddress` | Individual sensor details      |
| `/commands`           | Command functionality          |
| `/topology`           | Network topology functionality |

The commands and topology sections currently provide the foundation for future functionality.

---

# API Reference

The frontend communicates with the backend through Axios.

The API base URL is:

```text
http://localhost:5231
```

## Sensor API

```text
GET    /api/sensor
GET    /api/sensor/{macAddress}
POST   /api/sensor
PUT    /api/sensor/{macAddress}
DELETE /api/sensor/{macAddress}
```

## Telemetry API

```text
GET    /api/telemetrydata
GET    /api/telemetrydata/{id}
POST   /api/telemetrydata
PUT    /api/telemetrydata/{id}
DELETE /api/telemetrydata/{id}
```

## Active Alert API

```text
GET    /api/activealert
GET    /api/activealert/{id}
POST   /api/activealert
PUT    /api/activealert/{id}
DELETE /api/activealert/{id}
```

## Attachment API

```text
GET    /api/attachment
GET    /api/attachment/{id}
GET    /api/attachment/sensor/{sensorMac}
POST   /api/attachment
PUT    /api/attachment/{id}
DELETE /api/attachment/{id}

POST   /api/attachment/upload
GET    /api/attachment/download/{id}
GET    /api/attachment/view/{id}
```

## Command History API

```text
GET    /api/commandhistory
GET    /api/commandhistory/{id}
POST   /api/commandhistory
PUT    /api/commandhistory/{id}
DELETE /api/commandhistory/{id}
```

## Network Topology API

```text
GET    /api/networktopology
GET    /api/networktopology/{id}
POST   /api/networktopology
PUT    /api/networktopology/{id}
DELETE /api/networktopology/{id}
```

## Health API

```text
GET /health
```

---

# API Client

The frontend centralizes backend communication in:

```text
frontend/src/services/apiClient.js
```

The Axios client provides:

* Centralized API configuration
* Request logging
* Response logging
* Error handling
* JSON requests
* Multipart form handling
* Blob responses for file downloads

The service layer separates HTTP communication from the UI components.

---

# Custom React Hooks

The project contains reusable hooks in:

```text
frontend/src/hooks/index.js
```

## useNotification

Manages application toast notifications.

## useForm

Provides:

* Form state
* Validation
* Submission handling
* Error handling

## useAsync

Provides a reusable structure for asynchronous operations and loading/error states.

## useFileUpload

Handles file-upload state and progress information.

---

# UI and UX

Smart-X uses Tailwind CSS to provide a responsive and consistent interface.

## Responsive Breakpoints

The application supports:

| Breakpoint    |           Width |
| ------------- | --------------: |
| Mobile        | Less than 640px |
| Small tablet  |          640px+ |
| Tablet        |          768px+ |
| Desktop       |         1024px+ |
| Large desktop |         1280px+ |

The dashboard uses responsive grid layouts so sensor cards adapt to the available screen size.

---

# Dark Mode

The interface includes dark-mode-compatible styling.

Tailwind's dark-mode classes are used throughout the application for:

* Backgrounds
* Text
* Cards
* Borders
* Alerts
* Buttons
* Dashboard components

---

# Accessibility

The frontend uses semantic HTML elements and accessible interface patterns wherever appropriate.

Examples include:

* Button elements for interactive controls
* Descriptive text for interface elements
* Structured headings
* Form labels
* Clear error messages
* Responsive layouts

---

# Development

## Start Development Server

```bash
npm run dev
```

## Run ESLint

```bash
npm run lint
```

## Build the Frontend

```bash
npm run build
```

## Preview Production Build

```bash
npm run preview
```

---

# Production Build

The React frontend can be compiled into an optimized production build:

```bash
npm run build
```

The generated files are placed in:

```text
dist/
```

The backend can be built using:

```bash
dotnet build
```

The complete application therefore consists of independently deployable frontend and backend components.

---

# Validation and Error Handling

Smart-X performs validation at the frontend level before submitting data to the API.

Examples include:

* Required field validation
* MAC address validation
* File type validation
* File size validation
* API error handling
* Failed request handling
* Empty data handling

The backend remains responsible for server-side validation and persistence.

Client-side validation is therefore treated as a usability feature rather than the application's only security boundary.

---

# Security Considerations

The application includes several security-oriented practices.

## Input Handling

React automatically escapes rendered JSX content, reducing the risk of directly injecting HTML through user-provided values.

## MAC Address Validation

MAC addresses are validated using a standard hexadecimal MAC address pattern.

Example:

```text
00:1A:2B:3C:4D:5E
```

## File Validation

Uploaded files are restricted according to supported file types and file-size limits.

## API Separation

The frontend does not directly communicate with the database.

All persistent application operations are performed through the backend API.

## CORS

The backend API must be configured to permit requests from the frontend development origin.

---

# Performance

The application uses several techniques to maintain responsive performance:

* React component-based architecture
* Reusable hooks
* Centralized API communication
* Efficient telemetry lookup by sensor MAC address
* Periodic data refresh
* Tailwind utility classes
* Vite production bundling
* Responsive CSS grids

Telemetry data is mapped by normalized sensor MAC address to allow the dashboard to associate readings with the appropriate sensor.

---

# Testing and Verification

The application has been developed and verified through functional testing of the major user workflows.

Tested functionality includes:

* Dashboard loading
* Sensor retrieval
* Sensor registration
* Sensor details navigation
* Telemetry retrieval
* Telemetry submission
* Telemetry simulation
* Alert retrieval
* File upload
* File download/view operations
* Toast notifications
* API error handling
* Responsive dashboard layouts
* Dark-mode styling
* Frontend production builds

The backend API can additionally be tested independently using tools such as Postman.

---

# Troubleshooting

## Backend Connection Error

If the frontend cannot retrieve data, verify that the backend is running:

```text
http://localhost:5231
```

Then test:

```text
http://localhost:5231/health
```

If the API is unavailable, the frontend will not be able to retrieve sensor, telemetry, alert, or attachment data.

---

## CORS Error

If the browser reports a CORS error, verify that the backend allows requests from the frontend's development origin.

---

## Sensor Data Not Appearing

Check:

1. The API is running.
2. The `/api/sensor` endpoint returns data.
3. The browser console contains no API errors.
4. The frontend API base URL is correct.
5. The database contains registered sensors.

---

## Telemetry Not Updating

Check:

1. The `/api/telemetrydata` endpoint is available.
2. The sensor MAC address matches between the sensor and telemetry records.
3. Telemetry records contain valid timestamps.
4. The telemetry simulation is running if demonstration data is required.

---

## File Upload Failure

Check:

1. The selected file is supported.
2. The file does not exceed 50 MB.
3. A valid sensor MAC address has been supplied.
4. The backend attachment endpoint is running.
5. The browser console does not report a multipart request error.

---

# Future Development

Smart-X has been structured to support additional functionality beyond the currently implemented telemetry pillar.

## Command Stream and History

Future development can introduce:

* Sensor command execution
* Command queues
* Scheduled commands
* Command results
* Command history filtering
* Command status tracking

## Network Topology

Future development can introduce:

* Interactive topology diagrams
* Deployment nodes
* Sensor relationships
* Mesh routing
* Network health monitoring
* Connectivity analytics

## Additional IoT Features

The architecture can also be extended with:

* More sensor categories
* Historical telemetry graphs
* Advanced reporting
* User authentication and authorization
* Role-based access
* More detailed alert management
* Sensor configuration management
* Automated anomaly detection

---

# Project Ownership

This project was developed as an individual academic project for the **PROG7312 2026 POE**.

| Information         | Details              |
| ------------------- | -------------------- |
| Student Number      | **ST10439046**       |
| Project             | Smart-X IoT Platform |
| Ownership           | Individual           |
| Primary Contributor | **ST10439046**       |
| Academic Module     | PROG7312             |
| Year                | 2026                 |

There are no additional project contributors. The Smart-X application, frontend, backend integration, implementation, documentation, and project structure are maintained by **ST10439046**.

---

# Repository

The complete Smart-X project is maintained in the combined repository:

```text
https://github.com/EMKNDN/emkndn-prog7312-g2-2026-prog7312-poe-st10439046
```

The repository contains both the frontend and backend components required to develop and run the Smart-X platform.

---

# License

This project was developed for academic assessment purposes as part of the PROG7312 2026 POE.

All project source code and documentation are maintained by **ST10439046**.

---

# Conclusion

Smart-X provides a structured IoT management platform centred around sensor registration, telemetry ingestion, monitoring, alerts, and supporting file management.

The application separates the React frontend from the ASP.NET Core backend API while keeping both components within a single repository. This separation provides a clear boundary between presentation, API communication, business logic, and data persistence.

The current implementation establishes the Sensor Data Ingestion and Telemetry pillar as the primary functional area while providing a foundation for the future Command Stream and Network Topology pillars.

**Smart-X IoT Platform**
**PROG7312 POE 2026**
**ST10439046**
