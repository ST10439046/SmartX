import { useState } from 'react';
import SensorRegistrationForm from '../components/SensorRegistrationForm';
import TelemetryDashboard from '../components/TelemetryDashboard';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/Tabs';

const DashboardPage = () => {
  const [activeTab, setActiveTab] = useState('overview');

  return (
    <div className="space-y-8">
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="overview">Telemetry Overview</TabsTrigger>
          <TabsTrigger value="register">Register Sensor</TabsTrigger>
        </TabsList>

        <TabsContent value="overview">
          <TelemetryDashboard />
        </TabsContent>

        <TabsContent value="register">
          <SensorRegistrationForm
            onSubmitSuccess={() => setActiveTab('overview')}
          />
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default DashboardPage;
