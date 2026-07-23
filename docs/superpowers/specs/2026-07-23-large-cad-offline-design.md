# CAD lớn và chế độ offline – Đặc tả thiết kế

## Mục tiêu

Cho phép ứng dụng mở và hiển thị các file CAD có số lượng điểm rất lớn mà không làm giao diện treo hoặc tiêu thụ bộ nhớ quá mức, đồng thời loại bỏ toàn bộ tích hợp MQTT/web khỏi runtime của ứng dụng. Dữ liệu đầy đủ vẫn được giữ cho việc tạo chương trình và điều khiển PLC trực tiếp.

## Phạm vi

- Giữ nguyên luồng mở file CAD từ máy tính.
- Giữ nguyên dữ liệu hình học đầy đủ dùng để tạo lệnh PLC.
- Tạo mô hình dữ liệu nhẹ riêng cho preview và bảng giao diện.
- Giới hạn số điểm dùng cho preview theo khả năng hiển thị, không thay đổi dữ liệu chạy máy.
- Không tạo nhiều bản sao đầy đủ của một tài liệu CAD lớn.
- Tính giới hạn CAD bằng một lượt duyệt dữ liệu, không tạo thêm các danh sách X/Y lớn.
- Không khởi tạo, kết nối, publish, subscribe hoặc xử lý lệnh MQTT.
- Không nhận CAD từ web, không gửi CAD lên web và không khởi động dịch vụ WebRTC dùng cho web.
- Giữ PLC trực tiếp, camera cục bộ và các chức năng không liên quan.

## Thiết kế xử lý CAD

`CadLoadResult` gốc là nguồn dữ liệu duy nhất cho chương trình PLC. Khi cập nhật giao diện, app sẽ tạo một tài liệu preview rút gọn:

- Mỗi primitive được lấy mẫu theo giới hạn điểm preview.
- Tổng số điểm preview có giới hạn cứng để tránh một polyline 500.000 điểm đi thẳng vào WPF geometry.
- Bảng điểm chỉ giữ dữ liệu cần cho vùng hiển thị ban đầu và tải thêm theo batch hiện có.
- Preview dùng một geometry nhẹ; không dựng lặp lại ảnh, geometry engrave, geometry cut và danh sách primitive đầy đủ từ cùng một tập điểm lớn.
- Nếu offset chỉ phục vụ hiển thị, offset được áp dụng trên dữ liệu preview, không clone lại toàn bộ tài liệu gốc.

Các giới hạn preview chỉ áp dụng cho UI. `Primitives` gốc và dữ liệu chương trình vẫn giữ đủ điểm để tạo chuyển động PLC.

## Thiết kế offline

Các điểm gọi MQTT/web trong luồng khởi động, mở CAD, publish trạng thái, nhận lệnh và upload CAD sẽ được loại khỏi runtime. Các file hỗ trợ có thể vẫn tồn tại trong source nếu cần để giảm rủi ro thay đổi ngoài phạm vi, nhưng không được app khởi tạo hoặc gọi. Bộ cài không cần đưa giao diện web hoặc dịch vụ WebRTC web vào luồng chạy chính.

## Kiểm tra và tiêu chí đạt

- Test chứng minh dữ liệu PLC không bị cắt khi preview bị giới hạn.
- Test chứng minh preview không vượt giới hạn điểm/primitive đã định.
- Test chứng minh tính giới hạn không tạo hai danh sách X/Y bổ sung.
- Test chứng minh app không khởi tạo MQTT/web runtime.
- Toàn bộ test hiện có pass.
- Build Release x86 thành công.

