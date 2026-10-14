# Smart-X IoT Platform

**PROG7312 POE 2026**
**Student Number:** ST10439046
**Owner and Contributor:** ST10439046

Smart-X is an IoT monitoring and management platform developed as part of the PROG7312 Programming 3 POE assignment. The system combines a React-based web frontend with an ASP.NET Core .NET 10 backend API to provide sensor registration, telemetry monitoring, alert management, file attachments, and the foundation for future command and network-management functionality.

The platform is designed around a smart-building and smart-facility environment where connected sensors can be registered, monitored, and managed through a central web interface.

**Demo Video Link:** https://youtu.be/7UVAFmF4p54
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
* [Alerts](#alerts)\n* [Command Stream, Predictive Insights, and Alert Acknowledgement](#command-stream-predictive-insights-and-alert-acknowledgement)\n* [Testing](#testing)
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

The platform implements sensor ingestion and telemetry plus a live command-stream dashboard. Operators can monitor readings, configure SQL-backed threshold rules, issue simulated actuator commands, inspect command history, acknowledge active alerts, and review explainable operator-pattern insights. The command actions remain simulated until a real device transport is integrated.

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

Implemented functionality includes:

* Priority processing of critical packets before routine packets
* Persisted simulated device output state and command history
* Undo for commands issued during the current API session
* Configurable SQL-backed telemetry threshold rules
* Automatic recovery of threshold alerts when readings return to range
* Manual acknowledgement metadata for active alerts
* Persisted search and command activity for explainable pattern detection
* Bounded, single-flight dashboard polling to avoid overlapping requests

The live operations page is available in the application navigation.

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

# Command Stream, Predictive Insights, and Alert Acknowledgement

The Command Stream page refreshes dashboard data every five seconds. Refresh calls are coalesced while a previous request is still running, and failed requests are reported in the UI instead of blocking the page. Search activity logging is debounced so every keystroke is not treated as a separate operator action.

## Explainable operator-pattern detection

Searches and manual commands are stored in SQL Server in the `OperatorActivities` table. High-frequency sensor packets are not copied into this operator-history table. The engine checks the previous 30 days and looks for repeated sequences where an operator searches for a device and then issues a command to that same device within 30 minutes. It requires at least two observed sequences before reporting a recurring pattern. Suggestions include the observed count and a confidence indicator; they are historical correlations, not guaranteed predictions, and they never issue a command automatically.

Activity history is bounded by retention cleanup: entries older than 90 days are removed and the table is capped at 5,000 recent records. This keeps the pattern engine explainable and limits database growth. Search events that cannot be associated with a registered sensor are still recorded but are not linked to device-specific command patterns.

## Alert lifecycle

* **Raised:** an enabled alert rule or built-in safety threshold is breached.
* **Acknowledged:** an operator clicks **Acknowledge**. The API records the acknowledgement timestamp and operator label. Acknowledgement means the alert has been seen, not that the condition has been fixed.
* **Resolved:** rule-based alerts resolve automatically when a later reading returns inside the configured range. Built-in threshold alerts are also resolved when the reading recovers. Resolved alerts no longer appear in the active-alert list.
* **Deduplication:** a continuing breach does not create a new active copy of the same rule alert for every reading.

The acknowledgement endpoint is:

```http
POST /api/commandstream/alerts/{id}/acknowledge
Content-Type: application/json

{
  "acknowledgedBy": "Operator"
}
```

An alert cannot be acknowledged after it has been resolved. Repeating an acknowledgement request is safe and returns the existing acknowledgement.

## Useful command-stream endpoints

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/commandstream/dashboard` | Dashboard, active alerts, sensors, recent commands, and queue metrics |
| POST | `/api/commandstream/telemetry` | Submit a telemetry packet for rule evaluation |
| POST | `/api/commandstream/commands` | Apply a simulated device command |
| POST | `/api/commandstream/search` | Record a search for operator-pattern history |
| GET | `/api/commandstream/insights` | Return threshold and recurring-activity suggestions |
| POST | `/api/commandstream/alerts/{id}/acknowledge` | Record manual acknowledgement |
| GET | `/api/commandstream/metrics` | Read in-memory queue and processing metrics |

Example telemetry request:

```json
{
  "sensorMac": "00:1A:2B:3C:4D:5E",
  "dataType": "temperature",
  "dataValue": 42
}
```

Example manual command request:

```json
{
  "sensorMac": "00:1A:2B:3C:4D:5F",
  "commandType": "START"
}
```

Command execution currently updates a simulated state in SQL Server. It does not control physical sensors or actuators. The in-memory undo stack and packet timeline reset when the API restarts, while database-backed telemetry, alert state, commands, device state, and operator activity remain persisted.

---

# Testing

Run these commands from the repository root.

## Backend build and unit tests

```bash
dotnet restore backend/SmartIotApi.csproj
dotnet build backend/SmartIotApi.csproj --configuration Release
dotnet test tests/SmartIotApi.Tests/SmartIotApi.Tests.csproj --configuration Release
```

The .NET unit tests cover critical-packet priority, repeated search-to-command pattern detection, avoiding pattern claims from a single sequence, and concurrent queue access.

## Manually test predictive operator patterns

Run the Docker stack and open `http://localhost:8088`. Navigate to **Command Stream & History** (the live operations page). The prediction is an explainable rule-based correlation, not an AI model, and it never executes commands automatically.

1. In **Device registry**, search for a known sensor using its node ID or MAC address. A search of at least two characters is logged after a short debounce. The query must match a sensor identifier or attribute so the search can be associated with that sensor.
2. For that same sensor, click a simulated command such as **ON** or **OFF**. Search and command must occur within 30 minutes of one another.
3. Repeat the same search and then issue the same command to that same sensor a second time.
4. Wait for the next dashboard refresh (up to five seconds) and inspect **Automated insights**. Look for **Recurring operator pattern detected**, including the observed sequence count and confidence.
5. As a negative check, perform only one search-command sequence for another sensor/command. It should not qualify as a recurring pattern by itself.
6. For a separate threshold check, click **Test alert**. This sends a demo low-moisture packet to a sensor and should produce an active alert/threshold insight. Acknowledge the alert and confirm acknowledgement does not itself resolve it.

The search-to-command sequence must be repeated for the same sensor and command, with the search preceding the command. The pattern history is persisted in SQL Server and evaluated from recent operator activity. Use test/demo sensors because all device commands in this project are simulated.

## Frontend async behavior, lint, and production build

```bash
cd frontend
npm install
npm test
npm run lint
npm run build
```

The dependency-free Node test suite checks that overlapping dashboard refreshes are coalesced, a failed request releases the guard for retry, and a pending request does not block other event-loop work.

GitHub Actions runs backend build/tests, frontend tests/lint/build, and Docker Compose validation/builds. Treat a green CI run as the verification gate before merging or deploying.

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


---

# Part 2: Real-Time Command Stream and Device Management

Pillar 2 is available from the **Real-Time Command Stream and History** navigation entry. Pillar 1 remains available, and Pillar 3 remains disabled.

## Part 2 implementation

The API exposes the following routes:

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/commandstream/dashboard` | Dashboard snapshot, sensors, active alerts, recent telemetry, command history and in-memory metrics |
| POST | `/api/commandstream/telemetry` | Validate a sensor packet, queue it, prioritize critical packets, persist telemetry and create a deduplicated active alert |
| POST | `/api/commandstream/commands` | Record a simulated manual override for an existing sensor |
| POST | `/api/commandstream/undo` | Record the inverse of the latest override in the current API session |
| GET | `/api/commandstream/device/{sensorMac}` | Look up one device and return recent telemetry and commands |
| GET | `/api/commandstream/insights` | Return threshold-based suggested actions |
| POST | `/api/commandstream/search` | Record operator searches for simple repeated-search insights |
| GET | `/api/commandstream/metrics` | Return stream data-structure counts |

## Required C# data structures

- `Queue<StreamPacket>`: routine packets are processed FIFO.
- `PriorityQueue<StreamPacket, int>`: critical packets are dequeued before routine packets.
- `Stack<CommandUndoEntry>`: stores the inverse of issued commands for an undo action.
- `Dictionary<string, DeviceSnapshot>`: provides MAC-address keyed live device lookups.
- `SortedDictionary<DateTime, List<TimelineEntry>>`: stores stream timeline entries by timestamp.
- `HashSet<string>`: deduplicates active in-memory alert keys.

These structures are in-memory and reset when the API process restarts. Existing SQL Server tables continue to store telemetry, active alerts and command history. No new database table is required for this implementation.

## How to test Part 2

1. Start SQL Server and ensure the existing SmartIot database is available to the API.
2. Start the API from the backend project directory with `dotnet run`.
3. Start the React app from `frontend` with `npm install` (first run only) and `npm run dev`.
4. Open the app and select **Real-Time Command Stream and History**.
5. Confirm that the device registry shows sensors already present in the database.
6. Use **Test critical alert**. It sends a low-moisture demo packet, persists the telemetry, and creates an active alert if that sensor does not already have the same unresolved error code.
7. Issue ON/OFF or actuator OPEN/CLOSE/START/STOP commands. Confirm that each appears in command history.
8. Select **Undo last**. Confirm that an inverse command is added to the command history. The undo stack is session-only.
9. Search for a device and use category and active-alert filters.
10. Check Automated Insights. A moisture/humidity value below 20, temperature at least 40, or power/wattage at least 1000 produces a threshold suggestion.
11. Verify API endpoints with Swagger/OpenAPI when running in Development, or call the routes above from Postman.

Example request body for a normal telemetry packet:

```json
{
  "sensorMac": "00:1A:2B:3C:4D:5E",
  "dataType": "temperature",
  "dataValue": 26.5
}
```

Example request body for a critical test packet:

```json
{
  "sensorMac": "00:1A:2B:3C:4D:5E",
  "dataType": "moisture",
  "dataValue": 12,
  "isCritical": true,
  "errorCode": "DEMO-MOISTURE-LOW"
}
```

Example override request:

```json
{
  "sensorMac": "00:1A:2B:3C:4D:60",
  "commandType": "ON"
}
```

**Safety note:** override commands are simulated and recorded in SQL Server. They do not operate physical relays, pumps, or valves. The queue and undo stack are currently process-local, so the undo history is cleared when the API restarts.

## Continuous integration

The workflow at `.github/workflows/ci.yml` runs on pushes and pull requests targeting `master` or `main`. It restores and builds the .NET 10 API, runs discovered .NET test projects if present, installs frontend dependencies with `npm ci`, runs ESLint, and creates a Vite production build. Check the Actions tab for the actual run result.


---

# Docker deployment (frontend + API + SQL Server)

The repository includes a Linux-container Docker Compose stack. It runs three application services plus a one-shot database bootstrap container:

- **frontend**: production Vite/React build served by Nginx on port 8088 by default.
- **backend**: .NET 10 API on port 5231 by default.
- **sqlserver**: SQL Server 2022 Developer in its own container, with a persistent Docker volume.
- **database-init**: waits for SQL Server, then runs `database/init/001-create-database.sql` to create `SmartIot` if it does not exist.

On the first API startup, EF Core `EnsureCreated()` builds the tables and relationships from the current `SmartIotContext` model, including `Sensors`, `TelemetryData`, `ActiveAlerts`, `CommandHistory`, `Attachments`, `DeploymentNodes`, `NetworkTopology`, `AlertRules`, and `CommandDeviceStates`. The API seeds demo sensors and telemetry when starting against an empty database.

## Start on Windows

Run these commands from the repository root in Command Prompt:

```cmd
copy .env.example .env
notepad .env
docker compose up --build -d
docker compose ps
docker compose logs -f backend
```

Before starting, change `MSSQL_SA_PASSWORD` in `.env` to a strong password that meets SQL Server's complexity requirements. Open the app at `http://localhost:8088`. The API is also exposed at `http://localhost:5231`, and SQL Server is exposed on host port 14330 by default (container port 1433), avoiding a clash with a SQL Server instance already installed on Windows.

To connect from SSMS, use server `localhost,14330`, SQL Server Authentication, login `sa`, and the password from `.env`. Select the `SmartIot` database.

The SQL data is persisted in the `smartx-sql-data` Docker volume. Stopping the stack with `docker compose down` preserves the database. **`docker compose down -v` deletes the persistent database volume and all data stored in it.** The container database is separate from any SQL Server instance already installed on the Windows host. Existing host data is not automatically copied into the container.

## Database setup scripts

- `database/init/001-create-database.sql`: safe, repeatable creation of the `SmartIot` database for a new SQL Server container. EF Core creates the full schema from the application models on first API startup.
- `database/command-stream-alert-settings.sql`: upgrade script for an existing `SmartIot` database that already contains the core tables but predates the command-stream tables. Run it in SSMS against the existing `SmartIot` database before using the command-stream alert settings on that older database. It creates `AlertRules` and `CommandDeviceStates` and provisions sensor-specific defaults.

## Sensor-specific alert defaults

When the command-stream dashboard loads, each sensor that has no existing sensor-scoped rules receives defaults based on its `Category`:

| Sensor category | Default metrics and thresholds |
| --- | --- |
| Environmental | Temperature >= 40 (critical), temperature <= 0 (warning), humidity >= 85 (warning), moisture < 20 (warning) |
| Power | Power wattage >= 1000 (critical), power wattage <= 0 (warning) |
| Actuator | Valve state >= 2 or < 0 (critical), because expected simulated states are 0 or 1 |
| Other categories | Generic reading >= 100 or < 0 (warning) |

Defaults are tied to the sensor's MAC address. Rules can be edited, disabled, or deleted from the command-stream page, and operators can still create an all-sensors rule intentionally. Thresholds should be tuned to the actual units and capabilities of deployed hardware. Actuator commands remain simulated and do not operate physical equipment.
