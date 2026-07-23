# Thiết kế sửa vùng pan CAD Preview

## Hiện trạng

`CadViewport` có thể cao hơn tỷ lệ `1000:620`, nhưng `CadSurface` đang nằm trong
`Viewbox Stretch="Uniform"` và bật `ClipToBounds`. Vì vậy WPF tạo hai dải trống
trên/dưới; CAD bị cắt khi pan vào các dải này.

## Các phương án

1. Tắt cắt trên `CadSurface` và `Viewbox`, đồng thời giữ cắt tại `CadViewport`:
   thay đổi nhỏ, giữ nguyên toàn bộ phép đổi tọa độ và vẫn không cho CAD vẽ ra
   ngoài panel.
2. Kéo giãn `CadSurface` theo toàn bộ panel: loại bỏ dải trống nhưng làm sai tỷ lệ
   hình học và vùng làm việc.
3. Thay toàn bộ cấu trúc bằng một Canvas kích thước động: linh hoạt hơn nhưng phải
   viết lại phép quy đổi pan, zoom và hit-test.

Phương án 1 được chọn vì đạt đúng mục tiêu chỉ cắt tại viền ngoài với thay đổi tối
thiểu và không làm sai hệ tọa độ CAD.

## Thiết kế được duyệt

- `CadViewport` là lớp duy nhất cắt nội dung.
- Lớp pan/zoom có kích thước bằng toàn bộ viewport.
- Mặt phẳng CAD và khung làm việc vẫn dùng hệ tọa độ `1000×620`.
- Pan, zoom, chọn đường và touch tiếp tục quy đổi trên cùng hệ tọa độ CAD.
- Không thay đổi dữ liệu DXF, G-code hoặc dữ liệu lệnh PLC.

## Kiểm thử

- Kiểm thử cấu trúc XAML phải xác nhận lớp CAD bên trong không còn tự cắt nội dung.
- Kiểm thử hiện có cho pan/zoom, hit-test và giới hạn CAD phải tiếp tục đạt.
- Build WPF phải thành công.
