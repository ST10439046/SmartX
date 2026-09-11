import React, { useState, useEffect } from 'react';
import {
  Plus,
  Edit2,
  Trash2,
  Search,
  ChevronLeft,
  ChevronRight,
  AlertCircle,
} from 'lucide-react';
import { sensorApi } from '../services/apiClient';
import { useNotification } from '../hooks';
import SensorForm from '../components/SensorForm';
import SensorListTable from '../components/SensorListTable';
import SensorDeleteDialog from '../components/SensorDeleteDialog';

const SensorCRUDPage = () => {
  const { show } = useNotification();
  const [sensors, setSensors] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage] = useState(10);
  const [showForm, setShowForm] = useState(false);
  const [editingSensor, setEditingSensor] = useState(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [sensorToDelete, setSensorToDelete] = useState(null);

  // Fetch all sensors
  const fetchSensors = async () => {
    try {
      setLoading(true);
      const response = await sensorApi.getAllSensors();
      setSensors(response.data);
    } catch (error) {
      show({
        type: 'error',
        message: 'Failed to fetch sensors: ' + (error.response?.data?.message || error.message),
      });
      setSensors([]);
    } finally {
      setLoading(false);
    }
  };

  // Load sensors on mount
  useEffect(() => {
    fetchSensors();
  }, []);

  // Filter sensors based on search term
  const filteredSensors = sensors.filter((sensor) => {
    const searchLower = searchTerm.toLowerCase();
    return (
      sensor.macaddress?.toLowerCase().includes(searchLower) ||
      sensor.nodeId?.toLowerCase().includes(searchLower) ||
      sensor.category?.toLowerCase().includes(searchLower) ||
      sensor.zone?.toLowerCase().includes(searchLower) ||
      sensor.room?.toLowerCase().includes(searchLower)
    );
  });

  // Pagination
  const totalPages = Math.ceil(filteredSensors.length / itemsPerPage);
  const startIndex = (currentPage - 1) * itemsPerPage;
  const paginatedSensors = filteredSensors.slice(
    startIndex,
    startIndex + itemsPerPage
  );

  const handleAddSensor = () => {
    setEditingSensor(null);
    setShowForm(true);
  };

  const handleEditSensor = (sensor) => {
    setEditingSensor(sensor);
    setShowForm(true);
  };

  const handleDeleteClick = (sensor) => {
    setSensorToDelete(sensor);
    setDeleteDialogOpen(true);
  };

  const handleConfirmDelete = async () => {
    if (!sensorToDelete) return;

    try {
      await sensorApi.deleteSensor(sensorToDelete.macaddress);
      show({
        type: 'success',
        message: `Sensor ${sensorToDelete.macaddress} deleted successfully`,
      });
      setSensors(sensors.filter((s) => s.macaddress !== sensorToDelete.macaddress));
      setDeleteDialogOpen(false);
      setSensorToDelete(null);
      setCurrentPage(1);
    } catch (error) {
      show({
        type: 'error',
        message: 'Failed to delete sensor: ' + (error.response?.data?.message || error.message),
      });
    }
  };

  const handleFormSubmit = async (formData) => {
    try {
      if (editingSensor) {
        // Update existing sensor
        await sensorApi.updateSensor(editingSensor.macaddress, formData);
        setSensors(
          sensors.map((s) =>
            s.macaddress === editingSensor.macaddress
              ? { ...s, ...formData }
              : s
          )
        );
        show({
          type: 'success',
          message: 'Sensor updated successfully',
        });
      } else {
        // Create new sensor
        const response = await sensorApi.createSensor(formData);
        setSensors([...sensors, response.data]);
        show({
          type: 'success',
          message: 'Sensor created successfully',
        });
      }
      setShowForm(false);
      setEditingSensor(null);
    } catch (error) {
      show({
        type: 'error',
        message:
          'Failed to save sensor: ' + (error.response?.data?.message || error.message),
      });
    }
  };

  const handleFormCancel = () => {
    setShowForm(false);
    setEditingSensor(null);
  };

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
            Sensor Management
          </h1>
          <p className="mt-2 text-gray-600 dark:text-gray-400">
            Manage your IoT sensors - create, read, update, and delete
          </p>
        </div>
        <button
          onClick={handleAddSensor}
          className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 font-medium text-white hover:bg-blue-700 transition-colors"
        >
          <Plus className="h-5 w-5" />
          Add Sensor
        </button>
      </div>

      {/* Search Bar */}
      {!showForm && (
        <div className="relative">
          <Search className="absolute left-3 top-3 h-5 w-5 text-gray-400" />
          <input
            type="text"
            placeholder="Search by MAC, Node ID, Category, Zone, or Room..."
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value);
              setCurrentPage(1);
            }}
            className="w-full rounded-lg border border-gray-300 bg-white pl-10 pr-4 py-2 text-gray-900 placeholder-gray-500 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-800 dark:text-white dark:placeholder-gray-400"
          />
        </div>
      )}

      {/* Form */}
      {showForm && (
        <SensorForm
          sensor={editingSensor}
          onSubmit={handleFormSubmit}
          onCancel={handleFormCancel}
        />
      )}

      {/* Loading State */}
      {loading && !showForm && (
        <div className="flex items-center justify-center rounded-lg border border-gray-200 bg-white py-12 dark:border-gray-700 dark:bg-gray-800">
          <div className="space-y-4 text-center">
            <div className="inline-flex h-12 w-12 animate-spin rounded-full border-4 border-gray-200 border-t-blue-600 dark:border-gray-700 dark:border-t-blue-500"></div>
            <p className="text-gray-600 dark:text-gray-400">Loading sensors...</p>
          </div>
        </div>
      )}

      {/* Empty State */}
      {!loading && !showForm && sensors.length === 0 && (
        <div className="flex items-center justify-center rounded-lg border border-dashed border-gray-300 bg-white py-12 dark:border-gray-600 dark:bg-gray-800">
          <div className="space-y-4 text-center">
            <AlertCircle className="mx-auto h-12 w-12 text-gray-400" />
            <p className="text-gray-600 dark:text-gray-400">
              No sensors found. Start by adding your first sensor.
            </p>
            <button
              onClick={handleAddSensor}
              className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 font-medium text-white hover:bg-blue-700 transition-colors"
            >
              <Plus className="h-5 w-5" />
              Add Sensor
            </button>
          </div>
        </div>
      )}

      {/* Sensors Table */}
      {!loading && !showForm && sensors.length > 0 && (
        <>
          {filteredSensors.length === 0 ? (
            <div className="flex items-center justify-center rounded-lg border border-dashed border-gray-300 bg-white py-12 dark:border-gray-600 dark:bg-gray-800">
              <div className="text-center">
                <Search className="mx-auto h-12 w-12 text-gray-400" />
                <p className="mt-4 text-gray-600 dark:text-gray-400">
                  No sensors match your search.
                </p>
              </div>
            </div>
          ) : (
            <>
              <SensorListTable
                sensors={paginatedSensors}
                onEdit={handleEditSensor}
                onDelete={handleDeleteClick}
              />

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex items-center justify-between rounded-lg border border-gray-200 bg-white px-4 py-3 dark:border-gray-700 dark:bg-gray-800">
                  <div className="text-sm text-gray-600 dark:text-gray-400">
                    Showing {startIndex + 1} to{' '}
                    {Math.min(startIndex + itemsPerPage, filteredSensors.length)} of{' '}
                    {filteredSensors.length} sensors
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() =>
                        setCurrentPage((p) => Math.max(1, p - 1))
                      }
                      disabled={currentPage === 1}
                      className="rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700"
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </button>
                    <div className="flex items-center gap-1">
                      {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                        let pageNum;
                        if (totalPages <= 5) {
                          pageNum = i + 1;
                        } else if (currentPage <= 3) {
                          pageNum = i + 1;
                        } else if (currentPage >= totalPages - 2) {
                          pageNum = totalPages - 4 + i;
                        } else {
                          pageNum = currentPage - 2 + i;
                        }
                        return (
                          <button
                            key={pageNum}
                            onClick={() => setCurrentPage(pageNum)}
                            className={`rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                              currentPage === pageNum
                                ? 'bg-blue-600 text-white'
                                : 'border border-gray-300 text-gray-700 hover:bg-gray-50 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700'
                            }`}
                          >
                            {pageNum}
                          </button>
                        );
                      })}
                    </div>
                    <button
                      onClick={() =>
                        setCurrentPage((p) => Math.min(totalPages, p + 1))
                      }
                      disabled={currentPage === totalPages}
                      className="rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700"
                    >
                      <ChevronRight className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </>
      )}

      {/* Delete Confirmation Dialog */}
      <SensorDeleteDialog
        isOpen={deleteDialogOpen}
        sensor={sensorToDelete}
        onConfirm={handleConfirmDelete}
        onCancel={() => {
          setDeleteDialogOpen(false);
          setSensorToDelete(null);
        }}
      />
    </div>
  );
};

export default SensorCRUDPage;
