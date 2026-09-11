import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Sidebar from './components/Sidebar';
import { ToastContainer } from './components/Toast';
import { useNotification } from './hooks';
import DashboardPage from './pages/DashboardPage';
import SensorCRUDPage from './pages/SensorCRUDPage';
import SensorDetailsPage from './pages/SensorDetailsPage';
import ComingSoonPage from './pages/ComingSoonPage';

function App() {
  const { notifications, dismiss } = useNotification();

  return (
    <BrowserRouter>
      <div className="flex h-screen bg-gray-50 dark:bg-gray-950">
        {/* Sidebar */}
        <Sidebar />

        {/* Main Content */}
        <main className="flex-1 overflow-auto">
          <div className="w-full max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
            <Routes>
              <Route path="/" element={<DashboardPage />} />
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/sensors" element={<SensorCRUDPage />} />
              <Route path="/sensor/:macaddress" element={<SensorDetailsPage />} />
              <Route
                path="/commands"
                element={
                  <ComingSoonPage
                    title="Real-Time Command Stream and History"
                    description="Send commands to your sensors and view the complete history of all sensor operations."
                  />
                }
              />
              <Route
                path="/topology"
                element={
                  <ComingSoonPage
                    title="Network Topology and Mesh Routing"
                    description="Visualize your sensor network topology, monitor mesh routing, and optimize network performance."
                  />
                }
              />
            </Routes>
          </div>
        </main>

        {/* Toast Container */}
        <ToastContainer notifications={notifications} onDismiss={dismiss} />
      </div>
    </BrowserRouter>
  );
}

export default App;