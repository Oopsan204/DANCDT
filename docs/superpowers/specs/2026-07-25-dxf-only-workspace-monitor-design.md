# Thiết kế DXF-only, Workspace động và DXF Point Monitor

Ngày: 2026-07-25  
Trạng thái: Đã được người dùng duyệt về hướng thiết kế

## 1. Mục tiêu

Ứng dụng chỉ tiếp nhận và xử lý file DXF. Toàn bộ giao diện, lệnh, dịch vụ và nhánh xử lý dành riêng cho G-code được loại bỏ.

Đồng thời:

- Giá trị Workspace Width/Height phải được áp dụng ngay khi người dùng bấm Apply.
- Scan Limits và khung vùng làm việc phải dùng đúng cùng một giá trị Workspace, không còn giới hạn ghi cứng 170 mm.
- Khu vực G-code Editor trong tab DXF được thay bằng bảng DXF Point Monitor theo mẫu đã duyệt.
- Dữ liệu lệnh DXF nội bộ, Ring Buffer và quá trình chạy PLC vẫn hoạt động như hiện tại.

## 2. Nguyên nhân lỗi Workspace

Có hai nguồn gây ra hiện tượng đặt Workspace 175 mm nhưng kết quả vẫn là 170 mm:

1. `ApplyDxfSettingsAsync` cập nhật offset, tốc độ và dwell nhưng bỏ sót `workspaceWidth` và `workspaceHeight`. Vì vậy file cấu hình có thể đã lưu 175 trong khi phiên ứng dụng đang chạy vẫn giữ giá trị cũ.
2. `HandleScanLimitsAsync` khai báo trực tiếp `LimitX = 170.0` và `LimitY = 170.0`, nên Scan Limits không sử dụng cấu hình Workspace.

Hai lỗi phải được sửa cùng nhau để phần lưu cấu hình, preview và kiểm tra giới hạn có cùng kết quả.

## 3. Workspace là nguồn dữ liệu duy nhất

`workspaceWidth` và `workspaceHeight` trong trạng thái ứng dụng là nguồn dữ liệu duy nhất cho:

- Khung vùng làm việc trong CAD Preview.
- Phép chiếu tọa độ DXF lên preview.
- Scan Limits.
- Thông báo vượt giới hạn.
- Giá trị được lưu vào file cấu hình.

Khi bấm Apply DXF Settings:

1. Đọc Width/Height mới từ giao diện.
2. Chỉ chấp nhận giá trị hữu hạn và lớn hơn 0.
3. Cập nhật trạng thái ứng dụng trước khi rebuild, scan và push giao diện.
4. Lưu file cấu hình.
5. Vẽ lại khung Workspace và kiểm tra lại giới hạn ngay, không yêu cầu khởi động lại.

Nếu giá trị không hợp lệ, ứng dụng giữ nguyên Workspace hợp lệ trước đó và hiển thị thông báo lỗi.

Ví dụ: Workspace đặt 175 × 175 mm thì Scan Limits phải so sánh tọa độ máy với X=175 và Y=175. File có biên 170 mm vẫn được báo là nằm trong Workspace 175 mm; con số 170 chỉ còn là biên thực tế của hình DXF, không phải giới hạn máy.

## 4. Chuyển ứng dụng sang DXF-only

### 4.1 Giao diện

Loại bỏ:

- Nút `New Gcode`.
- `G-code Editor`.
- Nút `Preview`.
- Nút `Save G-code`.
- Các trường cài đặt chỉ dành cho G-code, gồm G-code M3 Speed và WCS G54–G59.
- Các nhãn `DXF / GCODE Run`, thay bằng `DXF Run`.
- Nội dung Help hướng dẫn mở, chỉnh sửa hoặc chạy G-code.

Hộp chọn file chỉ cho phép chọn `.dxf`.

### 4.2 Luồng xử lý

Loại bỏ các nhánh:

- Nhận diện phần mở rộng G-code, NC, TAP hoặc TXT.
- Đọc văn bản G-code.
- Làm sạch G-code.
- Chuyển G-code thành hình học CAD.
- Preview, chỉnh sửa và lưu G-code.
- Biên dịch process rows từ nguồn G-code.
- Áp dụng tốc độ, WCS hoặc thiết lập chỉ phục vụ G-code.

Các command, handler, state property và service không còn người gọi sẽ được xóa. Các file nguồn chuyên biệt như dịch vụ đọc G-code, bộ làm sạch và bộ chuẩn hóa dòng G-code sẽ được gỡ khỏi project sau khi kiểm tra không còn phụ thuộc DXF. Dependency `Gcode.Utils` cũng được gỡ nếu không còn người dùng sau khi biên dịch.

Các khóa G-code cũ trong file cấu hình được coi là khóa không còn sử dụng. Ứng dụng không ghi lại chúng; file cấu hình cũ vẫn tải được vì khóa thừa được bỏ qua.

### 4.3 Phần phải giữ nguyên

- Import và phân tích DXF.
- CAD Preview, chọn đường Engrave/Cut và khung Workspace.
- `processRows` nội bộ tạo từ DXF.
- Truyền dữ liệu QD75 và Ring Buffer.
- RUN, PAUSE, CONTINUE, STOP, HOME, RESET.
- Test Area, Clear Buffer và Export QD75.
- Camera, Monitor, Logs và điều khiển PLC trực tiếp.

Tên `MotionType` và các mã lệnh nội bộ dùng cho QD75 không phải là chức năng nhập G-code, nên vẫn được giữ.

## 5. DXF Point Monitor trong tab DXF

Khu vực bên phải CAD Preview thay thế hoàn toàn G-code Editor.

### 5.1 Nội dung

Phần đầu bảng hiển thị:

- Tiêu đề `DXF Point Monitor`.
- Tên file DXF đang chạy.
- `Active data no.` và trạng thái kết nối PLC.
- `Running line .../... (...)`.
- Thanh tiến độ chạy.
- Thanh tiến độ tải dữ liệu khi có.

Bảng gồm các cột:

- `Run`
- `No.`
- `DXF Point`
- `M`
- `Speed`
- `End X;Y`

Dòng hiện tại có nền xanh, chữ trắng, in đậm và nhãn `RUN`.

### 5.2 Nguồn dữ liệu

Bảng dùng trực tiếp:

- `ProgramRows`
- `ActiveProgramIndex`
- `ActiveProgramText`
- `RunProgressText`
- `RunProgressPercent`
- `ProgressPercent`

Không tạo thêm bản sao toàn bộ danh sách lệnh. Cơ chế cửa sổ dữ liệu hiện tại tiếp tục giới hạn số dòng WPF phải giữ trên giao diện, trong khi danh sách đầy đủ vẫn nằm trong bộ nhớ nội bộ để chạy PLC.

### 5.3 Hiệu năng

- Bật row virtualization và column virtualization.
- Dùng content scrolling.
- Không gọi `ScrollIntoView` cho từng điểm PLC.
- Gộp yêu cầu cuộn và thực hiện tối đa 10 lần mỗi giây.
- Chỉ cuộn đến trạng thái mới nhất.
- Không thay đổi tần suất đọc Md.44 hoặc truyền Ring Buffer.

Để hạn chế rủi ro giao diện, bảng được thêm trực tiếp vào `DxfRunView`; không tái cấu trúc `MonitorView` thành component dùng chung trong thay đổi này.

## 6. Luồng hoạt động sau thay đổi

1. Người dùng mở tab `DXF Run`.
2. Người dùng bấm `Import DXF` và chỉ có thể chọn file `.dxf`.
3. Ứng dụng đọc hình học, tạo CAD Preview và process rows DXF.
4. Khung preview dùng Workspace đang cấu hình.
5. Scan Limits so sánh hình sau offset với đúng Width/Height đang cấu hình.
6. DXF Point Monitor hiển thị danh sách lệnh.
7. Khi PLC chạy, Md.44 cập nhật dòng hiện tại, tiến độ và đánh dấu xanh.
8. Với chương trình lớn hơn 600 điểm, Ring Buffer tiếp tục nạp dữ liệu theo cơ chế hiện tại.

## 7. Xử lý lỗi

- File không phải DXF: không xuất hiện trong hộp chọn; nếu truyền đường dẫn bằng cách khác thì bị từ chối với thông báo rõ ràng.
- DXF không có hình học được hỗ trợ: báo không tìm thấy đường hợp lệ và không bật RUN.
- Workspace không hợp lệ: giữ giá trị trước đó, không scan hoặc lưu giá trị sai.
- DXF vượt Workspace: vẫn cho xem preview và thông báo rõ trục cùng khoảng tọa độ vượt giới hạn. Thay đổi này giữ nguyên hành vi hiện tại, không bổ sung cơ chế tự động khóa RUN.
- PLC chưa kết nối: bảng vẫn hiển thị lệnh DXF nhưng không có dòng RUN đang hoạt động.

## 8. Kiểm thử

Phải có kiểm thử trước khi sửa mã cho các hành vi:

1. Apply DXF Settings cập nhật `workspaceWidth` và `workspaceHeight`.
2. Scan Limits không còn hằng số 170 và dùng Workspace cấu hình.
3. Workspace 175 chấp nhận tọa độ 171 nhưng Workspace 170 báo vượt.
4. Tab DXF không còn G-code Editor và các nút G-code.
5. Tab DXF có DXF Point Monitor với đúng binding và cột.
6. File dialog chỉ nhận `.dxf`.
7. Không còn command hoặc handler G-code có thể chạy.
8. Các file xử lý G-code chuyên biệt được gỡ khỏi project.
9. Bảng DXF dùng virtualization và cuộn được giới hạn 100 ms.
10. Import DXF, tạo process rows và truyền PLC vẫn vượt qua kiểm thử hồi quy.

Sau cùng phải chạy toàn bộ test và rebuild WPF Release bằng MSBuild đầy đủ.

## 9. Ngoài phạm vi

- Không thay đổi giao thức PLC.
- Không thay đổi cấu trúc QD75 hoặc Ring Buffer.
- Không thay đổi Camera, Monitor hoặc Logs ngoài việc tiếp tục nhận trạng thái hiện tại.
- Không tạo installer trong thay đổi này.
