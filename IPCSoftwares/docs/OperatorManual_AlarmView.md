# AOI System Operator Manual (Alarm View)

> **Project:** IPCSoftware.app  
> **Scope:** Operator-facing steps to monitor, acknowledge, and reset alarms via Alarm View.  
> **Audience:** Line Operators / Supervisors

---

## 1. Purpose
- Monitor active alarms in real time.
- Acknowledge alarms (per policy) and perform global reset when authorized.
- View alarm details such as number, severity, message, timestamps.

## 2. Screen Overview
- **Header:** "Active Alarms" title and `RESET SYSTEM` button.
- **Grid:** Lists active alarms with Alarm No, Severity, Message, Raised At, Reset At.
- **Severity badge:** High = red, Medium = amber.

> _Screenshot placeholder_: **Alarm View Overview**  
> (Insert full view showing header, reset button, and alarm grid.)

## 3. Reading the Grid
- **Alarm No:** Unique alarm identifier (from PLC/ system). 
- **Severity:** High/Medium (color-coded). 
- **Message:** Alarm description text. 
- **Raised At:** Timestamp when the alarm was raised. 
- **Reset At:** Timestamp when the alarm was reset (blank if not reset). 

> _Screenshot placeholder_: **Alarm Grid Columns**  
> (Highlight each column header.)

## 4. Acknowledge & Reset
- **Row Acknowledge:** (if enabled) Acknowledge individual alarms. In this screen the per-row acknowledge is currently disabled/commented; acknowledgments may be automatic or handled elsewhere.
- **Global Reset:** Click `RESET SYSTEM` to issue a global reset pulse. This updates reset time for active alarms and sends a PLC tag write. Use only if authorized.

> _Screenshot placeholder_: **Global Reset Button**  
> (Highlight RESET SYSTEM control.)

## 5. Typical Operator Flow
1) Open `Alarm View` from sidebar (Dashboard ? Alarm View). 
2) Review active alarms in the grid. 
3) If instructed, perform local troubleshooting per SOP. 
4) If allowed, click `RESET SYSTEM` after resolving root cause. 
5) Verify alarms clear from the grid. If alarms reappear, escalate to Maintenance.

## 6. Troubleshooting
- **Alarms not clearing:** Ensure the physical condition is resolved; if still present, contact Maintenance. 
- **RESET SYSTEM not working:** Check PLC connectivity; if still failing, escalate. 
- **Missing timestamps:** If `Reset At` stays blank, alarm may not have been reset or acknowledgment is pending.

## 7. Safety & Policy
- Only perform `RESET SYSTEM` when the machine is safe and local SOP permits. 
- Do not ignore High severity alarms; always investigate and escalate per policy. 

## 8. Escalation
- **First:** Shift Supervisor / Maintenance. 
- **Provide:** Alarm No, Severity, Message, time observed, actions taken.

---

_End of Alarm View section. Add this to the full Operator Manual and replace placeholders with actual screenshots before distribution._
