# WmiQueryTool

## 📖 Project Overview
`WmiQueryTool` is a robust WPF diagnostic utility demonstrating how to interact with **WMI (Windows Management Instrumentation)** to query, analyze, and manipulate Windows system state telemetry. Designed with `.NET 10`, this application showcases production-ready patterns for handling large dataset retrieval, featuring dynamic keyword filtering, secure multi-format data export, and high-performance **client-side data pagination/statistics**.

- **Target Framework**: `.NET 10.0`
- **Core Technology**: `System.Management` (WMI Consumer API)
- **Key Focus**: Windows Diagnostics, Client-Side Data Paging, Telemetry Filtering, and Forensic CSV Export.

---

## ✨ Key Features
- **Dynamic WMI Consumption**: Supports custom WQL (WMI Query Language) execution along with pre-configured operational templates (CPU, Memory, Physical Storage, etc.).
- **Query Persistent History**: Caches the last 10 execution traces locally for rapid workflow reuse.
- **Advanced Telemetry Visualization**: Renders complex object properties into a responsive DataGrid with runtime filtering capabilities.
- **High-Performance Pagination & Metrics**:
  - Implements custom memory-efficient pagination (defaulting to 10 records per view) to mitigate UI overhead.
  - Dynamically calculates runtime metrics: `"Page X of Y | Total Properties: N"`.
  - Provides seamless, lag-free structural switching via Next/Previous control logic.
- **Forensic CSV Export**: Serializes structural WMI query results into standardized CSV format for post-incident reporting and SysOps analysis.

---

## 🤖 Headless Mode: AI-Driven Cross-Process Integration
Beyond normal GUI interaction, this application also supports a **headless** invocation mode. When launched with a command-line argument matching one of the predefined categories (`OS`, `CPU`, `GPU`, `Memory`, `Disk`), the program skips the normal UI display flow, matches the corresponding WQL query template, executes the query, and prints the results to standard output (stdout) before terminating immediately — The normal UI is skipped; a window may briefly flash when the program is launched from a console.

This mechanism is designed to be invoked by an external process. The actual caller is a sibling project, **LocalAIApp**, a local SLM (Small Language Model) inference gateway. When it detects a custom protocol tag emitted by the AI model's output, it invokes this program via `Process.Start` with `RedirectStandardOutput`, and reads back the returned query results to populate the response shown to the user. Because the caller actively reads the redirected pipe, all output logic in this program has been tuned to stay clean and free of debug messages, and it calls `Console.Out.Flush()` before exiting to ensure buffered content is always written out before the process is terminated by WPF's shutdown flow.

---

## 🚀 Usage & Workflow
1. **Initialization**: Launch the application with **Administrator privileges** (required for low-level or restricted WMI namespaces).
2. **Telemetry Selection**: 
   - Choose a quick-access class from the pre-configured templates (e.g., `Win32_Processor`, `Win32_OperatingSystem`).
   - Alternatively, write specialized custom WQL statements in the query interface.
3. **Execution**: Click **"Run Query"** to dispatch the asynchronous infrastructure request.
4. **Data Browsing & Filtering**:
   - Apply real-time keywords in the filter mechanism to dynamically isolate critical system indicators.
   - Traverse voluminous properties smoothly using the structured pagination dashboard.
5. **Reporting**: Click **"Export to CSV"** to archive the telemetry output into a file.

---

## ⚙️ Technical Stack
- **Framework & Engine**: .NET 10 / WPF desktop architecture
- **Core Namespace**: `System.Management` (Native WMI Management API Bridge)
- **UI & Layout**: DataGrid, ComboBox, Layered Controls, and StatusBar Data Binding
- **Pagination Core**: Optimized `List<T>.GetRange()` algorithmic slicing driven by stateful `currentPage` and `pageSize` properties
- **Serialization Engine**: Structured `StringBuilder` implementation engineered for reliable, memory-efficient data writing

---

## 📌 Roadmap & Future Enhancements
- [ ] **Data Visualization Dashboards**: Render hardware capacity telemetry (CPU usage, memory allocation) via graphical charts (Bar/Pie charts).
- [ ] **Namespace Expansion**: Integrate advanced WMI Providers including Network Adapter Configuration and Storage Volume Metrics.
- [ ] **Advanced Data Grid Virtualization**: Implement native UI Virtualization to seamlessly handle continuous multi-thousand-row data pipelines.
- [ ] **Resilient Exception Handling**: Embed fine-grained WMI error trapping (e.g., handling Access Denied or Invalid Class exceptions gracefully).