# Changelog

## 0.2.0 - 2026-07-12

### Added

- Single-instance activation.
- Live settings panel and persistent compact mode.
- Time-based charts with statistics and thresholds.
- Sustained CPU/GPU temperature alerts.
- Network, storage, fan, and VRAM metrics.
- Stable multi-GPU selection and exported diagnostics.
- Dark, graphite, and light themes.
- Non-elevated UI with a privileged sensor helper over named pipes.
- Safe autostart behavior and Inno Setup packaging.
- Optional RTSS FPS and frametime reader.
- Bounded diagnostic logging.
- Automated tests, CI, Dependabot, and release workflow.

### Fixed

- Physical memory no longer includes pagefile values.
- Settings persist while window position is unset.
- Sensor events no longer access WPF objects from a worker thread.
