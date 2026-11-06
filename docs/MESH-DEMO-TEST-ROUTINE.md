# Smart-X mesh network demo and test routine

This guide tests the mesh implementation against the deterministic sample data seeded by the API at startup.

## 1. Start with a clean, predictable demo

Open **Windows Command Prompt** in the repository folder.

If you have not configured Docker before:

```cmd
copy .env.example .env
notepad .env
```

Set `MSSQL_SA_PASSWORD` to a strong SQL Server password, save the file, then start the stack:

```cmd
docker compose up --build -d
docker compose ps
```

Wait until the SQL Server, database-init, backend and frontend services have started. Open:

- Frontend: http://localhost:8088
- Backend API: http://localhost:5231

The database volume is persistent. The seed routine inserts missing sample sensors and links without wiping operator data. If you specifically need to reset the database to a completely fresh demo, be aware that the following command permanently deletes the SQL Server data volume:

```cmd
docker compose down -v
docker compose up --build -d
```

Only use `down -v` when you are happy to erase the local Smart-X Docker database.

## 2. Sample network you should see

The seed routine creates six registered sensors. ENV-01 is the initial gateway. Five sensors form the connected mesh; ENV-06 is intentionally isolated.

| Node | MAC address | Role | Expected topology |
|---|---|---|---|
| ENV-01 | `00:1A:2B:3C:4D:5E` | Environmental | Gateway |
| PWR-02 | `00:1A:2B:3C:4D:5F` | Power | Connected |
| ACT-03 | `00:1A:2B:3C:4D:60` | Actuator | Connected |
| ENV-04 | `00:1A:2B:3C:4D:61` | Environmental | Connected |
| PWR-05 | `00:1A:2B:3C:4D:62` | Power | Connected |
| ENV-06 | `00:1A:2B:3C:4D:63` | Environmental | Isolated test node |

Seeded links:

| Link | Signal quality | Routing cost |
|---|---:|---:|
| ENV-01 ↔ PWR-02 | 90% | 11 |
| ENV-01 ↔ ACT-03 | 72% | 29 |
| PWR-02 ↔ ENV-04 | 84% | 17 |
| ACT-03 ↔ ENV-04 | 63% | 38 |
| ACT-03 ↔ PWR-05 | 95% | 6 |
| ENV-04 ↔ PWR-05 | 78% | 23 |

**Signal quality** is a simulated percentage from 0 to 100, not a radio measurement in dBm. The routing cost formula is `101 - signal quality`; lower cost is better.

The initial dashboard should report:

- 6 registered nodes
- 6 mesh links
- 2 connected components
- 1 selected gateway (ENV-01)
- After the simulator has sent fresh readings, the six seeded sensors should show Online

The offline rule is based on the latest telemetry timestamp: no reading for more than 30 seconds means Offline. A disconnected node can still be Online because connectivity and telemetry freshness are separate concepts.

## 3. Dashboard walkthrough

1. Open http://localhost:8088.
2. Select **Network Topology and Mesh Routing** in the left navigation.
3. Wait for the mesh overview to load. Click each node in the graph to inspect its location, MAC address, status and gateway role.
4. Confirm ENV-01 has the gateway ring and the ENV-06 node has no connecting lines.
5. Confirm link labels show the seeded signal quality percentages.

If the initial seeded sensors show Offline, wait up to 15 seconds for the telemetry simulator to publish fresh readings, then select **Refresh network**.

## 4. Gateway selection test

1. In Gateway configuration, select PWR-02.
2. Select **Save gateway**.
3. Expected: a success notice appears, PWR-02 receives the gateway ring, and ENV-01 loses it.
4. Refresh the page. PWR-02 should remain the selected gateway.
5. Select ENV-01 again and save it to restore the default for the remaining tests.

The database filtered unique index should prevent more than one sensor from being selected as gateway. The API rejects unknown and disabled sensors. An offline-but-enabled gateway remains selected; the system does not silently switch gateways.

You can also verify the gateway through CMD:

```cmd
curl.exe http://localhost:5231/api/mesh/overview
```

Inspect the `gateway` object and the `isGateway` values in `nodes`. Exactly one node should have `isGateway: true`.

## 5. BFS and DFS discovery tests

Restore ENV-01 as gateway first.

In the dashboard, choose **Breadth-first search (BFS)** and select **Run discovery**.

Expected BFS order:

```text
ENV-01 → PWR-02 → ACT-03 → ENV-04 → PWR-05
```

ENV-06 must not appear because it is disconnected from the gateway.

Now choose **Depth-first search (DFS)** and run discovery.

Expected DFS order:

```text
ENV-01 → PWR-02 → ENV-04 → ACT-03 → PWR-05
```

The order is deterministic because neighbours are visited in sorted MAC-address order. Both algorithms should visit five nodes, exactly once each.

API equivalents:

```cmd
curl.exe "http://localhost:5231/api/mesh/discovery?algorithm=bfs"
curl.exe "http://localhost:5231/api/mesh/discovery?algorithm=dfs"
```

## 6. Dijkstra shortest-route test

1. Click PWR-05 in the graph.
2. Under Shortest route, select **Calculate route**.

Expected result:

```text
ENV-01 → ACT-03 → PWR-05
Total routing cost: 35
Hops: 2
```

Why: ENV-01 to ACT-03 costs 29, and ACT-03 to PWR-05 costs 6. Total = 35. The alternative through PWR-02 and ENV-04 costs 51.

API equivalent:

```cmd
curl.exe "http://localhost:5231/api/mesh/routes?destination=00:1A:2B:3C:4D:62"
```

Now click ENV-06 and calculate its route. Expected: the API reports that no route exists (HTTP 404), because ENV-06 is intentionally isolated.

## 7. Prim minimum-spanning-forest test

Select **Run Prim**.

Expected:

- 4 selected links for the five-node connected component
- 2 components in the overall network (the five-node mesh and isolated ENV-06)
- Total routing cost: 57

One valid minimum spanning tree for the connected component is:

| Selected link | Cost |
|---|---:|
| ACT-03 ↔ PWR-05 | 6 |
| ENV-01 ↔ PWR-02 | 11 |
| PWR-02 ↔ ENV-04 | 17 |
| ENV-04 ↔ PWR-05 | 23 |
| **Total** | **57** |

API equivalent:

```cmd
curl.exe http://localhost:5231/api/mesh/mst
```

The endpoint returns a minimum spanning **forest**, not a misleading single tree, because the seeded network is disconnected.

## 8. Failure simulation test

1. Click ENV-04.
2. Select **Simulate node failure**.

Expected:

- The simulation reports that the gateway is still reachable.
- Four nodes remain reachable from ENV-01: ENV-01, PWR-02, ACT-03 and PWR-05.
- ENV-06 remains isolated.
- The component count becomes 2.
- A warning explains that the simulation did not modify stored sensors, telemetry or links.

API equivalent:

```cmd
curl.exe -X POST http://localhost:5231/api/mesh/simulations/failures -H "Content-Type: application/json" -d "{\"failedSensorMacs\":[\"00:1A:2B:3C:4D:61\"],\"failedLinkIds\":[]}"
```

After the simulation, refresh the real network. ENV-04 must still exist and its stored links must still be present. Failure simulation only changes a temporary in-memory graph.

## 9. AVL historical telemetry test

1. In Historical telemetry range, choose a range covering the last 24 hours.
2. Select ENV-01 in the graph first if you want to filter to one sensor.
3. Select **Query AVL range**.

Expected:

- The response identifies the index as **AVL timestamp index**.
- Seeded ENV-01 data should return at least four readings, plus any readings added by the simulator.
- Results appear in ascending timestamp order.
- Duplicate timestamps, if present, are retained rather than overwritten.

The API accepts a maximum range of 31 days and returns at most 10,000 records per request.

## 10. Direct API smoke checks

Run these in CMD after Docker is ready:

```cmd
curl.exe http://localhost:5231/api/mesh/overview
curl.exe "http://localhost:5231/api/mesh/discovery?algorithm=bfs"
curl.exe "http://localhost:5231/api/mesh/discovery?algorithm=dfs"
curl.exe "http://localhost:5231/api/mesh/routes?destination=00:1A:2B:3C:4D:62"
curl.exe http://localhost:5231/api/mesh/mst
```

A successful response should be JSON. A route request for isolated ENV-06 should return HTTP 404 with a no-route message. A discovery request before a gateway is selected should return HTTP 409.

## 11. Automated checks

From the repository root:

```cmd
dotnet test tests\SmartIotApi.Tests\SmartIotApi.Tests.csproj
cd frontend
npm test
npm run lint
npm run build
cd ..
docker compose config
docker compose ps
```

The backend unit tests cover traversal, shortest paths, disconnected spanning forests, invalid signal-quality values and AVL range ordering. Run the full suite after pulling this branch; do not treat the manual dashboard walkthrough as a replacement for automated tests.

## 12. If something does not look right

Collect the service logs:

```cmd
docker compose logs --tail=150 backend
docker compose logs --tail=100 frontend
docker compose logs --tail=100 sqlserver
```

For startup/database problems, include the backend and SQL Server logs. For a page/API problem, include the browser console error and the failing request URL/status. Do not run `docker compose down -v` unless you intentionally want to reset the database volume.
