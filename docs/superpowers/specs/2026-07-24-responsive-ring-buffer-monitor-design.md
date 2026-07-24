# Thiết kế chống treo giao diện khi chạy CAD lớn

## Hiện trạng và nguyên nhân

`QD75RingBufferRunner.StartAsync()` được gọi từ UI thread. Sau mỗi lần chờ
`Task.Delay(50 ms)`, continuation quay lại UI thread rồi thực hiện
`ReadBuffer`/`WriteBuffer` đồng bộ. Với chương trình 151.370 lệnh, vòng refill
lặp hàng trăm lần và chiếm UI thread.

`DashboardView` và `MonitorView` còn gọi `ScrollIntoView` theo mọi thay đổi của
`ActiveProgramIndex` (tối đa khoảng 60 lần/giây), tạo thêm tải bố cục và hàng
đợi Dispatcher.

## Phương án được duyệt

1. Nạp 599 điểm ring buffer ban đầu bằng background thread.
2. `HandleSendCadXAsync` phải chờ nạp ban đầu thành công trước khi bật RUN.
3. Vòng đọc Md.44 và refill tiếp tục chạy hoàn toàn ngoài UI thread.
4. Sự kiện tiến độ, log, hoàn tất và lỗi vẫn được chuyển về UI qua cơ chế hiện
   có.
5. Dashboard và Monitor gom các yêu cầu tự cuộn, chỉ xử lý trạng thái mới nhất
   tối đa 10 lần/giây.

## Phạm vi không thay đổi

- Không thay đổi dữ liệu lệnh hoặc thứ tự lệnh gửi PLC.
- Không thay đổi kích thước hai vùng ring buffer và điểm JUMP 600.
- Không thay đổi chu kỳ đọc Md.44 là 50 ms.
- Không tắt bảng Monitor và không bỏ đánh dấu dòng đang chạy.
- Không thay đổi CAD Preview, G-code Editor hoặc camera.

## Xử lý lỗi

Nếu nạp 599 điểm ban đầu thất bại hoặc bị hủy, `StartAsync` trả về `false`,
không bật RUN và phát sự kiện lỗi hiện có. Lỗi phát sinh trong vòng refill vẫn
phát `OnError` và kết thúc trạng thái `IsRunning`.

## Kiểm thử

- Kiểm thử hồi quy xác nhận initial load và monitor loop không giữ UI context.
- Kiểm thử xác nhận RUN chỉ được bật sau khi initial load trả về thành công.
- Kiểm thử xác nhận cả Dashboard và Monitor dùng bộ gom tự cuộn 100 ms, không
  tạo `Dispatcher.BeginInvoke` mới cho từng dòng.
- Chạy toàn bộ test và build WPF Release bằng MSBuild đầy đủ.
