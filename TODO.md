# TODO

## Naming and Structure

- Rename `PartialServiceRoute` to `Segment` where appropriate.
- Review related method names, including:
  - `GetNextPartialServiceRoute()`
  - `CurrentPartialServiceRoute`
  - `PartialServiceRoutes`

## Comments and Exceptions

- Refine code comments to align with the segment-based logic.
- Improve exception messages for routing, booking, loading, and discharging logic.
- Standardize timestamp format in debug logs and exception messages.

## Logic and Testing

- Continue debugging loading and discharging logic under repeated-port scenarios.
- Add tests for:
  - Initial vessels with `CurrentPartialServiceRoute == null`
  - Loading based on next segment
  - Discharging based on current segment
  - Repeated ports in the same service route
  - Capacity constraints during loading

## Statistics and Scenario Settings

- Collect simulation statistics for:
  - vessel utilization
  - shipment waiting time
  - berth utilization
  - loading/discharging volume
  - unmet demand or delayed shipments

- Tune scenario settings, including:
  - vessel capacity
  - service route frequency
  - shipment demand volume
  - berth capacity
  - loading/discharging assumptions