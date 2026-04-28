# TODO

## Statistics and Scenario Settings

### Vessel-Level Statistics

- Collect vessel loading and utilization statistics, including:
  - TEU load after each loading/discharging activity;
  - average vessel load;
  - maximum vessel load;
  - vessel capacity utilization over time;
  - utilization by service route or vessel class, if applicable.

### Berth- and Port-Level Statistics

- Collect utilization statistics for each berth, including:
  - busy time;
  - idle time;
  - berth utilization ratio.

- Collect port-level queue and inventory statistics, including:
  - number of vessels waiting at each port;
  - number of shipments waiting at each port;
  - TEU volume waiting at each port;
  - loading and discharging volume by port;
  - loading and discharging volume by time period, if needed.

- Collect congestion-related statistics, including:
  - vessel waiting time by port;
  - shipment waiting time by port;
  - peak vessel queue length;
  - peak shipment inventory at port.

### Scenario Settings

- Tune vessel-related settings, including:
  - vessel capacity;
  - vessel class and sailing speed;
  - number of vessels assigned to each service route;
  - service route frequency and initial deployment pattern.

- Tune demand-related settings, including:
  - shipment demand volume;
  - demand OD pairs;
  - shipment TEU size distribution;
  - shipment arrival pattern.

- Tune port and berth settings, including:
  - number of berths at each port;
  - berth capacity assumptions;
  - berthing and unberthing time assumptions;
  - cargo handling rate or handling time assumptions.

- Tune routing and transshipment assumptions, including:
  - service route design;
  - booking logic;
  - routing choices for direct and transshipment shipments;
  - treatment of repeated ports within a single service route.

### Implementation Notes

- Statistics should be collected at the activity level where possible, so that waiting, sailing, service, loading, and discharging durations can be traced consistently.

- Shipment statistics should be linked to the associated demand OD pair, so that performance can be compared across different origin-destination pairs.

- Vessel statistics should distinguish between sailing, waiting, service, and idle states to support vessel utilization analysis.

- Port-level statistics should combine berth usage, vessel queues, and shipment inventories to support congestion analysis.

- Debug logs and statistics timestamps should use the standardized full simulation clock format.