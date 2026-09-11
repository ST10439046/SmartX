import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Radio, Activity, Zap, Lock, Menu, X, Database } from 'lucide-react';
import { useState } from 'react';

const Sidebar = () => {
  const [isOpen, setIsOpen] = useState(false);
  const location = useLocation();

  const architecturalPillars = [
    {
      id: 'telemetry',
      name: 'Sensor Data Ingestion and Telemetry',
      icon: Radio,
      path: '/dashboard',
      enabled: true,
      description: 'Ingest and visualize sensor telemetry data',
    },
    {
      id: 'sensors',
      name: 'Sensor Management',
      icon: Database,
      path: '/sensors',
      enabled: true,
      description: 'Create, read, update, and delete sensors',
    },
    {
      id: 'commands',
      name: 'Real-Time Command Stream and History',
      icon: Activity,
      path: '/commands',
      enabled: false,
      description: 'Send and track real-time commands to sensors',
    },
    {
      id: 'topology',
      name: 'Network Topology and Mesh Routing',
      icon: Zap,
      path: '/topology',
      enabled: false,
      description: 'Visualize sensor network topology and routing',
    },
  ];

  const isActive = (path) => location.pathname === path;

  return (
    <>
      {/* Mobile menu button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="lg:hidden fixed top-4 left-4 z-50 p-2 bg-primary-600 text-white rounded-lg"
      >
        {isOpen ? <X size={24} /> : <Menu size={24} />}
      </button>

      {/* Sidebar */}
      <aside
        className={`${
          isOpen ? 'translate-x-0' : '-translate-x-full'
        } lg:translate-x-0 fixed lg:relative top-0 left-0 z-40 w-64 h-screen bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 overflow-y-auto transition-transform duration-300 ease-out`}
      >
        {/* Header */}
        <div className="h-20 flex items-center gap-3 px-6 border-b border-gray-200 dark:border-gray-800">
          <div className="p-2 bg-primary-100 dark:bg-primary-900/30 rounded-lg">
            <Radio size={24} className="text-primary-600 dark:text-primary-400" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-gray-900 dark:text-white">
              Smart-X
            </h1>
            <p className="text-xs text-gray-500 dark:text-gray-400">IoT Platform</p>
          </div>
        </div>

        {/* Navigation */}
        <nav className="p-4 space-y-2">
          <div className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-4 px-2">
            Architectural Pillars
          </div>

          {architecturalPillars.map((pillar) => {
            const Icon = pillar.icon;
            const isItemActive = isActive(pillar.path);
            const opacity = pillar.enabled ? 'opacity-100' : 'opacity-50';
            const cursor = pillar.enabled ? 'cursor-pointer' : 'cursor-not-allowed';
            const hoverBg = pillar.enabled
              ? 'hover:bg-gray-100 dark:hover:bg-gray-800'
              : '';

            return (
              <Link
                key={pillar.id}
                to={pillar.enabled ? pillar.path : '#'}
                onClick={(e) => {
                  if (!pillar.enabled) {
                    e.preventDefault();
                  } else {
                    setIsOpen(false);
                  }
                }}
                className={`
                  block p-3 rounded-lg transition-all duration-200 group
                  ${cursor} ${opacity} ${hoverBg}
                  ${
                    isItemActive
                      ? 'bg-primary-50 dark:bg-primary-900/20 border-l-4 border-primary-600'
                      : 'border-l-4 border-transparent'
                  }
                `}
                title={pillar.enabled ? '' : 'Coming soon'}
              >
                <div className="flex items-center gap-3">
                  <Icon
                    size={20}
                    className={
                      isItemActive
                        ? 'text-primary-600 dark:text-primary-400'
                        : 'text-gray-500 dark:text-gray-400 group-hover:text-gray-700 dark:group-hover:text-gray-300'
                    }
                  />
                  <div className="flex-1">
                    <p
                      className={`text-sm font-medium ${
                        isItemActive
                          ? 'text-primary-700 dark:text-primary-300'
                          : 'text-gray-700 dark:text-gray-300'
                      }`}
                    >
                      {pillar.name}
                    </p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      {pillar.enabled ? (
                        <span className="inline-block px-2 py-1 bg-success-100 dark:bg-success-900/30 text-success-700 dark:text-success-300 rounded text-xs font-medium">
                          Active
                        </span>
                      ) : (
                        <span className="inline-block px-2 py-1 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 rounded text-xs font-medium">
                          Coming Soon
                        </span>
                      )}
                    </p>
                  </div>
                  {!pillar.enabled && (
                    <Lock size={16} className="text-gray-400 dark:text-gray-600" />
                  )}
                </div>
              </Link>
            );
          })}
        </nav>

        {/* Footer Info */}
        <div className="absolute bottom-0 left-0 right-0 p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
          <div className="text-xs text-gray-600 dark:text-gray-400">
            <p className="font-medium text-gray-700 dark:text-gray-300 mb-1">
              API Status
            </p>
            <div className="flex items-center gap-2">
              <div className="w-2 h-2 bg-success-500 rounded-full animate-pulse"></div>
              <span>Connected to localhost:5231</span>
            </div>
          </div>
        </div>
      </aside>

      {/* Mobile overlay */}
      {isOpen && (
        <div
          className="fixed inset-0 bg-black/50 lg:hidden z-30"
          onClick={() => setIsOpen(false)}
        />
      )}
    </>
  );
};

export default Sidebar;
