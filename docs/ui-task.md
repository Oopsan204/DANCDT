# Current UI task

- Goal: Extend the SVG to DXF converter operator view with a destination path editor and CAD geometry preview.
- Screens: `SvgToDxfView` only.
- Existing bindings/commands to use: `SvgInputPath`, `SvgOutputPath`, `SvgConversionStatus`, `BrowseSvgCommand`, `BrowseSvgOutputCommand`, `ConvertSvgToDxfCommand`, `SvgDxfPreviewGeometry`, `HasSvgDxfPreview`, `SvgDxfPreviewBoundsText`, `SvgDxfPreviewPathCount`, `SvgDxfPreviewVertexCount`, `LoadConvertedDxfToRunCommand`.
- Files Antigravity may edit: `src/DACDT_2026.App/Views/SvgToDxfView.xaml` only.
- Visual constraints: Use existing styles; heading `SVG to DXF Converter`; editable source and output path displays with `SELECT SVG` and `BROWSE...` buttons; `CONVERT AND SAVE DXF` button; a dark CAD preview viewport showing `SvgDxfPreviewGeometry` with an `OPEN IN CAD RUN VIEW` button; concise support/limitations note; no navigation controls or code-behind.
- Acceptance checks: XAML is valid WPF, all bindings exactly match the supplied contract, and no logic files are edited.

Antigravity must follow `docs/ui-contract.md` and must not edit forbidden logic paths.

