#!/usr/bin/env python3
"""
MQTT Simulator - Bắn dữ liệu giả lên HiveMQ để test Web UI
Topic: DACDT/machine/state

Cách dùng:
  1. pip install paho-mqtt
  2. Sửa USERNAME và PASSWORD bên dưới cho đúng tài khoản HiveMQ
  3. python mqtt_simulator.py
"""

import json
import time
import random
import math
import ssl
import paho.mqtt.client as mqtt

# ====== CẤU HÌNH ======
BROKER = "beb7179d08fa43f79d440a9be9b95f24.s1.eu.hivemq.cloud"
PORT = 8883
USERNAME = "DACDT2026"        # <-- Sửa lại đúng username HiveMQ của bạn
PASSWORD = "trungaN123@"    # <-- Sửa lại đúng password HiveMQ của bạn
TOPIC = "DACDT/machine/state"
INTERVAL = 1.0            # Gửi mỗi 1 giây
# =======================

# Trạng thái giả lập cho 4 trục
sim_axes = [
    {"pos": 0.0, "speed": 0.0, "target": 100.0, "dir": 1},
    {"pos": 0.0, "speed": 0.0, "target": 80.0, "dir": 1},
    {"pos": 0.0, "speed": 0.0, "target": 50.0, "dir": 1},
    {"pos": 0.0, "speed": 0.0, "target": 30.0, "dir": 1},
]

status_list = ["Stopped", "Running", "Positioning", "JOG+", "JOG-", "Home Return"]
integrity_states = ["OK", "WARNING", "ERROR"]
tick = 0


def update_simulation():
    """Cập nhật vị trí giả lập cho các trục"""
    global tick
    tick += 1

    for i, ax in enumerate(sim_axes):
        # Tốc độ dao động
        ax["speed"] = round(random.uniform(5.0, 50.0), 3)

        # Di chuyển vị trí qua lại
        step = ax["speed"] * INTERVAL * 0.02 * ax["dir"]
        ax["pos"] += step

        if ax["pos"] >= ax["target"]:
            ax["pos"] = ax["target"]
            ax["dir"] = -1
        elif ax["pos"] <= 0:
            ax["pos"] = 0.0
            ax["dir"] = 1


def build_payload():
    """Tạo JSON payload giống hệt format của Form1.StatePublisher.cs"""
    update_simulation()

    axes = []
    for i, ax in enumerate(sim_axes):
        raw_status = random.choice([0, 1, 2]) if tick % 10 != 0 else 0
        axes.append({
            "idx": i,
            "pos": f"{ax['pos']:.4f}",
            "speed": f"{ax['speed']:.4f}",
            "mCode": random.choice([0, 0, 0, 3, 4, 5]),
            "error": 0,
            "warning": 0,
            "status": "Stopped" if raw_status == 0 else random.choice(status_list),
            "dataNo": (tick + i) % 20,
            "limitMinus": ax["pos"] <= 0.1,
            "limitPlus": ax["pos"] >= ax["target"] - 0.1,
            "homeDog": ax["pos"] <= 1.0,
            "isComplete": raw_status == 0
        })

    payload = {
        "connected": True,
        "connectionBanner": "PLC Connected (Simulated)",
        "integrityState": random.choice(["OK", "OK", "OK", "WARNING"]),
        "integrityDetail": "Simulator running normally",
        "integrityTone": "normal",
        "jogSpeed": random.randint(100, 1000),
        "axes": axes,
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%S.000Z", time.gmtime())
    }
    return json.dumps(payload)


def on_connect(client, userdata, connect_flags, reason_code, properties):
    if reason_code == 0 or str(reason_code) == "Success":
        print("✅ Đã kết nối MQTT broker thành công!")
        print(f"   Broker: {BROKER}:{PORT}")
        print(f"   Topic:  {TOPIC}")
        print(f"   Interval: {INTERVAL}s")
        print("-" * 50)
    else:
        print(f"❌ Kết nối thất bại: {reason_code}")
        print("   ➡ Kiểm tra lại USERNAME/PASSWORD trong script.")


def on_disconnect(client, userdata, disconnect_flags, reason_code, properties):
    print(f"⚠️  Mất kết nối MQTT (reason={reason_code})")


def main():
    print("=" * 50)
    print("  MQTT SIMULATOR - DACDT/machine/state")
    print("=" * 50)

    client = mqtt.Client(
        client_id=f"simulator_{random.randint(1000,9999)}",
        protocol=mqtt.MQTTv311,
        callback_api_version=mqtt.CallbackAPIVersion.VERSION2
    )
    client.username_pw_set(USERNAME, PASSWORD)
    client.tls_set(tls_version=ssl.PROTOCOL_TLSv1_2)
    client.tls_insecure_set(False)
    client.on_connect = on_connect
    client.on_disconnect = on_disconnect

    print(f"🔌 Đang kết nối tới {BROKER}:{PORT}...")
    client.connect(BROKER, PORT, keepalive=60)
    client.loop_start()

    time.sleep(2)  # Chờ kết nối

    try:
        count = 0
        while True:
            payload = build_payload()
            result = client.publish(TOPIC, payload, qos=1)
            count += 1

            # In tóm tắt mỗi 5 lần
            if count % 5 == 1:
                data = json.loads(payload)
                ax0 = data["axes"][0]
                print(
                    f"[{count:>5}] "
                    f"Axis0: pos={ax0['pos']}, speed={ax0['speed']}, status={ax0['status']} | "
                    f"integrity={data['integrityState']}"
                )

            time.sleep(INTERVAL)

    except KeyboardInterrupt:
        print("\n🛑 Dừng simulator.")
    finally:
        client.loop_stop()
        client.disconnect()
        print("👋 Đã ngắt kết nối.")


if __name__ == "__main__":
    main()