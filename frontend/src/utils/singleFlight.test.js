import test from 'node:test';
import assert from 'node:assert/strict';
import { createSingleFlight } from './singleFlight.js';

test('coalesces overlapping refreshes into one request', async () => {
  const runSingleFlight = createSingleFlight();
  let calls = 0;
  let release;
  const task = () => {
    calls += 1;
    return new Promise((resolve) => { release = resolve; });
  };

  const first = runSingleFlight(task);
  const second = runSingleFlight(task);
  await Promise.resolve();
  assert.equal(calls, 1);
  release('dashboard');
  assert.equal(await first, 'dashboard');
  assert.equal(await second, 'dashboard');
});

test('releases the gate after a failed request so a later refresh can retry', async () => {
  const runSingleFlight = createSingleFlight();
  await assert.rejects(runSingleFlight(async () => { throw new Error('network failure'); }), /network failure/);
  assert.equal(await runSingleFlight(async () => 'recovered'), 'recovered');
});

test('does not block the event loop while a request is pending', async () => {
  const runSingleFlight = createSingleFlight();
  let release;
  const pending = runSingleFlight(() => new Promise((resolve) => { release = resolve; }));
  let responsive = false;
  await Promise.resolve().then(() => { responsive = true; });
  assert.equal(responsive, true);
  release('done');
  assert.equal(await pending, 'done');
});
