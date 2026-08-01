# Synthetic Close Icon and MA Marker Design

## Goal

Make active synthetic baskets directly closable from the bottom Synthetic Baskets tab and remove moving-average hover dots that interfere with chart drawings.

## Design

- Add an Actions column to the Synthetic Baskets table.
- Render one icon-only close button for each active basket. The button has an accessible label and tooltip naming the basket.
- Clicking the icon stops row-selection propagation and opens the existing close confirmation. It never sends a close mutation directly.
- The existing demo-session, busy-state, acknowledgement, and one-shot confirmation safeguards remain authoritative.
- Set `crosshairMarkerVisible: false` on MA20, MA50, and MA200. Moving-average lines remain visible while candle crosshair and drawing tools remain unchanged.

## Verification

- Runtime tests verify the icon is present, its first click only opens confirmation, and confirmation remains required before a host close message is posted.
- Runtime tests verify all three moving-average series disable crosshair markers.
- The full desktop test suite and self-contained Windows publish must pass.
