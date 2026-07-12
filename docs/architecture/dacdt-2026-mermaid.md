# DACDT 2026 Mermaid Architecture

This diagram summarizes the current project layout and runtime data flow.

Rendered SVG: [dacdt-2026-architecture.svg](dacdt-2026-architecture.svg)

```mermaid
flowchart LR
  operator["Operator<br/>WPF controls"]
  files["DXF / NC / G-code<br/>input files"]
  camera["USB camera<br/>local device"]

  subgraph app["src/DACDT_2026.App - WPF control app"]
    shell["Program + Form1<br/>main UI shell"]
    views["Views<br/>Dashboard / DXF / Monitor / Settings"]
    cad["CAD and G-code services<br/>CadDocumentService / GcodeDocumentService / cleaners"]
    qd75writer["QD75 motion data<br/>QD75BufferWriter / QD75RingBufferRunner"]
    plcfast["PLC control fast lane<br/>Form1.PlcControl / PLCCommunication"]
    state["State publisher<br/>WpfUiState / Form1.StatePublisher"]
    cammodule["Camera module<br/>Form1.Camera / WebRtcBridgeClient"]
    mqttsvc["MQTT service<br/>MqttPublishService"]
  end

  subgraph machine["Machine side"]
    plc["Mitsubishi Q PLC<br/>MX Component"]
    qd75["QD75 positioning module<br/>buffer memory U0\\G"]
    gantry["Gantry / laser machine<br/>axes, limits, laser"]
  end

  subgraph web["Network and browser"]
    broker["MQTT broker<br/>state, command, WebRTC signaling"]
    dashboard["docs/index.html<br/>web dashboard"]
    webrtcsvc["src/WebRtcCameraService<br/>x64 WebRTC bridge"]
    viewer["Browser WebRTC viewer"]
  end

  operator --> shell
  shell --> views
  files --> cad
  cad --> qd75writer
  qd75writer --> qd75
  shell --> plcfast
  plcfast <--> plc
  plc <--> qd75
  qd75 --> gantry
  gantry --> plc

  plcfast --> state
  cad --> state
  cammodule --> state
  state --> mqttsvc
  mqttsvc <--> broker
  broker <--> dashboard

  camera --> cammodule
  cammodule --> webrtcsvc
  webrtcsvc <--> broker
  webrtcsvc --> viewer
  broker <--> viewer

  shell -. exit safety: stop / home / clear buffer .-> plcfast

  classDef app fill:#e7f0ff,stroke:#3b6ea8,color:#102033;
  classDef machine fill:#fff0e6,stroke:#c46a2d,color:#2c1608;
  classDef network fill:#e9f8f2,stroke:#2f8a65,color:#0c2b20;
  classDef input fill:#f7f2ff,stroke:#7a5eb5,color:#241840;

  class shell,views,cad,qd75writer,plcfast,state,cammodule,mqttsvc app;
  class plc,qd75,gantry machine;
  class broker,dashboard,webrtcsvc,viewer network;
  class operator,files,camera input;
```

## Main Flow

The WPF app is the center of the system. Operators work through `Form1` and the WPF views. DXF, NC, and G-code files are parsed and cleaned, then converted into QD75 positioning data. The PLC/QD75 path is the realtime machine path; MQTT, the web dashboard, logs, and WebRTC camera streaming sit around it as monitoring and remote-control channels.

## Source Notes

- Main WPF app: `src/DACDT_2026.App`
- Background WebRTC bridge: `src/WebRtcCameraService`
- Web dashboard copied into app output: `docs/index.html`
- Existing interactive SVG diagram: `docs/architecture/dacdt-2026-architecture.html`
