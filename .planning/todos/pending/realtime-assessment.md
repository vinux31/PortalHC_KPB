---
created: 2026-03-09
source: phase-133-checkpoint
priority: medium
---

# Real-time Assessment System

User wants the assessment system to be real-time throughout the exam lifecycle:
- When HC presses "Reset" on monitoring page, the worker's exam page should update immediately (currently requires page reload)
- All assessment interactions from exam start to submit should be real-time for both HC/Admin and Worker sides
- Applies to: monitoring status updates, reset actions, force close, exam progress

Implementation would likely require SignalR or similar WebSocket-based solution.
