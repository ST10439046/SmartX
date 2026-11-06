import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Activity, AlertTriangle, GitBranch, Network, RefreshCw, Route, Router,
  ShieldCheck, Wifi, WifiOff,
} from 'lucide-react';
import { meshApi } from '../services/apiClient';

const statusStyles = {
  Online: { dot: 'bg-emerald-500', text: 'text-emerald-600 dark:text-emerald-400' },
  Offline: { dot: 'bg-rose-500', text: 'text-rose-600 dark:text-rose-400' },
  Unknown: { dot: 'bg-amber-500', text: 'text-amber-600 dark:text-amber-400' },
  Disabled: { dot: 'bg-gray-400', text: 'text-gray-500' },
};

function displayName(node) {
  return node.nodeId || node.macaddress;
}

function MeshGraph({ nodes, links, selectedMac, onSelect }) {
  const positions = useMemo(() => {
    const centerX = 350;
    const centerY = 205;
    const radius = Math.min(145, 65 + nodes.length * 13);
    return new Map(nodes.map((node, index) => {
      const angle = (Math.PI * 2 * index) / Math.max(nodes.length, 1) - Math.PI / 2;
      return [node.macaddress, {
        x: centerX + Math.cos(angle) * radius,
        y: centerY + Math.sin(angle) * radius,
      }];
    }));
  }, [nodes]);

  return (
    <div className="w-full overflow-hidden rounded-xl bg-slate-950 p-2">
      <svg viewBox="0 0 700 410" role="img" aria-label="Interactive sensor mesh network graph" className="w-full">
        {links.map((link) => {
          const from = positions.get(link.fromNodeMac);
          const to = positions.get(link.toNodeMac);
          if (!from || !to) return null;
          const quality = Number(link.signalStrength);
          return (
            <g key={link.linkId}>
              <line x1={from.x} y1={from.y} x2={to.x} y2={to.y}
                stroke={quality >= 80 ? '#34d399' : quality >= 60 ? '#fbbf24' : '#fb7185'}
                strokeWidth="3" opacity="0.72" />
              <text x={(from.x + to.x) / 2} y={(from.y + to.y) / 2 - 8}
                fill="#e2e8f0" fontSize="12" textAnchor="middle">{quality}%</text>
            </g>
          );
        })}
        {nodes.map((node) => {
          const point = positions.get(node.macaddress);
          const active = node.macaddress === selectedMac;
          const fill = node.status === 'Online' ? '#059669' : node.status === 'Offline' ? '#e11d48' : '#d97706';
          return (
            <g key={node.macaddress} role="button" tabIndex="0"
              aria-label={`Select ${displayName(node)}`}
              onClick={() => onSelect(node)}
              onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') onSelect(node); }}
              className="cursor-pointer">
              <circle cx={point.x} cy={point.y} r={active ? 29 : 25}
                fill={fill} stroke={active ? '#f8fafc' : '#94a3b8'} strokeWidth={active ? 4 : 2} />
              {node.isGateway && <circle cx={point.x} cy={point.y} r="35" fill="none" stroke="#38bdf8" strokeWidth="2" strokeDasharray="5 4" />}
              <text x={point.x} y={point.y + 4} textAnchor="middle" fill="white" fontSize="12" fontWeight="700">
                {node.nodeId?.slice(-2) || 'IoT'}
              </text>
              <text x={point.x} y={point.y + 46} textAnchor="middle" fill="#e2e8f0" fontSize="12">
                {displayName(node)}
              </text>
              {node.isGateway && <text x={point.x} y={point.y - 43} textAnchor="middle" fill="#7dd3fc" fontSize="11">GATEWAY</text>}
            </g>
          );
        })}
        {nodes.length === 0 && <text x="350" y="205" textAnchor="middle" fill="#cbd5e1">No registered sensors found</text>}
      </svg>
      <div className="flex flex-wrap items-center justify-center gap-4 pb-2 text-xs text-slate-300">
        <span className="flex items-center gap-2"><i className="h-2.5 w-2.5 rounded-full bg-emerald-500" />Online</span>
        <span className="flex items-center gap-2"><i className="h-2.5 w-2.5 rounded-full bg-rose-500" />Offline</span>
        <span className="flex items-center gap-2"><i className="h-2.5 w-2.5 rounded-full bg-amber-500" />Unknown</span>
        <span className="text-sky-300">Dashed ring = gateway</span>
      </div>
    </div>
  );
}

function StatCard({ label, value, icon: Icon, detail }) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-4 shadow-sm dark:border-gray-800 dark:bg-gray-900">
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-500 dark:text-gray-400">{label}</p>
        <Icon size={18} className="text-primary-600 dark:text-primary-400" />
      </div>
      <p className="mt-2 text-2xl font-bold text-gray-900 dark:text-white">{value}</p>
      <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{detail}</p>
    </div>
  );
}

export default function MeshTopologyPage() {
  const [overview, setOverview] = useState(null);
  const [selectedMac, setSelectedMac] = useState('');
  const [gatewayMac, setGatewayMac] = useState('');
  const [algorithm, setAlgorithm] = useState('bfs');
  const [discovery, setDiscovery] = useState(null);
  const [route, setRoute] = useState(null);
  const [mst, setMst] = useState(null);
  const [simulation, setSimulation] = useState(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadOverview = useCallback(async () => {
    setBusy(true);
    setError('');
    try {
      const response = await meshApi.getOverview();
      setOverview(response.data);
      setGatewayMac(response.data.gateway?.macaddress || '');
      setSelectedMac((current) => current || response.data.nodes?.[0]?.macaddress || '');
    } catch (requestError) {
      setError(requestError.response?.data?.message || 'Could not load the mesh network.');
    } finally {
      setBusy(false);
    }
  }, []);

  useEffect(() => { loadOverview(); }, [loadOverview]);

  const selectedNode = overview?.nodes?.find((node) => node.macaddress === selectedMac);
  const gatewayNode = overview?.nodes?.find((node) => node.isGateway);

  const saveGateway = async () => {
    setBusy(true);
    setError('');
    setNotice('');
    try {
      await meshApi.setGateway(gatewayMac);
      setNotice('Gateway selection saved.');
      await loadOverview();
    } catch (requestError) {
      setError(requestError.response?.data?.message || 'Could not save the gateway.');
    } finally {
      setBusy(false);
    }
  };

  const runDiscovery = async () => {
    setBusy(true);
    setError('');
    try {
      const response = await meshApi.discover(algorithm);
      setDiscovery(response.data);
    } catch (requestError) {
      setError(requestError.response?.data?.message || 'Discovery could not run.');
    } finally {
      setBusy(false);
    }
  };

  const findRoute = async () => {
    if (!selectedMac) return;
    setBusy(true);
    setError('');
    try {
      const response = await meshApi.getRoute(selectedMac);
      setRoute(response.data);
    } catch (requestError) {
      setError(requestError.response?.data?.message || 'No route could be calculated.');
      setRoute(null);
    } finally {
      setBusy(false);
    }
  };

  const calculateMst = async () => {
    setBusy(true);
    setError('');
    try {
      const response = await meshApi.getMst();
      setMst(response.data);
    } catch (requestError) {
      setError(requestError.response?.data?.message || 'Could not calculate the spanning forest.');
    } finally {
      setBusy(false);
    }
  };

  const simulateFailure = async () => {
    if (!selectedMac) return;
    setBusy(true);
    setError('');
    try {
      const response = await meshApi.simulateFailure([selectedMac], []);
      setSimulation(response.data);
    } catch (requestError) {
      setError(requestError.response?.data?.message || 'Failure simulation failed.');
    } finally {
      setBusy(false);
    }
  };

  const clearResults = () => {
    setDiscovery(null);
    setRoute(null);
    setMst(null);
    setSimulation(null);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 text-primary-600 dark:text-primary-400">
            <Network size={20} />
            <span className="text-sm font-semibold uppercase tracking-wide">Architectural pillar 4</span>
          </div>
          <h1 className="mt-2 text-3xl font-bold text-gray-900 dark:text-white">Network topology & mesh routing</h1>
          <p className="mt-1 max-w-3xl text-sm text-gray-500 dark:text-gray-400">
            Explore sensor connectivity, select the gateway, discover reachable devices and calculate resilient routes.
          </p>
        </div>
        <button type="button" onClick={loadOverview} disabled={busy}
          className="inline-flex items-center gap-2 rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100 disabled:opacity-50 dark:border-gray-700 dark:text-gray-200 dark:hover:bg-gray-800">
          <RefreshCw size={16} className={busy ? 'animate-spin' : ''} /> Refresh network
        </button>
      </div>

      {error && <div role="alert" className="rounded-lg border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700 dark:border-rose-900 dark:bg-rose-950/40 dark:text-rose-300">{error}</div>}
      {notice && <div role="status" className="rounded-lg border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-300">{notice}</div>}

      <div className="grid grid-cols-2 gap-3 xl:grid-cols-4">
        <StatCard label="Registered nodes" value={overview?.summary?.totalNodes ?? '...'} icon={Router} detail="All registered sensors" />
        <StatCard label="Online" value={overview?.summary?.onlineNodes ?? '...'} icon={Wifi} detail="Telemetry seen within 30 seconds" />
        <StatCard label="Offline" value={overview?.summary?.offlineNodes ?? '...'} icon={WifiOff} detail="No recent telemetry" />
        <StatCard label="Mesh links" value={overview?.summary?.totalLinks ?? '...'} icon={GitBranch} detail={`${overview?.summary?.connectedComponents ?? 0} connected component(s)`} />
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.7fr)_minmax(300px,1fr)]">
        <section className="rounded-xl border border-gray-200 bg-white p-4 shadow-sm dark:border-gray-800 dark:bg-gray-900">
          <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
            <div>
              <h2 className="font-semibold text-gray-900 dark:text-white">Live mesh map</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400">Select a node to inspect it</p>
            </div>
            <span className="rounded-full bg-slate-100 px-3 py-1 text-xs text-slate-700 dark:bg-slate-800 dark:text-slate-200">
              Signal quality: 0 to 100%
            </span>
          </div>
          <MeshGraph nodes={overview?.nodes || []} links={overview?.links || []}
            selectedMac={selectedMac} onSelect={(node) => { setSelectedMac(node.macaddress); clearResults(); }} />
          <p className="mt-3 text-xs text-gray-500 dark:text-gray-400">
            Link labels show simulated signal quality. Routing cost is calculated as 101 minus signal quality. Lower cost is better. This is not a physical dBm reading.
          </p>
        </section>

        <section className="space-y-4 rounded-xl border border-gray-200 bg-white p-4 shadow-sm dark:border-gray-800 dark:bg-gray-900">
          <div>
            <h2 className="font-semibold text-gray-900 dark:text-white">Gateway configuration</h2>
            <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">Exactly one active registered sensor can be selected.</p>
          </div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300" htmlFor="mesh-gateway">Selected gateway</label>
          <select id="mesh-gateway" value={gatewayMac} onChange={(event) => setGatewayMac(event.target.value)}
            className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-950 dark:text-white">
            {(overview?.nodes || []).filter((node) => node.isActive).map((node) => (
              <option key={node.macaddress} value={node.macaddress}>{displayName(node)} · {node.status}</option>
            ))}
          </select>
          <button type="button" onClick={saveGateway} disabled={busy || !gatewayMac}
            className="w-full rounded-lg bg-primary-600 px-4 py-2 text-sm font-semibold text-white hover:bg-primary-700 disabled:opacity-50">
            Save gateway
          </button>
          <div className="rounded-lg bg-gray-50 p-3 text-sm dark:bg-gray-800">
            <p className="flex items-center gap-2 font-medium text-gray-800 dark:text-gray-100"><ShieldCheck size={16} /> Current gateway</p>
            <p className="mt-1 text-gray-600 dark:text-gray-300">{gatewayNode ? displayName(gatewayNode) : 'Not selected'}</p>
            <p className={`mt-1 text-xs ${statusStyles[gatewayNode?.status]?.text || 'text-gray-500'}`}>
              {gatewayNode?.status || 'Awaiting selection'}
            </p>
          </div>
          <div className="border-t border-gray-200 pt-4 dark:border-gray-800">
            <h3 className="font-semibold text-gray-900 dark:text-white">Selected node</h3>
            {selectedNode ? (
              <dl className="mt-2 space-y-2 text-sm">
                <div className="flex justify-between gap-2"><dt className="text-gray-500">Node</dt><dd className="font-medium text-gray-900 dark:text-white">{displayName(selectedNode)}</dd></div>
                <div className="flex justify-between gap-2"><dt className="text-gray-500">MAC</dt><dd className="text-right text-xs text-gray-700 dark:text-gray-300">{selectedNode.macaddress}</dd></div>
                <div className="flex justify-between gap-2"><dt className="text-gray-500">Location</dt><dd className="text-right text-gray-700 dark:text-gray-300">{[selectedNode.room, selectedNode.zone].filter(Boolean).join(' · ') || 'Not set'}</dd></div>
                <div className="flex justify-between gap-2"><dt className="text-gray-500">Status</dt><dd className={statusStyles[selectedNode.status]?.text}>{selectedNode.status}</dd></div>
                <div className="flex justify-between gap-2"><dt className="text-gray-500">Role</dt><dd className="text-gray-700 dark:text-gray-300">{selectedNode.isGateway ? 'Gateway' : 'Mesh node'}</dd></div>
              </dl>
            ) : <p className="mt-2 text-sm text-gray-500">Select a node from the map.</p>}
          </div>
        </section>
      </div>

      <section className="rounded-xl border border-gray-200 bg-white p-4 shadow-sm dark:border-gray-800 dark:bg-gray-900">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="font-semibold text-gray-900 dark:text-white">Network algorithms</h2>
            <p className="text-xs text-gray-500 dark:text-gray-400">Run graph algorithms against the selected gateway and stored topology.</p>
          </div>
          <button type="button" onClick={clearResults} className="text-sm text-gray-500 hover:text-gray-900 dark:hover:text-white">Clear results</button>
        </div>
        <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          <div className="rounded-lg border border-gray-200 p-3 dark:border-gray-800">
            <label htmlFor="mesh-algorithm" className="text-sm font-medium text-gray-700 dark:text-gray-300">Traversal</label>
            <select id="mesh-algorithm" value={algorithm} onChange={(event) => setAlgorithm(event.target.value)}
              className="mt-2 w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-950 dark:text-white">
              <option value="bfs">Breadth-first search (BFS)</option>
              <option value="dfs">Depth-first search (DFS)</option>
            </select>
            <button type="button" onClick={runDiscovery} disabled={busy} className="mt-3 w-full rounded-lg bg-primary-600 px-3 py-2 text-sm font-medium text-white disabled:opacity-50">Run discovery</button>
          </div>
          <div className="rounded-lg border border-gray-200 p-3 dark:border-gray-800">
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300">Shortest route</p>
            <p className="mt-2 text-xs text-gray-500">Find the lowest-cost path from the gateway to the selected node.</p>
            <button type="button" onClick={findRoute} disabled={busy || !selectedMac || selectedMac === gatewayMac}
              className="mt-3 w-full rounded-lg bg-primary-600 px-3 py-2 text-sm font-medium text-white disabled:opacity-50"><Route size={14} className="mr-1 inline" /> Calculate route</button>
          </div>
          <div className="rounded-lg border border-gray-200 p-3 dark:border-gray-800">
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300">Minimum spanning forest</p>
            <p className="mt-2 text-xs text-gray-500">Prim's algorithm connects each component with minimum total cost.</p>
            <button type="button" onClick={calculateMst} disabled={busy} className="mt-3 w-full rounded-lg bg-primary-600 px-3 py-2 text-sm font-medium text-white disabled:opacity-50">Run Prim</button>
          </div>
          <div className="rounded-lg border border-rose-200 p-3 dark:border-rose-900">
            <p className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300"><AlertTriangle size={15} className="text-rose-500" /> Failure simulation</p>
            <p className="mt-2 text-xs text-gray-500">Temporarily remove the selected node and inspect the remaining network.</p>
            <button type="button" onClick={simulateFailure} disabled={busy || !selectedMac} className="mt-3 w-full rounded-lg border border-rose-300 px-3 py-2 text-sm font-medium text-rose-700 hover:bg-rose-50 disabled:opacity-50 dark:border-rose-900 dark:text-rose-300 dark:hover:bg-rose-950/40">Simulate node failure</button>
          </div>
        </div>

        {discovery && <div className="mt-4 rounded-lg bg-gray-50 p-4 dark:bg-gray-800">
          <h3 className="font-semibold text-gray-900 dark:text-white">{discovery.algorithm} discovery order</h3>
          <p className="mt-2 text-sm text-gray-700 dark:text-gray-200">{discovery.visitedNodes.map((mac) => overview?.nodes?.find((node) => node.macaddress === mac)?.nodeId || mac).join(' → ')}</p>
          <p className="mt-1 text-xs text-gray-500">Visited {discovery.visitedCount} reachable node(s), starting from the gateway.</p>
        </div>}
        {route && <div className="mt-4 rounded-lg bg-gray-50 p-4 dark:bg-gray-800">
          <h3 className="font-semibold text-gray-900 dark:text-white">Lowest-cost route</h3>
          <p className="mt-2 break-words text-sm text-gray-700 dark:text-gray-200">{route.path.map((mac) => overview?.nodes?.find((node) => node.macaddress === mac)?.nodeId || mac).join(' → ')}</p>
          <p className="mt-1 text-xs text-gray-500">Total routing cost: {route.totalCost} · Hops: {route.hops}</p>
        </div>}
        {mst && <div className="mt-4 rounded-lg bg-gray-50 p-4 dark:bg-gray-800">
          <h3 className="font-semibold text-gray-900 dark:text-white">Prim's minimum spanning forest</h3>
          <p className="mt-1 text-xs text-gray-500">Total cost: {mst.totalCost} · Edges selected: {mst.edges.length} · Components: {mst.components.length}</p>
          <ul className="mt-3 space-y-1 text-sm text-gray-700 dark:text-gray-200">
            {mst.edges.map((edge, index) => <li key={`${edge.from}-${edge.to}-${index}`}>{edge.from} ↔ {edge.to} · {edge.signalStrength}% quality · cost {edge.routingCost}</li>)}
          </ul>
        </div>}
        {simulation && <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-4 dark:border-amber-900 dark:bg-amber-950/30">
          <h3 className="font-semibold text-amber-900 dark:text-amber-200">Failure simulation result</h3>
          <p className="mt-2 text-sm text-amber-800 dark:text-amber-100">Gateway reachable: {simulation.gatewayReachable ? 'Yes' : 'No'} · Remaining reachable nodes: {simulation.discovery.length}</p>
          <p className="mt-1 text-xs text-amber-700 dark:text-amber-300">{simulation.warning}</p>
          <p className="mt-2 text-xs text-amber-800 dark:text-amber-200">Connected components after failure: {simulation.components.length}</p>
        </div>}
      </section>

      <section className="rounded-xl border border-sky-200 bg-sky-50 p-4 dark:border-sky-900 dark:bg-sky-950/30">
        <div className="flex items-start gap-3">
          <Activity size={19} className="mt-0.5 text-sky-700 dark:text-sky-300" />
          <div>
            <h2 className="font-semibold text-sky-900 dark:text-sky-200">How to read this demo</h2>
            <p className="mt-1 text-sm text-sky-800 dark:text-sky-100">
              The selected gateway is the root for BFS, DFS and Dijkstra. Offline means no telemetry has arrived for more than 30 seconds. A failed node in the simulation is removed only from a temporary graph, never from the database.
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}
