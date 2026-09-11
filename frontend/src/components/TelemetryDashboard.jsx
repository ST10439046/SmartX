import { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    AlertCircle,
    Zap,
    Wifi,
    WifiOff,
    Loader,
    Play,
    Square,
} from 'lucide-react';

import {
    sensorApi,
    telemetryApi,
    alertApi,
} from '../services/apiClient';

import { useNotification } from '../hooks';


// =====================================================
// HELPER FUNCTIONS
// =====================================================

const normalizeMac = (mac) => {
    return mac?.trim().toUpperCase() || '';
};

const getSensorMac = (sensor) => {
    return normalizeMac(
        sensor?.macaddress ||
        sensor?.macAddress ||
        sensor?.sensorMac ||
        ''
    );
};

const getTelemetryTimestamp = (telemetry) => {
    return (
        telemetry?.timestamp ||
        telemetry?.createdAt ||
        telemetry?.recordedAt ||
        null
    );
};


// =====================================================
// SENSOR CARD
// =====================================================

const SensorCard = ({
    sensor,
    telemetry,
    isConnected,
    onCardClick,
}) => {
    const currentTelemetry =
        telemetry ||
        (
            Array.isArray(sensor.telemetryData) &&
                sensor.telemetryData.length > 0
                ? sensor.telemetryData[sensor.telemetryData.length - 1]
                : null
        );

    const currentValue =
        currentTelemetry?.dataValue ??
        currentTelemetry?.value ??
        currentTelemetry?.reading ??
        currentTelemetry?.measurement ??
        '--';

    const dataType =
        currentTelemetry?.dataType ||
        sensor.category ||
        'Reading';

    const getUnit = () => {
        const type = dataType.toLowerCase();

        if (type.includes('temperature')) {
            return '°C';
        }

        if (
            type.includes('power') ||
            type.includes('watt')
        ) {
            return 'W';
        }

        if (
            type.includes('humidity')
        ) {
            return '%';
        }

        return '';
    };

    const sensorIsActive = sensor.isActive === true;

    return (
        <div
            onClick={onCardClick}
            className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-5 cursor-pointer hover:shadow-lg transition-shadow border border-gray-200 dark:border-gray-700"
        >
            {/* Header */}
            <div className="flex items-start justify-between mb-4">
                <div>
                    <h3 className="font-semibold text-gray-900 dark:text-white">
                        {sensor.nodeId || 'Unnamed Sensor'}
                    </h3>

                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                        {sensor.macaddress}
                    </p>
                </div>

                <div className="flex flex-col items-end gap-2">
                    {/* Active / Inactive */}
                    <div className="flex items-center gap-1.5">
                        <span
                            className={`w-2 h-2 rounded-full ${sensorIsActive
                                    ? 'bg-green-500'
                                    : 'bg-gray-400'
                                }`}
                        />

                        <span
                            className={`text-xs font-medium ${sensorIsActive
                                    ? 'text-green-600 dark:text-green-400'
                                    : 'text-gray-500 dark:text-gray-400'
                                }`}
                        >
                            {sensorIsActive
                                ? 'Active'
                                : 'Inactive'}
                        </span>
                    </div>

                    {/* Online / Offline */}
                    <div className="flex items-center gap-1.5">
                        {isConnected ? (
                            <>
                                <Wifi className="w-4 h-4 text-green-500" />

                                <span className="text-xs font-medium text-green-600 dark:text-green-400">
                                    Online
                                </span>
                            </>
                        ) : (
                            <>
                                <WifiOff className="w-4 h-4 text-gray-400" />

                                <span className="text-xs font-medium text-gray-500 dark:text-gray-400">
                                    Offline
                                </span>
                            </>
                        )}
                    </div>
                </div>
            </div>

            {/* Current reading */}
            <div className="mb-4">
                <p className="text-sm text-gray-500 dark:text-gray-400">
                    Current Reading
                </p>

                <div className="flex items-baseline gap-2 mt-1">
                    <span className="text-3xl font-bold text-gray-900 dark:text-white">
                        {currentValue}
                    </span>

                    {getUnit() && (
                        <span className="text-sm text-gray-500 dark:text-gray-400">
                            {getUnit()}
                        </span>
                    )}
                </div>
            </div>

            {/* Sensor information */}
            <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                    <span className="text-gray-500 dark:text-gray-400">
                        Category
                    </span>

                    <span className="font-medium text-gray-900 dark:text-white">
                        {sensor.category || 'Unknown'}
                    </span>
                </div>

                <div className="flex justify-between">
                    <span className="text-gray-500 dark:text-gray-400">
                        Room
                    </span>

                    <span className="font-medium text-gray-900 dark:text-white">
                        {sensor.room || 'Unknown'}
                    </span>
                </div>

                <div className="flex justify-between">
                    <span className="text-gray-500 dark:text-gray-400">
                        Zone
                    </span>

                    <span className="font-medium text-gray-900 dark:text-white">
                        {sensor.zone || 'Unknown'}
                    </span>
                </div>
            </div>
        </div>
    );
};


// =====================================================
// ALERT CARD
// =====================================================

const TelemetryAlertCard = ({ alert }) => {
    const severityConfig = {
        info: {
            bgColor: 'bg-blue-50 dark:bg-blue-950/40',
            borderColor: 'border-blue-200 dark:border-blue-800',
            textColor: 'text-blue-900 dark:text-blue-100',
            secondaryText: 'text-blue-700 dark:text-blue-300',
            icon: 'ℹ️',
        },

        warning: {
            bgColor: 'bg-yellow-50 dark:bg-yellow-950/40',
            borderColor: 'border-yellow-200 dark:border-yellow-800',
            textColor: 'text-yellow-900 dark:text-yellow-100',
            secondaryText: 'text-yellow-700 dark:text-yellow-300',
            icon: '⚠️',
        },

        critical: {
            bgColor: 'bg-red-50 dark:bg-red-950/40',
            borderColor: 'border-red-200 dark:border-red-800',
            textColor: 'text-red-900 dark:text-red-100',
            secondaryText: 'text-red-700 dark:text-red-300',
            icon: '🚨',
        },
    };

    const severity =
        alert.severity?.toLowerCase() || 'info';

    const config =
        severityConfig[severity] ||
        severityConfig.info;

    return (
        <div
            className={`rounded-lg border p-4 ${config.bgColor} ${config.borderColor}`}
        >
            <div className="flex items-start gap-3">
                <span className="text-lg">
                    {config.icon}
                </span>

                <div className="flex-1">
                    <h4
                        className={`font-semibold ${config.textColor}`}
                    >
                        {alert.errorCode ||
                            alert.title ||
                            'Alert'}
                    </h4>

                    <p
                        className={`text-sm mt-1 ${config.secondaryText}`}
                    >
                        {alert.description ||
                            alert.message ||
                            'No description available'}
                    </p>

                    {alert.sensorMac && (
                        <p
                            className={`text-xs mt-2 ${config.secondaryText}`}
                        >
                            Sensor: {alert.sensorMac}
                        </p>
                    )}
                </div>
            </div>
        </div>
    );
};


// =====================================================
// MAIN TELEMETRY DASHBOARD
// =====================================================

const TelemetryDashboard = () => {
    const navigate = useNavigate();
    const { show } = useNotification();

    const [sensors, setSensors] = useState([]);
    const [telemetryData, setTelemetryData] = useState({});
    const [alerts, setAlerts] = useState([]);

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const [isSimulating, setIsSimulating] = useState(false);

    const simulationRef = useRef(null);


    // =================================================
    // FETCH DATA
    // =================================================

    const fetchData = useCallback(async () => {
        try {
            setLoading(true);
            setError(null);

            // -----------------------------------------
            // SENSORS
            // -----------------------------------------

            const sensorsResponse =
                await sensorApi.getAllSensors();

            const sensorsData =
                Array.isArray(sensorsResponse.data)
                    ? sensorsResponse.data
                    : [];

            setSensors(sensorsData);


            // -----------------------------------------
            // TELEMETRY
            // -----------------------------------------

            try {
                const telemetryResponse =
                    await telemetryApi.getAllTelemetry();

                const telemetryArray =
                    Array.isArray(telemetryResponse.data)
                        ? telemetryResponse.data
                        : [];

                const telemetryByMac = {};

                telemetryArray.forEach((telemetry) => {
                    const mac = normalizeMac(
                        telemetry.sensorMac ||
                        telemetry.sensorMacAddress ||
                        telemetry.macaddress ||
                        telemetry.macAddress
                    );

                    if (!mac) {
                        return;
                    }

                    const currentTimestamp =
                        getTelemetryTimestamp(telemetry);

                    const existingTelemetry =
                        telemetryByMac[mac];

                    const existingTimestamp =
                        getTelemetryTimestamp(
                            existingTelemetry
                        );

                    if (
                        !existingTelemetry ||
                        (
                            currentTimestamp &&
                            (
                                !existingTimestamp ||
                                new Date(currentTimestamp) >
                                new Date(existingTimestamp)
                            )
                        )
                    ) {
                        telemetryByMac[mac] = telemetry;
                    }
                });

                setTelemetryData(telemetryByMac);

            } catch (telemetryError) {
                console.warn(
                    'Could not fetch telemetry:',
                    telemetryError
                );

                setTelemetryData({});
            }


            // -----------------------------------------
            // ALERTS
            // -----------------------------------------

            try {
                const alertsResponse =
                    await alertApi.getAllAlerts();

                const alertsData =
                    Array.isArray(alertsResponse.data)
                        ? alertsResponse.data.slice(0, 5)
                        : [];

                setAlerts(alertsData);

            } catch (alertError) {
                console.warn(
                    'Could not fetch alerts:',
                    alertError
                );

                setAlerts([]);
            }

        } catch (err) {
            console.error(
                'Error fetching sensor data:',
                err
            );

            setError(
                'Failed to load data from backend'
            );

            show(
                'Failed to load sensor data',
                'error'
            );

        } finally {
            setLoading(false);
        }
    }, [show]);


    // =================================================
    // INITIAL LOAD + AUTO REFRESH
    // =================================================

    useEffect(() => {
        let cancelled = false;

        const loadInitialData = async () => {
            if (cancelled) {
                return;
            }

            await fetchData();
        };

        loadInitialData();

        const refreshInterval = setInterval(() => {
            if (!cancelled) {
                fetchData();
            }
        }, 10000);

        return () => {
            cancelled = true;
            clearInterval(refreshInterval);
        };
    }, [fetchData]);


    // =================================================
    // SENSOR STATUS
    // =================================================

    const isSensorActive = (sensor) => {
        return sensor.isActive === true;
    };


    const isSensorOnline = (sensor) => {
        const mac = getSensorMac(sensor);

        if (!mac) {
            return false;
        }

        const telemetry = telemetryData[mac];

        if (!telemetry) {
            return false;
        }

        const timestamp =
            getTelemetryTimestamp(telemetry);

        if (!timestamp) {
            return false;
        }

        const lastSeen = new Date(timestamp);

        if (Number.isNaN(lastSeen.getTime())) {
            return false;
        }

        const now = new Date();

        const differenceInSeconds =
            (now.getTime() - lastSeen.getTime()) / 1000;

        return (
            differenceInSeconds >= 0 &&
            differenceInSeconds <= 60
        );
    };


    // =================================================
    // COUNTS
    // =================================================

    const totalSensors = sensors.length;

    const activeSensors = sensors.filter(
        (sensor) => isSensorActive(sensor)
    ).length;

    const inactiveSensors = sensors.filter(
        (sensor) => !isSensorActive(sensor)
    ).length;

    const onlineSensors = sensors.filter(
        (sensor) =>
            isSensorActive(sensor) &&
            isSensorOnline(sensor)
    ).length;

    const offlineSensors = sensors.filter(
        (sensor) =>
            isSensorActive(sensor) &&
            !isSensorOnline(sensor)
    ).length;


    // =================================================
    // SIMULATION PAYLOAD
    // =================================================

    const createSimulationPayload = (sensor) => {
        const mac = sensor.macaddress;
        const category =
            (sensor.category || '').toLowerCase();

        const timestamp =
            new Date().toISOString();


        // Environmental sensor

        if (
            mac === '00:1A:2B:3C:4D:5E' ||
            category.includes('environment')
        ) {
            const temperature =
                22 + Math.random() * 8;

            return {
                sensorMac: mac,
                dataType: 'temperature',
                dataValue:
                    Number(temperature.toFixed(1)),
                timestamp,
            };
        }


        // Power sensor

        if (
            mac === '00:1A:2B:3C:4D:5F' ||
            category.includes('power')
        ) {
            const watts =
                320 + Math.random() * 180;

            return {
                sensorMac: mac,
                dataType: 'powerWattage',
                dataValue: Math.round(watts),
                timestamp,
            };
        }


        // Actuator

        if (
            mac === '00:1A:2B:3C:4D:60' ||
            category.includes('actuator')
        ) {
            return {
                sensorMac: mac,
                dataType: 'valveState',
                dataValue:
                    Math.random() > 0.5
                        ? 1
                        : 0,
                timestamp,
            };
        }


        // Generic reading

        return {
            sensorMac: mac,
            dataType: 'reading',
            dataValue:
                Number(
                    (Math.random() * 100).toFixed(2)
                ),
            timestamp,
        };
    };


    // =================================================
    // START SIMULATION
    // =================================================

    const startSimulation = () => {
        if (isSimulating) {
            return;
        }

        if (sensors.length === 0) {
            show(
                'No sensors available for simulation',
                'warning'
            );

            return;
        }

        setIsSimulating(true);


        const sendTelemetry = async () => {
            try {
                const randomSensor =
                    sensors[
                    Math.floor(
                        Math.random() *
                        sensors.length
                    )
                    ];

                const payload =
                    createSimulationPayload(
                        randomSensor
                    );

                await telemetryApi.createTelemetry(
                    payload
                );

                await fetchData();

            } catch (error) {
                console.error(
                    'Simulation telemetry error:',
                    error
                );
            }
        };


        sendTelemetry();

        simulationRef.current =
            setInterval(
                sendTelemetry,
                2000
            );
    };


    // =================================================
    // STOP SIMULATION
    // =================================================

    const stopSimulation = () => {
        if (simulationRef.current) {
            clearInterval(
                simulationRef.current
            );

            simulationRef.current = null;
        }

        setIsSimulating(false);
    };


    // =================================================
    // CLEAN UP SIMULATION
    // =================================================

    useEffect(() => {
        return () => {
            if (simulationRef.current) {
                clearInterval(
                    simulationRef.current
                );
            }
        };
    }, []);


    // =================================================
    // SENSOR CLICK
    // =================================================

    const handleSensorClick = (sensor) => {
        const mac = sensor.macaddress;

        navigate(`/sensor/${mac}`);
    };


    // =================================================
    // LOADING
    // =================================================

    if (loading && sensors.length === 0) {
        return (
            <div className="flex items-center justify-center py-20">
                <Loader className="w-8 h-8 animate-spin text-primary-600" />
            </div>
        );
    }


    // =================================================
    // ERROR
    // =================================================

    if (error && sensors.length === 0) {
        return (
            <div className="bg-red-50 dark:bg-red-950/30 border border-red-200 dark:border-red-800 rounded-lg p-6">
                <div className="flex items-center gap-3">
                    <AlertCircle className="w-6 h-6 text-red-600" />

                    <div>
                        <h3 className="font-semibold text-red-900 dark:text-red-100">
                            Unable to load dashboard
                        </h3>

                        <p className="text-sm text-red-700 dark:text-red-300 mt-1">
                            {error}
                        </p>
                    </div>
                </div>
            </div>
        );
    }


    // =================================================
    // RENDER
    // =================================================

    return (
        <div className="space-y-6">

            {/* =========================================
                DASHBOARD HEADER
            ========================================= */}

            <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">

                <div>
                    <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
                        Telemetry Overview
                    </h2>

                    <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                        Monitor sensor status and telemetry data
                    </p>
                </div>


                {/* Simulation controls */}

                <div className="flex items-center gap-2">

                    {!isSimulating ? (
                        <button
                            onClick={startSimulation}
                            className="flex items-center gap-2 px-4 py-2 rounded-lg bg-green-600 hover:bg-green-700 text-white text-sm font-medium transition-colors"
                        >
                            <Play className="w-4 h-4" />

                            Start Simulation
                        </button>
                    ) : (
                        <button
                            onClick={stopSimulation}
                            className="flex items-center gap-2 px-4 py-2 rounded-lg bg-red-600 hover:bg-red-700 text-white text-sm font-medium transition-colors"
                        >
                            <Square className="w-4 h-4" />

                            Stop Simulation
                        </button>
                    )}

                </div>
            </div>


            {/* =========================================
                STATISTICS
            ========================================= */}

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">

                {/* Total */}

                <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-5 border border-gray-200 dark:border-gray-700">
                    <div className="flex items-center justify-between">
                        <div>
                            <p className="text-sm text-gray-500 dark:text-gray-400">
                                Total Sensors
                            </p>

                            <p className="text-2xl font-bold text-gray-900 dark:text-white mt-1">
                                {totalSensors}
                            </p>
                        </div>

                        <div className="p-3 rounded-lg bg-blue-50 dark:bg-blue-950/40">
                            <Zap className="w-6 h-6 text-blue-600 dark:text-blue-400" />
                        </div>
                    </div>
                </div>


                {/* Active */}

                <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-5 border border-gray-200 dark:border-gray-700">
                    <div className="flex items-center justify-between">
                        <div>
                            <p className="text-sm text-gray-500 dark:text-gray-400">
                                Active
                            </p>

                            <p className="text-2xl font-bold text-green-600 dark:text-green-400 mt-1">
                                {activeSensors}
                            </p>
                        </div>

                        <div className="p-3 rounded-lg bg-green-50 dark:bg-green-950/40">
                            <Wifi className="w-6 h-6 text-green-600 dark:text-green-400" />
                        </div>
                    </div>
                </div>


                {/* Inactive */}

                <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-5 border border-gray-200 dark:border-gray-700">
                    <div className="flex items-center justify-between">
                        <div>
                            <p className="text-sm text-gray-500 dark:text-gray-400">
                                Inactive
                            </p>

                            <p className="text-2xl font-bold text-gray-500 dark:text-gray-400 mt-1">
                                {inactiveSensors}
                            </p>
                        </div>

                        <div className="p-3 rounded-lg bg-gray-100 dark:bg-gray-700">
                            <WifiOff className="w-6 h-6 text-gray-500" />
                        </div>
                    </div>
                </div>


           

            </div>


            {/* =========================================
                SENSORS
            ========================================= */}

            <div>
                <div className="flex items-center justify-between mb-4">
                    <div>
                        <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                            Sensors
                        </h3>

                        
                    </div>
                </div>


                {sensors.length === 0 ? (
                    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md border border-gray-200 dark:border-gray-700 p-10 text-center">
                        <Zap className="w-10 h-10 mx-auto text-gray-400 mb-3" />

                        <h3 className="font-semibold text-gray-900 dark:text-white">
                            No sensors registered
                        </h3>

                        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                            Register a sensor to start monitoring telemetry.
                        </p>
                    </div>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
                        {sensors.map((sensor) => {
                            const mac =
                                getSensorMac(sensor);

                            return (
                                <SensorCard
                                    key={mac}
                                    sensor={sensor}
                                    telemetry={
                                        telemetryData[mac]
                                    }
                                    isConnected={
                                        isSensorOnline(sensor)
                                    }
                                    onCardClick={() =>
                                        handleSensorClick(
                                            sensor
                                        )
                                    }
                                />
                            );
                        })}
                    </div>
                )}

            </div>


            {/* =========================================
                ACTIVE ALERTS
            ========================================= */}

            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4 border border-gray-200 dark:border-gray-700">

                <div className="flex items-center justify-between mb-4">

                    <div>
                        <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                            Active Alerts
                        </h3>

                        <p className="text-sm text-gray-500 dark:text-gray-400">
                            Recent sensor alerts
                        </p>
                    </div>

                    <AlertCircle className="w-5 h-5 text-gray-500" />

                </div>


                {alerts.length === 0 ? (
                    <div className="py-8 text-center">
                        <p className="text-sm text-gray-500 dark:text-gray-400">
                            No active alerts
                        </p>
                    </div>
                ) : (
                    <div className="space-y-3">
                        {alerts.map((alert, index) => (
                            <TelemetryAlertCard
                                key={
                                    alert.alertId ||
                                    alert.id ||
                                    index
                                }
                                alert={alert}
                            />
                        ))}
                    </div>
                )}

            </div>

        </div>
    );
};

export default TelemetryDashboard;