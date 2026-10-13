import { useCallback, useEffect, useMemo, useState } from 'react';
import { Activity, AlertTriangle, ArrowDownToLine, Bot, CircleAlert, Clock3, Command, RefreshCw, Search, ShieldCheck, Undo2, Zap, BellRing, Pencil, Trash2, Save, X } from 'lucide-react';
import apiClient from '../services/apiClient';

const formatDate = (value) => value ? new Date(value).toLocaleString() : 'No readings yet';
const displayDevice = (sensor) => sensor.nodeId || sensor.macaddress;
const blankRule = { name: '', sensorMac: '', dataType: 'temperature', comparison: '>=', threshold: '40', severity: 'warning', isEnabled: true };

function Metric({ icon: Icon, label, value, hint }) {
  return (
    <div className="rounded-2xl border border-gray-200 bg-white p-4 shadow-sm dark:border-gray-800 dark:bg-gray-900">
      <div className="flex items-center justify-between"><span className="text-sm text-gray-500 dark:text-gray-400">{label}</span><Icon size={18} className="text-primary-600" /></div>
      <div className="mt-3 text-2xl font-bold text-gray-900 dark:text-white">{value}</div>
      <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{hint}</p>
    </div>
  );
}

function CommandStreamPage() {
  const [dashboard, setDashboard] = useState(null);
  const [insights, setInsights] = useState([]);
  const [query, setQuery] = useState('');
  const [category, setCategory] = useState('all');
  const [alertFilter, setAlertFilter] = useState('all');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [lastUpdated, setLastUpdated] = useState(null);
  const [ruleForm, setRuleForm] = useState(blankRule);
  const [editingRuleId, setEditingRuleId] = useState(null);

  const loadDashboard = useCallback(async () => {
    try {
      const [dashboardResponse, insightResponse] = await Promise.all([
        apiClient.get('/api/commandstream/dashboard'),
        apiClient.get('/api/commandstream/insights'),
      ]);
      setDashboard(dashboardResponse.data);
      setInsights(insightResponse.data);
      setLastUpdated(new Date());
      setError('');
    } catch (loadError) {
      setError(loadError.response?.data?.message || 'Unable to load the command stream. Check the API and run the SQL setup script.');
    }
  }, []);

  useEffect(() => {
    loadDashboard();
    const timer = window.setInterval(loadDashboard, 5000);
    return () => window.clearInterval(timer);
  }, [loadDashboard]);

  const sensors = dashboard?.sensors || [];
  const alerts = dashboard?.alerts || [];
  const stream = dashboard?.stream || {};
  const metrics = dashboard?.metrics || {};
  const commands = dashboard?.commands || [];
  const rules = dashboard?.alertRules || [];
  const deviceStates = useMemo(() => new Map((dashboard?.deviceStates || []).map((state) => [state.sensorMac?.toLowerCase(), state.currentState])), [dashboard?.deviceStates]);
  const activeAlertMacs = useMemo(() => new Set(alerts.map((alert) => alert.sensorMac?.toLowerCase())), [alerts]);

  const filteredSensors = useMemo(() => sensors.filter((sensor) => {
    const searchable = [sensor.macaddress, sensor.nodeId, sensor.room, sensor.zone, sensor.category].filter(Boolean).join(' ').toLowerCase();
    const matchesQuery = searchable.includes(query.toLowerCase());
    const matchesCategory = category === 'all' || (sensor.category || '').toLowerCase() === category;
    const hasAlert = activeAlertMacs.has(sensor.macaddress?.toLowerCase());
    const matchesAlert = alertFilter === 'all' || (alertFilter === 'alert' ? hasAlert : !hasAlert);
    return matchesQuery && matchesCategory && matchesAlert;
  }), [sensors, query, category, alertFilter, activeAlertMacs]);

  const categories = [...new Set(sensors.map((sensor) => sensor.category).filter(Boolean))];

  const runAction = async (action) => {
    setBusy(true);
    setNotice('');
    setError('');
    try {
      const response = await action();
      setNotice(response.data?.message || 'Action completed.');
      await loadDashboard();
    } catch (actionError) {
      setError(actionError.response?.data?.message || 'The requested action failed. Check that the SQL setup script has been run.');
    } finally {
      setBusy(false);
    }
  };

  const issueCommand = (sensor, commandType) => runAction(() => apiClient.post('/api/commandstream/commands', { sensorMac: sensor.macaddress, commandType }));
  const undo = () => runAction(() => apiClient.post('/api/commandstream/undo'));
  const searchChanged = async (value) => {
    setQuery(value);
    if (value.trim().length >= 2) {
      try { await apiClient.post('/api/commandstream/search', { query: value.trim() }); } catch { /* Search filtering still works if history logging is unavailable. */ }
    }
  };

  const simulateCritical = async () => {
    const target = filteredSensors[0] || sensors[0];
    if (!target) {
      setError('Register a sensor before sending a test packet.');
      return;
    }
    await runAction(() => apiClient.post('/api/commandstream/telemetry', {
      sensorMac: target.macaddress, dataType: 'moisture', dataValue: 12, isCritical: true, errorCode: 'DEMO-MOISTURE-LOW',
    }));
  };

  const editRule = (rule) => {
    setEditingRuleId(rule.alertRuleId);
    setRuleForm({
      name: rule.name || '',
      sensorMac: rule.sensorMac || '',
      dataType: rule.dataType || 'temperature',
      comparison: rule.comparison || '>=',
      threshold: String(rule.threshold ?? 0),
      severity: rule.severity || 'warning',
      isEnabled: Boolean(rule.isEnabled),
    });
    setError('');
    setNotice('Editing alert rule. Save changes when ready.');
  };

  const resetRuleForm = () => {
    setEditingRuleId(null);
    setRuleForm(blankRule);
  };

  const saveRule = async (event) => {
    event.preventDefault();
    const payload = {
      ...ruleForm,
      sensorMac: ruleForm.sensorMac || null,
      threshold: Number(ruleForm.threshold),
    };
    await runAction(() => editingRuleId
      ? apiClient.put(`/api/commandstream/alert-rules/${editingRuleId}`, payload)
      : apiClient.post('/api/commandstream/alert-rules', payload));
    resetRuleForm();
  };

  const deleteRule = (id) => runAction(async () => {
    const response = await apiClient.delete(`/api/commandstream/alert-rules/${id}`);
    resetRuleForm();
    return { ...response, data: { message: 'Alert rule deleted.' } };
  });

  const toggleRule = (rule) => runAction(() => apiClient.put(`/api/commandstream/alert-rules/${rule.alertRuleId}`, {
    name: rule.name, sensorMac: rule.sensorMac, dataType: rule.dataType, comparison: rule.comparison,
    threshold: rule.threshold, severity: rule.severity, isEnabled: !rule.isEnabled,
  }));

  return (
    <div className="space-y-6 pb-10">
      <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-primary-600"><Activity size={17} /> PILLAR 2 · LIVE OPERATIONS</div>
          <h1 className="text-3xl font-bold tracking-tight text-gray-900 dark:text-white">Command Stream & History</h1>
          <p className="mt-2 max-w-2xl text-sm text-gray-500 dark:text-gray-400">Monitor telemetry, configure threshold alerts, and control the persisted simulated state of devices. Refreshes every five seconds.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button type="button" onClick={loadDashboard} className="inline-flex items-center gap-2 rounded-xl border border-gray-300 px-4 py-2 text-sm font-semibold hover:bg-gray-50 dark:border-gray-700 dark:hover:bg-gray-800"><RefreshCw size={16} /> Refresh</button>
          <button type="button" disabled={busy} onClick={simulateCritical} className="inline-flex items-center gap-2 rounded-xl bg-rose-600 px-4 py-2 text-sm font-semibold text-white hover:bg-rose-700 disabled:opacity-50"><Zap size={16} /> Test alert</button>
        </div>
      </div>

      {error && <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-800 dark:border-rose-900 dark:bg-rose-950/40 dark:text-rose-200">{error}</div>}
      {notice && <div role="status" className="rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-800 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-200">{notice}</div>}

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <Metric icon={Activity} label="Packets processed" value={metrics.processedCount ?? 0} hint="Since this API session started" />
        <Metric icon={Zap} label="Critical queue" value={metrics.criticalQueueDepth ?? 0} hint="Processed before routine packets" />
        <Metric icon={ArrowDownToLine} label="Routine queue" value={metrics.queueDepth ?? 0} hint="First-in, first-out ordering" />
        <Metric icon={ShieldCheck} label="Active alerts" value={alerts.length} hint="Unresolved alerts from SQL Server" />
      </div>

      <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm dark:border-gray-800 dark:bg-gray-900">
        <div className="mb-4 flex items-start gap-3"><BellRing className="mt-1 text-primary-600" size={21} /><div><h2 className="text-lg font-bold text-gray-900 dark:text-white">Alert settings</h2><p className="mt-1 text-sm text-gray-500 dark:text-gray-400">Create threshold rules. Rules are saved in SQL Server, evaluate matching incoming telemetry, create an alert when breached, and auto-resolve when readings return to normal.</p></div></div>
        <form onSubmit={saveRule} className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          <label className="text-xs font-semibold text-gray-600 dark:text-gray-300">Rule name<input required maxLength={100} value={ruleForm.name} onChange={(e) => setRuleForm({ ...ruleForm, name: e.target.value })} placeholder="High greenhouse temperature" className="mt-1 w-full rounded-xl border border-gray-300 bg-transparent px-3 py-2.5 text-sm font-normal dark:border-gray-700" /></label>
          <label className="text-xs font-semibold text-gray-600 dark:text-gray-300">Device<select value={ruleForm.sensorMac} onChange={(e) => setRuleForm({ ...ruleForm, sensorMac: e.target.value })} className="mt-1 w-full rounded-xl border border-gray-300 bg-transparent px-3 py-2.5 text-sm font-normal dark:border-gray-700"><option value="">All devices</option>{sensors.map((sensor) => <option key={sensor.macaddress} value={sensor.macaddress}>{displayDevice(sensor)} · {sensor.macaddress}</option>)}</select></label>
          <label className="text-xs font-semibold text-gray-600 dark:text-gray-300">Telemetry metric<input required maxLength={50} value={ruleForm.dataType} onChange={(e) => setRuleForm({ ...ruleForm, dataType: e.target.value })} placeholder="temperature, moisture, powerWattage" className="mt-1 w-full rounded-xl border border-gray-300 bg-transparent px-3 py-2.5 text-sm font-normal dark:border-gray-700" /></label>
          <div className="grid grid-cols-[100px_1fr] gap-2">
            <label className="text-xs font-semibold text-gray-600 dark:text-gray-300">Condition<select value={ruleForm.comparison} onChange={(e) => setRuleForm({ ...ruleForm, comparison: e.target.value })} className="mt-1 w-full rounded-xl border border-gray-300 bg-transparent px-3 py-2.5 text-sm font-normal dark:border-gray-700">{['>', '>=', '<', '<=', '='].map((op) => <option key={op} value={op}>{op}</option>)}</select></label>
            <label className="text-xs font-semibold text-gray-600 dark:text-gray-300">Threshold<input required type="number" step="any" value={ruleForm.threshold} onChange={(e) => setRuleForm({ ...ruleForm, threshold: e.target.value })} className="mt-1 w-full rounded-xl border border-gray-300 bg-transparent px-3 py-2.5 text-sm font-normal dark:border-gray-700" /></label>
          </div>
          <label className="text-xs font-semibold text-gray-600 dark:text-gray-300">Severity<select value={ruleForm.severity} onChange={(e) => setRuleForm({ ...ruleForm, severity: e.target.value })} className="mt-1 w-full rounded-xl border border-gray-300 bg-transparent px-3 py-2.5 text-sm font-normal dark:border-gray-700"><option value="info">Info</option><option value="warning">Warning</option><option value="critical">Critical</option></select></label>
          <label className="flex items-center gap-2 self-end rounded-xl border border-gray-200 px-3 py-3 text-sm text-gray-700 dark:border-gray-700 dark:text-gray-300"><input type="checkbox" checked={ruleForm.isEnabled} onChange={(e) => setRuleForm({ ...ruleForm, isEnabled: e.target.checked })} /> Rule enabled</label>
          <div className="flex flex-wrap items-end gap-2">
            <button type="submit" disabled={busy} className="inline-flex items-center gap-2 rounded-xl bg-primary-600 px-4 py-2.5 text-sm font-semibold text-white hover:opacity-90 disabled:opacity-50"><Save size={16} /> {editingRuleId ? 'Save changes' : 'Create rule'}</button>
            {editingRuleId && <button type="button" onClick={resetRuleForm} className="inline-flex items-center gap-2 rounded-xl border border-gray-300 px-3 py-2.5 text-sm dark:border-gray-700"><X size={16} /> Cancel</button>}
          </div>
        </form>
        <div className="mt-5 space-y-2">
          {rules.map((rule) => <div key={rule.alertRuleId} className="flex flex-col gap-3 rounded-xl border border-gray-200 p-3 dark:border-gray-800 sm:flex-row sm:items-center sm:justify-between">
            <div className="min-w-0"><div className="flex flex-wrap items-center gap-2"><p className="font-semibold text-gray-900 dark:text-white">{rule.name}</p><span className={`rounded-full px-2 py-0.5 text-xs font-semibold ${rule.severity === 'critical' ? 'bg-rose-100 text-rose-700 dark:bg-rose-950 dark:text-rose-300' : rule.severity === 'warning' ? 'bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-300' : 'bg-sky-100 text-sky-700 dark:bg-sky-950 dark:text-sky-300'}`}>{rule.severity}</span><span className={`rounded-full px-2 py-0.5 text-xs ${rule.isEnabled ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300' : 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-300'}`}>{rule.isEnabled ? 'Enabled' : 'Disabled'}</span></div><p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{rule.sensorMac ? (displayDevice(sensors.find((s) => s.macaddress === rule.sensorMac) || { macaddress: rule.sensorMac })) : 'All devices'} · {rule.dataType} {rule.comparison} {rule.threshold}</p></div>
            <div className="flex shrink-0 gap-2"><button type="button" disabled={busy} onClick={() => toggleRule(rule)} className="rounded-lg border border-gray-300 px-3 py-1.5 text-xs font-semibold dark:border-gray-700">{rule.isEnabled ? 'Disable' : 'Enable'}</button><button type="button" disabled={busy} onClick={() => editRule(rule)} aria-label={`Edit ${rule.name}`} className="rounded-lg border border-gray-300 p-2 dark:border-gray-700"><Pencil size={15} /></button><button type="button" disabled={busy} onClick={() => deleteRule(rule.alertRuleId)} aria-label={`Delete ${rule.name}`} className="rounded-lg border border-rose-200 p-2 text-rose-600 dark:border-rose-900"><Trash2 size={15} /></button></div>
          </div>)}
          {rules.length === 0 && <p className="rounded-xl border border-dashed border-gray-300 p-5 text-center text-sm text-gray-500 dark:border-gray-700">No custom rules yet. Create one above, then send telemetry or use Test alert to evaluate it.</p>}
        </div>
      </section>

      <div className="grid gap-5 xl:grid-cols-[1.4fr_0.8fr]">
        <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm dark:border-gray-800 dark:bg-gray-900">
          <div className="flex flex-wrap items-start justify-between gap-3"><div><h2 className="text-lg font-bold text-gray-900 dark:text-white">Device registry</h2><p className="mt-1 text-sm text-gray-500 dark:text-gray-400">Command buttons now update the persisted simulated output state as well as command history.</p></div><span className="rounded-full bg-gray-100 px-3 py-1 text-xs font-semibold text-gray-600 dark:bg-gray-800 dark:text-gray-300">{filteredSensors.length} devices</span></div>
          <div className="mt-4 grid gap-3 md:grid-cols-[1fr_160px_150px]">
            <label className="relative block"><Search size={16} className="absolute left-3 top-3 text-gray-400" /><input aria-label="Search devices" value={query} onChange={(event) => searchChanged(event.target.value)} placeholder="Search name, MAC, zone..." className="w-full rounded-xl border border-gray-300 bg-transparent py-2.5 pl-9 pr-3 text-sm dark:border-gray-700" /></label>
            <select aria-label="Filter by category" value={category} onChange={(event) => setCategory(event.target.value)} className="rounded-xl border border-gray-300 bg-transparent px-3 py-2 text-sm dark:border-gray-700"><option value="all">All categories</option>{categories.map((item) => <option key={item} value={item.toLowerCase()}>{item}</option>)}</select>
            <select aria-label="Filter by alert state" value={alertFilter} onChange={(event) => setAlertFilter(event.target.value)} className="rounded-xl border border-gray-300 bg-transparent px-3 py-2 text-sm dark:border-gray-700"><option value="all">All alert states</option><option value="alert">Has active alert</option><option value="clear">No active alert</option></select>
          </div>
          <div className="mt-4 space-y-3">
            {filteredSensors.map((sensor) => {
              const hasAlert = activeAlertMacs.has(sensor.macaddress?.toLowerCase());
              const currentState = deviceStates.get(sensor.macaddress?.toLowerCase()) || 'UNKNOWN';
              const isActuator = (sensor.category || '').toLowerCase().includes('actuator');
              const buttons = isActuator ? ['ON', 'OFF', 'OPEN', 'CLOSE', 'START', 'STOP'] : ['ON', 'OFF'];
              return <div key={sensor.macaddress} className="rounded-xl border border-gray-200 p-4 dark:border-gray-800">
                <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                  <div className="min-w-0"><div className="flex flex-wrap items-center gap-2"><h3 className="font-semibold text-gray-900 dark:text-white">{displayDevice(sensor)}</h3><span className={`rounded-full px-2 py-0.5 text-xs font-medium ${hasAlert ? 'bg-rose-100 text-rose-700 dark:bg-rose-950 dark:text-rose-300' : 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300'}`}>{hasAlert ? 'Alert active' : 'No active alert'}</span><span className="rounded-full bg-primary-50 px-2 py-0.5 text-xs font-semibold text-primary-700 dark:bg-primary-950 dark:text-primary-300">State: {currentState}</span></div><p className="mt-1 truncate text-xs text-gray-500 dark:text-gray-400">{sensor.macaddress} · {sensor.category || 'Uncategorized'} · {sensor.room || sensor.zone || 'Location not set'}</p></div>
                  <div className="flex flex-wrap gap-2">{buttons.map((command) => <button key={command} type="button" disabled={busy || currentState === command} onClick={() => issueCommand(sensor, command)} className={`rounded-lg border px-3 py-1.5 text-xs font-semibold disabled:opacity-40 ${currentState === command ? 'border-primary-500 bg-primary-50 text-primary-700 dark:bg-primary-950 dark:text-primary-300' : 'border-gray-300 hover:border-primary-500 hover:text-primary-600 dark:border-gray-700'}`}>{command}</button>)}</div>
                </div>
              </div>;
            })}
            {filteredSensors.length === 0 && <p className="rounded-xl border border-dashed border-gray-300 p-8 text-center text-sm text-gray-500 dark:border-gray-700">No devices match those filters.</p>}
          </div>
        </section>

        <div className="space-y-5">
          <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm dark:border-gray-800 dark:bg-gray-900">
            <div className="flex items-center justify-between gap-3"><div><h2 className="text-lg font-bold text-gray-900 dark:text-white">Automated insights</h2><p className="mt-1 text-sm text-gray-500 dark:text-gray-400">Threshold and operator-pattern suggestions.</p></div><Bot size={21} className="text-primary-600" /></div>
            <div className="mt-4 space-y-3">{insights.map((insight, index) => <div key={`${insight.sensorMac}-${insight.message}-${index}`} className="rounded-xl border border-gray-200 p-3 dark:border-gray-800"><div className="flex items-start gap-2">{insight.severity === 'critical' ? <CircleAlert size={17} className="mt-0.5 text-rose-500" /> : <AlertTriangle size={17} className="mt-0.5 text-amber-500" />}<div><p className="text-sm font-semibold text-gray-900 dark:text-white">{insight.deviceName}</p><p className="mt-1 text-sm text-gray-600 dark:text-gray-300">{insight.message}</p><p className="mt-2 text-xs text-primary-700 dark:text-primary-300">{insight.recommendation}</p></div></div></div>)}{insights.length === 0 && <p className="rounded-xl bg-gray-50 p-4 text-sm text-gray-500 dark:bg-gray-800/60">No out-of-range readings detected.</p>}</div>
          </section>
          <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm dark:border-gray-800 dark:bg-gray-900">
            <div className="flex items-center justify-between gap-3"><div><h2 className="text-lg font-bold text-gray-900 dark:text-white">Command history</h2><p className="mt-1 text-sm text-gray-500 dark:text-gray-400">Recent persisted simulated actions.</p></div><button type="button" disabled={busy || !(metrics.undoDepth > 0)} onClick={undo} className="inline-flex items-center gap-1.5 rounded-lg border border-gray-300 px-3 py-2 text-xs font-semibold hover:bg-gray-50 disabled:opacity-40 dark:border-gray-700 dark:hover:bg-gray-800"><Undo2 size={15} /> Undo last</button></div>
            <div className="mt-4 space-y-3">{commands.slice(0, 8).map((command) => <div key={command.commandId} className="flex items-start justify-between gap-3 border-b border-gray-100 pb-3 last:border-0 dark:border-gray-800"><div className="min-w-0"><p className="truncate text-sm font-semibold text-gray-900 dark:text-white">{command.commandType} <span className="font-normal text-gray-500">· {command.sensorMac}</span></p><p className="mt-1 text-xs text-gray-500">{command.status || 'Recorded'} · {formatDate(command.createdAt)}</p></div><Command size={16} className="shrink-0 text-gray-400" /></div>)}{commands.length === 0 && <p className="text-sm text-gray-500">No commands recorded yet.</p>}</div>
          </section>
        </div>
      </div>

      <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm dark:border-gray-800 dark:bg-gray-900">
        <div className="flex flex-wrap items-center justify-between gap-2"><div><h2 className="text-lg font-bold text-gray-900 dark:text-white">Telemetry timeline</h2><p className="mt-1 text-sm text-gray-500 dark:text-gray-400">Timestamp-sorted log. Critical packets are processed before routine packets.</p></div><p className="text-xs text-gray-500">Updated {formatDate(lastUpdated)}</p></div>
        <div className="mt-4 overflow-x-auto"><table className="w-full min-w-[650px] text-left text-sm"><thead><tr className="border-b border-gray-200 text-xs uppercase tracking-wide text-gray-500 dark:border-gray-800"><th className="py-3 pr-4">Timestamp</th><th className="py-3 pr-4">Device</th><th className="py-3 pr-4">Metric</th><th className="py-3 pr-4">Value</th><th className="py-3">Priority</th></tr></thead><tbody>{(stream.timeline || []).slice(0, 20).map((item, index) => <tr key={`${item.sensorMac}-${item.timestamp}-${index}`} className="border-b border-gray-100 last:border-0 dark:border-gray-800"><td className="py-3 pr-4 text-gray-500">{formatDate(item.timestamp)}</td><td className="py-3 pr-4 font-medium text-gray-900 dark:text-white">{item.sensorMac}</td><td className="py-3 pr-4">{item.dataType}</td><td className="py-3 pr-4">{item.dataValue}</td><td className="py-3">{item.isCritical ? <span className="rounded-full bg-rose-100 px-2 py-1 text-xs font-semibold text-rose-700 dark:bg-rose-950 dark:text-rose-300">Critical</span> : <span className="rounded-full bg-gray-100 px-2 py-1 text-xs text-gray-600 dark:bg-gray-800 dark:text-gray-300">Routine</span>}</td></tr>)}</tbody></table>{!(stream.timeline || []).length && <p className="py-8 text-center text-sm text-gray-500">No in-memory packets yet. Use Test alert to enqueue a demonstration packet.</p>}</div>
      </section>
      <p className="flex items-center gap-2 text-xs text-gray-500"><Clock3 size={14} /> {metrics.deviceRegistryCount ?? sensors.length} devices registered. Output actions are persisted simulated state changes only; they do not control physical hardware without a connected device integration.</p>
    </div>
  );
}

export default CommandStreamPage;
