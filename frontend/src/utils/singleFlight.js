// Single-flight helper: coalesces overlapping refreshes into one in-flight request.
export function createSingleFlight() {
  let inFlight = null;

  return function run(task) {
    if (inFlight) return inFlight;
    inFlight = Promise.resolve()
      .then(task)
      .finally(() => {
        inFlight = null;
      });
    return inFlight;
  };
}
