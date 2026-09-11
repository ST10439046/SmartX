import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Loader, AlertCircle, RefreshCw, Download, Trash2 } from 'lucide-react';
import { sensorApi, telemetryApi, alertApi, commandApi, attachmentApi } from '../services/apiClient';
import { useNotification } from '../hooks';
import AttachmentUpload from '../components/AttachmentUpload';

const SensorDetailsPage = () => {
    const { macaddress } = useParams();
    const navigate = useNavigate();
    const { show } = useNotification();

    const [sensor, setSensor] = useState(null);
    const [telemetry, setTelemetry] = useState([]);
    const [alerts, setAlerts] = useState([]);
    const [commands, setCommands] = useState([]);
    const [attachments, setAttachments] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [refreshing, setRefreshing] = useState(false);

    const fetchData = useCallback(async () => {
        try {
            setLoading(true);
            setError(null);

            // Fetch sensor details
            const sensorResponse = await sensorApi.getSensorByMac(macaddress);
            setSensor(sensorResponse.data);

            // Fetch all telemetry and filter by sensor MAC
            try {
                const telemetryResponse = await telemetryApi.getAllTelemetry();
                const telemetryArray = Array.isArray(telemetryResponse.data)
                    ? telemetryResponse.data
                    : [];

                const sensorTelemetry = telemetryArray
                    .filter((t) => {
                        const mac = t.sensorMac || t.sensorMacAddress || t.macaddress;
                        return mac === macaddress;
                    })
                    .sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp));

                setTelemetry(sensorTelemetry);
            } catch (telemetryError) {
                console.warn('Could not fetch telemetry:', telemetryError);
                setTelemetry([]);
            }

            // Fetch all alerts and filter by sensor MAC
            try {
                const alertsResponse = await alertApi.getAllAlerts();
                const alertsArray = Array.isArray(alertsResponse.data)
                    ? alertsResponse.data
                    : [];

                const sensorAlerts = alertsArray
                    .filter((a) => a.sensorMac === macaddress)
                    .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));

                setAlerts(sensorAlerts);
            } catch (alertsError) {
                console.warn('Could not fetch alerts:', alertsError);
                setAlerts([]);
            }

            // Fetch all commands and filter by sensor MAC
            try {
                const commandsResponse = await commandApi.getAllCommands();
                const commandsArray = Array.isArray(commandsResponse.data)
                    ? commandsResponse.data
                    : [];

                const sensorCommands = commandsArray
                    .filter((c) => c.sensorMac === macaddress)
                    .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));

                setCommands(sensorCommands);
            } catch (commandsError) {
                console.warn('Could not fetch commands:', commandsError);
                setCommands([]);
            }

            // Fetch all attachments and filter by sensor MAC
            try {
                const attachmentsResponse = await attachmentApi.getAttachmentsBySensor(macaddress);
                const attachmentsArray = Array.isArray(attachmentsResponse.data)
                    ? attachmentsResponse.data
                    : [];

                setAttachments(attachmentsArray);
            } catch (attachmentsError) {
                console.warn('Could not fetch attachments:', attachmentsError);
                setAttachments([]);
            }
        } catch (err) {
            console.error('Error fetching sensor details:', err);
            setError('Failed to load sensor details');
            show('Failed to load sensor details', 'error');
        } finally {
            setLoading(false);
        }
    }, [macaddress, show]);

    useEffect(() => {
        // eslint-disable-next-line react-hooks/exhaustive-deps
        (async () => {
            await fetchData();
        })();
    }, [fetchData]);

    const handleRefresh = async () => {
        setRefreshing(true);
        await fetchData();
        setRefreshing(false);
        show('Data refreshed', 'success');
    };

    const handleDeleteAttachment = async (attachmentId) => {
        if (!window.confirm('Are you sure you want to delete this attachment?')) {
            return;
        }

        try {
            await attachmentApi.deleteAttachment(attachmentId);
            show('Attachment deleted successfully', 'success');
            await fetchData();
        } catch (err) {
            console.error('Error deleting attachment:', err);
            show('Failed to delete attachment', 'error');
        }
    };

    const handleDownloadAttachment = async (attachmentId) => {
        try {
            const response = await attachmentApi.downloadAttachment(attachmentId);
            // Get the attachment details to get the filename
            const attachment = attachments.find((a) => a.attachmentId === attachmentId);
            const filename = attachment?.fileName || 'download';

            // Create a blob URL and trigger download
            const url = window.URL.createObjectURL(new Blob([response.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', filename);
            document.body.appendChild(link);
            link.click();
            link.parentNode.removeChild(link);
            window.URL.revokeObjectURL(url);

            show('File downloaded successfully', 'success');
        } catch (err) {
            console.error('Error downloading attachment:', err);
            show('Failed to download attachment', 'error');
        }
    };

    const handleAttachmentUploadComplete = async () => {
        await fetchData();
        show('Attachment uploaded successfully', 'success');
    };

    if (loading) {
        return (
            <div className="flex items-center justify-center min-h-screen">
                <div className="text-center">
                    <Loader
                        size={40}
                        className="mx-auto text-primary-600 mb-4 animate-spin"
                    />
                    <p className="text-gray-600 dark:text-gray-400">
                        Loading sensor details...
                    </p>
                </div>
            </div>
        );
    }

    if (error || !sensor) {
        return (
            <div className="space-y-4">
                <button
                    onClick={() => navigate('/dashboard')}
                    className="inline-flex items-center gap-2 text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
                >
                    <ArrowLeft size={16} />
                    Back to Overview
                </button>

                <div className="bg-danger-50 dark:bg-danger-900/20 border border-danger-300 dark:border-danger-700 text-danger-800 dark:text-danger-200 rounded-lg p-6 flex items-start gap-4">
                    <AlertCircle size={24} className="flex-shrink-0 mt-0.5" />
                    <div>
                        <h2 className="font-bold text-lg mb-1">Sensor Not Found</h2>
                        <p>{error || 'The sensor you are looking for does not exist.'}</p>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-8">
            {/* Header */}
            <div className="flex items-start justify-between gap-4">
                <div className="flex items-center gap-3">
                    <button
                        onClick={() => navigate('/dashboard')}
                        className="p-2 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition"
                    >
                        <ArrowLeft size={20} className="text-gray-600 dark:text-gray-400" />
                    </button>

                    <div>
                        <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
                            {sensor.zone || sensor.room || `Node ${sensor.nodeId || 'Unknown'}`}
                        </h1>
                        <p className="text-gray-600 dark:text-gray-400 mt-1">
                            MAC: {sensor.macaddress}
                        </p>
                    </div>
                </div>

                <button
                    onClick={handleRefresh}
                    disabled={refreshing}
                    className="inline-flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed transition"
                >
                    <RefreshCw size={16} className={refreshing ? 'animate-spin' : ''} />
                    Refresh
                </button>
            </div>

            {/* Sensor Info Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Category</p>
                    <p className="text-lg font-semibold text-gray-900 dark:text-white">
                        {sensor.category || 'Unknown'}
                    </p>
                </div>

                <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Node ID</p>
                    <p className="text-lg font-semibold text-gray-900 dark:text-white">
                        {sensor.nodeId || 'Unknown'}
                    </p>
                </div>

                <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Location</p>
                    <p className="text-lg font-semibold text-gray-900 dark:text-white">
                        {sensor.room || sensor.zone || 'Unknown'}
                    </p>
                </div>

                <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Registered</p>
                    <p className="text-lg font-semibold text-gray-900 dark:text-white">
                        {sensor.registeredAt
                            ? new Date(sensor.registeredAt).toLocaleDateString()
                            : 'N/A'}
                    </p>
                </div>
            </div>

            {/* Main Content Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Telemetry History */}
                <div className="lg:col-span-2">
                    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                        <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-4">
                            Telemetry History
                        </h2>

                        {telemetry.length > 0 ? (
                            <div className="overflow-x-auto">
                                <table className="w-full">
                                    <thead>
                                        <tr className="border-b border-gray-200 dark:border-gray-700">
                                            <th className="text-left py-3 px-4 text-sm font-semibold text-gray-700 dark:text-gray-300">
                                                Timestamp
                                            </th>
                                            <th className="text-left py-3 px-4 text-sm font-semibold text-gray-700 dark:text-gray-300">
                                                Type
                                            </th>
                                            <th className="text-left py-3 px-4 text-sm font-semibold text-gray-700 dark:text-gray-300">
                                                Value
                                            </th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {telemetry.map((record, idx) => (
                                            <tr
                                                key={idx}
                                                className="border-b border-gray-100 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition"
                                            >
                                                <td className="py-3 px-4 text-sm text-gray-700 dark:text-gray-300">
                                                    {new Date(record.timestamp).toLocaleString()}
                                                </td>
                                                <td className="py-3 px-4 text-sm text-gray-700 dark:text-gray-300">
                                                    {record.dataType || 'unknown'}
                                                </td>
                                                <td className="py-3 px-4 text-sm font-medium text-gray-900 dark:text-white">
                                                    {record.dataType === 'valveState'
                                                        ? Number(record.dataValue) === 1
                                                            ? 'Open'
                                                            : 'Closed'
                                                        : record.dataValue}
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        ) : (
                            <div className="text-center py-8 text-gray-500 dark:text-gray-400">
                                No telemetry data available for this sensor.
                            </div>
                        )}
                    </div>
                </div>

                {/* Sidebar: Alerts & Commands */}
                <div className="space-y-6">
                    {/* Alerts */}
                    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                        <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-4">
                            Active Alerts ({alerts.length})
                        </h3>

                        {alerts.length > 0 ? (
                            <div className="space-y-3">
                                {alerts.slice(0, 5).map((alert, idx) => {
                                    const severity = (alert.severity || 'info').toLowerCase();
                                    const severityStyles = {
                                        info: 'bg-primary-50 dark:bg-primary-900/20 border-primary-300 dark:border-primary-700 text-primary-800 dark:text-primary-200',
                                        warning: 'bg-warning-50 dark:bg-warning-900/20 border-warning-300 dark:border-warning-700 text-warning-800 dark:text-warning-200',
                                        critical: 'bg-danger-50 dark:bg-danger-900/20 border-danger-300 dark:border-danger-700 text-danger-800 dark:text-danger-200',
                                    };

                                    return (
                                        <div
                                            key={idx}
                                            className={`border rounded p-3 text-sm ${
                                                severityStyles[severity] || severityStyles.info
                                            }`}
                                        >
                                            <p className="font-medium">
                                                {alert.alertMessage || alert.description}
                                            </p>
                                            <p className="text-xs opacity-75 mt-1">
                                                {new Date(alert.createdAt || alert.alertDate).toLocaleString()}
                                            </p>
                                        </div>
                                    );
                                })}
                            </div>
                        ) : (
                            <div className="text-center py-4 text-gray-500 dark:text-gray-400">
                                No active alerts
                            </div>
                        )}
                    </div>

                    {/* Commands */}
                    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                        <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-4">
                            Command History ({commands.length})
                        </h3>

                        {commands.length > 0 ? (
                            <div className="space-y-3">
                                {commands.slice(0, 5).map((cmd, idx) => (
                                    <div
                                        key={idx}
                                        className="border border-gray-200 dark:border-gray-700 rounded p-3 text-sm"
                                    >
                                        <div className="flex items-center justify-between mb-1">
                                            <p className="font-medium text-gray-900 dark:text-white">
                                                {cmd.commandType}
                                            </p>
                                            <span
                                                className={`text-xs px-2 py-1 rounded ${
                                                    cmd.status === 'Success'
                                                        ? 'bg-success-100 dark:bg-success-900/30 text-success-800 dark:text-success-200'
                                                        : 'bg-warning-100 dark:bg-warning-900/30 text-warning-800 dark:text-warning-200'
                                                }`}
                                            >
                                                {cmd.status}
                                            </span>
                                        </div>
                                        <p className="text-xs text-gray-600 dark:text-gray-400">
                                            {new Date(cmd.createdAt).toLocaleString()}
                                        </p>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="text-center py-4 text-gray-500 dark:text-gray-400">
                                No command history
                            </div>
                        )}
                    </div>

                    {/* Attachments */}
                    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                        <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-4">
                            Attachments ({attachments.length})
                        </h3>

                        {attachments.length > 0 ? (
                            <div className="space-y-3 mb-6">
                                {attachments.map((attachment, idx) => (
                                    <div
                                        key={idx}
                                        className="border border-gray-200 dark:border-gray-700 rounded p-3 text-sm hover:bg-gray-50 dark:hover:bg-gray-700/50 transition"
                                    >
                                        <div className="flex items-center justify-between mb-2">
                                            <div className="flex-1 min-w-0">
                                                <p className="font-medium text-gray-900 dark:text-white truncate">
                                                    {attachment.fileName}
                                                </p>
                                                <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                                                    Type: <span className="font-medium capitalize">{attachment.fileType || 'unknown'}</span>
                                                </p>
                                            </div>

                                            <div className="flex items-center gap-2 flex-shrink-0 ml-3">
                                                <button
                                                    onClick={() => handleDownloadAttachment(attachment.attachmentId)}
                                                    className="p-1.5 hover:bg-gray-200 dark:hover:bg-gray-600 rounded transition text-gray-600 dark:text-gray-400"
                                                    title="Download file"
                                                >
                                                    <Download size={16} />
                                                </button>
                                                <button
                                                    onClick={() => handleDeleteAttachment(attachment.attachmentId)}
                                                    className="p-1.5 hover:bg-danger-100 dark:hover:bg-danger-900/30 rounded transition text-danger-600 dark:text-danger-400"
                                                    title="Delete attachment"
                                                >
                                                    <Trash2 size={16} />
                                                </button>
                                            </div>
                                        </div>

                                        <p className="text-xs text-gray-500 dark:text-gray-400">
                                            Uploaded: {attachment.uploadedAt ? new Date(attachment.uploadedAt).toLocaleString() : 'Unknown'}
                                        </p>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="text-center py-4 text-gray-500 dark:text-gray-400 mb-6">
                                No attachments yet
                            </div>
                        )}

                        {/* Upload Section */}
                        <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
                            <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-3">
                                Upload New Attachment
                            </h4>
                            <AttachmentUpload
                                sensorMac={macaddress}
                                onUploadSuccess={handleAttachmentUploadComplete}
                            />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default SensorDetailsPage;
