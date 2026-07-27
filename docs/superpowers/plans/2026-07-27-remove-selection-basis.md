# Remove Selection Basis Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the permanent Selection Basis panel so useful formula, order, and component content receives the reclaimed rail space.

**Architecture:** Keep the existing payload contract unchanged for compatibility. Remove only the terminal HTML presentation block and its DOM update, then enforce absence through the existing static terminal contract test.

**Tech Stack:** .NET 8, WPF WebView2, HTML/CSS/JavaScript, custom console test suite

## Global Constraints

- Do not add a replacement card, label, or placeholder.
- Preserve synthetic formula, order preview, and component rendering.
- Keep the payload field for compatibility.

---

### Task 1: Reclaim the terminal rail space

**Files:**
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`

**Interfaces:**
- Consumes: `SyntheticTerminalPayload.SelectionBasis`, retained without UI rendering.
- Produces: a side rail containing formula, order preview, and components without `#selection-basis`.

- [ ] **Step 1: Write the failing contract assertion**

Add assertions that reject `id="selection-basis"`, the `Selection Basis` title, and `getElementById('selection-basis')` while retaining existing formula/order/component requirements.

- [ ] **Step 2: Run the suite and verify failure**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -c Release`

Expected: failure reporting that the permanent Selection Basis panel still exists.

- [ ] **Step 3: Remove the panel and renderer**

Delete the Selection Basis `<section class="panel">` and the corresponding JavaScript assignment. Leave adjacent sections unchanged so normal document flow reclaims the height.

- [ ] **Step 4: Run verification**

Run the complete desktop tests and `dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release --no-restore -warnaserror`.

Expected: tests pass and build completes with zero warnings and errors.

- [ ] **Step 5: Publish and visually verify**

Publish self-contained `win-x64` output to `desktop/publish/cap.com-terminal-v4-complete`, launch `CAPETF.exe`, and confirm the side rail no longer renders Selection Basis.

- [ ] **Step 6: Commit**

```powershell
git add desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs desktop/CAPETF.Desktop/Assets/synthetic-terminal.html
git commit -m "fix: reclaim terminal selection basis space"
```
