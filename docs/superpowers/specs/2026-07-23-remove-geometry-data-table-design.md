# Remove Geometry Data Table

## Mục tiêu

Giảm tải giao diện khi mở DXF/G-code lớn bằng cách xóa hoàn toàn bảng `Geometry Data` và không còn tạo danh sách tọa độ phục vụ bảng này.

## Phạm vi

- Giữ nguyên `CAD Preview`.
- Giữ nguyên `G-code Editor`.
- Giữ nguyên `Process Table` vì bảng này phục vụ theo dõi và chạy lệnh PLC.
- Không thay đổi cơ chế chiếu/fit preview hiện tại.
- Không thay đổi dữ liệu CAD đầy đủ dùng cho biên dịch và gửi PLC.

## Thiết kế

- Xóa panel `Geometry Data` và `DataGrid` tọa độ khỏi `DxfRunView.xaml`.
- Xóa nhánh xử lý cuộn/lazy-load riêng cho `GeometryDataGrid`.
- Trong `PushDxfStateAsync`, bỏ bước xây dựng `geometryRows` và không gọi `SetGeometryRows`.
- Giữ các model/collection tọa độ còn lại nếu chúng đang được dùng bởi luồng CAD/PLC khác; không thực hiện refactor ngoài phạm vi.

## Kiểm thử

- Test hiện có vẫn phải đạt.
- Build ứng dụng WPF cấu hình Release x86.
- Kiểm tra tĩnh rằng `DxfRunView.xaml` không còn `Geometry Data`/`GeometryDataGrid`, và luồng `PushDxfStateAsync` không còn gọi `BuildGeometryRows` hoặc `SetGeometryRows`.
- Kiểm tra `Process Table`, `CAD Preview` và `G-code Editor` vẫn còn trong XAML.
