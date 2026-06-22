// QD75 and PLC Error/Warning Codes Database
const ERROR_CODES_DB = [
    {
        "code": "4101",
        "type": "Lỗi vận hành (Operation Error)",
        "description": "Truy cập ngoài phạm vi thiết bị hoặc mã NULL 00H không tồn tại.",
        "cause": "Phạm vi các điểm n vượt quá giới hạn thiết bị tương ứng; mã NULL 00H không được tìm thấy trong phạm vi thiết bị khi xử lý chuỗi ký tự; hoặc các thiết bị nguồn và đích ghi đè lên nhau không hợp lệ.",
        "remedy": "Kiểm tra lại số lượng điểm n được chỉ định và đảm bảo rằng thiết bị đích có đủ dung lượng; thêm mã NULL 00H vào cuối chuỗi ký tự.",
        "source": "1"
    },
    {
        "code": "4100",
        "type": "Lỗi vận hành (Operation Error)",
        "description": "Dữ liệu thiết bị nằm ngoài phạm vi cài đặt.",
        "cause": "Giá trị của thiết bị (S) hoặc (D) không hợp lệ (ví dụ: dữ liệu BCD không nằm trong khoảng 0-9999 hoặc số chia bằng 0).",
        "remedy": "Kiểm tra và hiệu chỉnh dữ liệu đầu vào sao cho nằm trong phạm vi cho phép của tập lệnh (ví dụ: 0 đến 9999 đối với BCD 4 chữ số).",
        "source": "1"
    },
    {
        "code": "4140",
        "type": "Lỗi vận hành (Operation Error)",
        "description": "Giá trị số thực (Floating-point) không hợp lệ.",
        "cause": "Thiết bị được chỉ định chứa giá trị -0, số không chuẩn hóa, không phải là số (NaN) hoặc vô cùng (±∞).",
        "remedy": "Đảm bảo các số thực được sử dụng trong phép toán nằm trong dải cho phép (2^-126 đến 2^128 cho độ chính xác đơn).",
        "source": "1"
    },
    {
        "code": "4200",
        "type": "Lỗi vận hành (Operation Error)",
        "description": "Lỗi cấu trúc vòng lặp FOR-NEXT.",
        "cause": "Lệnh FEND, END hoặc STOP được thực thi trước khi lệnh NEXT kết thúc vòng lặp FOR tương ứng.",
        "remedy": "Đảm bảo mọi cấu trúc FOR đều có lệnh NEXT tương ứng trước khi kết thúc chương trình chính.",
        "source": "1"
    },
    {
        "code": "4210",
        "type": "Lỗi vận hành (Operation Error)",
        "description": "Lỗi con trỏ (Pointer error).",
        "cause": "Số con trỏ được chỉ định cho lệnh nhảy (Jump) hoặc gọi chương trình con (Call) không tồn tại trong cùng một tệp chương trình.",
        "remedy": "Kiểm tra nhãn con trỏ (P*) và đảm bảo nó đã được định nghĩa đúng vị trí trong chương trình.",
        "source": "1"
    },
    {
        "code": "1402",
        "type": "Lỗi module thông minh",
        "description": "Lỗi module chức năng thông minh.",
        "cause": "Phát hiện lỗi tại module thông minh khi thực hiện lệnh đọc/ghi bộ đệm (FROM/TO).",
        "remedy": "Kiểm tra trạng thái phần cứng của module thông minh và cấu hình cài đặt trong thông số I/O.",
        "source": "1"
    },
    {
        "code": "2410",
        "type": "Lỗi tệp (File Error)",
        "description": "Tệp chỉ định không tồn tại.",
        "cause": "Tên chương trình hoặc tệp dữ liệu được chỉ định trong ổ đĩa không tìm thấy.",
        "remedy": "Kiểm tra lại tên tệp và đảm bảo tệp đã được tải vào đúng ổ đĩa chỉ định (Drive 0, 1, 2, hoặc 4).",
        "source": "1"
    },
    {
        "code": "9100 - 9124",
        "type": "Lỗi lệnh PID",
        "description": "Lỗi trong quá trình tính toán hoặc cài đặt PID/Auto-tuning.",
        "cause": "Chu kỳ lấy mẫu Ts <= 0; các hằng số KP, TI, TD nằm ngoài dải; hoặc quá trình Auto-tuning thất bại do biến động PV không bình thường.",
        "remedy": "Kiểm tra lại các tham số trong khối điều khiển (S3) và đảm bảo hệ thống ổn định trước khi bắt đầu Auto-tuning.",
        "source": "1"
    },
    {
        "code": "2500",
        "type": "Lỗi",
        "description": "CAN'T EXE. PRG.",
        "cause": "Thay đổi số đầu của thanh ghi chỉ số sử dụng trong tham số nhưng không ghi tham số vào PLC cùng với chương trình.",
        "remedy": "Đảm bảo ghi tham số vào bộ điều khiển lập trình cùng với chương trình tương ứng.",
        "source": "2"
    },
    {
        "code": "4101",
        "type": "Lỗi",
        "description": "OPERATION ERROR",
        "cause": "Truy cập vượt quá phạm vi thiết bị được chỉ định, hoặc thực hiện sửa đổi chỉ số (index modification) vượt giới hạn thiết bị, hoặc truy cập thanh ghi file (R, ZR) mà chưa thiết lập file thanh ghi.",
        "remedy": "Kiểm tra lại phạm vi thiết bị trong chương trình và cài đặt thanh ghi file trong tham số PLC.",
        "source": "2"
    },
    {
        "code": "3101",
        "type": "Lỗi",
        "description": "LINK PARA ERROR",
        "cause": "Số ổ đĩa (drive number) bị thay đổi bằng lệnh QDRSET khi thiết bị \"ZR\" được chỉ định trong các module CPU không phải Universal model QCPU.",
        "remedy": "Không thay đổi số ổ đĩa bằng lệnh QDRSET khi sử dụng thiết bị ZR trên các dòng CPU này.",
        "source": "2"
    },
    {
        "code": "1103",
        "type": "Lỗi",
        "description": "DEVICE RANGE OVER",
        "cause": "Dữ liệu sau khi sửa đổi chỉ số vượt quá phạm vi thiết bị chỉ định của người dùng và ghi vào thiết bị hệ thống.",
        "remedy": "Điều chỉnh giá trị thanh ghi chỉ số hoặc phạm vi thiết bị để không xâm phạm vùng nhớ hệ thống.",
        "source": "2"
    },
    {
        "code": "4200",
        "type": "Lỗi",
        "description": "FOR-NEXT ERROR",
        "cause": "Thực thi lệnh FEND, END hoặc STOP bên trong vòng lặp FOR-NEXT trước khi gặp lệnh NEXT.",
        "remedy": "Sửa lại cấu trúc chương trình để đảm bảo các lệnh kết thúc không nằm trong vòng lặp.",
        "source": "2"
    },
    {
        "code": "4211",
        "type": "Lỗi",
        "description": "SUBROUTINE ERROR",
        "cause": "Thực thi lệnh END, FEND, GOEND hoặc STOP sau khi gọi chương trình con (CALL) nhưng trước khi gặp lệnh RET.",
        "remedy": "Đảm bảo mọi chương trình con đều kết thúc bằng lệnh RET trước khi kết thúc chương trình chính.",
        "source": "2"
    },
    {
        "code": "4221",
        "type": "Lỗi",
        "description": "INTERRUPT ERROR",
        "cause": "Thực thi lệnh FEND, END hoặc STOP bên trong chương trình ngắt trước khi thực hiện lệnh IRET.",
        "remedy": "Sửa lại cấu trúc chương trình ngắt, đảm bảo kết thúc bằng lệnh IRET.",
        "source": "2"
    },
    {
        "code": "4230",
        "type": "Lỗi",
        "description": "CHK INSTRUCTION ERROR",
        "cause": "Thực thi các lệnh kết thúc chương trình hoặc lệnh STOP giữa các lệnh CHKCIR và CHKEND.",
        "remedy": "Kiểm tra lại cấu trúc các lệnh kiểm tra lỗi đặc biệt.",
        "source": "2"
    },
    {
        "code": "4140",
        "type": "Lỗi",
        "description": "FLOATING POINT DATA ERROR",
        "cause": "Giá trị thiết bị được chỉ định là -0, số không chuẩn (subnormal number), NaN (không phải số), hoặc vô cực (+/- infinity).",
        "remedy": "Kiểm tra dữ liệu đầu vào của các phép toán số thực để đảm bảo giá trị nằm trong phạm vi hợp lệ.",
        "source": "2"
    },
    {
        "code": "4100",
        "type": "Lỗi",
        "description": "DATA SETTING ERROR",
        "cause": "Giá trị nguồn (S) hoặc giá trị cài đặt n nằm ngoài phạm vi cho phép của lệnh (ví dụ: dữ liệu BCD không hợp lệ, chia cho 0, hoặc giá trị n âm).",
        "remedy": "Đảm bảo dữ liệu nguồn và các hằng số cài đặt phù hợp với quy định của từng lệnh kỹ thuật.",
        "source": "2"
    },
    {
        "code": "2400",
        "type": "Lỗi",
        "description": "FILE SET ERROR",
        "cause": "File chú thích (comment file) được thiết lập trong tham số PLC nhưng không tồn tại khi bật nguồn hoặc reset.",
        "remedy": "Kiểm tra sự tồn tại của file trong bộ nhớ hoặc điều chỉnh lại thiết lập PLC File.",
        "source": "2"
    },
    {
        "code": "9010",
        "type": "Lỗi",
        "description": "CHK DETECTED ERROR",
        "cause": "Phát hiện lỗi hệ thống thông qua lệnh CHK (kiểm tra lỗi định dạng đặc biệt).",
        "remedy": "Tra cứu mã số tiếp điểm và mã số cuộn dây lưu trong SD80 để xác định vị trí lỗi cụ thể.",
        "source": "2"
    },
    {
        "code": "1000",
        "type": "Lỗi nghiêm trọng (Major)",
        "description": "MAIN CPU DOWN CPU bị treo hoặc hỏng.",
        "cause": "Sự cố do nhiễu hoặc hỏng hóc phần cứng.",
        "remedy": "Thực hiện các biện pháp giảm nhiễu. Reset module CPU và chạy lại. Nếu lỗi vẫn còn, đó là lỗi phần cứng.",
        "source": "3"
    },
    {
        "code": "1009",
        "type": "Lỗi nghiêm trọng (Major)",
        "description": "MAIN CPU DOWN Lỗi nguồn hoặc lỗi kết nối bus hệ thống.",
        "cause": "Dạng sóng điện áp nguồn ngoài phạm vi cho phép hoặc lỗi ở bộ nguồn, module CPU, đơn vị đế hoặc cáp mở rộng.",
        "remedy": "Kiểm tra điện áp nguồn. Reset CPU. Nếu lỗi tiếp diễn, kiểm tra và thay thế các linh kiện phần cứng bị lỗi.",
        "source": "3"
    },
    {
        "code": "1300",
        "type": "Lỗi trung bình/nhẹ (Moderate/Minor)",
        "description": "FUSE BREAK OFF Có module đầu ra bị đứt cầu chì.",
        "cause": "Cầu chì của module đầu ra bị đứt.",
        "remedy": "Kiểm tra đèn LED FUSE của các module đầu ra, thay thế module bị đứt cầu chì hoặc kiểm tra kết nối cáp mở rộng.",
        "source": "3"
    },
    {
        "code": "1600",
        "type": "Cảnh báo (Minor)",
        "description": "BATTERY ERROR Điện áp pin của module CPU giảm xuống dưới mức quy định.",
        "cause": "Pin hết điện hoặc đầu nối pin không được kết nối đúng cách.",
        "remedy": "Thay pin mới hoặc kiểm tra lại kết nối đầu nối pin.",
        "source": "3"
    },
    {
        "code": "2000",
        "type": "Lỗi trung bình (Moderate)",
        "description": "UNIT VERIFY ERR. Trạng thái module I/O khác với thông tin khi bật nguồn.",
        "cause": "Module I/O bị lỏng, bị tháo ra hoặc lắp vào khi hệ thống đang chạy.",
        "remedy": "Kiểm tra module tương ứng tại vị trí lỗi (Slot No.) và lắp lại chắc chắn.",
        "source": "3"
    },
    {
        "code": "2100",
        "type": "Lỗi trung bình (Moderate)",
        "description": "SP.UNIT LAY ERR. Lỗi bố trí module chức năng thông minh.",
        "cause": "Cài đặt thông số I/O Assignment không khớp với module thực tế hoặc số điểm I/O gán ít hơn module thực tế.",
        "remedy": "Cài đặt lại thông số I/O Assignment trong PLC Parameter để khớp với thực tế.",
        "source": "3"
    },
    {
        "code": "2124",
        "type": "Lỗi trung bình (Moderate)",
        "description": "SP.UNIT LAY ERR. Vượt quá số lượng module hoặc số điểm I/O cho phép.",
        "cause": "Lắp module ở vị trí vượt quá phạm vi điểm I/O (ví dụ: vượt quá 4096 điểm đối với dòng High Performance).",
        "remedy": "Giảm số lượng module hoặc thay thế module để tổng số điểm I/O nằm trong phạm vi cho phép của CPU.",
        "source": "3"
    },
    {
        "code": "2400",
        "type": "Lỗi trung bình (Moderate)",
        "description": "FILE SET ERROR File được chỉ định trong tham số không tồn tại.",
        "cause": "Thiếu file chương trình hoặc file tham số trong ổ đĩa được chỉ định.",
        "remedy": "Kiểm tra mã lỗi để xác định file thiếu. Tạo file và nạp lại vào module CPU.",
        "source": "3"
    },
    {
        "code": "3000",
        "type": "Lỗi trung bình (Moderate)",
        "description": "PARAMETER ERROR Lỗi cài đặt tham số.",
        "cause": "Cài đặt Timer, RUN-PAUSE, hoặc số lượng khe trống vượt quá dải cho phép của CPU.",
        "remedy": "Kiểm tra thông tin chi tiết lỗi (Parameter No.), sửa lại tham số trong phần mềm lập trình và nạp lại.",
        "source": "3"
    },
    {
        "code": "4100",
        "type": "Lỗi trung bình (Moderate)",
        "description": "OPERATION ERROR Lệnh không thể xử lý dữ liệu chứa bên trong.",
        "cause": "Dữ liệu lệnh sai lệch hoặc lỗi truy cập thẻ nhớ.",
        "remedy": "Kiểm tra vị trí lỗi trong chương trình (Program error location) và chỉnh sửa lại lệnh hoặc dữ liệu.",
        "source": "3"
    },
    {
        "code": "100",
        "type": "Cảnh báo",
        "description": "Bắt đầu trong khi vận hành",
        "cause": "Tín hiệu khởi động định vị được bật trong khi tín hiệu BUSY đang Bật.",
        "remedy": "Đảm bảo rằng tín hiệu khởi động định vị được bật chỉ sau khi tín hiệu BUSY đã Tắt.",
        "source": "4"
    },
    {
        "code": "104",
        "type": "Cảnh báo",
        "description": "Không thể khởi động lại",
        "cause": "Lệnh khởi động lại được đưa ra khi trạng thái hoạt động của trục không phải là 'Bị dừng' hoặc sau khi thao tác bị ngắt bởi yêu cầu ngắt thao tác liên tục.",
        "remedy": "Đảm bảo lệnh khởi động lại chỉ được thực hiện khi trục ở trạng thái 'Bị dừng'. Kiểm tra xem có yêu cầu ngắt thao tác liên tục trước đó không.",
        "source": "4"
    },
    {
        "code": "106",
        "type": "Lỗi",
        "description": "Tín hiệu dừng Bật khi bắt đầu",
        "cause": "Thực hiện lệnh khởi động lại trong khi tín hiệu dừng vẫn đang Bật.",
        "remedy": "Tắt tín hiệu dừng trước khi thực hiện lệnh khởi động lại.",
        "source": "4"
    },
    {
        "code": "110",
        "type": "Cảnh báo",
        "description": "Thấp hơn tốc độ tối thiểu",
        "cause": "Tốc độ thực tế thấp hơn đơn vị tối thiểu do thiết lập ghi đè (override) 1% hoặc giá trị nhỏ khác.",
        "remedy": "Điều chỉnh tốc độ lệnh hoặc giá trị ghi đè để tốc độ tính toán không thấp hơn đơn vị tối thiểu.",
        "source": "4"
    },
    {
        "code": "201",
        "type": "Lỗi",
        "description": "Bắt đầu tại điểm gốc (OP)",
        "cause": "Thực hiện lệnh OPR máy khi máy đã ở vị trí điểm gốc và chức năng thử lại OPR (retry) không được thiết lập.",
        "remedy": "Di chuyển máy ra khỏi vị trí điểm gốc bằng vận hành JOG trước khi thực hiện OPR hoặc kích hoạt chức năng thử lại OPR.",
        "source": "4"
    },
    {
        "code": "203",
        "type": "Lỗi",
        "description": "Lỗi thời điểm phát hiện Dog",
        "cause": "Tín hiệu near-point dog bị tắt trước khi máy giảm tốc xuống tốc độ creep trong phương pháp near-point dog.",
        "remedy": "Tăng chiều dài của near-point dog hoặc giảm tốc độ OPR.",
        "source": "4"
    },
    {
        "code": "204",
        "type": "Lỗi",
        "description": "Lỗi thời điểm phát hiện điểm gốc (OP)",
        "cause": "Tín hiệu zero được nhập trước khi giảm tốc xuống tốc độ creep trong phương pháp stopper.",
        "remedy": "Đảm bảo tín hiệu zero chỉ được gửi sau khi máy đã chạm vào stopper ở tốc độ creep.",
        "source": "4"
    },
    {
        "code": "205",
        "type": "Lỗi",
        "description": "Lỗi thời gian chờ (Dwell time)",
        "cause": "Thời gian chờ OPR kết thúc trong quá trình giảm tốc từ tốc độ OPR trong phương pháp stopper 1.",
        "remedy": "Tăng thời gian chờ OPR (Pr.49) hoặc giảm tốc độ OPR.",
        "source": "4"
    },
    {
        "code": "206",
        "type": "Lỗi",
        "description": "Lỗi lượng di chuyển phương pháp Count",
        "cause": "Lượng di chuyển sau near-point dog ON nhỏ hơn khoảng cách giảm tốc từ tốc độ OPR xuống tốc độ creep.",
        "remedy": "Tăng giá trị 'Setting for movement amount after near-point dog ON' (Pr.50).",
        "source": "4"
    },
    {
        "code": "207",
        "type": "Lỗi",
        "description": "Yêu cầu OPR đang Bật",
        "cause": "Thực hiện OPR nhanh (Fast OPR) khi điểm gốc chưa được thiết lập bằng OPR máy.",
        "remedy": "Thực hiện OPR máy trước khi sử dụng chức năng OPR nhanh.",
        "source": "4"
    },
    {
        "code": "209",
        "type": "Lỗi",
        "description": "Không thể khởi động lại OPR",
        "cause": "Thực hiện lệnh khởi động lại sau khi OPR máy hoặc OPR nhanh bị dừng.",
        "remedy": "Thực hiện lại toàn bộ quy trình OPR từ đầu.",
        "source": "4"
    },
    {
        "code": "502",
        "type": "Lỗi",
        "description": "Mã dữ liệu không hợp lệ",
        "cause": "Số dữ liệu định vị đích của lệnh JUMP trùng với số dữ liệu của chính lệnh JUMP đó.",
        "remedy": "Chỉ định một số dữ liệu định vị khác làm đích đến cho lệnh JUMP.",
        "source": "4"
    },
    {
        "code": "503",
        "type": "Lỗi",
        "description": "Không có tốc độ lệnh",
        "cause": "Tốc độ lệnh được đặt là -1 cho dữ liệu định vị đầu tiên khi bắt đầu, hoặc không có giá trị tốc độ hợp lệ.",
        "remedy": "Đặt giá trị tốc độ lệnh cụ thể (khác -1) cho điểm định vị đầu tiên.",
        "source": "4"
    },
    {
        "code": "504",
        "type": "Lỗi",
        "description": "Nằm ngoài phạm vi lượng di chuyển tuyến tính",
        "cause": "Lượng di chuyển vượt quá 1073741824 khi sử dụng tốc độ tổng hợp trong điều khiển nội suy.",
        "remedy": "Giảm lượng di chuyển của mỗi trục nội suy hoặc không sử dụng tốc độ tổng hợp.",
        "source": "4"
    },
    {
        "code": "506",
        "type": "Lỗi",
        "description": "Sai lệch lỗi cung tròn lớn",
        "cause": "Lỗi tính toán đường dẫn cung tròn vượt quá phạm vi cho phép được thiết lập.",
        "remedy": "Kiểm tra địa chỉ bắt đầu, địa chỉ kết thúc và địa chỉ cung tròn. Tăng giá trị 'Allowable circular interpolation error width' (Pr.41) nếu cần.",
        "source": "4"
    },
    {
        "code": "507",
        "type": "Lỗi",
        "description": "Giới hạn hành trình phần mềm +",
        "cause": "Địa chỉ đích hoặc vị trí hiện tại vượt quá giới hạn hành trình phần mềm trên.",
        "remedy": "Kiểm tra dữ liệu định vị và thiết lập giới hạn hành trình phần mềm.",
        "source": "4"
    },
    {
        "code": "508",
        "type": "Lỗi",
        "description": "Giới hạn hành trình phần mềm -",
        "cause": "Địa chỉ đích hoặc vị trí hiện tại vượt quá giới hạn hành trình phần mềm dưới.",
        "remedy": "Kiểm tra dữ liệu định vị và thiết lập giới hạn hành trình phần mềm.",
        "source": "4"
    },
    {
        "code": "513",
        "type": "Cảnh báo",
        "description": "Khoảng cách di chuyển không đủ",
        "cause": "Khoảng cách di chuyển quá nhỏ so với tốc độ đích, không đủ để thực hiện giảm tốc tự động.",
        "remedy": "Giảm tốc độ lệnh hoặc tăng khoảng cách di chuyển.",
        "source": "4"
    },
    {
        "code": "514",
        "type": "Lỗi",
        "description": "Nằm ngoài phạm vi giá trị hiện tại mới",
        "cause": "Giá trị thay đổi hiện tại mới nằm ngoài phạm vi cho phép (0 đến 359.99999 khi đơn vị là độ).",
        "remedy": "Đặt giá trị hiện tại mới trong phạm vi quy định.",
        "source": "4"
    },
    {
        "code": "515",
        "type": "Lỗi",
        "description": "Giá trị hiện tại mới không khả thi",
        "cause": "Cố gắng thay đổi giá trị hiện tại trong khi đang thực hiện điều khiển đường dẫn liên tục.",
        "remedy": "Không thực hiện thay đổi giá trị hiện tại trong chế độ điều khiển đường dẫn liên tục.",
        "source": "4"
    },
    {
        "code": "516",
        "type": "Lỗi",
        "description": "Điều khiển đường dẫn liên tục không khả thi",
        "cause": "Thiết lập điều khiển đường dẫn liên tục cho các phương pháp không hỗ trợ như Fixed-feed hoặc Speed-position switching.",
        "remedy": "Thay đổi mẫu vận hành hoặc phương pháp điều khiển cho phù hợp.",
        "source": "4"
    },
    {
        "code": "519",
        "type": "Lỗi",
        "description": "Nội suy trong khi trục nội suy đang bận (BUSY)",
        "cause": "Trục tham chiếu cố gắng bắt đầu nội suy trong khi trục nội suy đi kèm đang bận.",
        "remedy": "Đảm bảo trục nội suy không bận trước khi bắt đầu điều khiển từ trục tham chiếu.",
        "source": "4"
    },
    {
        "code": "521",
        "type": "Lỗi",
        "description": "Lệnh mô tả nội suy không hợp lệ",
        "cause": "Trục nội suy được đặt trùng với trục tham chiếu hoặc kết hợp trục không hợp lệ.",
        "remedy": "Chỉ định một trục khác làm trục nội suy.",
        "source": "4"
    },
    {
        "code": "523",
        "type": "Lỗi",
        "description": "Lỗi chế độ nội suy",
        "cause": "Chỉ định tốc độ tổng hợp cho các chế độ chỉ hỗ trợ tốc độ trục tham chiếu (ví dụ: nội suy 4 trục hoặc điều khiển tốc độ).",
        "remedy": "Đặt lại 'Interpolation speed designation method' thành tốc độ trục tham chiếu.",
        "source": "4"
    },
    {
        "code": "524",
        "type": "Lỗi",
        "description": "Lỗi thiết lập hệ thống điều khiển",
        "cause": "Thay đổi số lượng trục nội suy hoặc kết hợp trục ở giữa dữ liệu định vị liên tục, hoặc thiết lập không được hỗ trợ trên phần cứng cũ.",
        "remedy": "Giữ nguyên các trục nội suy trong suốt chuỗi dữ liệu liên tục. Kiểm tra phiên bản module.",
        "source": "4"
    },
    {
        "code": "525",
        "type": "Lỗi",
        "description": "Lỗi thiết lập điểm phụ",
        "cause": "Điểm phụ cung tròn trùng với điểm bắt đầu/kết thúc, hoặc nằm ngoài phạm vi, hoặc 3 điểm nằm trên đường thẳng.",
        "remedy": "Chỉnh sửa địa chỉ điểm phụ (arc address) sao cho nó tạo thành một cung tròn hợp lệ.",
        "source": "4"
    },
    {
        "code": "526",
        "type": "Lỗi",
        "description": "Lỗi thiết lập điểm kết thúc",
        "cause": "Điểm kết thúc trùng với điểm bắt đầu trong nội suy cung tròn (trừ khi cố ý quay vòng tròn đầy đủ) hoặc nằm ngoài phạm vi.",
        "remedy": "Chỉnh sửa địa chỉ kết thúc (positioning address).",
        "source": "4"
    },
    {
        "code": "527",
        "type": "Lỗi",
        "description": "Lỗi thiết lập điểm trung tâm",
        "cause": "Điểm trung tâm trùng với điểm bắt đầu hoặc điểm kết thúc, hoặc nằm ngoài phạm vi.",
        "remedy": "Chỉnh sửa địa chỉ trung tâm cung tròn (arc address).",
        "source": "4"
    },
    {
        "code": "530",
        "type": "Lỗi",
        "description": "Nằm ngoài phạm vi địa chỉ",
        "cause": "Lượng di chuyển được đặt là giá trị âm trong điều khiển chuyển đổi Tốc độ-Vị trí.",
        "remedy": "Chỉ sử dụng giá trị dương cho lượng di chuyển sau khi chuyển đổi.",
        "source": "4"
    },
    {
        "code": "533",
        "type": "Lỗi",
        "description": "Lỗi dữ liệu điều kiện",
        "cause": "Tham số 1 (P1) lớn hơn tham số 2 (P2) trong thiết lập phạm vi của dữ liệu điều kiện.",
        "remedy": "Đặt P1 nhỏ hơn hoặc bằng P2.",
        "source": "4"
    },
    {
        "code": "535",
        "type": "Lỗi",
        "description": "Nội suy cung tròn không khả thi",
        "cause": "Cố gắng thực hiện nội suy cung tròn khi đơn vị điều khiển được đặt là 'độ'.",
        "remedy": "Thay đổi đơn vị điều khiển sang mm, inch hoặc pulse để thực hiện nội suy cung tròn.",
        "source": "4"
    },
    {
        "code": "536",
        "type": "Lỗi",
        "description": "Bắt đầu khi tín hiệu M code đang Bật",
        "cause": "Một thao tác định vị mới được bắt đầu trong khi tín hiệu M code ON của trục đó vẫn đang Bật.",
        "remedy": "Tắt tín hiệu M code bằng lệnh 'M code OFF request' trước khi bắt đầu định vị mới.",
        "source": "4"
    },
    {
        "code": "543",
        "type": "Lỗi",
        "description": "Nằm ngoài phạm vi số bắt đầu",
        "cause": "Sử dụng số bắt đầu khối (7000-7004) khi đang thực hiện chức năng bắt đầu đọc trước (Pre-reading).",
        "remedy": "Sử dụng số bắt đầu từ 1-600 khi thực hiện chức năng đọc trước.",
        "source": "4"
    },
    {
        "code": "544",
        "type": "Lỗi",
        "description": "Nằm ngoài phạm vi bán kính",
        "cause": "Bán kính cung tròn tính toán vượt quá 536870912.",
        "remedy": "Điều chỉnh các điểm bắt đầu, kết thúc hoặc điểm phụ để giảm bán kính cung tròn.",
        "source": "4"
    },
    {
        "code": "545",
        "type": "Lỗi",
        "description": "Lỗi thiết lập vòng lặp (LOOP)",
        "cause": "Số chu kỳ lặp lại được đặt là 0.",
        "remedy": "Đặt số chu kỳ lặp lại là một số nguyên dương (1-65535).",
        "source": "4"
    },
    {
        "code": "546",
        "type": "Lỗi",
        "description": "Thiết lập hướng ABS trong đơn vị độ không hợp lệ",
        "cause": "Chỉ định hướng quay ABS trong khi giới hạn hành trình phần mềm đang có hiệu lực.",
        "remedy": "Vô hiệu hóa giới hạn hành trình phần mềm (đặt giới hạn trên = giới hạn dưới) trước khi sử dụng chức năng chỉ định hướng quay ABS.",
        "source": "4"
    },
    {
        "code": "805",
        "type": "Lỗi",
        "description": "Lỗi số lần ghi Flash ROM",
        "cause": "Số lần ghi vào Flash ROM vượt quá 25 lần kể từ khi bật nguồn.",
        "remedy": "Hạn chế số lần ghi vào Flash ROM. Reset lỗi để xóa bộ đếm tạm thời.",
        "source": "4"
    },
    {
        "code": "910",
        "type": "Lỗi",
        "description": "Nằm ngoài phạm vi giới hạn tốc độ",
        "cause": "Tốc độ OPR hoặc tốc độ định vị vượt quá giá trị giới hạn tốc độ được thiết lập trong tham số.",
        "remedy": "Điều chỉnh tốc độ lệnh hoặc tăng giá trị 'Speed limit value' (Pr.8).",
        "source": "4"
    },
    {
        "code": "935",
        "type": "Lỗi",
        "description": "Lỗi lựa chọn chức năng Tốc độ-Vị trí",
        "cause": "Thiết lập sai kết hợp giữa đơn vị, giới hạn hành trình hoặc cập nhật giá trị hiện tại trong chế độ ABS.",
        "remedy": "Đảm bảo đơn vị là 'độ', giới hạn hành trình phần mềm bị vô hiệu hóa và cập nhật giá trị hiện tại được bật.",
        "source": "4"
    },
    {
        "code": "956",
        "type": "Lỗi",
        "description": "Lỗi giới hạn tốc độ JOG",
        "cause": "Giá trị giới hạn tốc độ JOG được đặt cao hơn giới hạn tốc độ hệ thống (Pr.8).",
        "remedy": "Đặt giới hạn tốc độ JOG nhỏ hơn hoặc bằng 'Speed limit value' (Pr.8).",
        "source": "4"
    },
    {
        "code": "4100",
        "type": "Lỗi",
        "description": "Lỗi giá trị (s) ngoài phạm vi.",
        "cause": "Giá trị BCD cho hướng dẫn BCD(P) không nằm trong phạm vi 0 đến 9999 hoặc bộ chia (s2) là 0; hoặc số lượng dữ liệu âm được chỉ định.",
        "remedy": "Kiểm tra lại các tham số đầu vào và đảm bảo giá trị nằm trong phạm vi tài liệu cho phép.",
        "source": "5"
    },
    {
        "code": "4101",
        "type": "Lỗi",
        "description": "Lỗi vượt quá phạm vi thiết bị.",
        "cause": "Phạm vi thiết bị được chỉ định vượt quá giới hạn của bộ nhớ module điều khiển hoặc chồng lấn vùng nhớ.",
        "remedy": "Điều chỉnh địa chỉ thiết bị hoặc số lượng điểm dữ liệu để không vượt quá dải địa chỉ của module.",
        "source": "5"
    },
    {
        "code": "4140",
        "type": "Lỗi",
        "description": "Lỗi dữ liệu số thực dấu phẩy động.",
        "cause": "Giá trị của thiết bị được chỉ định là -0, số không chuẩn (unnormalized), không phải là số (nonnumeric) hoặc vô cùng.",
        "remedy": "Kiểm tra tính hợp lệ của dữ liệu số thực trước khi thực hiện các phép toán so sánh hoặc chuyển đổi.",
        "source": "5"
    },
    {
        "code": "4141",
        "type": "Lỗi",
        "description": "Lỗi tràn số (Overflow).",
        "cause": "Kết quả của phép toán vượt quá phạm vi lưu trữ của số thực dấu phẩy động 32-bit hoặc 64-bit.",
        "remedy": "Kiểm tra thuật toán tính toán để đảm bảo kết quả nằm trong phạm vi hiển thị của hệ thống.",
        "source": "5"
    },
    {
        "code": "4200",
        "type": "Lỗi",
        "description": "Lỗi cấu trúc chương trình (FOR-NEXT).",
        "cause": "Lệnh FEND, GOEND hoặc STOP được thực hiện bên trong vòng lặp FOR-NEXT.",
        "remedy": "Đảm bảo các lệnh kết thúc chương trình nằm ngoài các cấu trúc lặp.",
        "source": "5"
    },
    {
        "code": "4210",
        "type": "Lỗi",
        "description": "Lỗi con trỏ (Pointer).",
        "cause": "Số con trỏ không tồn tại trong chương trình hoặc nhảy đến con trỏ ở file chương trình khác.",
        "remedy": "Khai báo lại nhãn con trỏ (P) chính xác trong cùng một file chương trình.",
        "source": "5"
    },
    {
        "code": "4235",
        "type": "Lỗi",
        "description": "Lỗi lệnh CHK.",
        "cause": "Sử dụng quá 150 tiếp điểm, lệnh CHK không đúng vị trí sau CHKST hoặc dùng lệnh CHK ở quá 2 vị trí trong file chương trình.",
        "remedy": "Xem lại sơ đồ thang (ladder) và tuân thủ các quy tắc lập trình lệnh chẩn đoán lỗi CHK.",
        "source": "5"
    },
    {
        "code": "1402",
        "type": "Lỗi",
        "description": "Lỗi phát hiện tại module chức năng thông minh.",
        "cause": "Module được chỉ định bởi n1 gặp sự cố trong quá trình thực hiện lệnh FROM/TO.",
        "remedy": "Kiểm tra trạng thái phần cứng của module và cáp kết nối.",
        "source": "5"
    },
    {
        "code": "2410",
        "type": "Lỗi",
        "description": "Tên file không tồn tại.",
        "cause": "File được chỉ định trong lệnh QDRSET hoặc SP_FWRITE không tồn tại trong ổ đĩa.",
        "remedy": "Kiểm tra lại tên file và số hiệu ổ đĩa (Drive No.) đã được cài đặt.",
        "source": "5"
    },
    {
        "code": "100",
        "type": "Lỗi",
        "description": "Vượt quá số lần thử lại ENQ.",
        "cause": "Nhiễu hệ thống hoặc sự cố đường truyền.",
        "remedy": "Thực hiện các biện pháp chống nhiễu.",
        "source": "6"
    },
    {
        "code": "102",
        "type": "Lỗi",
        "description": "Vượt quá số lần thử lại NACK.",
        "cause": "Lỗi phản hồi từ thiết bị.",
        "remedy": "Kiểm tra lại thiết bị kết nối và nhiễu.",
        "source": "6"
    },
    {
        "code": "103",
        "type": "Lỗi",
        "description": "Thông điệp quá dài.",
        "cause": "Kích thước gói tin vượt quá giới hạn cho phép.",
        "remedy": "Kiểm tra và điều chỉnh lại độ dài thông điệp.",
        "source": "6"
    },
    {
        "code": "104",
        "type": "Lỗi",
        "description": "Hết thời gian chờ nhận dữ liệu (Reception time-out).",
        "cause": "Không nhận được phản hồi trong thời gian quy định.",
        "remedy": "Kiểm tra kết nối cáp.",
        "source": "6"
    },
    {
        "code": "105",
        "type": "Lỗi",
        "description": "Không phát hiện tín hiệu DSR.",
        "cause": "Tín hiệu Data Set Ready bị mất.",
        "remedy": "Kiểm tra trạng thái thiết bị và cáp nối.",
        "source": "6"
    },
    {
        "code": "106",
        "type": "Lỗi",
        "description": "Đường truyền bị ngắt kết nối.",
        "cause": "Cáp bị lỏng hoặc đứt, hoặc thiết bị ngoại vi bị tắt nguồn.",
        "remedy": "Kiểm tra kết nối cáp. Thực hiện mở lại (Open) cổng truyền thông.",
        "source": "6"
    },
    {
        "code": "107",
        "type": "Lỗi",
        "description": "Hết thời gian chờ truyền dữ liệu (Transmission time-out).",
        "cause": "Không thể gửi dữ liệu đi trong thời gian cho phép.",
        "remedy": "Kiểm tra kết nối cáp.",
        "source": "6"
    },
    {
        "code": "108",
        "type": "Lỗi",
        "description": "Số thứ tự (Sequence number) không chính xác.",
        "cause": "Dữ liệu bị sai lệch do nhiễu.",
        "remedy": "Thực hiện các biện pháp chống nhiễu.",
        "source": "6"
    },
    {
        "code": "0x01010002",
        "type": "Lỗi",
        "description": "Lỗi hết thời gian chờ (Time-out error).",
        "cause": "Cáp hỏng, cài đặt thông số sai hoặc PLC không phản hồi.",
        "remedy": "Kiểm tra thuộc tính timeout, cài đặt trong tiện ích truyền thông, kiểm tra PLC, cài đặt module và cáp. Thử đóng và mở lại kết nối.",
        "source": "6"
    },
    {
        "code": "0x01010010",
        "type": "Lỗi",
        "description": "Lỗi số trạm PLC (Programmable controller No. error).",
        "cause": "Không thể giao tiếp với số trạm đã chỉ định.",
        "remedy": "Kiểm tra lại số trạm đã thiết lập trong Communication Setup Utility và thuộc tính ActStationNumber.",
        "source": "6"
    },
    {
        "code": "0x01802001",
        "type": "Lỗi",
        "description": "Lỗi thiết bị (Device error).",
        "cause": "Chuỗi ký tự thiết bị được chỉ định trong phương thức không hợp lệ.",
        "remedy": "Xem lại tên thiết bị đã nhập.",
        "source": "6"
    },
    {
        "code": "0x01802002",
        "type": "Lỗi",
        "description": "Lỗi số thiết bị (Device number error).",
        "cause": "Số của thiết bị được chỉ định không hợp lệ.",
        "remedy": "Xem lại số thứ tự thiết bị.",
        "source": "6"
    },
    {
        "code": "0x01802005",
        "type": "Lỗi",
        "description": "Lỗi kích thước (Size error).",
        "cause": "Số điểm (points) được chỉ định không hợp lệ.",
        "remedy": "Kiểm tra lại số điểm đã chỉ định trong phương thức; kiểm tra cài đặt module và trạng thái cáp.",
        "source": "6"
    },
    {
        "code": "0x0180840B",
        "type": "Lỗi",
        "description": "Lỗi hết thời gian chờ (Time-out error).",
        "cause": "Hết thời gian chờ nhưng không nhận được dữ liệu.",
        "remedy": "Xem lại giá trị timeout, kiểm tra kết nối bằng lệnh Ping, kiểm tra PLC và module.",
        "source": "6"
    },
    {
        "code": "0xF1000001",
        "type": "Lỗi",
        "description": "Lỗi chuyển đổi mã ký tự (Character code conversion error).",
        "cause": "Chuyển đổi giữa UNICODE và mã ASCII thất bại.",
        "remedy": "Kiểm tra chuỗi ký tự chỉ định trong phương thức; kiểm tra lại hệ thống và cáp.",
        "source": "6"
    },
    {
        "code": "1160",
        "type": "Lỗi (Stop)",
        "description": "RAM ERROR (Lỗi chương trình)",
        "cause": "Dữ liệu chương trình đang thực thi không khớp với chương trình được ghi trong bộ nhớ chương trình do nhiễu hoặc hỏng bộ nhớ.",
        "remedy": "Thực hiện chức năng tự động phục hồi bộ nhớ đệm hoặc ghi lại chương trình vào bộ nhớ CPU. Kiểm tra môi trường hoạt động chống nhiễu.",
        "source": "7"
    },
    {
        "code": "1161",
        "type": "Lỗi (Stop)",
        "description": "RAM ERROR (Lỗi bộ nhớ thiết bị)",
        "cause": "CPU phát hiện sự thay đổi dữ liệu trong bộ nhớ thiết bị.",
        "remedy": "Ghi lại dữ liệu thiết bị hoặc reset CPU. Kiểm tra thông tin thay đổi dữ liệu trong SD927 và SD928.",
        "source": "7"
    },
    {
        "code": "1610",
        "type": "Cảnh báo/Lỗi",
        "description": "FLASH ROM ERROR",
        "cause": "Số lần ghi vào Standard ROM vượt quá 100,000 lần.",
        "remedy": "Thay thế CPU nếu cần thiết và hạn chế ghi dữ liệu vào ROM thường xuyên.",
        "source": "7"
    },
    {
        "code": "2200",
        "type": "Lỗi",
        "description": "MISSING PARA (Thiếu tham số)",
        "cause": "CPU bị khóa bởi khóa bảo mật và các tham số được lưu trong thẻ nhớ (SD) nhưng không có tham số trong bộ nhớ chương trình.",
        "remedy": "Kiểm tra lại vị trí lưu trữ tham số và cài đặt khóa bảo mật.",
        "source": "7"
    },
    {
        "code": "2213",
        "type": "Lỗi",
        "description": "BOOT ERROR (Lỗi khởi động)",
        "cause": "Có nhiều tệp khởi động nhưng mật khẩu tệp không khớp.",
        "remedy": "Kiểm tra lại mật khẩu tệp trong phần cài đặt mật khẩu tệp 32 ký tự.",
        "source": "7"
    },
    {
        "code": "2214",
        "type": "Lỗi",
        "description": "BOOT ERROR (Lỗi khởi động)",
        "cause": "Thực hiện thao tác khởi động (boot) trong khi CPU đang bị khóa bởi khóa bảo mật.",
        "remedy": "Mở khóa CPU bằng khóa bảo mật trước khi thực hiện khởi động.",
        "source": "7"
    },
    {
        "code": "2220",
        "type": "Lỗi",
        "description": "RESTORE ERROR (Lỗi phục hồi)",
        "cause": "Số lượng điểm thiết bị trong cài đặt tham số khác với số lượng tại thời điểm sao lưu.",
        "remedy": "Khôi phục lại trạng thái dữ liệu khi sao lưu tham số hoặc xóa dữ liệu sao lưu cũ và thực hiện sao lưu lại.",
        "source": "7"
    },
    {
        "code": "2221",
        "type": "Lỗi",
        "description": "RESTORE ERROR (Lỗi phục hồi)",
        "cause": "CPU bị mất điện hoặc bị reset trong quá trình sao lưu dữ liệu chốt (latch).",
        "remedy": "Thực hiện sao lưu lại dữ liệu và đảm bảo nguồn điện ổn định.",
        "source": "7"
    },
    {
        "code": "2225",
        "type": "Lỗi",
        "description": "RESTORE ERROR (Lỗi phục hồi)",
        "cause": "Model của CPU đích khác với CPU nguồn đã sao lưu.",
        "remedy": "Đảm bảo CPU đích và nguồn có cùng model.",
        "source": "7"
    },
    {
        "code": "2226",
        "type": "Lỗi",
        "description": "RESTORE ERROR (Lỗi phục hồi)",
        "cause": "Tệp sao lưu bị hỏng hoặc công tắc chống ghi trên thẻ nhớ đang bật.",
        "remedy": "Kiểm tra tính toàn vẹn của tệp hoặc tắt công tắc chống ghi trên thẻ nhớ.",
        "source": "7"
    },
    {
        "code": "2228",
        "type": "Lỗi",
        "description": "RESTORE ERROR (Không đủ bộ nhớ)",
        "cause": "Dung lượng trống của Standard RAM trên CPU đích không đủ để phục hồi dữ liệu sao lưu.",
        "remedy": "Giải phóng bộ nhớ Standard RAM hoặc lắp thêm thẻ nhớ SRAM mở rộng phù hợp.",
        "source": "7"
    },
    {
        "code": "3000",
        "type": "Lỗi",
        "description": "PARAMETER ERROR (Lỗi tham số)",
        "cause": "Cài đặt sai tổ hợp loại module gắn trên đế so với bảng gán I/O hoặc gán thiết bị cục bộ sai.",
        "remedy": "Kiểm tra lại cài đặt I/O Assignment và File Usability Setting trong PLC Parameter.",
        "source": "7"
    },
    {
        "code": "3002",
        "type": "Lỗi",
        "description": "PARAMETER ERROR (Lỗi tệp thanh ghi)",
        "cause": "Tệp thanh ghi tệp (File register) được chỉ định không tồn tại trên ổ đĩa hoặc chọn sai ổ đĩa lưu trữ (như chọn Standard ROM cho thanh ghi ghi được).",
        "remedy": "Tạo tệp thanh ghi tệp đúng tên và đúng ổ đĩa trong cài đặt PLC File.",
        "source": "7"
    },
    {
        "code": "4100",
        "type": "Lỗi vận hành (Operation Error)",
        "description": "OPERATION ERROR",
        "cause": "Ghi dữ liệu thời gian (DATEWR) ngoài dải cho phép hoặc chuyển đổi thanh ghi tệp không hợp lệ.",
        "remedy": "Kiểm tra giá trị đầu vào cho lệnh và cài đặt thanh ghi tệp.",
        "source": "7"
    },
    {
        "code": "4101",
        "type": "Lỗi vận hành (Operation Error)",
        "description": "OPERATION ERROR",
        "cause": "Truy cập thanh ghi tệp vượt quá kích thước đã đăng ký hoặc số thiết bị vượt quá dải cài đặt do hiệu chỉnh chỉ số (index modification).",
        "remedy": "Kiểm tra kích thước tệp thanh ghi đã đăng ký (SD647) và giới hạn index modification.",
        "source": "7"
    },
    {
        "code": "4109",
        "type": "Lỗi",
        "description": "Online communication timeout",
        "cause": "Xung đột khi nhiều ứng dụng truy cập cùng một lộ trình giao tiếp trong khi đang đặt điều kiện giám sát.",
        "remedy": "Đảm bảo chỉ có một ứng dụng thực hiện giám sát có điều kiện hoặc kiểm tra lại lộ trình kết nối.",
        "source": "7"
    },
    {
        "code": "5010",
        "type": "Cảnh báo (Continue)",
        "description": "PRG. TIME OVER",
        "cause": "Thời gian quét (scan time) thực tế dài hơn thời gian quét không đổi (constant scan) đã cài đặt.",
        "remedy": "Tăng giá trị cài đặt Constant Scan trong PLC RAS tab hoặc tối ưu hóa chương trình.",
        "source": "7"
    },
    {
        "code": "11",
        "type": "Lỗi (Protective Function)",
        "description": "Bảo vệ sụt áp nguồn điều khiển",
        "cause": "Điện áp nguồn thấp; Mất điện tức thời; Thiếu công suất nguồn do dòng khởi động khi bật nguồn chính; Lỗi driver.",
        "remedy": "Đo điện áp tại L1C và L2C; Tăng công suất nguồn; Thay thế driver mới.",
        "source": "8"
    },
    {
        "code": "12",
        "type": "Lỗi (Protective Function)",
        "description": "Bảo vệ quá áp",
        "cause": "Điện áp nguồn vượt quá mức cho phép; Tăng vọt điện áp do tụ bù hoặc UPS; Đứt dây điện trở xả; Điện trở xả ngoài không phù hợp; Lỗi driver.",
        "remedy": "Đo điện áp L1, L2, L3; Nhập điện áp đúng; Kiểm tra điện trở xả ngoài (thay thế nếu giá trị là vô hạn); Thay đổi điện trở xả phù hợp; Thay driver.",
        "source": "8"
    },
    {
        "code": "13",
        "type": "Lỗi (Protective Function)",
        "description": "Bảo vệ sụt áp nguồn chính",
        "cause": "Mất điện tức thời lâu hơn cài đặt Pr6D; Điện áp nguồn chính thấp; Thiếu công suất nguồn; Mất pha (đầu vào 1 pha cho driver 3 pha); Lỗi driver.",
        "remedy": "Đo điện áp L1, L2, L3; Tăng công suất nguồn; Kiểm tra cài đặt Pr6D; Kết nối đúng các pha nguồn.",
        "source": "8"
    },
    {
        "code": "14",
        "type": "Lỗi (Protective Function)",
        "description": "Bảo vệ quá dòng",
        "cause": "Lỗi driver (mạch, IGBT); Ngắn mạch dây động cơ (U, V, W); Lỗi chạm đất; Cháy động cơ; Tiếp xúc dây kém; Vận hành Servo-ON/OFF quá thường xuyên; Quá nhiệt mạch phanh động năng (F-frame).",
        "remedy": "Ngắt kết nối động cơ và kiểm tra driver; Kiểm tra ngắn mạch và đấu dây động cơ; Đo điện trở cách điện; Kiểm tra sự cân bằng điện trở các pha; Thay driver/động cơ.",
        "source": "8"
    },
    {
        "code": "15",
        "type": "Lỗi (Protective Function)",
        "description": "Bảo vệ quá nhiệt",
        "cause": "Nhiệt độ tản nhiệt hoặc thiết bị công suất vượt mức; Nhiệt độ môi trường quá cao; Quá tải.",
        "remedy": "Cải thiện nhiệt độ môi trường và điều kiện làm việc; Tăng công suất driver và động cơ; Tăng thời gian tăng/giảm tốc.",
        "source": "8"
    },
    {
        "code": "16",
        "type": "Lỗi (Protective Function)",
        "description": "Bảo vệ quá tải",
        "cause": "Tải quá nặng vượt định mức trong thời gian dài; Cài đặt thông số (Pr20) không đúng gây rung lắc; Đấu dây sai; Phanh điện từ vẫn đóng; Va chạm máy hoặc kẹt cơ khí.",
        "remedy": "Tăng công suất driver/động cơ; Điều chỉnh lại thông số; Kiểm tra sơ đồ đấu dây (U, V, W); Kiểm tra cơ khí và phanh; Đặt lại Pr72 về 0.",
        "source": "8"
    },
    {
        "code": "18",
        "type": "Lỗi (Protective Function)",
        "description": "Bảo vệ quá tải xả (quá tái sinh)",
        "cause": "Năng lượng tái sinh vượt quá khả năng của điện trở xả; Quán tính tải lớn; Tốc độ động cơ quá cao; Giới hạn hoạt động điện trở xả ngoài bị vượt quá.",
        "remedy": "Kiểm tra hệ số tải xả trên monitor; Tăng thời gian giảm tốc; Hạ tốc độ động cơ; Sử dụng điện trở xả ngoài và cài đặt Pr6C.",
        "source": "8"
    },
    {
        "code": "21",
        "type": "Lỗi (Protective Function)",
        "description": "Lỗi giao tiếp bộ mã hóa (Encoder)",
        "cause": "Giao tiếp giữa bộ mã hóa và driver bị gián đoạn; Phát hiện đứt dây; Lỗi kết nối chân connector.",
        "remedy": "Đấu dây lại theo sơ đồ; Kiểm tra nguồn cấp encoder DC 5V; Tách riêng cáp encoder và cáp động cơ; Kết nối vỏ chống nhiễu với FG.",
        "source": "8"
    },
    {
        "code": "24",
        "type": "Lỗi (Protective Function)",
        "description": "Bảo vệ quá lệch vị trí",
        "cause": "Động cơ không theo kịp lệnh; Chênh lệch xung vượt cài đặt Pr70; Điều chỉnh gain kém; Momen đầu ra bị giới hạn (Pr5E/5F).",
        "remedy": "Kiểm tra động cơ theo xung lệnh; Kiểm tra momen đầu ra; Điều chỉnh gain; Tăng giá trị Pr70 hoặc đặt về 0 (vô hiệu hóa).",
        "source": "8"
    },
    {
        "code": "25",
        "type": "Lỗi (Protective Function)",
        "description": "Bảo vệ lỗi lệch lai (Hybrid deviation)",
        "cause": "Vị trí tải (thước đo ngoài) và vị trí động cơ lệch quá cài đặt Pr7B; Kết nối giữa động cơ và tải lỏng lẻo; Cài đặt tỷ lệ thước ngoài sai.",
        "remedy": "Kiểm tra kết nối động cơ và tải; Kiểm tra chiều thước đo và cài đặt thông số Pr78, 79, 7A, 7C.",
        "source": "8"
    },
    {
        "code": "Over-regeneration alarm",
        "type": "Cảnh báo (Alarm)",
        "description": "Cảnh báo quá tái sinh",
        "cause": "Tải tái sinh đạt hơn 85% mức kích hoạt bảo vệ quá tái sinh.",
        "remedy": "Kiểm tra điều kiện hoạt động, giảm quán tính hoặc kéo dài thời gian giảm tốc.",
        "source": "8"
    },
    {
        "code": "Overload alarm",
        "type": "Cảnh báo (Alarm)",
        "description": "Cảnh báo quá tải",
        "cause": "Tải đạt hơn 85% mức kích hoạt bảo vệ quá tải.",
        "remedy": "Kiểm tra cơ khí, giảm tải hoặc tăng công suất động cơ.",
        "source": "8"
    },
    {
        "code": "Battery alarm",
        "type": "Cảnh báo (Alarm)",
        "description": "Cảnh báo pin",
        "cause": "Điện áp pin cho bộ mã hóa tuyệt đối giảm xuống dưới mức cảnh báo (khoảng 3.2V).",
        "remedy": "Thay pin mới cho bộ mã hóa tuyệt đối.",
        "source": "8"
    },
    {
        "code": "7101H",
        "type": "Lỗi",
        "description": "Lỗi hệ thống",
        "cause": "Hệ điều hành (OS) của module Q series C24 phát hiện thấy một số lỗi.",
        "remedy": "Kiểm tra tình trạng lắp đặt module, nguồn điện và môi trường hoạt động. Nếu lỗi vẫn tiếp tục, hãy liên hệ đại diện Mitsubishi.",
        "source": "9"
    },
    {
        "code": "7103H",
        "type": "Lỗi",
        "description": "Lỗi truy cập bộ điều khiển lập trình",
        "cause": "Không thể giao tiếp với CPU của module Q series C24.",
        "remedy": "Tăng thời gian watchdog timer (timer 1). Thực hiện kiểm tra self-loopback để kiểm tra CPU.",
        "source": "9"
    },
    {
        "code": "7140H",
        "type": "Lỗi",
        "description": "Lỗi dữ liệu yêu cầu",
        "cause": "Số lượng điểm yêu cầu vượt quá phạm vi lệnh hoặc thiết bị từ xa không hợp lệ.",
        "remedy": "Kiểm tra và sửa tin nhắn truyền của thiết bị ngoại vi. Xóa thông tin CPU và thử lại.",
        "source": "9"
    },
    {
        "code": "7142H",
        "type": "Lỗi",
        "description": "Lỗi tên thiết bị",
        "cause": "Một thiết bị không thể định danh bởi lệnh đã cho đã được chỉ định.",
        "remedy": "Kiểm tra và sửa tin nhắn truyền của thiết bị ngoại vi. Xóa thông tin CPU và thử lại.",
        "source": "9"
    },
    {
        "code": "714AH",
        "type": "Lỗi",
        "description": "Không thể thực hiện lệnh khi đang RUN",
        "cause": "Lệnh ghi được chỉ định khi thiết lập 'Cấm ghi khi đang RUN'.",
        "remedy": "Thay đổi cài đặt thành 'Cho phép ghi khi đang RUN' hoặc dừng CPU trước khi truyền dữ liệu.",
        "source": "9"
    },
    {
        "code": "7D00H",
        "type": "Lỗi",
        "description": "Lỗi cài đặt số hiệu giao thức (Protocol No.)",
        "cause": "Trong dữ liệu điều khiển của lệnh CPRTCL, số hiệu giao thức chỉ định nằm ngoài phạm vi.",
        "remedy": "Chỉnh sửa lại cài đặt số hiệu giao thức.",
        "source": "9"
    },
    {
        "code": "7D12H",
        "type": "Lỗi",
        "description": "Lỗi quá thời gian giám sát truyền dẫn",
        "cause": "Thời gian giám sát truyền đã hết. Việc truyền dữ liệu không thành công sau số lần thử lại đã chỉ định.",
        "remedy": "Kiểm tra xem truyền dẫn có bị gián đoạn do kiểm soát DTR không. Kiểm tra tín hiệu CS và cáp kết nối.",
        "source": "9"
    },
    {
        "code": "7D13H",
        "type": "Lỗi",
        "description": "Lỗi quá thời gian chờ nhận dữ liệu",
        "cause": "Thời gian chờ nhận đã hết hạn.",
        "remedy": "Kiểm tra cáp kết nối, lỗi ở thiết bị gửi hoặc sử dụng chức năng circuit trace để kiểm tra dữ liệu từ thiết bị khác.",
        "source": "9"
    },
    {
        "code": "7F24H",
        "type": "Lỗi",
        "description": "Lỗi mã kiểm tra tổng (Sum check error)",
        "cause": "Mã kiểm tra tổng tính toán được không khớp với mã nhận được.",
        "remedy": "Kiểm tra mã kiểm tra tổng của thiết bị ngoại vi hoặc cài đặt định dạng gói tin trong GX Configurator-SC.",
        "source": "9"
    },
    {
        "code": "7F31H",
        "type": "Lỗi",
        "description": "Lỗi truyền dẫn đồng thời",
        "cause": "Module C24 và thiết bị ngoại vi bắt đầu truyền dữ liệu cùng một lúc.",
        "remedy": "Xử lý theo thỏa thuận với thiết bị ngoại vi hoặc thay đổi cài đặt chỉ định dữ liệu truyền đồng thời trong buffer memory.",
        "source": "9"
    },
    {
        "code": "7F68H",
        "type": "Lỗi",
        "description": "Lỗi khung hình (Framing error)",
        "cause": "Dữ liệu không khớp với cài đặt stop bit hoặc do nhiễu mạng.",
        "remedy": "Khớp cài đặt giữa module C24 và thiết bị ngoại vi. Thực hiện xóa lỗi qua tín hiệu YE/YF.",
        "source": "9"
    },
    {
        "code": "7F69H",
        "type": "Lỗi",
        "description": "Lỗi chẵn lẻ (Parity error)",
        "cause": "Dữ liệu không khớp với cài đặt parity bit.",
        "remedy": "Khớp cài đặt parity giữa module C24 và thiết bị ngoại vi. Kiểm tra và biện pháp chống nhiễu.",
        "source": "9"
    },
    {
        "code": "7FEFH",
        "type": "Lỗi",
        "description": "Lỗi cài đặt công tắc (Switch setting error)",
        "cause": "Có lỗi trong việc cài đặt công tắc thông qua GX Developer.",
        "remedy": "Chỉnh sửa giá trị cài đặt công tắc trong tham số và khởi động lại PLC.",
        "source": "9"
    },
    {
        "code": "7FF0H",
        "type": "Lỗi",
        "description": "Lỗi thực hiện đồng thời các lệnh chuyên dụng",
        "cause": "Thực hiện các lệnh chuyên dụng đồng thời trên cùng một kênh.",
        "remedy": "Không sử dụng đồng thời các lệnh chuyên dụng trên cùng một kênh giao tiếp.",
        "source": "9"
    },
    {
        "code": "4100",
        "type": "Lỗi",
        "description": "Giá trị đối số n nằm ngoài phạm vi cho phép trong lệnh ghi dữ liệu.",
        "cause": "Giá trị của n nằm ngoài phạm vi từ 1 đến 10 trong lệnh LOGTRG hoặc LOGTRGR.",
        "remedy": "Kiểm tra và điều chỉnh giá trị n để đảm bảo nằm trong phạm vi từ 1 đến 10.",
        "source": "10"
    },
    {
        "code": "Khác 0",
        "type": "Lỗi",
        "description": "Lỗi hoàn thành lệnh phục hồi vị trí tuyệt đối (Z_ABRST).",
        "cause": "Sự cố trong quá trình giao tiếp với bộ khuếch đại servo hoặc thiết lập dữ liệu không hợp lệ.",
        "remedy": "Kiểm tra mã lỗi cụ thể được lưu trữ trong thiết bị (s)10 và tham khảo tài liệu kỹ thuật của bộ điều khiển vị trí.",
        "source": "10"
    },
    {
        "code": "Khác 0",
        "type": "Lỗi",
        "description": "Lỗi khi thực hiện lệnh bắt đầu vị trí (ZP_PSTRT).",
        "cause": "Số bắt đầu (Start No.) không hợp lệ hoặc module đang ở trạng thái lỗi.",
        "remedy": "Kiểm tra mã lỗi trong (s)10 và xác nhận số bắt đầu nằm trong phạm vi cho phép (1-600, 7000-7004, 9001-9004).",
        "source": "10"
    },
    {
        "code": "Khác 0",
        "type": "Lỗi",
        "description": "Lỗi lệnh dạy (Teaching - ZP_TEACH).",
        "cause": "Số dữ liệu vị trí không chính xác hoặc điều kiện thực hiện lệnh không thỏa mãn.",
        "remedy": "Xác minh số dữ liệu vị trí (1 đến 600) và kiểm tra mã lỗi tại (s)10.",
        "source": "10"
    },
    {
        "code": "Khác 0",
        "type": "Lỗi",
        "description": "Lỗi ghi vào Flash ROM (ZP_PFWRT).",
        "cause": "Lỗi phần cứng Flash ROM hoặc module chưa sẵn sàng.",
        "remedy": "Đợi module ở trạng thái sẵn sàng và kiểm tra mã lỗi tại (s)10.",
        "source": "10"
    },
    {
        "code": "Khác 0",
        "type": "Lỗi",
        "description": "Lỗi khởi tạo dữ liệu thiết lập (ZP_PINIT).",
        "cause": "Dữ liệu thiết lập bị hỏng hoặc lỗi bộ nhớ đệm.",
        "remedy": "Kiểm tra mã lỗi tại (s)10 để xác định nguyên nhân chi tiết.",
        "source": "10"
    },
    {
        "code": "C1009",
        "type": "Lỗi chuyển đổi",
        "description": "Tồn tại ký tự không thể phân tích.",
        "cause": "Định dạng sai hoặc sử dụng ký tự không được hỗ trợ (ví dụ: , !, \\",
        "remedy": ").",
        "source": "Chỉnh sửa lại chuỗi ký tự."
    },
    {
        "code": "C1010",
        "type": "Lỗi chuyển đổi",
        "description": "Tồn tại toán tử không thể phân tích.",
        "cause": "Sử dụng toán tử sai quy cách.",
        "remedy": "Chỉnh sửa lại toán tử.",
        "source": "11"
    },
    {
        "code": "C1013",
        "type": "Lỗi chuyển đổi",
        "description": "Hằng số số thực bị sai.",
        "cause": "Mô tả hằng số số thực không hợp lệ (ví dụ: 1., 0.1E).",
        "remedy": "Chỉnh sửa lại mô tả hằng số số thực.",
        "source": "11"
    },
    {
        "code": "C1014",
        "type": "Lỗi chuyển đổi",
        "description": "Mô tả thiết bị (device) bị sai.",
        "cause": "Chỉ định số bit của thiết bị word sai hoặc ký tự thiết bị không hợp lệ.",
        "remedy": "Chỉnh sửa lại mô tả thiết bị.",
        "source": "11"
    },
    {
        "code": "C1018",
        "type": "Lỗi chuyển đổi",
        "description": "Mô tả chú thích (comment) bị sai.",
        "cause": "Không viết đúng định dạng (* *) hoặc thiếu dấu ngoặc/dấu sao.",
        "remedy": "Chỉnh sửa lại mô tả chú thích.",
        "source": "11"
    },
    {
        "code": "C1028",
        "type": "Lỗi chuyển đổi",
        "description": "Biến chưa được định nghĩa.",
        "cause": "Sử dụng nhãn (label) mà không khai báo hoặc dùng sai ký tự trong hệ thập lục phân.",
        "remedy": "Khai báo biến trước khi sử dụng.",
        "source": "11"
    },
    {
        "code": "C1033",
        "type": "Lỗi chuyển đổi",
        "description": "Lỗi chỉ định phần tử mảng.",
        "cause": "Phương pháp chỉ định phần tử mảng sai định dạng so với định nghĩa.",
        "remedy": "Chỉnh sửa lại mô tả mảng.",
        "source": "11"
    },
    {
        "code": "C2021",
        "type": "Lỗi chuyển đổi",
        "description": "Sử dụng sai hằng số trong đối số.",
        "cause": "Sử dụng giá trị khác hằng số cho đối số yêu cầu phải là hằng số.",
        "remedy": "Sử dụng hằng số trong đối số được chỉ định.",
        "source": "11"
    },
    {
        "code": "C2054",
        "type": "Lỗi chuyển đổi",
        "description": "Lỗi cú pháp (Syntax error).",
        "cause": "Mô tả ngữ pháp sai (thiếu dấu =, dùng sai toán tử, sai cấu trúc mảng/cấu trúc điều khiển).",
        "remedy": "Chỉnh sửa lại ngữ pháp cho đúng.",
        "source": "11"
    },
    {
        "code": "C8006",
        "type": "Lỗi chuyển đổi",
        "description": "Thiếu từ khóa kết thúc.",
        "cause": "Thiếu các từ khóa như END_IF, END_FOR, END_WHILE hoặc dấu ;.",
        "remedy": "Thêm từ khóa kết thúc hoặc dấu ; tương ứng.",
        "source": "11"
    },
    {
        "code": "C8021",
        "type": "Lỗi chuyển đổi",
        "description": "Kiểu dữ liệu chỉ số mảng không hợp lệ.",
        "cause": "Sử dụng kiểu dữ liệu khác INT cho số phần tử của biến mảng.",
        "remedy": "Thay đổi kiểu dữ liệu của chỉ số phần tử thành kiểu word (INT).",
        "source": "11"
    },
    {
        "code": "C8022",
        "type": "Lỗi chuyển đổi",
        "description": "Chỉ số mảng vượt quá phạm vi.",
        "cause": "Số phần tử được chỉ định vượt quá phạm vi định nghĩa của mảng.",
        "remedy": "Thay đổi số phần tử nằm trong phạm vi định nghĩa mảng.",
        "source": "11"
    },
    {
        "code": "C9017",
        "type": "Lỗi chuyển đổi",
        "description": "Quá nhiều tầng lồng nhau hoặc điều kiện quá dài.",
        "cause": "Vượt quá giới hạn lồng nhau (ví dụ: IF > 598 cấp, FOR > 299 cấp) hoặc quá nhiều giá trị lựa chọn trong CASE.",
        "remedy": "Rút ngắn chương trình, giảm số lượng cấp lồng nhau hoặc điều kiện.",
        "source": "11"
    },
    {
        "code": "C9065",
        "type": "Lỗi chuyển đổi",
        "description": "Lỗi chia cho số 0.",
        "cause": "Sử dụng 0 làm số chia trong phép toán.",
        "remedy": "Sửa lại phần số chia khác 0.",
        "source": "11"
    },
    {
        "code": "F0102",
        "type": "Lỗi chuyển đổi",
        "description": "Số lượng ký tự vượt quá tối đa.",
        "cause": "Số lượng ký tự sử dụng lớn hơn 32 ký tự.",
        "remedy": "Thay đổi chuỗi ký tự nằm trong phạm vi 32 ký tự.",
        "source": "11"
    },
    {
        "code": "2000",
        "type": "Lỗi (UNIT VERIFY ERROR)",
        "description": "Lỗi xác nhận đơn vị (Unit verify error) xảy ra khi module QCPU phiên bản chức năng A được sử dụng trong hệ thống đa CPU.",
        "cause": "Sử dụng module QCPU phiên bản chức năng A trong hệ thống đa CPU.",
        "remedy": "Để cấu hình hệ thống đa CPU với các QCPU, hãy sử dụng các module CPU phiên bản chức năng B trở lên.",
        "source": "12"
    },
    {
        "code": "2110",
        "type": "Lỗi (SP.UNIT ERROR)",
        "description": "Lỗi module chức năng đặc biệt khi truy cập vào module CPU không được lắp đặt thực tế.",
        "cause": "Truy cập vào module CPU không thực sự được lắp đặt bằng cách sử dụng các lệnh thiết bị vùng truyền cyclic (U3En\\G).",
        "remedy": "Kiểm tra cấu hình lắp đặt thực tế và đảm bảo địa chỉ I/O của CPU trong lệnh là chính xác.",
        "source": "12"
    },
    {
        "code": "2114",
        "type": "Lỗi (SP.UNIT ERROR)",
        "description": "Lỗi module chức năng đặc biệt liên quan đến việc đọc/ghi bộ nhớ chia sẻ CPU trên High Performance QCPU hoặc Process CPU.",
        "cause": "Thực hiện ghi bằng lệnh thiết bị vùng truyền cyclic (U3En\\G) hoặc thực hiện đọc bằng bất kỳ lệnh đọc nào vào bộ nhớ chia sẻ CPU của chính nó trên module High Performance model QCPU hoặc Process CPU.",
        "remedy": "Sử dụng lệnh S.TO để ghi. Lưu ý rằng High Performance model QCPU hoặc Process CPU không hỗ trợ đọc bộ nhớ chia sẻ của chính nó bằng lệnh đọc.",
        "source": "12"
    },
    {
        "code": "2115",
        "type": "Lỗi (SP.UNIT ERROR)",
        "description": "Lỗi module chức năng đặc biệt khi ghi vào bộ nhớ chia sẻ của CPU khác.",
        "cause": "Cố gắng ghi dữ liệu vào bộ nhớ chia sẻ CPU của các module CPU khác bằng lệnh ghi (TO, S.TO hoặc U3En\\G).",
        "remedy": "Không ghi trực tiếp vào bộ nhớ chia sẻ của CPU khác. Dữ liệu chỉ nên được ghi bởi CPU sở hữu bộ nhớ đó và được đọc bởi các CPU khác.",
        "source": "12"
    },
    {
        "code": "2116",
        "type": "Lỗi (SP.UNIT ERROR)",
        "description": "Lỗi module chức năng đặc biệt khi ghi vào bộ nhớ đệm của module điều khiển bởi CPU khác.",
        "cause": "Dữ liệu được ghi vào bộ nhớ đệm của một module chức năng thông minh đang được điều khiển bởi một module CPU khác.",
        "remedy": "Chỉ ghi dữ liệu vào bộ nhớ đệm của module từ CPU được thiết lập là CPU điều khiển (Control CPU) của module đó.",
        "source": "12"
    },
    {
        "code": "2124",
        "type": "Lỗi (SP.UNIT LAY ERR)",
        "description": "Lỗi lắp đặt module (SP.UNIT LAY ERR) do vượt quá số lượng module I/O tối đa.",
        "cause": "Số lượng module I/O được lắp đặt vượt quá giới hạn tối đa cho phép (ví dụ: 25 hoặc 65 trừ đi số lượng CPU tùy cấu hình).",
        "remedy": "Giảm số lượng module I/O lắp đặt hoặc kiểm tra lại cấu hình hệ thống để đảm bảo nằm trong giới hạn cho phép.",
        "source": "12"
    },
    {
        "code": "2125",
        "type": "Lỗi (SP.UNIT LAY ERROR)",
        "description": "Lỗi lắp đặt module chức năng đặc biệt liên quan đến phiên bản chức năng CPU.",
        "cause": "Xảy ra lỗi ở các CPU khác CPU số 1 khi có sự không tương thích về phiên bản chức năng A và B giữa các CPU.",
        "remedy": "Đảm bảo tất cả các module QCPU trong hệ thống đa CPU đều từ phiên bản chức năng B trở lên.",
        "source": "12"
    },
    {
        "code": "2150",
        "type": "Lỗi (SP.UNIT VER.ERR)",
        "description": "Lỗi phiên bản module (SP.UNIT VER.ERR) khiến hệ thống đa CPU không khởi động được.",
        "cause": "Thiết lập bất kỳ CPU nào từ No.2 đến No.4 làm CPU điều khiển cho các module chức năng thông minh phiên bản chức năng A.",
        "remedy": "Chỉ thiết lập CPU No.1 làm CPU điều khiển cho các module chức năng thông minh phiên bản chức năng A.",
        "source": "12"
    },
    {
        "code": "3009",
        "type": "Lỗi (PARAMETER ERROR)",
        "description": "Lỗi tham số khi thiết lập CPU điều khiển cho module dòng AnS/A.",
        "cause": "Các module dòng AnS/A trong cùng một hệ thống được thiết lập các CPU điều khiển khác nhau.",
        "remedy": "Thiết lập cùng một module CPU làm CPU điều khiển cho tất cả các khe cắm lắp module dòng AnS/A.",
        "source": "12"
    },
    {
        "code": "3012",
        "type": "Lỗi (PARAMETER ERROR)",
        "description": "Lỗi tham số do không nhất quán giữa các CPU.",
        "cause": "Tham số của module CPU không khớp với tham số của CPU No.1 hoặc CPU đang chạy có số hiệu thấp nhất.",
        "remedy": "Kiểm tra và thiết lập các tham số hệ thống đa CPU giống nhau trên tất cả các module CPU.",
        "source": "12"
    },
    {
        "code": "3015",
        "type": "Lỗi (PARAMETER ERROR)",
        "description": "Lỗi tham số trong quá trình kiểm tra tính nhất quán (Consistency check).",
        "cause": "Tham số thiết lập khởi động đồng bộ đa CPU hoặc các tham số đa CPU khác không giống nhau giữa các CPU trong hệ thống.",
        "remedy": "Đảm bảo các tham số trong mục \"Multiple CPU Setting\" được thiết lập giống hệt nhau cho tất cả các CPU.",
        "source": "12"
    },
    {
        "code": "4102",
        "type": "Lỗi (OPERATION ERROR)",
        "description": "Lỗi vận hành khi sử dụng thiết bị liên kết trực tiếp (link direct device).",
        "cause": "Thực hiện lệnh sử dụng thiết bị liên kết trực tiếp để truy cập vào module được điều khiển bởi một CPU khác.",
        "remedy": "Chỉ sử dụng CPU điều khiển (Control CPU) để thực hiện các lệnh truy cập trực tiếp vào module đó.",
        "source": "12"
    },
    {
        "code": "4107",
        "type": "Lỗi (OPERATION ERROR)",
        "description": "Lỗi vận hành do tích lũy quá nhiều lệnh chưa xử lý.",
        "cause": "Có từ 33 lệnh chuyên dụng chuyển động (motion dedicated) hoặc lệnh truyền tin đa CPU trở lên được tích lũy chưa xử lý xong.",
        "remedy": "Giảm số lượng lệnh thực hiện đồng thời trong một chu kỳ quét (tối đa 32 lệnh).",
        "source": "12"
    },
    {
        "code": "7000",
        "type": "Lỗi (MULTI CPU DOWN)",
        "description": "Lỗi dừng toàn bộ hệ thống đa CPU.",
        "cause": "Xảy ra khi CPU No.1 bị lỗi dừng, hoặc một CPU khác bị lỗi dừng (khi thiết lập Operation Mode là dừng tất cả), hoặc khi một CPU khác No.1 bị reset riêng lẻ.",
        "remedy": "Kiểm tra nguyên nhân gây lỗi ở module CPU cụ thể trong cửa sổ PLC Diagnostics, khắc phục lỗi đó, sau đó reset CPU No.1 hoặc tắt/bật nguồn toàn hệ thống.",
        "source": "12"
    },
    {
        "code": "7010",
        "type": "Lỗi (MULTI EXE. ERROR)",
        "description": "Lỗi thực thi đa CPU liên quan đến phiên bản chức năng.",
        "cause": "Kết hợp module CPU phiên bản chức năng A và chức năng B trong cùng hệ thống đa CPU.",
        "remedy": "Sử dụng đồng nhất các module CPU phiên bản chức năng B trở lên.",
        "source": "12"
    },
    {
        "code": "7020",
        "type": "Lỗi (MULTI EXE. ERROR)",
        "description": "Lỗi thực thi đa CPU nhưng hệ thống vẫn tiếp tục vận hành.",
        "cause": "Xảy ra ở các CPU khác khi một CPU (không phải No.1) bị lỗi dừng nhưng tham số \"Operation Mode\" được thiết lập là không dừng các trạm khác.",
        "remedy": "Khắc phục nguyên nhân gây lỗi tại module CPU đang bị dừng để khôi phục trạng thái hoạt động bình thường của toàn hệ thống.",
        "source": "12"
    },
    {
        "code": "4620",
        "type": "Lỗi",
        "description": "BLOCK EXE. ERROR",
        "cause": "Cố gắng bắt đầu một khối (block) đã đang hoạt động khi chế độ vận hành kích hoạt khối trùng lặp được thiết lập là STOP.",
        "remedy": "Kiểm tra logic chương trình để đảm bảo không có yêu cầu kích hoạt khối (Block START) khi khối đó đang chạy, hoặc thay đổi cài đặt chế độ vận hành sang WAIT.",
        "source": "13"
    },
    {
        "code": "4621",
        "type": "Lỗi",
        "description": "BLOCK EXE. ERROR",
        "cause": "Lệnh điều khiển SFC liên quan đến khối được thực thi khi SM321 (SFC program start/stop) đang OFF, khối không tồn tại, chương trình SFC đang ở trạng thái chờ, hoặc khối bắt đầu được mô tả trong chương trình quản lý thực thi.",
        "remedy": "Đảm bảo SM321 đang ON trước khi thực thi lệnh, kiểm tra sự tồn tại của khối và không sử dụng các bước bắt đầu khối trong chương trình SFC quản lý thực thi.",
        "source": "13"
    },
    {
        "code": "4631",
        "type": "Lỗi",
        "description": "STEP EXE. ERROR",
        "cause": "Lệnh điều khiển SFC liên quan đến bước (step) hoặc điều kiện chuyển tiếp (transition) được thực thi khi SM321 đang OFF, bước/điều kiện không tồn tại, hoặc chương trình SFC đang ở trạng thái chờ/dừng.",
        "remedy": "Kiểm tra trạng thái SM321 và đảm bảo số hiệu bước hoặc mã điều kiện chuyển tiếp được chỉ định là chính xác và tồn tại trong khối mục tiêu.",
        "source": "13"
    },
    {
        "code": "4101",
        "type": "Lỗi",
        "description": "OPERATION ERROR",
        "cause": "Chỉ định một bước không tồn tại khi không thực hiện chỉ định khối, vượt quá số hiệu bước tối đa (8191) hoặc vượt quá phạm vi rơle bước (S).",
        "remedy": "Kiểm tra và hiệu chỉnh lại số hiệu bước trong lệnh đọc hàng loạt bước hoạt động (Active step batch readout) để nằm trong phạm vi cho phép.",
        "source": "13"
    },
    {
        "code": "4100",
        "type": "Lỗi",
        "description": "OPERATION ERROR",
        "cause": "Số hiệu khối SFC chỉ định nằm ngoài phạm vi 0-319, hoặc số lượng bình luận cần đọc/số lượng đọc trong một chu kỳ quét nằm ngoài phạm vi 0-256.",
        "remedy": "Chỉnh sửa các tham số n1, n2, n3 trong lệnh S(P).SFCSCOMR hoặc S(P).SFCTCOMR cho đúng phạm vi kỹ thuật.",
        "source": "13"
    },
    {
        "code": "2400",
        "type": "Lỗi",
        "description": "FILE SET ERROR",
        "cause": "File bình luận được thiết lập trong PLC Parameter không tồn tại tại thời điểm bật nguồn hoặc reset.",
        "remedy": "Kiểm tra lại cài đặt file trong tab PLC File và đảm bảo file bình luận đã được nạp vào bộ nhớ PLC.",
        "source": "13"
    },
    {
        "code": "2410",
        "type": "Lỗi",
        "description": "FILE SET ERROR / PROGRAM NOT FOUND",
        "cause": "File chương trình được chỉ định không tồn tại hoặc file bình luận chỉ định khi thực hiện lệnh S(P).SFCSCOMR/SFCTCOMR không tồn tại.",
        "remedy": "Kiểm tra tên file chương trình hoặc file bình luận và đảm bảo chúng đã được đăng ký/nạp vào PLC.",
        "source": "13"
    },
    {
        "code": "4130",
        "type": "Lỗi",
        "description": "OPERATION ERROR",
        "cause": "Lệnh S(P).SFCSCOMR/SFCTCOMR được thực thi đối với file bình luận lưu trữ trong thẻ ATA hoặc thẻ nhớ SD.",
        "remedy": "Chuyển file bình luận sang các bộ nhớ được hỗ trợ như SRAM card, Flash card hoặc Standard ROM.",
        "source": "13"
    },
    {
        "code": "5001",
        "type": "Lỗi",
        "description": "WDT ERROR",
        "cause": "Vòng lặp vô tận xảy ra trong một chu kỳ quét khi sử dụng chế độ 'Continuous transition' với lệnh Jump, hoặc thời gian xử lý lệnh kiểm tra chuyển tiếp cưỡng bức quá dài.",
        "remedy": "Kiểm tra lại cấu trúc vòng lặp Jump, hoặc tăng giá trị thiết lập WDT trong PLC RAS của PLC Parameter.",
        "source": "13"
    },
    {
        "code": "4505",
        "type": "Lỗi",
        "description": "OPERATION ERROR",
        "cause": "Sử dụng chính bước hiện tại làm số hiệu bước mục tiêu trong lệnh kết thúc bước (RST Sn).",
        "remedy": "Không được chỉ định chính bước đang thực thi lệnh để tự kết thúc nó thông qua lệnh RST Sn.",
        "source": "13"
    },
    {
        "code": "2504",
        "type": "Lỗi",
        "description": "CAN'T EXE.PRG.",
        "cause": "Đã tồn tại một chương trình SFC loại thực thi quét (scan execution) khi cố gắng chuyển đổi một chương trình SFC khác sang loại này bằng lệnh PSCAN.",
        "remedy": "Sử dụng lệnh POFF để chuyển chương trình SFC hiện tại sang trạng thái chờ (stand-by) trước khi kích hoạt chương trình mới.",
        "source": "13"
    },
    {
        "code": "4100",
        "type": "Lỗi vận hành (Operation error)",
        "description": "Lỗi giá trị nhập vào vượt quá phạm vi cho phép khi thực hiện các lệnh chuyển đổi kiểu dữ liệu.",
        "cause": "Giá trị nhập vào vượt quá 9999 đối với INT_TO_BCD hoặc vượt quá 99999999 đối với DINT_TO_BCD. Đối với REAL_TO_INT, giá trị ngoài phạm vi -32768 đến 32767. Đối với STR_TO_REAL, số lượng ký tự bằng 0 hoặc vượt quá 24, hoặc có ký tự không hợp lệ.",
        "remedy": "Kiểm tra và điều chỉnh giá trị đầu vào của các lệnh chuyển đổi để đảm bảo chúng nằm trong phạm vi dữ liệu hợp lệ được quy định cho từng lệnh cụ thể.",
        "source": "14"
    },
    {
        "code": "4140",
        "type": "Lỗi vận hành (Operation error)",
        "description": "Lỗi giá trị số thực dấu phẩy động hoặc số thực độ chính xác kép ngoài phạm vi.",
        "cause": "Giá trị nhập vào là -0 hoặc nằm ngoài phạm vi cho phép của kiểu dữ liệu LREAL (độ chính xác kép) hoặc REAL (số thực) khi thực hiện các phép toán chuyển đổi hoặc số học.",
        "remedy": "Đảm bảo giá trị số thực nhập vào nằm trong phạm vi có thể xử lý được của module (ví dụ: 2^-1022 <= \\",
        "source": "(s)\\"
    },
    {
        "code": "4141",
        "type": "Lỗi vận hành (Operation error)",
        "description": "Lỗi tràn số (Overflow) trong kết quả phép toán số thực.",
        "cause": "Kết quả của phép toán vượt quá phạm vi biểu diễn của kiểu dữ liệu độ chính xác kép (2^1024 <= \\",
        "remedy": "kết quả\\",
        "source": ")."
    },
    {
        "code": "4101",
        "type": "Lỗi vận hành (Operation error)",
        "description": "Thiết bị được chỉ định vượt quá phạm vi thiết bị tương ứng.",
        "cause": "Thiết bị đích (destination) hoặc thiết bị nguồn (source) được chỉ định trong lệnh (như MIDR, STR_TO_WORD, TOF) nằm ngoài dải địa chỉ hợp lệ của CPU.",
        "remedy": "Kiểm tra lại địa chỉ thiết bị trong chương trình và đảm bảo dải địa chỉ được sử dụng nằm trong cấu hình bộ nhớ của CPU đang sử dụng.",
        "source": "14"
    },
    {
        "code": "C9026",
        "type": "Cảnh báo (Warning)",
        "description": "Cảnh báo kiểu dữ liệu không khớp trong quá trình biên dịch.",
        "cause": "Xảy ra khi kiểu dữ liệu WORD (không dấu)/16-bit string hoặc DWORD (không dấu)/32-bit string được chỉ định cho đầu ra của các lệnh lựa chọn giá trị cực đại/cực tiểu hoặc kiểm soát giới hạn.",
        "remedy": "Xác nhận lại kiểu dữ liệu đầu ra để đảm bảo tính nhất quán của chương trình, mặc dù lệnh vẫn có thể thực hiện.",
        "source": "14"
    },
    {
        "code": "C9047",
        "type": "Cảnh báo (Warning)",
        "description": "Cảnh báo cài đặt đơn vị đo lường timer.",
        "cause": "Đơn vị đo lường (time period) cho timer tốc độ cao hoặc tốc độ thấp bị thay đổi so với giá trị mặc định trong PLC Parameter.",
        "remedy": "Kiểm tra lại cấu hình thông số Timer limit setting trong PLC System của PLC Parameter để đảm bảo đúng với yêu cầu thiết kế của hệ thống.",
        "source": "14"
    },
    {
        "code": "4620",
        "type": "Lỗi (Error)",
        "description": "Lỗi thực thi khối (BLOCK EXE. ERROR) khi thực hiện lệnh khởi động khối gấp đôi (block double START).",
        "cause": "Xảy ra khi một khối đã được khởi động hoặc đang hoạt động lại nhận thêm một yêu cầu khởi động khác trong khi cài đặt chế độ hoạt động là \"STOP\".",
        "remedy": "Kiểm tra lại chương trình SFC để đảm bảo không có nhiều yêu cầu khởi động cùng một khối đồng thời, hoặc thay đổi cài đặt chế độ hoạt động sang \"WAIT\".",
        "source": "15"
    },
    {
        "code": "4621",
        "type": "Lỗi (Error)",
        "description": "Lỗi thực thi khối (BLOCK EXE. ERROR) liên quan đến lệnh điều khiển SFC.",
        "cause": "Cố gắng thực hiện lệnh điều khiển SFC cho một khối không tồn tại hoặc khi rơle đặc biệt cho phép chạy chương trình SFC (SM321) đang ở trạng thái OFF.",
        "remedy": "Kiểm tra sự tồn tại của số khối được chỉ định và đảm bảo SM321 đang ON trước khi thực hiện các lệnh điều khiển khối.",
        "source": "15"
    },
    {
        "code": "4631",
        "type": "Lỗi (Error)",
        "description": "Lỗi thực thi bước (STEP EXE. ERROR).",
        "cause": "Chỉ định một bước không tồn tại trong chương trình SFC hoặc thực hiện lệnh điều khiển bước khi chương trình SFC đang ở trạng thái chờ (stand-by).",
        "remedy": "Xác nhận số hiệu bước tồn tại trong khối và đảm bảo chương trình SFC đang trong trạng thái thực thi (scan execution type).",
        "source": "15"
    },
    {
        "code": "4101",
        "type": "Lỗi (Error)",
        "description": "Lỗi vận hành (OPERATION ERROR) liên quan đến chỉ số thiết bị.",
        "cause": "Số hiệu bước (Sn) vượt quá phạm vi tối đa cho phép (8191) hoặc chỉ định một bước không tồn tại khi không thực hiện chỉ định khối cụ thể.",
        "remedy": "Kiểm tra lại số hiệu bước trong lệnh BMOV/MOV và đảm bảo nằm trong phạm vi cấu hình của thiết bị.",
        "source": "15"
    },
    {
        "code": "5001",
        "type": "Lỗi (Error)",
        "description": "Lỗi WDT (Watchdog Timer Error).",
        "cause": "Xảy ra vòng lặp vô hạn trong một lần quét khi sử dụng cài đặt \"chuyển tiếp liên tục\" (with continuous transition) hoặc thời gian xử lý lệnh kiểm tra chuyển tiếp quá lâu.",
        "remedy": "Kiểm tra cấu trúc vòng lặp trong chương trình SFC, điều chỉnh các điều kiện chuyển tiếp hoặc tăng giá trị cài đặt WDT trong thông số PLC RAS.",
        "source": "15"
    },
    {
        "code": "2400",
        "type": "Lỗi (Error)",
        "description": "Lỗi thiết lập file (FILE SET ERROR).",
        "cause": "File ghi chú (comment file) được cấu hình trong tham số PLC nhưng không tồn tại khi bật nguồn hoặc reset.",
        "remedy": "Kiểm tra sự tồn tại của file ghi chú trong bộ nhớ PLC hoặc tạo lại file tương ứng bằng công cụ lập trình.",
        "source": "15"
    },
    {
        "code": "2410",
        "type": "Lỗi (Error)",
        "description": "Lỗi không tìm thấy file ghi chú hoặc chương trình.",
        "cause": "File chương trình hoặc file ghi chú được chỉ định trong các lệnh đọc ghi chú SFC (SFCSCOMR/SFCTCOMR) không tồn tại.",
        "remedy": "Xác nhận tên file và đường dẫn của file ghi chú/chương trình trong bộ nhớ.",
        "source": "15"
    },
    {
        "code": "4130",
        "type": "Lỗi (Error)",
        "description": "Lỗi vận hành thiết bị lưu trữ.",
        "cause": "Thực hiện lệnh đọc ghi chú SFC (SFCSCOMR/SFCTCOMR) trực tiếp từ thẻ nhớ ATA hoặc thẻ nhớ SD.",
        "remedy": "Chuyển file ghi chú vào bộ nhớ SRAM hoặc Standard ROM trước khi thực hiện lệnh đọc.",
        "source": "15"
    },
    {
        "code": "4505",
        "type": "Lỗi (Error)",
        "description": "Lỗi chỉ định bước.",
        "cause": "Sử dụng chính số hiệu bước hiện tại làm tham số đích trong lệnh kết thúc bước (RST Sn) trên các dòng Basic, Universal hoặc LCPU.",
        "remedy": "Thay đổi logic chương trình để không tự kết thúc chính bước đang thực thi bằng lệnh điều khiển bước.",
        "source": "15"
    },
    {
        "code": "Lỗi tràn (Overflow error)",
        "type": "Lỗi",
        "description": "Lỗi tràn xảy ra khi giá trị bộ đếm vượt quá phạm vi giới hạn.",
        "cause": "Khi dùng bộ đếm tuyến tính, một xung cộng được nhập thêm từ giá trị hiện tại 2147483647. 2) Khi dùng bộ đếm tuyến tính, một xung trừ được nhập thêm từ giá trị hiện tại -2147483648.",
        "remedy": "Thực hiện chức năng Preset để xóa lỗi tràn và tiếp tục đếm.",
        "source": "16"
    },
    {
        "code": "Phát hiện đứt cầu chì (Fuse broken detection)",
        "type": "Lỗi",
        "description": "Cầu chì trong bộ phận đầu ra tín hiệu trùng khớp (coincidence signal) bị hỏng.",
        "cause": "Cầu chì cho phần đầu ra bên ngoài của tín hiệu trùng khớp đã bị đứt.",
        "remedy": "Vui lòng liên hệ với đại diện Mitsubishi tại địa phương của bạn để được hỗ trợ.",
        "source": "16"
    },
    {
        "code": "CAN'T EXE. PRG. (2500)",
        "type": "Lỗi",
        "description": "Lỗi khi ghi tham số vào PLC.",
        "cause": "Thay đổi số bắt đầu của các thanh ghi chỉ số được sử dụng trong cài đặt thiết bị của tham số PLC nhưng chỉ ghi tham số mà không ghi chương trình tương ứng vào bộ điều khiển.",
        "remedy": "Luôn ghi đồng thời cả tham số và chương trình vào bộ điều khiển lập trình.",
        "source": "17"
    },
    {
        "code": "4101",
        "type": "Lỗi",
        "description": "Lỗi vượt quá phạm vi thiết bị.",
        "cause": "Kết quả của việc cài đặt chỉ số (index setting) áp dụng cho các thanh ghi file (ZR), thanh ghi dữ liệu mở rộng (D) hoặc thanh ghi liên kết mở rộng (W) vượt quá phạm vi của các tệp thanh ghi file.",
        "remedy": "Kiểm tra lại giá trị cài đặt chỉ số và đảm bảo dữ liệu sau khi sửa đổi không vượt quá phạm vi thiết bị được người dùng chỉ định.",
        "source": "17"
    },
    {
        "code": "1103",
        "type": "Lỗi",
        "description": "Lỗi vượt quá phạm vi thiết bị hệ thống.",
        "cause": "Kết quả của cài đặt chỉ số vượt quá phạm vi thiết bị do người dùng chỉ định và dữ liệu được ghi vào các thiết bị dành riêng cho hệ thống.",
        "remedy": "Điều chỉnh lại chương trình để đảm bảo các giá trị đếm và cài đặt chỉ số nằm trong phạm vi cho phép.",
        "source": "17"
    },
    {
        "code": "6706",
        "type": "Lỗi",
        "description": "Lỗi vận hành (đối với dòng FXCPU).",
        "cause": "Áp dụng cài đặt chỉ số (index setting) vượt quá phạm vi thiết bị quy định.",
        "remedy": "Kiểm tra lại cấu trúc chương trình và giới hạn thiết bị trong tài liệu hướng dẫn dòng FX tương ứng.",
        "source": "17"
    },
    {
        "code": "4100",
        "type": "Lỗi",
        "description": "Giá trị dữ liệu cài đặt không hợp lệ hoặc vượt dải cho phép.",
        "cause": "Dữ liệu điều khiển PID nằm ngoài phạm vi; Giới hạn trên MV nhỏ hơn giới hạn dưới; Số vòng lặp sử dụng nhỏ hơn số vòng lặp thực hiện trong một lần quét.",
        "remedy": "Kiểm tra và hiệu chỉnh lại các thông số cài đặt dữ liệu điều khiển PID trong dải cho phép.",
        "source": "18"
    },
    {
        "code": "4101",
        "type": "Lỗi",
        "description": "Vượt quá phạm vi thiết bị được chỉ định.",
        "cause": "Phạm vi thiết bị được phân bổ cho vùng dữ liệu điều khiển PID vượt quá số hiệu thiết bị cuối cùng của thiết bị tương ứng.",
        "remedy": "Đảm bảo dải thiết bị được chỉ định cho dữ liệu PID nằm trong phạm vi bộ nhớ của CPU.",
        "source": "18"
    },
    {
        "code": "4103",
        "type": "Lỗi",
        "description": "Thứ tự thực thi lệnh không đúng.",
        "cause": "Lệnh S(P).PIDCONT hoặc S(P).PIDSTOP/RUN được thực hiện trước khi thực hiện lệnh khởi tạo S(P).PIDINIT.",
        "remedy": "Đảm bảo lệnh S(P).PIDINIT được thực thi thành công trước khi gọi các lệnh điều khiển hoặc thay đổi thông số.",
        "source": "18"
    },
    {
        "code": "2110",
        "type": "Lỗi",
        "description": "Lỗi giám sát màn hình (chỉ dành cho QnACPU).",
        "cause": "Lệnh CMODE chưa được thực hiện cho module AD57(S1) trước khi gọi lệnh giám sát PID57.",
        "remedy": "Thực hiện lệnh CMODE để thiết lập chế độ hiển thị chuẩn cho AD57(S1).",
        "source": "18"
    },
    {
        "code": "Cảnh báo thay đổi MV (b1)",
        "type": "Cảnh báo",
        "description": "Tốc độ thay đổi giá trị MV vượt quá giới hạn.",
        "cause": "Biến thiên giữa giá trị MV hiện tại và trước đó lớn hơn giới hạn Delta MVL đã thiết lập.",
        "remedy": "Kiểm tra lại đặc tính tải hoặc nới lỏng giới hạn Delta MVL nếu cần thiết.",
        "source": "18"
    },
    {
        "code": "Cảnh báo thay đổi PV (b0)",
        "type": "Cảnh báo",
        "description": "Tốc độ thay đổi giá trị PV vượt quá giới hạn.",
        "cause": "Biến thiên giữa giá trị PV hiện tại và trước đó lớn hơn giới hạn Delta PVL đã thiết lập.",
        "remedy": "Kiểm tra cảm biến đầu vào hoặc điều chỉnh thông số Delta PVL.",
        "source": "18"
    }
];
