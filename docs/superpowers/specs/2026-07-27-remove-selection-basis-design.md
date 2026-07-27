# Remove Selection Basis Panel

## Decision

Remove the permanent **Selection Basis** heading and explanatory paragraph from the terminal ticket rail.

## Layout

The legs, formula, sizing, and order-preview content will flow upward into the reclaimed space. No replacement card, label, or placeholder will be added.

## Data

The selection-basis value may remain in the host payload for compatibility, but the chart UI will not render it.

## Verification

Update the terminal HTML contract test to reject the removed panel and run the complete desktop test suite and Release build.
