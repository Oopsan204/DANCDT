# Cấu Trúc Khung Truyền Dữ Liệu DXF Xuống PLC (Mitsubishi QD75)

Tài liệu này mô tả chi tiết cách thức ứng dụng đóng gói và gửi dữ liệu tọa độ từ tab DXF xuống vùng nhớ Program Data của module điều khiển vị trí Mitsubishi (QD75).

## 1. Thông Tin Chung

- **Vùng nhớ sử dụng:** Buffer Memory (U0\G).
- **Địa chỉ bắt đầu (Axis 1):** `U0\G2000`.
- **Kích thước một điểm (Stride):** 10 thanh ghi (10 words).
- **Công thức địa chỉ điểm thứ `n`:** `Địa chỉ = Base + (n-1) * 10 + Offset`.

## 2. Bảng Phân Phối Thanh Ghi (Offsets)

| Offset       | Thông số                       | Kiểu dữ liệu | Mô tả                                                    |
| :----------- | :------------------------------- | :-------------- | :--------------------------------------------------------- |
| **+0** | **Positioning Identifier** | 16-bit          | Mã điều khiển kiểu chạy, hệ tọa độ và nội suy. |
| **+1** | **M Code**                 | 16-bit          | Mã phụ (ví dụ: 1-Bật keo, 2-Tắt keo).                |
| **+2** | **Dwell Time**             | 16-bit          | Thời gian chờ tại điểm (đơn vị: ms).               |
| **+4** | **Command Speed**          | 32-bit          | Tốc độ di chuyển (mm/min, đơn vị x100).             |
| **+6** | **Position Address**       | 32-bit          | Tọa độ đích (mm, đơn vị x10000).                   |
| **+8** | **Arc Address**            | 32-bit          | Tọa độ tâm cung tròn (chỉ dùng cho lệnh Arc).      |

## 3. Chi Tiết Positioning Identifier (Offset +0)

Thanh ghi này quyết định cách thức robot di chuyển. Cấu trúc bit như sau:

### Bit 0-1: Operation Pattern (Kiểu nối điểm)

- `00` (0): **Independent** - Dừng lại sau khi hoàn thành điểm này.
- `01` (1): **Continuous Positioning** - Chạy tiếp điểm sau nhưng có giảm tốc và dừng ảo.
- `11` (3): **Continuous Path** - Chạy liên tục qua điểm này mà không giảm tốc.

### Bit 8-15: Control System (Hệ thống điều khiển)

- `0x0A` (10): **Linear Interpolation 1** - Chạy đường thẳng (Hệ tọa độ tuyệt đối ABS).
- `0x0F` (15): **Arc CW** - Chạy cung tròn cùng chiều kim đồng hồ.
- `0x10` (16): **Arc CCW** - Chạy cung tròn ngược chiều kim đồng hồ.

### Bit 2-7: Các thông số khác

- Bit 2-3: Accel Time No. (0-3).
- Bit 4-5: Decel Time No. (0-3).
- Bit 6-7: Interpolated Axis (Dùng cho nội suy nhiều trục).

## 4. Địa Chỉ Gốc Các Trục (Base Addresses)

Dữ liệu được nạp vào vùng P rogram Data tương ứng cho từng trục:

- **Trục 1 (X):** `U0\G2000`
- **Trục 2 (Y):** `U0\G2100`
- **Trục 3 (Z):** `U0\G2200`
- **Trục 4:** `U0\G2300`

## 5. Lưu Ý Kỹ Thuật

- **Thứ tự ghi:** Dữ liệu 32-bit (Tốc độ, Vị trí) phải được ghi theo thứ tự **Low Word** vào địa chỉ thấp và **High Word** vào địa chỉ cao (+1).
- **Đơn vị truyền:**
  - Vị trí: 1.0mm truyền xuống là `10000`.
  - Tốc độ: 1.0mm/min truyền xuống là `100`.
- **Giới hạn:** Mỗi trục thường hỗ trợ tối đa 10 - 100 điểm trong vùng đệm này tùy theo cấu hình module.
