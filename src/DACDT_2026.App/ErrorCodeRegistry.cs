using System;
using System.Collections.Generic;

namespace DACDT_2026
{
    public static class ErrorCodeRegistry
    {
        public class ErrorDetails
        {
            public string Code { get; set; }
            public string Type { get; set; }
            public string Description { get; set; }
            public string Cause { get; set; }
            public string Remedy { get; set; }
            public string Source { get; set; }
        }

        public class RangeEntry
        {
            public int Min { get; set; }
            public int Max { get; set; }
            public ErrorDetails Details { get; set; }
        }

        private static readonly Dictionary<string, ErrorDetails> ErrorDb = new Dictionary<string, ErrorDetails>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<RangeEntry> Ranges = new List<RangeEntry>();

        static ErrorCodeRegistry()
        {
            var e_0 = new ErrorDetails { Code = "4101", Type = "Lỗi vận hành (Operation Error)", Description = "Truy cập ngoài phạm vi thiết bị hoặc mã NULL 00H không tồn tại.", Cause = "Phạm vi các điểm n vượt quá giới hạn thiết bị tương ứng; mã NULL 00H không được tìm thấy trong phạm vi thiết bị khi xử lý chuỗi ký tự; hoặc các thiết bị nguồn và đích ghi đè lên nhau không hợp lệ.", Remedy = "Kiểm tra lại số lượng điểm n được chỉ định và đảm bảo rằng thiết bị đích có đủ dung lượng; thêm mã NULL 00H vào cuối chuỗi ký tự.", Source = "1" };
            ErrorDb["4101"] = e_0;
            var e_1 = new ErrorDetails { Code = "4100", Type = "Lỗi vận hành (Operation Error)", Description = "Dữ liệu thiết bị nằm ngoài phạm vi cài đặt.", Cause = "Giá trị của thiết bị (S) hoặc (D) không hợp lệ (ví dụ: dữ liệu BCD không nằm trong khoảng 0-9999 hoặc số chia bằng 0).", Remedy = "Kiểm tra và hiệu chỉnh dữ liệu đầu vào sao cho nằm trong phạm vi cho phép của tập lệnh (ví dụ: 0 đến 9999 đối với BCD 4 chữ số).", Source = "1" };
            ErrorDb["4100"] = e_1;
            var e_2 = new ErrorDetails { Code = "4140", Type = "Lỗi vận hành (Operation Error)", Description = "Giá trị số thực (Floating-point) không hợp lệ.", Cause = "Thiết bị được chỉ định chứa giá trị -0, số không chuẩn hóa, không phải là số (NaN) hoặc vô cùng (±∞).", Remedy = "Đảm bảo các số thực được sử dụng trong phép toán nằm trong dải cho phép (2^-126 đến 2^128 cho độ chính xác đơn).", Source = "1" };
            ErrorDb["4140"] = e_2;
            var e_3 = new ErrorDetails { Code = "4200", Type = "Lỗi vận hành (Operation Error)", Description = "Lỗi cấu trúc vòng lặp FOR-NEXT.", Cause = "Lệnh FEND, END hoặc STOP được thực thi trước khi lệnh NEXT kết thúc vòng lặp FOR tương ứng.", Remedy = "Đảm bảo mọi cấu trúc FOR đều có lệnh NEXT tương ứng trước khi kết thúc chương trình chính.", Source = "1" };
            ErrorDb["4200"] = e_3;
            var e_4 = new ErrorDetails { Code = "4210", Type = "Lỗi vận hành (Operation Error)", Description = "Lỗi con trỏ (Pointer error).", Cause = "Số con trỏ được chỉ định cho lệnh nhảy (Jump) hoặc gọi chương trình con (Call) không tồn tại trong cùng một tệp chương trình.", Remedy = "Kiểm tra nhãn con trỏ (P*) và đảm bảo nó đã được định nghĩa đúng vị trí trong chương trình.", Source = "1" };
            ErrorDb["4210"] = e_4;
            var e_5 = new ErrorDetails { Code = "1402", Type = "Lỗi module thông minh", Description = "Lỗi module chức năng thông minh.", Cause = "Phát hiện lỗi tại module thông minh khi thực hiện lệnh đọc/ghi bộ đệm (FROM/TO).", Remedy = "Kiểm tra trạng thái phần cứng của module thông minh và cấu hình cài đặt trong thông số I/O.", Source = "1" };
            ErrorDb["1402"] = e_5;
            var e_6 = new ErrorDetails { Code = "2410", Type = "Lỗi tệp (File Error)", Description = "Tệp chỉ định không tồn tại.", Cause = "Tên chương trình hoặc tệp dữ liệu được chỉ định trong ổ đĩa không tìm thấy.", Remedy = "Kiểm tra lại tên tệp và đảm bảo tệp đã được tải vào đúng ổ đĩa chỉ định (Drive 0, 1, 2, hoặc 4).", Source = "1" };
            ErrorDb["2410"] = e_6;
            var e_7 = new ErrorDetails { Code = "9100 - 9124", Type = "Lỗi lệnh PID", Description = "Lỗi trong quá trình tính toán hoặc cài đặt PID/Auto-tuning.", Cause = "Chu kỳ lấy mẫu Ts <= 0; các hằng số KP, TI, TD nằm ngoài dải; hoặc quá trình Auto-tuning thất bại do biến động PV không bình thường.", Remedy = "Kiểm tra lại các tham số trong khối điều khiển (S3) và đảm bảo hệ thống ổn định trước khi bắt đầu Auto-tuning.", Source = "1" };
            Ranges.Add(new RangeEntry { Min = 9100, Max = 9124, Details = e_7 });
            var e_8 = new ErrorDetails { Code = "2500", Type = "Lỗi", Description = "CAN'T EXE. PRG.", Cause = "Thay đổi số đầu của thanh ghi chỉ số sử dụng trong tham số nhưng không ghi tham số vào PLC cùng với chương trình.", Remedy = "Đảm bảo ghi tham số vào bộ điều khiển lập trình cùng với chương trình tương ứng.", Source = "2" };
            ErrorDb["2500"] = e_8;
            var e_9 = new ErrorDetails { Code = "4101", Type = "Lỗi", Description = "OPERATION ERROR", Cause = "Truy cập vượt quá phạm vi thiết bị được chỉ định, hoặc thực hiện sửa đổi chỉ số (index modification) vượt giới hạn thiết bị, hoặc truy cập thanh ghi file (R, ZR) mà chưa thiết lập file thanh ghi.", Remedy = "Kiểm tra lại phạm vi thiết bị trong chương trình và cài đặt thanh ghi file trong tham số PLC.", Source = "2" };
            ErrorDb["4101"] = e_9;
            var e_10 = new ErrorDetails { Code = "3101", Type = "Lỗi", Description = "LINK PARA ERROR", Cause = "Số ổ đĩa (drive number) bị thay đổi bằng lệnh QDRSET khi thiết bị \"ZR\" được chỉ định trong các module CPU không phải Universal model QCPU.", Remedy = "Không thay đổi số ổ đĩa bằng lệnh QDRSET khi sử dụng thiết bị ZR trên các dòng CPU này.", Source = "2" };
            ErrorDb["3101"] = e_10;
            var e_11 = new ErrorDetails { Code = "1103", Type = "Lỗi", Description = "DEVICE RANGE OVER", Cause = "Dữ liệu sau khi sửa đổi chỉ số vượt quá phạm vi thiết bị chỉ định của người dùng và ghi vào thiết bị hệ thống.", Remedy = "Điều chỉnh giá trị thanh ghi chỉ số hoặc phạm vi thiết bị để không xâm phạm vùng nhớ hệ thống.", Source = "2" };
            ErrorDb["1103"] = e_11;
            var e_12 = new ErrorDetails { Code = "4200", Type = "Lỗi", Description = "FOR-NEXT ERROR", Cause = "Thực thi lệnh FEND, END hoặc STOP bên trong vòng lặp FOR-NEXT trước khi gặp lệnh NEXT.", Remedy = "Sửa lại cấu trúc chương trình để đảm bảo các lệnh kết thúc không nằm trong vòng lặp.", Source = "2" };
            ErrorDb["4200"] = e_12;
            var e_13 = new ErrorDetails { Code = "4211", Type = "Lỗi", Description = "SUBROUTINE ERROR", Cause = "Thực thi lệnh END, FEND, GOEND hoặc STOP sau khi gọi chương trình con (CALL) nhưng trước khi gặp lệnh RET.", Remedy = "Đảm bảo mọi chương trình con đều kết thúc bằng lệnh RET trước khi kết thúc chương trình chính.", Source = "2" };
            ErrorDb["4211"] = e_13;
            var e_14 = new ErrorDetails { Code = "4221", Type = "Lỗi", Description = "INTERRUPT ERROR", Cause = "Thực thi lệnh FEND, END hoặc STOP bên trong chương trình ngắt trước khi thực hiện lệnh IRET.", Remedy = "Sửa lại cấu trúc chương trình ngắt, đảm bảo kết thúc bằng lệnh IRET.", Source = "2" };
            ErrorDb["4221"] = e_14;
            var e_15 = new ErrorDetails { Code = "4230", Type = "Lỗi", Description = "CHK INSTRUCTION ERROR", Cause = "Thực thi các lệnh kết thúc chương trình hoặc lệnh STOP giữa các lệnh CHKCIR và CHKEND.", Remedy = "Kiểm tra lại cấu trúc các lệnh kiểm tra lỗi đặc biệt.", Source = "2" };
            ErrorDb["4230"] = e_15;
            var e_16 = new ErrorDetails { Code = "4140", Type = "Lỗi", Description = "FLOATING POINT DATA ERROR", Cause = "Giá trị thiết bị được chỉ định là -0, số không chuẩn (subnormal number), NaN (không phải số), hoặc vô cực (+/- infinity).", Remedy = "Kiểm tra dữ liệu đầu vào của các phép toán số thực để đảm bảo giá trị nằm trong phạm vi hợp lệ.", Source = "2" };
            ErrorDb["4140"] = e_16;
            var e_17 = new ErrorDetails { Code = "4100", Type = "Lỗi", Description = "DATA SETTING ERROR", Cause = "Giá trị nguồn (S) hoặc giá trị cài đặt n nằm ngoài phạm vi cho phép của lệnh (ví dụ: dữ liệu BCD không hợp lệ, chia cho 0, hoặc giá trị n âm).", Remedy = "Đảm bảo dữ liệu nguồn và các hằng số cài đặt phù hợp với quy định của từng lệnh kỹ thuật.", Source = "2" };
            ErrorDb["4100"] = e_17;
            var e_18 = new ErrorDetails { Code = "2400", Type = "Lỗi", Description = "FILE SET ERROR", Cause = "File chú thích (comment file) được thiết lập trong tham số PLC nhưng không tồn tại khi bật nguồn hoặc reset.", Remedy = "Kiểm tra sự tồn tại của file trong bộ nhớ hoặc điều chỉnh lại thiết lập PLC File.", Source = "2" };
            ErrorDb["2400"] = e_18;
            var e_19 = new ErrorDetails { Code = "9010", Type = "Lỗi", Description = "CHK DETECTED ERROR", Cause = "Phát hiện lỗi hệ thống thông qua lệnh CHK (kiểm tra lỗi định dạng đặc biệt).", Remedy = "Tra cứu mã số tiếp điểm và mã số cuộn dây lưu trong SD80 để xác định vị trí lỗi cụ thể.", Source = "2" };
            ErrorDb["9010"] = e_19;
            var e_20 = new ErrorDetails { Code = "1000", Type = "Lỗi nghiêm trọng (Major)", Description = "MAIN CPU DOWN CPU bị treo hoặc hỏng.", Cause = "Sự cố do nhiễu hoặc hỏng hóc phần cứng.", Remedy = "Thực hiện các biện pháp giảm nhiễu. Reset module CPU và chạy lại. Nếu lỗi vẫn còn, đó là lỗi phần cứng.", Source = "3" };
            ErrorDb["1000"] = e_20;
            var e_21 = new ErrorDetails { Code = "1009", Type = "Lỗi nghiêm trọng (Major)", Description = "MAIN CPU DOWN Lỗi nguồn hoặc lỗi kết nối bus hệ thống.", Cause = "Dạng sóng điện áp nguồn ngoài phạm vi cho phép hoặc lỗi ở bộ nguồn, module CPU, đơn vị đế hoặc cáp mở rộng.", Remedy = "Kiểm tra điện áp nguồn. Reset CPU. Nếu lỗi tiếp diễn, kiểm tra và thay thế các linh kiện phần cứng bị lỗi.", Source = "3" };
            ErrorDb["1009"] = e_21;
            var e_22 = new ErrorDetails { Code = "1300", Type = "Lỗi trung bình/nhẹ (Moderate/Minor)", Description = "FUSE BREAK OFF Có module đầu ra bị đứt cầu chì.", Cause = "Cầu chì của module đầu ra bị đứt.", Remedy = "Kiểm tra đèn LED FUSE của các module đầu ra, thay thế module bị đứt cầu chì hoặc kiểm tra kết nối cáp mở rộng.", Source = "3" };
            ErrorDb["1300"] = e_22;
            var e_23 = new ErrorDetails { Code = "1600", Type = "Cảnh báo (Minor)", Description = "BATTERY ERROR Điện áp pin của module CPU giảm xuống dưới mức quy định.", Cause = "Pin hết điện hoặc đầu nối pin không được kết nối đúng cách.", Remedy = "Thay pin mới hoặc kiểm tra lại kết nối đầu nối pin.", Source = "3" };
            ErrorDb["1600"] = e_23;
            var e_24 = new ErrorDetails { Code = "2000", Type = "Lỗi trung bình (Moderate)", Description = "UNIT VERIFY ERR. Trạng thái module I/O khác với thông tin khi bật nguồn.", Cause = "Module I/O bị lỏng, bị tháo ra hoặc lắp vào khi hệ thống đang chạy.", Remedy = "Kiểm tra module tương ứng tại vị trí lỗi (Slot No.) và lắp lại chắc chắn.", Source = "3" };
            ErrorDb["2000"] = e_24;
            var e_25 = new ErrorDetails { Code = "2100", Type = "Lỗi trung bình (Moderate)", Description = "SP.UNIT LAY ERR. Lỗi bố trí module chức năng thông minh.", Cause = "Cài đặt thông số I/O Assignment không khớp với module thực tế hoặc số điểm I/O gán ít hơn module thực tế.", Remedy = "Cài đặt lại thông số I/O Assignment trong PLC Parameter để khớp với thực tế.", Source = "3" };
            ErrorDb["2100"] = e_25;
            var e_26 = new ErrorDetails { Code = "2124", Type = "Lỗi trung bình (Moderate)", Description = "SP.UNIT LAY ERR. Vượt quá số lượng module hoặc số điểm I/O cho phép.", Cause = "Lắp module ở vị trí vượt quá phạm vi điểm I/O (ví dụ: vượt quá 4096 điểm đối với dòng High Performance).", Remedy = "Giảm số lượng module hoặc thay thế module để tổng số điểm I/O nằm trong phạm vi cho phép của CPU.", Source = "3" };
            ErrorDb["2124"] = e_26;
            var e_27 = new ErrorDetails { Code = "2400", Type = "Lỗi trung bình (Moderate)", Description = "FILE SET ERROR File được chỉ định trong tham số không tồn tại.", Cause = "Thiếu file chương trình hoặc file tham số trong ổ đĩa được chỉ định.", Remedy = "Kiểm tra mã lỗi để xác định file thiếu. Tạo file và nạp lại vào module CPU.", Source = "3" };
            ErrorDb["2400"] = e_27;
            var e_28 = new ErrorDetails { Code = "3000", Type = "Lỗi trung bình (Moderate)", Description = "PARAMETER ERROR Lỗi cài đặt tham số.", Cause = "Cài đặt Timer, RUN-PAUSE, hoặc số lượng khe trống vượt quá dải cho phép của CPU.", Remedy = "Kiểm tra thông tin chi tiết lỗi (Parameter No.), sửa lại tham số trong phần mềm lập trình và nạp lại.", Source = "3" };
            ErrorDb["3000"] = e_28;
            var e_29 = new ErrorDetails { Code = "4100", Type = "Lỗi trung bình (Moderate)", Description = "OPERATION ERROR Lệnh không thể xử lý dữ liệu chứa bên trong.", Cause = "Dữ liệu lệnh sai lệch hoặc lỗi truy cập thẻ nhớ.", Remedy = "Kiểm tra vị trí lỗi trong chương trình (Program error location) và chỉnh sửa lại lệnh hoặc dữ liệu.", Source = "3" };
            ErrorDb["4100"] = e_29;
            var e_30 = new ErrorDetails { Code = "100", Type = "Cảnh báo", Description = "Bắt đầu trong khi vận hành", Cause = "Tín hiệu khởi động định vị được bật trong khi tín hiệu BUSY đang Bật.", Remedy = "Đảm bảo rằng tín hiệu khởi động định vị được bật chỉ sau khi tín hiệu BUSY đã Tắt.", Source = "4" };
            ErrorDb["100"] = e_30;
            var e_31 = new ErrorDetails { Code = "104", Type = "Cảnh báo", Description = "Không thể khởi động lại", Cause = "Lệnh khởi động lại được đưa ra khi trạng thái hoạt động của trục không phải là 'Bị dừng' hoặc sau khi thao tác bị ngắt bởi yêu cầu ngắt thao tác liên tục.", Remedy = "Đảm bảo lệnh khởi động lại chỉ được thực hiện khi trục ở trạng thái 'Bị dừng'. Kiểm tra xem có yêu cầu ngắt thao tác liên tục trước đó không.", Source = "4" };
            ErrorDb["104"] = e_31;
            var e_32 = new ErrorDetails { Code = "106", Type = "Lỗi", Description = "Tín hiệu dừng Bật khi bắt đầu", Cause = "Thực hiện lệnh khởi động lại trong khi tín hiệu dừng vẫn đang Bật.", Remedy = "Tắt tín hiệu dừng trước khi thực hiện lệnh khởi động lại.", Source = "4" };
            ErrorDb["106"] = e_32;
            var e_33 = new ErrorDetails { Code = "110", Type = "Cảnh báo", Description = "Thấp hơn tốc độ tối thiểu", Cause = "Tốc độ thực tế thấp hơn đơn vị tối thiểu do thiết lập ghi đè (override) 1% hoặc giá trị nhỏ khác.", Remedy = "Điều chỉnh tốc độ lệnh hoặc giá trị ghi đè để tốc độ tính toán không thấp hơn đơn vị tối thiểu.", Source = "4" };
            ErrorDb["110"] = e_33;
            var e_34 = new ErrorDetails { Code = "201", Type = "Lỗi", Description = "Bắt đầu tại điểm gốc (OP)", Cause = "Thực hiện lệnh OPR máy khi máy đã ở vị trí điểm gốc và chức năng thử lại OPR (retry) không được thiết lập.", Remedy = "Di chuyển máy ra khỏi vị trí điểm gốc bằng vận hành JOG trước khi thực hiện OPR hoặc kích hoạt chức năng thử lại OPR.", Source = "4" };
            ErrorDb["201"] = e_34;
            var e_35 = new ErrorDetails { Code = "203", Type = "Lỗi", Description = "Lỗi thời điểm phát hiện Dog", Cause = "Tín hiệu near-point dog bị tắt trước khi máy giảm tốc xuống tốc độ creep trong phương pháp near-point dog.", Remedy = "Tăng chiều dài của near-point dog hoặc giảm tốc độ OPR.", Source = "4" };
            ErrorDb["203"] = e_35;
            var e_36 = new ErrorDetails { Code = "204", Type = "Lỗi", Description = "Lỗi thời điểm phát hiện điểm gốc (OP)", Cause = "Tín hiệu zero được nhập trước khi giảm tốc xuống tốc độ creep trong phương pháp stopper.", Remedy = "Đảm bảo tín hiệu zero chỉ được gửi sau khi máy đã chạm vào stopper ở tốc độ creep.", Source = "4" };
            ErrorDb["204"] = e_36;
            var e_37 = new ErrorDetails { Code = "205", Type = "Lỗi", Description = "Lỗi thời gian chờ (Dwell time)", Cause = "Thời gian chờ OPR kết thúc trong quá trình giảm tốc từ tốc độ OPR trong phương pháp stopper 1.", Remedy = "Tăng thời gian chờ OPR (Pr.49) hoặc giảm tốc độ OPR.", Source = "4" };
            ErrorDb["205"] = e_37;
            var e_38 = new ErrorDetails { Code = "206", Type = "Lỗi", Description = "Lỗi lượng di chuyển phương pháp Count", Cause = "Lượng di chuyển sau near-point dog ON nhỏ hơn khoảng cách giảm tốc từ tốc độ OPR xuống tốc độ creep.", Remedy = "Tăng giá trị 'Setting for movement amount after near-point dog ON' (Pr.50).", Source = "4" };
            ErrorDb["206"] = e_38;
            var e_39 = new ErrorDetails { Code = "207", Type = "Lỗi", Description = "Yêu cầu OPR đang Bật", Cause = "Thực hiện OPR nhanh (Fast OPR) khi điểm gốc chưa được thiết lập bằng OPR máy.", Remedy = "Thực hiện OPR máy trước khi sử dụng chức năng OPR nhanh.", Source = "4" };
            ErrorDb["207"] = e_39;
            var e_40 = new ErrorDetails { Code = "209", Type = "Lỗi", Description = "Không thể khởi động lại OPR", Cause = "Thực hiện lệnh khởi động lại sau khi OPR máy hoặc OPR nhanh bị dừng.", Remedy = "Thực hiện lại toàn bộ quy trình OPR từ đầu.", Source = "4" };
            ErrorDb["209"] = e_40;
            var e_41 = new ErrorDetails { Code = "502", Type = "Lỗi", Description = "Mã dữ liệu không hợp lệ", Cause = "Số dữ liệu định vị đích của lệnh JUMP trùng với số dữ liệu của chính lệnh JUMP đó.", Remedy = "Chỉ định một số dữ liệu định vị khác làm đích đến cho lệnh JUMP.", Source = "4" };
            ErrorDb["502"] = e_41;
            var e_42 = new ErrorDetails { Code = "503", Type = "Lỗi", Description = "Không có tốc độ lệnh", Cause = "Tốc độ lệnh được đặt là -1 cho dữ liệu định vị đầu tiên khi bắt đầu, hoặc không có giá trị tốc độ hợp lệ.", Remedy = "Đặt giá trị tốc độ lệnh cụ thể (khác -1) cho điểm định vị đầu tiên.", Source = "4" };
            ErrorDb["503"] = e_42;
            var e_43 = new ErrorDetails { Code = "504", Type = "Lỗi", Description = "Nằm ngoài phạm vi lượng di chuyển tuyến tính", Cause = "Lượng di chuyển vượt quá 1073741824 khi sử dụng tốc độ tổng hợp trong điều khiển nội suy.", Remedy = "Giảm lượng di chuyển của mỗi trục nội suy hoặc không sử dụng tốc độ tổng hợp.", Source = "4" };
            ErrorDb["504"] = e_43;
            var e_44 = new ErrorDetails { Code = "506", Type = "Lỗi", Description = "Sai lệch lỗi cung tròn lớn", Cause = "Lỗi tính toán đường dẫn cung tròn vượt quá phạm vi cho phép được thiết lập.", Remedy = "Kiểm tra địa chỉ bắt đầu, địa chỉ kết thúc và địa chỉ cung tròn. Tăng giá trị 'Allowable circular interpolation error width' (Pr.41) nếu cần.", Source = "4" };
            ErrorDb["506"] = e_44;
            var e_45 = new ErrorDetails { Code = "507", Type = "Lỗi", Description = "Giới hạn hành trình phần mềm +", Cause = "Địa chỉ đích hoặc vị trí hiện tại vượt quá giới hạn hành trình phần mềm trên.", Remedy = "Kiểm tra dữ liệu định vị và thiết lập giới hạn hành trình phần mềm.", Source = "4" };
            ErrorDb["507"] = e_45;
            var e_46 = new ErrorDetails { Code = "508", Type = "Lỗi", Description = "Giới hạn hành trình phần mềm -", Cause = "Địa chỉ đích hoặc vị trí hiện tại vượt quá giới hạn hành trình phần mềm dưới.", Remedy = "Kiểm tra dữ liệu định vị và thiết lập giới hạn hành trình phần mềm.", Source = "4" };
            ErrorDb["508"] = e_46;
            var e_47 = new ErrorDetails { Code = "513", Type = "Cảnh báo", Description = "Khoảng cách di chuyển không đủ", Cause = "Khoảng cách di chuyển quá nhỏ so với tốc độ đích, không đủ để thực hiện giảm tốc tự động.", Remedy = "Giảm tốc độ lệnh hoặc tăng khoảng cách di chuyển.", Source = "4" };
            ErrorDb["513"] = e_47;
            var e_48 = new ErrorDetails { Code = "514", Type = "Lỗi", Description = "Nằm ngoài phạm vi giá trị hiện tại mới", Cause = "Giá trị thay đổi hiện tại mới nằm ngoài phạm vi cho phép (0 đến 359.99999 khi đơn vị là độ).", Remedy = "Đặt giá trị hiện tại mới trong phạm vi quy định.", Source = "4" };
            ErrorDb["514"] = e_48;
            var e_49 = new ErrorDetails { Code = "515", Type = "Lỗi", Description = "Giá trị hiện tại mới không khả thi", Cause = "Cố gắng thay đổi giá trị hiện tại trong khi đang thực hiện điều khiển đường dẫn liên tục.", Remedy = "Không thực hiện thay đổi giá trị hiện tại trong chế độ điều khiển đường dẫn liên tục.", Source = "4" };
            ErrorDb["515"] = e_49;
            var e_50 = new ErrorDetails { Code = "516", Type = "Lỗi", Description = "Điều khiển đường dẫn liên tục không khả thi", Cause = "Thiết lập điều khiển đường dẫn liên tục cho các phương pháp không hỗ trợ như Fixed-feed hoặc Speed-position switching.", Remedy = "Thay đổi mẫu vận hành hoặc phương pháp điều khiển cho phù hợp.", Source = "4" };
            ErrorDb["516"] = e_50;
            var e_51 = new ErrorDetails { Code = "519", Type = "Lỗi", Description = "Nội suy trong khi trục nội suy đang bận (BUSY)", Cause = "Trục tham chiếu cố gắng bắt đầu nội suy trong khi trục nội suy đi kèm đang bận.", Remedy = "Đảm bảo trục nội suy không bận trước khi bắt đầu điều khiển từ trục tham chiếu.", Source = "4" };
            ErrorDb["519"] = e_51;
            var e_52 = new ErrorDetails { Code = "521", Type = "Lỗi", Description = "Lệnh mô tả nội suy không hợp lệ", Cause = "Trục nội suy được đặt trùng với trục tham chiếu hoặc kết hợp trục không hợp lệ.", Remedy = "Chỉ định một trục khác làm trục nội suy.", Source = "4" };
            ErrorDb["521"] = e_52;
            var e_53 = new ErrorDetails { Code = "523", Type = "Lỗi", Description = "Lỗi chế độ nội suy", Cause = "Chỉ định tốc độ tổng hợp cho các chế độ chỉ hỗ trợ tốc độ trục tham chiếu (ví dụ: nội suy 4 trục hoặc điều khiển tốc độ).", Remedy = "Đặt lại 'Interpolation speed designation method' thành tốc độ trục tham chiếu.", Source = "4" };
            ErrorDb["523"] = e_53;
            var e_54 = new ErrorDetails { Code = "524", Type = "Lỗi", Description = "Lỗi thiết lập hệ thống điều khiển", Cause = "Thay đổi số lượng trục nội suy hoặc kết hợp trục ở giữa dữ liệu định vị liên tục, hoặc thiết lập không được hỗ trợ trên phần cứng cũ.", Remedy = "Giữ nguyên các trục nội suy trong suốt chuỗi dữ liệu liên tục. Kiểm tra phiên bản module.", Source = "4" };
            ErrorDb["524"] = e_54;
            var e_55 = new ErrorDetails { Code = "525", Type = "Lỗi", Description = "Lỗi thiết lập điểm phụ", Cause = "Điểm phụ cung tròn trùng với điểm bắt đầu/kết thúc, hoặc nằm ngoài phạm vi, hoặc 3 điểm nằm trên đường thẳng.", Remedy = "Chỉnh sửa địa chỉ điểm phụ (arc address) sao cho nó tạo thành một cung tròn hợp lệ.", Source = "4" };
            ErrorDb["525"] = e_55;
            var e_56 = new ErrorDetails { Code = "526", Type = "Lỗi", Description = "Lỗi thiết lập điểm kết thúc", Cause = "Điểm kết thúc trùng với điểm bắt đầu trong nội suy cung tròn (trừ khi cố ý quay vòng tròn đầy đủ) hoặc nằm ngoài phạm vi.", Remedy = "Chỉnh sửa địa chỉ kết thúc (positioning address).", Source = "4" };
            ErrorDb["526"] = e_56;
            var e_57 = new ErrorDetails { Code = "527", Type = "Lỗi", Description = "Lỗi thiết lập điểm trung tâm", Cause = "Điểm trung tâm trùng với điểm bắt đầu hoặc điểm kết thúc, hoặc nằm ngoài phạm vi.", Remedy = "Chỉnh sửa địa chỉ trung tâm cung tròn (arc address).", Source = "4" };
            ErrorDb["527"] = e_57;
            var e_58 = new ErrorDetails { Code = "530", Type = "Lỗi", Description = "Nằm ngoài phạm vi địa chỉ", Cause = "Lượng di chuyển được đặt là giá trị âm trong điều khiển chuyển đổi Tốc độ-Vị trí.", Remedy = "Chỉ sử dụng giá trị dương cho lượng di chuyển sau khi chuyển đổi.", Source = "4" };
            ErrorDb["530"] = e_58;
            var e_59 = new ErrorDetails { Code = "533", Type = "Lỗi", Description = "Lỗi dữ liệu điều kiện", Cause = "Tham số 1 (P1) lớn hơn tham số 2 (P2) trong thiết lập phạm vi của dữ liệu điều kiện.", Remedy = "Đặt P1 nhỏ hơn hoặc bằng P2.", Source = "4" };
            ErrorDb["533"] = e_59;
            var e_60 = new ErrorDetails { Code = "535", Type = "Lỗi", Description = "Nội suy cung tròn không khả thi", Cause = "Cố gắng thực hiện nội suy cung tròn khi đơn vị điều khiển được đặt là 'độ'.", Remedy = "Thay đổi đơn vị điều khiển sang mm, inch hoặc pulse để thực hiện nội suy cung tròn.", Source = "4" };
            ErrorDb["535"] = e_60;
            var e_61 = new ErrorDetails { Code = "536", Type = "Lỗi", Description = "Bắt đầu khi tín hiệu M code đang Bật", Cause = "Một thao tác định vị mới được bắt đầu trong khi tín hiệu M code ON của trục đó vẫn đang Bật.", Remedy = "Tắt tín hiệu M code bằng lệnh 'M code OFF request' trước khi bắt đầu định vị mới.", Source = "4" };
            ErrorDb["536"] = e_61;
            var e_62 = new ErrorDetails { Code = "543", Type = "Lỗi", Description = "Nằm ngoài phạm vi số bắt đầu", Cause = "Sử dụng số bắt đầu khối (7000-7004) khi đang thực hiện chức năng bắt đầu đọc trước (Pre-reading).", Remedy = "Sử dụng số bắt đầu từ 1-600 khi thực hiện chức năng đọc trước.", Source = "4" };
            ErrorDb["543"] = e_62;
            var e_63 = new ErrorDetails { Code = "544", Type = "Lỗi", Description = "Nằm ngoài phạm vi bán kính", Cause = "Bán kính cung tròn tính toán vượt quá 536870912.", Remedy = "Điều chỉnh các điểm bắt đầu, kết thúc hoặc điểm phụ để giảm bán kính cung tròn.", Source = "4" };
            ErrorDb["544"] = e_63;
            var e_64 = new ErrorDetails { Code = "545", Type = "Lỗi", Description = "Lỗi thiết lập vòng lặp (LOOP)", Cause = "Số chu kỳ lặp lại được đặt là 0.", Remedy = "Đặt số chu kỳ lặp lại là một số nguyên dương (1-65535).", Source = "4" };
            ErrorDb["545"] = e_64;
            var e_65 = new ErrorDetails { Code = "546", Type = "Lỗi", Description = "Thiết lập hướng ABS trong đơn vị độ không hợp lệ", Cause = "Chỉ định hướng quay ABS trong khi giới hạn hành trình phần mềm đang có hiệu lực.", Remedy = "Vô hiệu hóa giới hạn hành trình phần mềm (đặt giới hạn trên = giới hạn dưới) trước khi sử dụng chức năng chỉ định hướng quay ABS.", Source = "4" };
            ErrorDb["546"] = e_65;
            var e_66 = new ErrorDetails { Code = "805", Type = "Lỗi", Description = "Lỗi số lần ghi Flash ROM", Cause = "Số lần ghi vào Flash ROM vượt quá 25 lần kể từ khi bật nguồn.", Remedy = "Hạn chế số lần ghi vào Flash ROM. Reset lỗi để xóa bộ đếm tạm thời.", Source = "4" };
            ErrorDb["805"] = e_66;
            var e_67 = new ErrorDetails { Code = "910", Type = "Lỗi", Description = "Nằm ngoài phạm vi giới hạn tốc độ", Cause = "Tốc độ OPR hoặc tốc độ định vị vượt quá giá trị giới hạn tốc độ được thiết lập trong tham số.", Remedy = "Điều chỉnh tốc độ lệnh hoặc tăng giá trị 'Speed limit value' (Pr.8).", Source = "4" };
            ErrorDb["910"] = e_67;
            var e_68 = new ErrorDetails { Code = "935", Type = "Lỗi", Description = "Lỗi lựa chọn chức năng Tốc độ-Vị trí", Cause = "Thiết lập sai kết hợp giữa đơn vị, giới hạn hành trình hoặc cập nhật giá trị hiện tại trong chế độ ABS.", Remedy = "Đảm bảo đơn vị là 'độ', giới hạn hành trình phần mềm bị vô hiệu hóa và cập nhật giá trị hiện tại được bật.", Source = "4" };
            ErrorDb["935"] = e_68;
            var e_69 = new ErrorDetails { Code = "956", Type = "Lỗi", Description = "Lỗi giới hạn tốc độ JOG", Cause = "Giá trị giới hạn tốc độ JOG được đặt cao hơn giới hạn tốc độ hệ thống (Pr.8).", Remedy = "Đặt giới hạn tốc độ JOG nhỏ hơn hoặc bằng 'Speed limit value' (Pr.8).", Source = "4" };
            ErrorDb["956"] = e_69;
            var e_70 = new ErrorDetails { Code = "4100", Type = "Lỗi", Description = "Lỗi giá trị (s) ngoài phạm vi.", Cause = "Giá trị BCD cho hướng dẫn BCD(P) không nằm trong phạm vi 0 đến 9999 hoặc bộ chia (s2) là 0; hoặc số lượng dữ liệu âm được chỉ định.", Remedy = "Kiểm tra lại các tham số đầu vào và đảm bảo giá trị nằm trong phạm vi tài liệu cho phép.", Source = "5" };
            ErrorDb["4100"] = e_70;
            var e_71 = new ErrorDetails { Code = "4101", Type = "Lỗi", Description = "Lỗi vượt quá phạm vi thiết bị.", Cause = "Phạm vi thiết bị được chỉ định vượt quá giới hạn của bộ nhớ module điều khiển hoặc chồng lấn vùng nhớ.", Remedy = "Điều chỉnh địa chỉ thiết bị hoặc số lượng điểm dữ liệu để không vượt quá dải địa chỉ của module.", Source = "5" };
            ErrorDb["4101"] = e_71;
            var e_72 = new ErrorDetails { Code = "4140", Type = "Lỗi", Description = "Lỗi dữ liệu số thực dấu phẩy động.", Cause = "Giá trị của thiết bị được chỉ định là -0, số không chuẩn (unnormalized), không phải là số (nonnumeric) hoặc vô cùng.", Remedy = "Kiểm tra tính hợp lệ của dữ liệu số thực trước khi thực hiện các phép toán so sánh hoặc chuyển đổi.", Source = "5" };
            ErrorDb["4140"] = e_72;
            var e_73 = new ErrorDetails { Code = "4141", Type = "Lỗi", Description = "Lỗi tràn số (Overflow).", Cause = "Kết quả của phép toán vượt quá phạm vi lưu trữ của số thực dấu phẩy động 32-bit hoặc 64-bit.", Remedy = "Kiểm tra thuật toán tính toán để đảm bảo kết quả nằm trong phạm vi hiển thị của hệ thống.", Source = "5" };
            ErrorDb["4141"] = e_73;
            var e_74 = new ErrorDetails { Code = "4200", Type = "Lỗi", Description = "Lỗi cấu trúc chương trình (FOR-NEXT).", Cause = "Lệnh FEND, GOEND hoặc STOP được thực hiện bên trong vòng lặp FOR-NEXT.", Remedy = "Đảm bảo các lệnh kết thúc chương trình nằm ngoài các cấu trúc lặp.", Source = "5" };
            ErrorDb["4200"] = e_74;
            var e_75 = new ErrorDetails { Code = "4210", Type = "Lỗi", Description = "Lỗi con trỏ (Pointer).", Cause = "Số con trỏ không tồn tại trong chương trình hoặc nhảy đến con trỏ ở file chương trình khác.", Remedy = "Khai báo lại nhãn con trỏ (P) chính xác trong cùng một file chương trình.", Source = "5" };
            ErrorDb["4210"] = e_75;
            var e_76 = new ErrorDetails { Code = "4235", Type = "Lỗi", Description = "Lỗi lệnh CHK.", Cause = "Sử dụng quá 150 tiếp điểm, lệnh CHK không đúng vị trí sau CHKST hoặc dùng lệnh CHK ở quá 2 vị trí trong file chương trình.", Remedy = "Xem lại sơ đồ thang (ladder) và tuân thủ các quy tắc lập trình lệnh chẩn đoán lỗi CHK.", Source = "5" };
            ErrorDb["4235"] = e_76;
            var e_77 = new ErrorDetails { Code = "1402", Type = "Lỗi", Description = "Lỗi phát hiện tại module chức năng thông minh.", Cause = "Module được chỉ định bởi n1 gặp sự cố trong quá trình thực hiện lệnh FROM/TO.", Remedy = "Kiểm tra trạng thái phần cứng của module và cáp kết nối.", Source = "5" };
            ErrorDb["1402"] = e_77;
            var e_78 = new ErrorDetails { Code = "2410", Type = "Lỗi", Description = "Tên file không tồn tại.", Cause = "File được chỉ định trong lệnh QDRSET hoặc SP_FWRITE không tồn tại trong ổ đĩa.", Remedy = "Kiểm tra lại tên file và số hiệu ổ đĩa (Drive No.) đã được cài đặt.", Source = "5" };
            ErrorDb["2410"] = e_78;
            var e_79 = new ErrorDetails { Code = "100", Type = "Lỗi", Description = "Vượt quá số lần thử lại ENQ.", Cause = "Nhiễu hệ thống hoặc sự cố đường truyền.", Remedy = "Thực hiện các biện pháp chống nhiễu.", Source = "6" };
            ErrorDb["100"] = e_79;
            var e_80 = new ErrorDetails { Code = "102", Type = "Lỗi", Description = "Vượt quá số lần thử lại NACK.", Cause = "Lỗi phản hồi từ thiết bị.", Remedy = "Kiểm tra lại thiết bị kết nối và nhiễu.", Source = "6" };
            ErrorDb["102"] = e_80;
            var e_81 = new ErrorDetails { Code = "103", Type = "Lỗi", Description = "Thông điệp quá dài.", Cause = "Kích thước gói tin vượt quá giới hạn cho phép.", Remedy = "Kiểm tra và điều chỉnh lại độ dài thông điệp.", Source = "6" };
            ErrorDb["103"] = e_81;
            var e_82 = new ErrorDetails { Code = "104", Type = "Lỗi", Description = "Hết thời gian chờ nhận dữ liệu (Reception time-out).", Cause = "Không nhận được phản hồi trong thời gian quy định.", Remedy = "Kiểm tra kết nối cáp.", Source = "6" };
            ErrorDb["104"] = e_82;
            var e_83 = new ErrorDetails { Code = "105", Type = "Lỗi", Description = "Không phát hiện tín hiệu DSR.", Cause = "Tín hiệu Data Set Ready bị mất.", Remedy = "Kiểm tra trạng thái thiết bị và cáp nối.", Source = "6" };
            ErrorDb["105"] = e_83;
            var e_84 = new ErrorDetails { Code = "106", Type = "Lỗi", Description = "Đường truyền bị ngắt kết nối.", Cause = "Cáp bị lỏng hoặc đứt, hoặc thiết bị ngoại vi bị tắt nguồn.", Remedy = "Kiểm tra kết nối cáp. Thực hiện mở lại (Open) cổng truyền thông.", Source = "6" };
            ErrorDb["106"] = e_84;
            var e_85 = new ErrorDetails { Code = "107", Type = "Lỗi", Description = "Hết thời gian chờ truyền dữ liệu (Transmission time-out).", Cause = "Không thể gửi dữ liệu đi trong thời gian cho phép.", Remedy = "Kiểm tra kết nối cáp.", Source = "6" };
            ErrorDb["107"] = e_85;
            var e_86 = new ErrorDetails { Code = "108", Type = "Lỗi", Description = "Số thứ tự (Sequence number) không chính xác.", Cause = "Dữ liệu bị sai lệch do nhiễu.", Remedy = "Thực hiện các biện pháp chống nhiễu.", Source = "6" };
            ErrorDb["108"] = e_86;
            var e_87 = new ErrorDetails { Code = "0x01010002", Type = "Lỗi", Description = "Lỗi hết thời gian chờ (Time-out error).", Cause = "Cáp hỏng, cài đặt thông số sai hoặc PLC không phản hồi.", Remedy = "Kiểm tra thuộc tính timeout, cài đặt trong tiện ích truyền thông, kiểm tra PLC, cài đặt module và cáp. Thử đóng và mở lại kết nối.", Source = "6" };
            ErrorDb["0x01010002"] = e_87;
            var e_88 = new ErrorDetails { Code = "0x01010010", Type = "Lỗi", Description = "Lỗi số trạm PLC (Programmable controller No. error).", Cause = "Không thể giao tiếp với số trạm đã chỉ định.", Remedy = "Kiểm tra lại số trạm đã thiết lập trong Communication Setup Utility và thuộc tính ActStationNumber.", Source = "6" };
            ErrorDb["0x01010010"] = e_88;
            var e_89 = new ErrorDetails { Code = "0x01802001", Type = "Lỗi", Description = "Lỗi thiết bị (Device error).", Cause = "Chuỗi ký tự thiết bị được chỉ định trong phương thức không hợp lệ.", Remedy = "Xem lại tên thiết bị đã nhập.", Source = "6" };
            ErrorDb["0x01802001"] = e_89;
            var e_90 = new ErrorDetails { Code = "0x01802002", Type = "Lỗi", Description = "Lỗi số thiết bị (Device number error).", Cause = "Số của thiết bị được chỉ định không hợp lệ.", Remedy = "Xem lại số thứ tự thiết bị.", Source = "6" };
            ErrorDb["0x01802002"] = e_90;
            var e_91 = new ErrorDetails { Code = "0x01802005", Type = "Lỗi", Description = "Lỗi kích thước (Size error).", Cause = "Số điểm (points) được chỉ định không hợp lệ.", Remedy = "Kiểm tra lại số điểm đã chỉ định trong phương thức; kiểm tra cài đặt module và trạng thái cáp.", Source = "6" };
            ErrorDb["0x01802005"] = e_91;
            var e_92 = new ErrorDetails { Code = "0x0180840B", Type = "Lỗi", Description = "Lỗi hết thời gian chờ (Time-out error).", Cause = "Hết thời gian chờ nhưng không nhận được dữ liệu.", Remedy = "Xem lại giá trị timeout, kiểm tra kết nối bằng lệnh Ping, kiểm tra PLC và module.", Source = "6" };
            ErrorDb["0x0180840B"] = e_92;
            var e_93 = new ErrorDetails { Code = "0xF1000001", Type = "Lỗi", Description = "Lỗi chuyển đổi mã ký tự (Character code conversion error).", Cause = "Chuyển đổi giữa UNICODE và mã ASCII thất bại.", Remedy = "Kiểm tra chuỗi ký tự chỉ định trong phương thức; kiểm tra lại hệ thống và cáp.", Source = "6" };
            ErrorDb["0xF1000001"] = e_93;
            var e_94 = new ErrorDetails { Code = "1160", Type = "Lỗi (Stop)", Description = "RAM ERROR (Lỗi chương trình)", Cause = "Dữ liệu chương trình đang thực thi không khớp với chương trình được ghi trong bộ nhớ chương trình do nhiễu hoặc hỏng bộ nhớ.", Remedy = "Thực hiện chức năng tự động phục hồi bộ nhớ đệm hoặc ghi lại chương trình vào bộ nhớ CPU. Kiểm tra môi trường hoạt động chống nhiễu.", Source = "7" };
            ErrorDb["1160"] = e_94;
            var e_95 = new ErrorDetails { Code = "1161", Type = "Lỗi (Stop)", Description = "RAM ERROR (Lỗi bộ nhớ thiết bị)", Cause = "CPU phát hiện sự thay đổi dữ liệu trong bộ nhớ thiết bị.", Remedy = "Ghi lại dữ liệu thiết bị hoặc reset CPU. Kiểm tra thông tin thay đổi dữ liệu trong SD927 và SD928.", Source = "7" };
            ErrorDb["1161"] = e_95;
            var e_96 = new ErrorDetails { Code = "1610", Type = "Cảnh báo/Lỗi", Description = "FLASH ROM ERROR", Cause = "Số lần ghi vào Standard ROM vượt quá 100,000 lần.", Remedy = "Thay thế CPU nếu cần thiết và hạn chế ghi dữ liệu vào ROM thường xuyên.", Source = "7" };
            ErrorDb["1610"] = e_96;
            var e_97 = new ErrorDetails { Code = "2200", Type = "Lỗi", Description = "MISSING PARA (Thiếu tham số)", Cause = "CPU bị khóa bởi khóa bảo mật và các tham số được lưu trong thẻ nhớ (SD) nhưng không có tham số trong bộ nhớ chương trình.", Remedy = "Kiểm tra lại vị trí lưu trữ tham số và cài đặt khóa bảo mật.", Source = "7" };
            ErrorDb["2200"] = e_97;
            var e_98 = new ErrorDetails { Code = "2213", Type = "Lỗi", Description = "BOOT ERROR (Lỗi khởi động)", Cause = "Có nhiều tệp khởi động nhưng mật khẩu tệp không khớp.", Remedy = "Kiểm tra lại mật khẩu tệp trong phần cài đặt mật khẩu tệp 32 ký tự.", Source = "7" };
            ErrorDb["2213"] = e_98;
            var e_99 = new ErrorDetails { Code = "2214", Type = "Lỗi", Description = "BOOT ERROR (Lỗi khởi động)", Cause = "Thực hiện thao tác khởi động (boot) trong khi CPU đang bị khóa bởi khóa bảo mật.", Remedy = "Mở khóa CPU bằng khóa bảo mật trước khi thực hiện khởi động.", Source = "7" };
            ErrorDb["2214"] = e_99;
            var e_100 = new ErrorDetails { Code = "2220", Type = "Lỗi", Description = "RESTORE ERROR (Lỗi phục hồi)", Cause = "Số lượng điểm thiết bị trong cài đặt tham số khác với số lượng tại thời điểm sao lưu.", Remedy = "Khôi phục lại trạng thái dữ liệu khi sao lưu tham số hoặc xóa dữ liệu sao lưu cũ và thực hiện sao lưu lại.", Source = "7" };
            ErrorDb["2220"] = e_100;
            var e_101 = new ErrorDetails { Code = "2221", Type = "Lỗi", Description = "RESTORE ERROR (Lỗi phục hồi)", Cause = "CPU bị mất điện hoặc bị reset trong quá trình sao lưu dữ liệu chốt (latch).", Remedy = "Thực hiện sao lưu lại dữ liệu và đảm bảo nguồn điện ổn định.", Source = "7" };
            ErrorDb["2221"] = e_101;
            var e_102 = new ErrorDetails { Code = "2225", Type = "Lỗi", Description = "RESTORE ERROR (Lỗi phục hồi)", Cause = "Model của CPU đích khác với CPU nguồn đã sao lưu.", Remedy = "Đảm bảo CPU đích và nguồn có cùng model.", Source = "7" };
            ErrorDb["2225"] = e_102;
            var e_103 = new ErrorDetails { Code = "2226", Type = "Lỗi", Description = "RESTORE ERROR (Lỗi phục hồi)", Cause = "Tệp sao lưu bị hỏng hoặc công tắc chống ghi trên thẻ nhớ đang bật.", Remedy = "Kiểm tra tính toàn vẹn của tệp hoặc tắt công tắc chống ghi trên thẻ nhớ.", Source = "7" };
            ErrorDb["2226"] = e_103;
            var e_104 = new ErrorDetails { Code = "2228", Type = "Lỗi", Description = "RESTORE ERROR (Không đủ bộ nhớ)", Cause = "Dung lượng trống của Standard RAM trên CPU đích không đủ để phục hồi dữ liệu sao lưu.", Remedy = "Giải phóng bộ nhớ Standard RAM hoặc lắp thêm thẻ nhớ SRAM mở rộng phù hợp.", Source = "7" };
            ErrorDb["2228"] = e_104;
            var e_105 = new ErrorDetails { Code = "3000", Type = "Lỗi", Description = "PARAMETER ERROR (Lỗi tham số)", Cause = "Cài đặt sai tổ hợp loại module gắn trên đế so với bảng gán I/O hoặc gán thiết bị cục bộ sai.", Remedy = "Kiểm tra lại cài đặt I/O Assignment và File Usability Setting trong PLC Parameter.", Source = "7" };
            ErrorDb["3000"] = e_105;
            var e_106 = new ErrorDetails { Code = "3002", Type = "Lỗi", Description = "PARAMETER ERROR (Lỗi tệp thanh ghi)", Cause = "Tệp thanh ghi tệp (File register) được chỉ định không tồn tại trên ổ đĩa hoặc chọn sai ổ đĩa lưu trữ (như chọn Standard ROM cho thanh ghi ghi được).", Remedy = "Tạo tệp thanh ghi tệp đúng tên và đúng ổ đĩa trong cài đặt PLC File.", Source = "7" };
            ErrorDb["3002"] = e_106;
            var e_107 = new ErrorDetails { Code = "4100", Type = "Lỗi vận hành (Operation Error)", Description = "OPERATION ERROR", Cause = "Ghi dữ liệu thời gian (DATEWR) ngoài dải cho phép hoặc chuyển đổi thanh ghi tệp không hợp lệ.", Remedy = "Kiểm tra giá trị đầu vào cho lệnh và cài đặt thanh ghi tệp.", Source = "7" };
            ErrorDb["4100"] = e_107;
            var e_108 = new ErrorDetails { Code = "4101", Type = "Lỗi vận hành (Operation Error)", Description = "OPERATION ERROR", Cause = "Truy cập thanh ghi tệp vượt quá kích thước đã đăng ký hoặc số thiết bị vượt quá dải cài đặt do hiệu chỉnh chỉ số (index modification).", Remedy = "Kiểm tra kích thước tệp thanh ghi đã đăng ký (SD647) và giới hạn index modification.", Source = "7" };
            ErrorDb["4101"] = e_108;
            var e_109 = new ErrorDetails { Code = "4109", Type = "Lỗi", Description = "Online communication timeout", Cause = "Xung đột khi nhiều ứng dụng truy cập cùng một lộ trình giao tiếp trong khi đang đặt điều kiện giám sát.", Remedy = "Đảm bảo chỉ có một ứng dụng thực hiện giám sát có điều kiện hoặc kiểm tra lại lộ trình kết nối.", Source = "7" };
            ErrorDb["4109"] = e_109;
            var e_110 = new ErrorDetails { Code = "5010", Type = "Cảnh báo (Continue)", Description = "PRG. TIME OVER", Cause = "Thời gian quét (scan time) thực tế dài hơn thời gian quét không đổi (constant scan) đã cài đặt.", Remedy = "Tăng giá trị cài đặt Constant Scan trong PLC RAS tab hoặc tối ưu hóa chương trình.", Source = "7" };
            ErrorDb["5010"] = e_110;
            var e_111 = new ErrorDetails { Code = "11", Type = "Lỗi (Protective Function)", Description = "Bảo vệ sụt áp nguồn điều khiển", Cause = "Điện áp nguồn thấp; Mất điện tức thời; Thiếu công suất nguồn do dòng khởi động khi bật nguồn chính; Lỗi driver.", Remedy = "Đo điện áp tại L1C và L2C; Tăng công suất nguồn; Thay thế driver mới.", Source = "8" };
            ErrorDb["11"] = e_111;
            var e_112 = new ErrorDetails { Code = "12", Type = "Lỗi (Protective Function)", Description = "Bảo vệ quá áp", Cause = "Điện áp nguồn vượt quá mức cho phép; Tăng vọt điện áp do tụ bù hoặc UPS; Đứt dây điện trở xả; Điện trở xả ngoài không phù hợp; Lỗi driver.", Remedy = "Đo điện áp L1, L2, L3; Nhập điện áp đúng; Kiểm tra điện trở xả ngoài (thay thế nếu giá trị là vô hạn); Thay đổi điện trở xả phù hợp; Thay driver.", Source = "8" };
            ErrorDb["12"] = e_112;
            var e_113 = new ErrorDetails { Code = "13", Type = "Lỗi (Protective Function)", Description = "Bảo vệ sụt áp nguồn chính", Cause = "Mất điện tức thời lâu hơn cài đặt Pr6D; Điện áp nguồn chính thấp; Thiếu công suất nguồn; Mất pha (đầu vào 1 pha cho driver 3 pha); Lỗi driver.", Remedy = "Đo điện áp L1, L2, L3; Tăng công suất nguồn; Kiểm tra cài đặt Pr6D; Kết nối đúng các pha nguồn.", Source = "8" };
            ErrorDb["13"] = e_113;
            var e_114 = new ErrorDetails { Code = "14", Type = "Lỗi (Protective Function)", Description = "Bảo vệ quá dòng", Cause = "Lỗi driver (mạch, IGBT); Ngắn mạch dây động cơ (U, V, W); Lỗi chạm đất; Cháy động cơ; Tiếp xúc dây kém; Vận hành Servo-ON/OFF quá thường xuyên; Quá nhiệt mạch phanh động năng (F-frame).", Remedy = "Ngắt kết nối động cơ và kiểm tra driver; Kiểm tra ngắn mạch và đấu dây động cơ; Đo điện trở cách điện; Kiểm tra sự cân bằng điện trở các pha; Thay driver/động cơ.", Source = "8" };
            ErrorDb["14"] = e_114;
            var e_115 = new ErrorDetails { Code = "15", Type = "Lỗi (Protective Function)", Description = "Bảo vệ quá nhiệt", Cause = "Nhiệt độ tản nhiệt hoặc thiết bị công suất vượt mức; Nhiệt độ môi trường quá cao; Quá tải.", Remedy = "Cải thiện nhiệt độ môi trường và điều kiện làm việc; Tăng công suất driver và động cơ; Tăng thời gian tăng/giảm tốc.", Source = "8" };
            ErrorDb["15"] = e_115;
            var e_116 = new ErrorDetails { Code = "16", Type = "Lỗi (Protective Function)", Description = "Bảo vệ quá tải", Cause = "Tải quá nặng vượt định mức trong thời gian dài; Cài đặt thông số (Pr20) không đúng gây rung lắc; Đấu dây sai; Phanh điện từ vẫn đóng; Va chạm máy hoặc kẹt cơ khí.", Remedy = "Tăng công suất driver/động cơ; Điều chỉnh lại thông số; Kiểm tra sơ đồ đấu dây (U, V, W); Kiểm tra cơ khí và phanh; Đặt lại Pr72 về 0.", Source = "8" };
            ErrorDb["16"] = e_116;
            var e_117 = new ErrorDetails { Code = "18", Type = "Lỗi (Protective Function)", Description = "Bảo vệ quá tải xả (quá tái sinh)", Cause = "Năng lượng tái sinh vượt quá khả năng của điện trở xả; Quán tính tải lớn; Tốc độ động cơ quá cao; Giới hạn hoạt động điện trở xả ngoài bị vượt quá.", Remedy = "Kiểm tra hệ số tải xả trên monitor; Tăng thời gian giảm tốc; Hạ tốc độ động cơ; Sử dụng điện trở xả ngoài và cài đặt Pr6C.", Source = "8" };
            ErrorDb["18"] = e_117;
            var e_118 = new ErrorDetails { Code = "21", Type = "Lỗi (Protective Function)", Description = "Lỗi giao tiếp bộ mã hóa (Encoder)", Cause = "Giao tiếp giữa bộ mã hóa và driver bị gián đoạn; Phát hiện đứt dây; Lỗi kết nối chân connector.", Remedy = "Đấu dây lại theo sơ đồ; Kiểm tra nguồn cấp encoder DC 5V; Tách riêng cáp encoder và cáp động cơ; Kết nối vỏ chống nhiễu với FG.", Source = "8" };
            ErrorDb["21"] = e_118;
            var e_119 = new ErrorDetails { Code = "24", Type = "Lỗi (Protective Function)", Description = "Bảo vệ quá lệch vị trí", Cause = "Động cơ không theo kịp lệnh; Chênh lệch xung vượt cài đặt Pr70; Điều chỉnh gain kém; Momen đầu ra bị giới hạn (Pr5E/5F).", Remedy = "Kiểm tra động cơ theo xung lệnh; Kiểm tra momen đầu ra; Điều chỉnh gain; Tăng giá trị Pr70 hoặc đặt về 0 (vô hiệu hóa).", Source = "8" };
            ErrorDb["24"] = e_119;
            var e_120 = new ErrorDetails { Code = "25", Type = "Lỗi (Protective Function)", Description = "Bảo vệ lỗi lệch lai (Hybrid deviation)", Cause = "Vị trí tải (thước đo ngoài) và vị trí động cơ lệch quá cài đặt Pr7B; Kết nối giữa động cơ và tải lỏng lẻo; Cài đặt tỷ lệ thước ngoài sai.", Remedy = "Kiểm tra kết nối động cơ và tải; Kiểm tra chiều thước đo và cài đặt thông số Pr78, 79, 7A, 7C.", Source = "8" };
            ErrorDb["25"] = e_120;
            var e_121 = new ErrorDetails { Code = "Over-regeneration alarm", Type = "Cảnh báo (Alarm)", Description = "Cảnh báo quá tái sinh", Cause = "Tải tái sinh đạt hơn 85% mức kích hoạt bảo vệ quá tái sinh.", Remedy = "Kiểm tra điều kiện hoạt động, giảm quán tính hoặc kéo dài thời gian giảm tốc.", Source = "8" };
            ErrorDb["Over-regeneration alarm"] = e_121;
            var e_122 = new ErrorDetails { Code = "Overload alarm", Type = "Cảnh báo (Alarm)", Description = "Cảnh báo quá tải", Cause = "Tải đạt hơn 85% mức kích hoạt bảo vệ quá tải.", Remedy = "Kiểm tra cơ khí, giảm tải hoặc tăng công suất động cơ.", Source = "8" };
            ErrorDb["Overload alarm"] = e_122;
            var e_123 = new ErrorDetails { Code = "Battery alarm", Type = "Cảnh báo (Alarm)", Description = "Cảnh báo pin", Cause = "Điện áp pin cho bộ mã hóa tuyệt đối giảm xuống dưới mức cảnh báo (khoảng 3.2V).", Remedy = "Thay pin mới cho bộ mã hóa tuyệt đối.", Source = "8" };
            ErrorDb["Battery alarm"] = e_123;
            var e_124 = new ErrorDetails { Code = "7101H", Type = "Lỗi", Description = "Lỗi hệ thống", Cause = "Hệ điều hành (OS) của module Q series C24 phát hiện thấy một số lỗi.", Remedy = "Kiểm tra tình trạng lắp đặt module, nguồn điện và môi trường hoạt động. Nếu lỗi vẫn tiếp tục, hãy liên hệ đại diện Mitsubishi.", Source = "9" };
            ErrorDb["7101H"] = e_124;
            var e_125 = new ErrorDetails { Code = "7103H", Type = "Lỗi", Description = "Lỗi truy cập bộ điều khiển lập trình", Cause = "Không thể giao tiếp với CPU của module Q series C24.", Remedy = "Tăng thời gian watchdog timer (timer 1). Thực hiện kiểm tra self-loopback để kiểm tra CPU.", Source = "9" };
            ErrorDb["7103H"] = e_125;
            var e_126 = new ErrorDetails { Code = "7140H", Type = "Lỗi", Description = "Lỗi dữ liệu yêu cầu", Cause = "Số lượng điểm yêu cầu vượt quá phạm vi lệnh hoặc thiết bị từ xa không hợp lệ.", Remedy = "Kiểm tra và sửa tin nhắn truyền của thiết bị ngoại vi. Xóa thông tin CPU và thử lại.", Source = "9" };
            ErrorDb["7140H"] = e_126;
            var e_127 = new ErrorDetails { Code = "7142H", Type = "Lỗi", Description = "Lỗi tên thiết bị", Cause = "Một thiết bị không thể định danh bởi lệnh đã cho đã được chỉ định.", Remedy = "Kiểm tra và sửa tin nhắn truyền của thiết bị ngoại vi. Xóa thông tin CPU và thử lại.", Source = "9" };
            ErrorDb["7142H"] = e_127;
            var e_128 = new ErrorDetails { Code = "714AH", Type = "Lỗi", Description = "Không thể thực hiện lệnh khi đang RUN", Cause = "Lệnh ghi được chỉ định khi thiết lập 'Cấm ghi khi đang RUN'.", Remedy = "Thay đổi cài đặt thành 'Cho phép ghi khi đang RUN' hoặc dừng CPU trước khi truyền dữ liệu.", Source = "9" };
            ErrorDb["714AH"] = e_128;
            var e_129 = new ErrorDetails { Code = "7D00H", Type = "Lỗi", Description = "Lỗi cài đặt số hiệu giao thức (Protocol No.)", Cause = "Trong dữ liệu điều khiển của lệnh CPRTCL, số hiệu giao thức chỉ định nằm ngoài phạm vi.", Remedy = "Chỉnh sửa lại cài đặt số hiệu giao thức.", Source = "9" };
            ErrorDb["7D00H"] = e_129;
            var e_130 = new ErrorDetails { Code = "7D12H", Type = "Lỗi", Description = "Lỗi quá thời gian giám sát truyền dẫn", Cause = "Thời gian giám sát truyền đã hết. Việc truyền dữ liệu không thành công sau số lần thử lại đã chỉ định.", Remedy = "Kiểm tra xem truyền dẫn có bị gián đoạn do kiểm soát DTR không. Kiểm tra tín hiệu CS và cáp kết nối.", Source = "9" };
            ErrorDb["7D12H"] = e_130;
            var e_131 = new ErrorDetails { Code = "7D13H", Type = "Lỗi", Description = "Lỗi quá thời gian chờ nhận dữ liệu", Cause = "Thời gian chờ nhận đã hết hạn.", Remedy = "Kiểm tra cáp kết nối, lỗi ở thiết bị gửi hoặc sử dụng chức năng circuit trace để kiểm tra dữ liệu từ thiết bị khác.", Source = "9" };
            ErrorDb["7D13H"] = e_131;
            var e_132 = new ErrorDetails { Code = "7F24H", Type = "Lỗi", Description = "Lỗi mã kiểm tra tổng (Sum check error)", Cause = "Mã kiểm tra tổng tính toán được không khớp với mã nhận được.", Remedy = "Kiểm tra mã kiểm tra tổng của thiết bị ngoại vi hoặc cài đặt định dạng gói tin trong GX Configurator-SC.", Source = "9" };
            ErrorDb["7F24H"] = e_132;
            var e_133 = new ErrorDetails { Code = "7F31H", Type = "Lỗi", Description = "Lỗi truyền dẫn đồng thời", Cause = "Module C24 và thiết bị ngoại vi bắt đầu truyền dữ liệu cùng một lúc.", Remedy = "Xử lý theo thỏa thuận với thiết bị ngoại vi hoặc thay đổi cài đặt chỉ định dữ liệu truyền đồng thời trong buffer memory.", Source = "9" };
            ErrorDb["7F31H"] = e_133;
            var e_134 = new ErrorDetails { Code = "7F68H", Type = "Lỗi", Description = "Lỗi khung hình (Framing error)", Cause = "Dữ liệu không khớp với cài đặt stop bit hoặc do nhiễu mạng.", Remedy = "Khớp cài đặt giữa module C24 và thiết bị ngoại vi. Thực hiện xóa lỗi qua tín hiệu YE/YF.", Source = "9" };
            ErrorDb["7F68H"] = e_134;
            var e_135 = new ErrorDetails { Code = "7F69H", Type = "Lỗi", Description = "Lỗi chẵn lẻ (Parity error)", Cause = "Dữ liệu không khớp với cài đặt parity bit.", Remedy = "Khớp cài đặt parity giữa module C24 và thiết bị ngoại vi. Kiểm tra và biện pháp chống nhiễu.", Source = "9" };
            ErrorDb["7F69H"] = e_135;
            var e_136 = new ErrorDetails { Code = "7FEFH", Type = "Lỗi", Description = "Lỗi cài đặt công tắc (Switch setting error)", Cause = "Có lỗi trong việc cài đặt công tắc thông qua GX Developer.", Remedy = "Chỉnh sửa giá trị cài đặt công tắc trong tham số và khởi động lại PLC.", Source = "9" };
            ErrorDb["7FEFH"] = e_136;
            var e_137 = new ErrorDetails { Code = "7FF0H", Type = "Lỗi", Description = "Lỗi thực hiện đồng thời các lệnh chuyên dụng", Cause = "Thực hiện các lệnh chuyên dụng đồng thời trên cùng một kênh.", Remedy = "Không sử dụng đồng thời các lệnh chuyên dụng trên cùng một kênh giao tiếp.", Source = "9" };
            ErrorDb["7FF0H"] = e_137;
            var e_138 = new ErrorDetails { Code = "4100", Type = "Lỗi", Description = "Giá trị đối số n nằm ngoài phạm vi cho phép trong lệnh ghi dữ liệu.", Cause = "Giá trị của n nằm ngoài phạm vi từ 1 đến 10 trong lệnh LOGTRG hoặc LOGTRGR.", Remedy = "Kiểm tra và điều chỉnh giá trị n để đảm bảo nằm trong phạm vi từ 1 đến 10.", Source = "10" };
            ErrorDb["4100"] = e_138;
            var e_139 = new ErrorDetails { Code = "Khác 0", Type = "Lỗi", Description = "Lỗi hoàn thành lệnh phục hồi vị trí tuyệt đối (Z_ABRST).", Cause = "Sự cố trong quá trình giao tiếp với bộ khuếch đại servo hoặc thiết lập dữ liệu không hợp lệ.", Remedy = "Kiểm tra mã lỗi cụ thể được lưu trữ trong thiết bị (s)10 và tham khảo tài liệu kỹ thuật của bộ điều khiển vị trí.", Source = "10" };
            ErrorDb["Khác 0"] = e_139;
            var e_140 = new ErrorDetails { Code = "Khác 0", Type = "Lỗi", Description = "Lỗi khi thực hiện lệnh bắt đầu vị trí (ZP_PSTRT).", Cause = "Số bắt đầu (Start No.) không hợp lệ hoặc module đang ở trạng thái lỗi.", Remedy = "Kiểm tra mã lỗi trong (s)10 và xác nhận số bắt đầu nằm trong phạm vi cho phép (1-600, 7000-7004, 9001-9004).", Source = "10" };
            ErrorDb["Khác 0"] = e_140;
            var e_141 = new ErrorDetails { Code = "Khác 0", Type = "Lỗi", Description = "Lỗi lệnh dạy (Teaching - ZP_TEACH).", Cause = "Số dữ liệu vị trí không chính xác hoặc điều kiện thực hiện lệnh không thỏa mãn.", Remedy = "Xác minh số dữ liệu vị trí (1 đến 600) và kiểm tra mã lỗi tại (s)10.", Source = "10" };
            ErrorDb["Khác 0"] = e_141;
            var e_142 = new ErrorDetails { Code = "Khác 0", Type = "Lỗi", Description = "Lỗi ghi vào Flash ROM (ZP_PFWRT).", Cause = "Lỗi phần cứng Flash ROM hoặc module chưa sẵn sàng.", Remedy = "Đợi module ở trạng thái sẵn sàng và kiểm tra mã lỗi tại (s)10.", Source = "10" };
            ErrorDb["Khác 0"] = e_142;
            var e_143 = new ErrorDetails { Code = "Khác 0", Type = "Lỗi", Description = "Lỗi khởi tạo dữ liệu thiết lập (ZP_PINIT).", Cause = "Dữ liệu thiết lập bị hỏng hoặc lỗi bộ nhớ đệm.", Remedy = "Kiểm tra mã lỗi tại (s)10 để xác định nguyên nhân chi tiết.", Source = "10" };
            ErrorDb["Khác 0"] = e_143;
            var e_144 = new ErrorDetails { Code = "C1009", Type = "Lỗi chuyển đổi", Description = "Tồn tại ký tự không thể phân tích.", Cause = "Định dạng sai hoặc sử dụng ký tự không được hỗ trợ (ví dụ: , !, \\", Remedy = ").", Source = "Chỉnh sửa lại chuỗi ký tự." };
            ErrorDb["C1009"] = e_144;
            var e_145 = new ErrorDetails { Code = "C1010", Type = "Lỗi chuyển đổi", Description = "Tồn tại toán tử không thể phân tích.", Cause = "Sử dụng toán tử sai quy cách.", Remedy = "Chỉnh sửa lại toán tử.", Source = "11" };
            ErrorDb["C1010"] = e_145;
            var e_146 = new ErrorDetails { Code = "C1013", Type = "Lỗi chuyển đổi", Description = "Hằng số số thực bị sai.", Cause = "Mô tả hằng số số thực không hợp lệ (ví dụ: 1., 0.1E).", Remedy = "Chỉnh sửa lại mô tả hằng số số thực.", Source = "11" };
            ErrorDb["C1013"] = e_146;
            var e_147 = new ErrorDetails { Code = "C1014", Type = "Lỗi chuyển đổi", Description = "Mô tả thiết bị (device) bị sai.", Cause = "Chỉ định số bit của thiết bị word sai hoặc ký tự thiết bị không hợp lệ.", Remedy = "Chỉnh sửa lại mô tả thiết bị.", Source = "11" };
            ErrorDb["C1014"] = e_147;
            var e_148 = new ErrorDetails { Code = "C1018", Type = "Lỗi chuyển đổi", Description = "Mô tả chú thích (comment) bị sai.", Cause = "Không viết đúng định dạng (* *) hoặc thiếu dấu ngoặc/dấu sao.", Remedy = "Chỉnh sửa lại mô tả chú thích.", Source = "11" };
            ErrorDb["C1018"] = e_148;
            var e_149 = new ErrorDetails { Code = "C1028", Type = "Lỗi chuyển đổi", Description = "Biến chưa được định nghĩa.", Cause = "Sử dụng nhãn (label) mà không khai báo hoặc dùng sai ký tự trong hệ thập lục phân.", Remedy = "Khai báo biến trước khi sử dụng.", Source = "11" };
            ErrorDb["C1028"] = e_149;
            var e_150 = new ErrorDetails { Code = "C1033", Type = "Lỗi chuyển đổi", Description = "Lỗi chỉ định phần tử mảng.", Cause = "Phương pháp chỉ định phần tử mảng sai định dạng so với định nghĩa.", Remedy = "Chỉnh sửa lại mô tả mảng.", Source = "11" };
            ErrorDb["C1033"] = e_150;
            var e_151 = new ErrorDetails { Code = "C2021", Type = "Lỗi chuyển đổi", Description = "Sử dụng sai hằng số trong đối số.", Cause = "Sử dụng giá trị khác hằng số cho đối số yêu cầu phải là hằng số.", Remedy = "Sử dụng hằng số trong đối số được chỉ định.", Source = "11" };
            ErrorDb["C2021"] = e_151;
            var e_152 = new ErrorDetails { Code = "C2054", Type = "Lỗi chuyển đổi", Description = "Lỗi cú pháp (Syntax error).", Cause = "Mô tả ngữ pháp sai (thiếu dấu =, dùng sai toán tử, sai cấu trúc mảng/cấu trúc điều khiển).", Remedy = "Chỉnh sửa lại ngữ pháp cho đúng.", Source = "11" };
            ErrorDb["C2054"] = e_152;
            var e_153 = new ErrorDetails { Code = "C8006", Type = "Lỗi chuyển đổi", Description = "Thiếu từ khóa kết thúc.", Cause = "Thiếu các từ khóa như END_IF, END_FOR, END_WHILE hoặc dấu ;.", Remedy = "Thêm từ khóa kết thúc hoặc dấu ; tương ứng.", Source = "11" };
            ErrorDb["C8006"] = e_153;
            var e_154 = new ErrorDetails { Code = "C8021", Type = "Lỗi chuyển đổi", Description = "Kiểu dữ liệu chỉ số mảng không hợp lệ.", Cause = "Sử dụng kiểu dữ liệu khác INT cho số phần tử của biến mảng.", Remedy = "Thay đổi kiểu dữ liệu của chỉ số phần tử thành kiểu word (INT).", Source = "11" };
            ErrorDb["C8021"] = e_154;
            var e_155 = new ErrorDetails { Code = "C8022", Type = "Lỗi chuyển đổi", Description = "Chỉ số mảng vượt quá phạm vi.", Cause = "Số phần tử được chỉ định vượt quá phạm vi định nghĩa của mảng.", Remedy = "Thay đổi số phần tử nằm trong phạm vi định nghĩa mảng.", Source = "11" };
            ErrorDb["C8022"] = e_155;
            var e_156 = new ErrorDetails { Code = "C9017", Type = "Lỗi chuyển đổi", Description = "Quá nhiều tầng lồng nhau hoặc điều kiện quá dài.", Cause = "Vượt quá giới hạn lồng nhau (ví dụ: IF > 598 cấp, FOR > 299 cấp) hoặc quá nhiều giá trị lựa chọn trong CASE.", Remedy = "Rút ngắn chương trình, giảm số lượng cấp lồng nhau hoặc điều kiện.", Source = "11" };
            ErrorDb["C9017"] = e_156;
            var e_157 = new ErrorDetails { Code = "C9065", Type = "Lỗi chuyển đổi", Description = "Lỗi chia cho số 0.", Cause = "Sử dụng 0 làm số chia trong phép toán.", Remedy = "Sửa lại phần số chia khác 0.", Source = "11" };
            ErrorDb["C9065"] = e_157;
            var e_158 = new ErrorDetails { Code = "F0102", Type = "Lỗi chuyển đổi", Description = "Số lượng ký tự vượt quá tối đa.", Cause = "Số lượng ký tự sử dụng lớn hơn 32 ký tự.", Remedy = "Thay đổi chuỗi ký tự nằm trong phạm vi 32 ký tự.", Source = "11" };
            ErrorDb["F0102"] = e_158;
            var e_159 = new ErrorDetails { Code = "2000", Type = "Lỗi (UNIT VERIFY ERROR)", Description = "Lỗi xác nhận đơn vị (Unit verify error) xảy ra khi module QCPU phiên bản chức năng A được sử dụng trong hệ thống đa CPU.", Cause = "Sử dụng module QCPU phiên bản chức năng A trong hệ thống đa CPU.", Remedy = "Để cấu hình hệ thống đa CPU với các QCPU, hãy sử dụng các module CPU phiên bản chức năng B trở lên.", Source = "12" };
            ErrorDb["2000"] = e_159;
            var e_160 = new ErrorDetails { Code = "2110", Type = "Lỗi (SP.UNIT ERROR)", Description = "Lỗi module chức năng đặc biệt khi truy cập vào module CPU không được lắp đặt thực tế.", Cause = "Truy cập vào module CPU không thực sự được lắp đặt bằng cách sử dụng các lệnh thiết bị vùng truyền cyclic (U3En\\G).", Remedy = "Kiểm tra cấu hình lắp đặt thực tế và đảm bảo địa chỉ I/O của CPU trong lệnh là chính xác.", Source = "12" };
            ErrorDb["2110"] = e_160;
            var e_161 = new ErrorDetails { Code = "2114", Type = "Lỗi (SP.UNIT ERROR)", Description = "Lỗi module chức năng đặc biệt liên quan đến việc đọc/ghi bộ nhớ chia sẻ CPU trên High Performance QCPU hoặc Process CPU.", Cause = "Thực hiện ghi bằng lệnh thiết bị vùng truyền cyclic (U3En\\G) hoặc thực hiện đọc bằng bất kỳ lệnh đọc nào vào bộ nhớ chia sẻ CPU của chính nó trên module High Performance model QCPU hoặc Process CPU.", Remedy = "Sử dụng lệnh S.TO để ghi. Lưu ý rằng High Performance model QCPU hoặc Process CPU không hỗ trợ đọc bộ nhớ chia sẻ của chính nó bằng lệnh đọc.", Source = "12" };
            ErrorDb["2114"] = e_161;
            var e_162 = new ErrorDetails { Code = "2115", Type = "Lỗi (SP.UNIT ERROR)", Description = "Lỗi module chức năng đặc biệt khi ghi vào bộ nhớ chia sẻ của CPU khác.", Cause = "Cố gắng ghi dữ liệu vào bộ nhớ chia sẻ CPU của các module CPU khác bằng lệnh ghi (TO, S.TO hoặc U3En\\G).", Remedy = "Không ghi trực tiếp vào bộ nhớ chia sẻ của CPU khác. Dữ liệu chỉ nên được ghi bởi CPU sở hữu bộ nhớ đó và được đọc bởi các CPU khác.", Source = "12" };
            ErrorDb["2115"] = e_162;
            var e_163 = new ErrorDetails { Code = "2116", Type = "Lỗi (SP.UNIT ERROR)", Description = "Lỗi module chức năng đặc biệt khi ghi vào bộ nhớ đệm của module điều khiển bởi CPU khác.", Cause = "Dữ liệu được ghi vào bộ nhớ đệm của một module chức năng thông minh đang được điều khiển bởi một module CPU khác.", Remedy = "Chỉ ghi dữ liệu vào bộ nhớ đệm của module từ CPU được thiết lập là CPU điều khiển (Control CPU) của module đó.", Source = "12" };
            ErrorDb["2116"] = e_163;
            var e_164 = new ErrorDetails { Code = "2124", Type = "Lỗi (SP.UNIT LAY ERR)", Description = "Lỗi lắp đặt module (SP.UNIT LAY ERR) do vượt quá số lượng module I/O tối đa.", Cause = "Số lượng module I/O được lắp đặt vượt quá giới hạn tối đa cho phép (ví dụ: 25 hoặc 65 trừ đi số lượng CPU tùy cấu hình).", Remedy = "Giảm số lượng module I/O lắp đặt hoặc kiểm tra lại cấu hình hệ thống để đảm bảo nằm trong giới hạn cho phép.", Source = "12" };
            ErrorDb["2124"] = e_164;
            var e_165 = new ErrorDetails { Code = "2125", Type = "Lỗi (SP.UNIT LAY ERROR)", Description = "Lỗi lắp đặt module chức năng đặc biệt liên quan đến phiên bản chức năng CPU.", Cause = "Xảy ra lỗi ở các CPU khác CPU số 1 khi có sự không tương thích về phiên bản chức năng A và B giữa các CPU.", Remedy = "Đảm bảo tất cả các module QCPU trong hệ thống đa CPU đều từ phiên bản chức năng B trở lên.", Source = "12" };
            ErrorDb["2125"] = e_165;
            var e_166 = new ErrorDetails { Code = "2150", Type = "Lỗi (SP.UNIT VER.ERR)", Description = "Lỗi phiên bản module (SP.UNIT VER.ERR) khiến hệ thống đa CPU không khởi động được.", Cause = "Thiết lập bất kỳ CPU nào từ No.2 đến No.4 làm CPU điều khiển cho các module chức năng thông minh phiên bản chức năng A.", Remedy = "Chỉ thiết lập CPU No.1 làm CPU điều khiển cho các module chức năng thông minh phiên bản chức năng A.", Source = "12" };
            ErrorDb["2150"] = e_166;
            var e_167 = new ErrorDetails { Code = "3009", Type = "Lỗi (PARAMETER ERROR)", Description = "Lỗi tham số khi thiết lập CPU điều khiển cho module dòng AnS/A.", Cause = "Các module dòng AnS/A trong cùng một hệ thống được thiết lập các CPU điều khiển khác nhau.", Remedy = "Thiết lập cùng một module CPU làm CPU điều khiển cho tất cả các khe cắm lắp module dòng AnS/A.", Source = "12" };
            ErrorDb["3009"] = e_167;
            var e_168 = new ErrorDetails { Code = "3012", Type = "Lỗi (PARAMETER ERROR)", Description = "Lỗi tham số do không nhất quán giữa các CPU.", Cause = "Tham số của module CPU không khớp với tham số của CPU No.1 hoặc CPU đang chạy có số hiệu thấp nhất.", Remedy = "Kiểm tra và thiết lập các tham số hệ thống đa CPU giống nhau trên tất cả các module CPU.", Source = "12" };
            ErrorDb["3012"] = e_168;
            var e_169 = new ErrorDetails { Code = "3015", Type = "Lỗi (PARAMETER ERROR)", Description = "Lỗi tham số trong quá trình kiểm tra tính nhất quán (Consistency check).", Cause = "Tham số thiết lập khởi động đồng bộ đa CPU hoặc các tham số đa CPU khác không giống nhau giữa các CPU trong hệ thống.", Remedy = "Đảm bảo các tham số trong mục \"Multiple CPU Setting\" được thiết lập giống hệt nhau cho tất cả các CPU.", Source = "12" };
            ErrorDb["3015"] = e_169;
            var e_170 = new ErrorDetails { Code = "4102", Type = "Lỗi (OPERATION ERROR)", Description = "Lỗi vận hành khi sử dụng thiết bị liên kết trực tiếp (link direct device).", Cause = "Thực hiện lệnh sử dụng thiết bị liên kết trực tiếp để truy cập vào module được điều khiển bởi một CPU khác.", Remedy = "Chỉ sử dụng CPU điều khiển (Control CPU) để thực hiện các lệnh truy cập trực tiếp vào module đó.", Source = "12" };
            ErrorDb["4102"] = e_170;
            var e_171 = new ErrorDetails { Code = "4107", Type = "Lỗi (OPERATION ERROR)", Description = "Lỗi vận hành do tích lũy quá nhiều lệnh chưa xử lý.", Cause = "Có từ 33 lệnh chuyên dụng chuyển động (motion dedicated) hoặc lệnh truyền tin đa CPU trở lên được tích lũy chưa xử lý xong.", Remedy = "Giảm số lượng lệnh thực hiện đồng thời trong một chu kỳ quét (tối đa 32 lệnh).", Source = "12" };
            ErrorDb["4107"] = e_171;
            var e_172 = new ErrorDetails { Code = "7000", Type = "Lỗi (MULTI CPU DOWN)", Description = "Lỗi dừng toàn bộ hệ thống đa CPU.", Cause = "Xảy ra khi CPU No.1 bị lỗi dừng, hoặc một CPU khác bị lỗi dừng (khi thiết lập Operation Mode là dừng tất cả), hoặc khi một CPU khác No.1 bị reset riêng lẻ.", Remedy = "Kiểm tra nguyên nhân gây lỗi ở module CPU cụ thể trong cửa sổ PLC Diagnostics, khắc phục lỗi đó, sau đó reset CPU No.1 hoặc tắt/bật nguồn toàn hệ thống.", Source = "12" };
            ErrorDb["7000"] = e_172;
            var e_173 = new ErrorDetails { Code = "7010", Type = "Lỗi (MULTI EXE. ERROR)", Description = "Lỗi thực thi đa CPU liên quan đến phiên bản chức năng.", Cause = "Kết hợp module CPU phiên bản chức năng A và chức năng B trong cùng hệ thống đa CPU.", Remedy = "Sử dụng đồng nhất các module CPU phiên bản chức năng B trở lên.", Source = "12" };
            ErrorDb["7010"] = e_173;
            var e_174 = new ErrorDetails { Code = "7020", Type = "Lỗi (MULTI EXE. ERROR)", Description = "Lỗi thực thi đa CPU nhưng hệ thống vẫn tiếp tục vận hành.", Cause = "Xảy ra ở các CPU khác khi một CPU (không phải No.1) bị lỗi dừng nhưng tham số \"Operation Mode\" được thiết lập là không dừng các trạm khác.", Remedy = "Khắc phục nguyên nhân gây lỗi tại module CPU đang bị dừng để khôi phục trạng thái hoạt động bình thường của toàn hệ thống.", Source = "12" };
            ErrorDb["7020"] = e_174;
            var e_175 = new ErrorDetails { Code = "4620", Type = "Lỗi", Description = "BLOCK EXE. ERROR", Cause = "Cố gắng bắt đầu một khối (block) đã đang hoạt động khi chế độ vận hành kích hoạt khối trùng lặp được thiết lập là STOP.", Remedy = "Kiểm tra logic chương trình để đảm bảo không có yêu cầu kích hoạt khối (Block START) khi khối đó đang chạy, hoặc thay đổi cài đặt chế độ vận hành sang WAIT.", Source = "13" };
            ErrorDb["4620"] = e_175;
            var e_176 = new ErrorDetails { Code = "4621", Type = "Lỗi", Description = "BLOCK EXE. ERROR", Cause = "Lệnh điều khiển SFC liên quan đến khối được thực thi khi SM321 (SFC program start/stop) đang OFF, khối không tồn tại, chương trình SFC đang ở trạng thái chờ, hoặc khối bắt đầu được mô tả trong chương trình quản lý thực thi.", Remedy = "Đảm bảo SM321 đang ON trước khi thực thi lệnh, kiểm tra sự tồn tại của khối và không sử dụng các bước bắt đầu khối trong chương trình SFC quản lý thực thi.", Source = "13" };
            ErrorDb["4621"] = e_176;
            var e_177 = new ErrorDetails { Code = "4631", Type = "Lỗi", Description = "STEP EXE. ERROR", Cause = "Lệnh điều khiển SFC liên quan đến bước (step) hoặc điều kiện chuyển tiếp (transition) được thực thi khi SM321 đang OFF, bước/điều kiện không tồn tại, hoặc chương trình SFC đang ở trạng thái chờ/dừng.", Remedy = "Kiểm tra trạng thái SM321 và đảm bảo số hiệu bước hoặc mã điều kiện chuyển tiếp được chỉ định là chính xác và tồn tại trong khối mục tiêu.", Source = "13" };
            ErrorDb["4631"] = e_177;
            var e_178 = new ErrorDetails { Code = "4101", Type = "Lỗi", Description = "OPERATION ERROR", Cause = "Chỉ định một bước không tồn tại khi không thực hiện chỉ định khối, vượt quá số hiệu bước tối đa (8191) hoặc vượt quá phạm vi rơle bước (S).", Remedy = "Kiểm tra và hiệu chỉnh lại số hiệu bước trong lệnh đọc hàng loạt bước hoạt động (Active step batch readout) để nằm trong phạm vi cho phép.", Source = "13" };
            ErrorDb["4101"] = e_178;
            var e_179 = new ErrorDetails { Code = "4100", Type = "Lỗi", Description = "OPERATION ERROR", Cause = "Số hiệu khối SFC chỉ định nằm ngoài phạm vi 0-319, hoặc số lượng bình luận cần đọc/số lượng đọc trong một chu kỳ quét nằm ngoài phạm vi 0-256.", Remedy = "Chỉnh sửa các tham số n1, n2, n3 trong lệnh S(P).SFCSCOMR hoặc S(P).SFCTCOMR cho đúng phạm vi kỹ thuật.", Source = "13" };
            ErrorDb["4100"] = e_179;
            var e_180 = new ErrorDetails { Code = "2400", Type = "Lỗi", Description = "FILE SET ERROR", Cause = "File bình luận được thiết lập trong PLC Parameter không tồn tại tại thời điểm bật nguồn hoặc reset.", Remedy = "Kiểm tra lại cài đặt file trong tab PLC File và đảm bảo file bình luận đã được nạp vào bộ nhớ PLC.", Source = "13" };
            ErrorDb["2400"] = e_180;
            var e_181 = new ErrorDetails { Code = "2410", Type = "Lỗi", Description = "FILE SET ERROR / PROGRAM NOT FOUND", Cause = "File chương trình được chỉ định không tồn tại hoặc file bình luận chỉ định khi thực hiện lệnh S(P).SFCSCOMR/SFCTCOMR không tồn tại.", Remedy = "Kiểm tra tên file chương trình hoặc file bình luận và đảm bảo chúng đã được đăng ký/nạp vào PLC.", Source = "13" };
            ErrorDb["2410"] = e_181;
            var e_182 = new ErrorDetails { Code = "4130", Type = "Lỗi", Description = "OPERATION ERROR", Cause = "Lệnh S(P).SFCSCOMR/SFCTCOMR được thực thi đối với file bình luận lưu trữ trong thẻ ATA hoặc thẻ nhớ SD.", Remedy = "Chuyển file bình luận sang các bộ nhớ được hỗ trợ như SRAM card, Flash card hoặc Standard ROM.", Source = "13" };
            ErrorDb["4130"] = e_182;
            var e_183 = new ErrorDetails { Code = "5001", Type = "Lỗi", Description = "WDT ERROR", Cause = "Vòng lặp vô tận xảy ra trong một chu kỳ quét khi sử dụng chế độ 'Continuous transition' với lệnh Jump, hoặc thời gian xử lý lệnh kiểm tra chuyển tiếp cưỡng bức quá dài.", Remedy = "Kiểm tra lại cấu trúc vòng lặp Jump, hoặc tăng giá trị thiết lập WDT trong PLC RAS của PLC Parameter.", Source = "13" };
            ErrorDb["5001"] = e_183;
            var e_184 = new ErrorDetails { Code = "4505", Type = "Lỗi", Description = "OPERATION ERROR", Cause = "Sử dụng chính bước hiện tại làm số hiệu bước mục tiêu trong lệnh kết thúc bước (RST Sn).", Remedy = "Không được chỉ định chính bước đang thực thi lệnh để tự kết thúc nó thông qua lệnh RST Sn.", Source = "13" };
            ErrorDb["4505"] = e_184;
            var e_185 = new ErrorDetails { Code = "2504", Type = "Lỗi", Description = "CAN'T EXE.PRG.", Cause = "Đã tồn tại một chương trình SFC loại thực thi quét (scan execution) khi cố gắng chuyển đổi một chương trình SFC khác sang loại này bằng lệnh PSCAN.", Remedy = "Sử dụng lệnh POFF để chuyển chương trình SFC hiện tại sang trạng thái chờ (stand-by) trước khi kích hoạt chương trình mới.", Source = "13" };
            ErrorDb["2504"] = e_185;
            var e_186 = new ErrorDetails { Code = "4100", Type = "Lỗi vận hành (Operation error)", Description = "Lỗi giá trị nhập vào vượt quá phạm vi cho phép khi thực hiện các lệnh chuyển đổi kiểu dữ liệu.", Cause = "Giá trị nhập vào vượt quá 9999 đối với INT_TO_BCD hoặc vượt quá 99999999 đối với DINT_TO_BCD. Đối với REAL_TO_INT, giá trị ngoài phạm vi -32768 đến 32767. Đối với STR_TO_REAL, số lượng ký tự bằng 0 hoặc vượt quá 24, hoặc có ký tự không hợp lệ.", Remedy = "Kiểm tra và điều chỉnh giá trị đầu vào của các lệnh chuyển đổi để đảm bảo chúng nằm trong phạm vi dữ liệu hợp lệ được quy định cho từng lệnh cụ thể.", Source = "14" };
            ErrorDb["4100"] = e_186;
            var e_187 = new ErrorDetails { Code = "4140", Type = "Lỗi vận hành (Operation error)", Description = "Lỗi giá trị số thực dấu phẩy động hoặc số thực độ chính xác kép ngoài phạm vi.", Cause = "Giá trị nhập vào là -0 hoặc nằm ngoài phạm vi cho phép của kiểu dữ liệu LREAL (độ chính xác kép) hoặc REAL (số thực) khi thực hiện các phép toán chuyển đổi hoặc số học.", Remedy = "Đảm bảo giá trị số thực nhập vào nằm trong phạm vi có thể xử lý được của module (ví dụ: 2^-1022 <= \\", Source = "(s)\\" };
            ErrorDb["4140"] = e_187;
            var e_188 = new ErrorDetails { Code = "4141", Type = "Lỗi vận hành (Operation error)", Description = "Lỗi tràn số (Overflow) trong kết quả phép toán số thực.", Cause = "Kết quả của phép toán vượt quá phạm vi biểu diễn của kiểu dữ liệu độ chính xác kép (2^1024 <= \\", Remedy = "kết quả\\", Source = ")." };
            ErrorDb["4141"] = e_188;
            var e_189 = new ErrorDetails { Code = "4101", Type = "Lỗi vận hành (Operation error)", Description = "Thiết bị được chỉ định vượt quá phạm vi thiết bị tương ứng.", Cause = "Thiết bị đích (destination) hoặc thiết bị nguồn (source) được chỉ định trong lệnh (như MIDR, STR_TO_WORD, TOF) nằm ngoài dải địa chỉ hợp lệ của CPU.", Remedy = "Kiểm tra lại địa chỉ thiết bị trong chương trình và đảm bảo dải địa chỉ được sử dụng nằm trong cấu hình bộ nhớ của CPU đang sử dụng.", Source = "14" };
            ErrorDb["4101"] = e_189;
            var e_190 = new ErrorDetails { Code = "C9026", Type = "Cảnh báo (Warning)", Description = "Cảnh báo kiểu dữ liệu không khớp trong quá trình biên dịch.", Cause = "Xảy ra khi kiểu dữ liệu WORD (không dấu)/16-bit string hoặc DWORD (không dấu)/32-bit string được chỉ định cho đầu ra của các lệnh lựa chọn giá trị cực đại/cực tiểu hoặc kiểm soát giới hạn.", Remedy = "Xác nhận lại kiểu dữ liệu đầu ra để đảm bảo tính nhất quán của chương trình, mặc dù lệnh vẫn có thể thực hiện.", Source = "14" };
            ErrorDb["C9026"] = e_190;
            var e_191 = new ErrorDetails { Code = "C9047", Type = "Cảnh báo (Warning)", Description = "Cảnh báo cài đặt đơn vị đo lường timer.", Cause = "Đơn vị đo lường (time period) cho timer tốc độ cao hoặc tốc độ thấp bị thay đổi so với giá trị mặc định trong PLC Parameter.", Remedy = "Kiểm tra lại cấu hình thông số Timer limit setting trong PLC System của PLC Parameter để đảm bảo đúng với yêu cầu thiết kế của hệ thống.", Source = "14" };
            ErrorDb["C9047"] = e_191;
            var e_192 = new ErrorDetails { Code = "4620", Type = "Lỗi (Error)", Description = "Lỗi thực thi khối (BLOCK EXE. ERROR) khi thực hiện lệnh khởi động khối gấp đôi (block double START).", Cause = "Xảy ra khi một khối đã được khởi động hoặc đang hoạt động lại nhận thêm một yêu cầu khởi động khác trong khi cài đặt chế độ hoạt động là \"STOP\".", Remedy = "Kiểm tra lại chương trình SFC để đảm bảo không có nhiều yêu cầu khởi động cùng một khối đồng thời, hoặc thay đổi cài đặt chế độ hoạt động sang \"WAIT\".", Source = "15" };
            ErrorDb["4620"] = e_192;
            var e_193 = new ErrorDetails { Code = "4621", Type = "Lỗi (Error)", Description = "Lỗi thực thi khối (BLOCK EXE. ERROR) liên quan đến lệnh điều khiển SFC.", Cause = "Cố gắng thực hiện lệnh điều khiển SFC cho một khối không tồn tại hoặc khi rơle đặc biệt cho phép chạy chương trình SFC (SM321) đang ở trạng thái OFF.", Remedy = "Kiểm tra sự tồn tại của số khối được chỉ định và đảm bảo SM321 đang ON trước khi thực hiện các lệnh điều khiển khối.", Source = "15" };
            ErrorDb["4621"] = e_193;
            var e_194 = new ErrorDetails { Code = "4631", Type = "Lỗi (Error)", Description = "Lỗi thực thi bước (STEP EXE. ERROR).", Cause = "Chỉ định một bước không tồn tại trong chương trình SFC hoặc thực hiện lệnh điều khiển bước khi chương trình SFC đang ở trạng thái chờ (stand-by).", Remedy = "Xác nhận số hiệu bước tồn tại trong khối và đảm bảo chương trình SFC đang trong trạng thái thực thi (scan execution type).", Source = "15" };
            ErrorDb["4631"] = e_194;
            var e_195 = new ErrorDetails { Code = "4101", Type = "Lỗi (Error)", Description = "Lỗi vận hành (OPERATION ERROR) liên quan đến chỉ số thiết bị.", Cause = "Số hiệu bước (Sn) vượt quá phạm vi tối đa cho phép (8191) hoặc chỉ định một bước không tồn tại khi không thực hiện chỉ định khối cụ thể.", Remedy = "Kiểm tra lại số hiệu bước trong lệnh BMOV/MOV và đảm bảo nằm trong phạm vi cấu hình của thiết bị.", Source = "15" };
            ErrorDb["4101"] = e_195;
            var e_196 = new ErrorDetails { Code = "5001", Type = "Lỗi (Error)", Description = "Lỗi WDT (Watchdog Timer Error).", Cause = "Xảy ra vòng lặp vô hạn trong một lần quét khi sử dụng cài đặt \"chuyển tiếp liên tục\" (with continuous transition) hoặc thời gian xử lý lệnh kiểm tra chuyển tiếp quá lâu.", Remedy = "Kiểm tra cấu trúc vòng lặp trong chương trình SFC, điều chỉnh các điều kiện chuyển tiếp hoặc tăng giá trị cài đặt WDT trong thông số PLC RAS.", Source = "15" };
            ErrorDb["5001"] = e_196;
            var e_197 = new ErrorDetails { Code = "2400", Type = "Lỗi (Error)", Description = "Lỗi thiết lập file (FILE SET ERROR).", Cause = "File ghi chú (comment file) được cấu hình trong tham số PLC nhưng không tồn tại khi bật nguồn hoặc reset.", Remedy = "Kiểm tra sự tồn tại của file ghi chú trong bộ nhớ PLC hoặc tạo lại file tương ứng bằng công cụ lập trình.", Source = "15" };
            ErrorDb["2400"] = e_197;
            var e_198 = new ErrorDetails { Code = "2410", Type = "Lỗi (Error)", Description = "Lỗi không tìm thấy file ghi chú hoặc chương trình.", Cause = "File chương trình hoặc file ghi chú được chỉ định trong các lệnh đọc ghi chú SFC (SFCSCOMR/SFCTCOMR) không tồn tại.", Remedy = "Xác nhận tên file và đường dẫn của file ghi chú/chương trình trong bộ nhớ.", Source = "15" };
            ErrorDb["2410"] = e_198;
            var e_199 = new ErrorDetails { Code = "4130", Type = "Lỗi (Error)", Description = "Lỗi vận hành thiết bị lưu trữ.", Cause = "Thực hiện lệnh đọc ghi chú SFC (SFCSCOMR/SFCTCOMR) trực tiếp từ thẻ nhớ ATA hoặc thẻ nhớ SD.", Remedy = "Chuyển file ghi chú vào bộ nhớ SRAM hoặc Standard ROM trước khi thực hiện lệnh đọc.", Source = "15" };
            ErrorDb["4130"] = e_199;
            var e_200 = new ErrorDetails { Code = "4505", Type = "Lỗi (Error)", Description = "Lỗi chỉ định bước.", Cause = "Sử dụng chính số hiệu bước hiện tại làm tham số đích trong lệnh kết thúc bước (RST Sn) trên các dòng Basic, Universal hoặc LCPU.", Remedy = "Thay đổi logic chương trình để không tự kết thúc chính bước đang thực thi bằng lệnh điều khiển bước.", Source = "15" };
            ErrorDb["4505"] = e_200;
            var e_201 = new ErrorDetails { Code = "Lỗi tràn (Overflow error)", Type = "Lỗi", Description = "Lỗi tràn xảy ra khi giá trị bộ đếm vượt quá phạm vi giới hạn.", Cause = "Khi dùng bộ đếm tuyến tính, một xung cộng được nhập thêm từ giá trị hiện tại 2147483647. 2) Khi dùng bộ đếm tuyến tính, một xung trừ được nhập thêm từ giá trị hiện tại -2147483648.", Remedy = "Thực hiện chức năng Preset để xóa lỗi tràn và tiếp tục đếm.", Source = "16" };
            ErrorDb["Lỗi tràn (Overflow error)"] = e_201;
            var e_202 = new ErrorDetails { Code = "Phát hiện đứt cầu chì (Fuse broken detection)", Type = "Lỗi", Description = "Cầu chì trong bộ phận đầu ra tín hiệu trùng khớp (coincidence signal) bị hỏng.", Cause = "Cầu chì cho phần đầu ra bên ngoài của tín hiệu trùng khớp đã bị đứt.", Remedy = "Vui lòng liên hệ với đại diện Mitsubishi tại địa phương của bạn để được hỗ trợ.", Source = "16" };
            ErrorDb["Phát hiện đứt cầu chì (Fuse broken detection)"] = e_202;
            var e_203 = new ErrorDetails { Code = "CAN'T EXE. PRG. (2500)", Type = "Lỗi", Description = "Lỗi khi ghi tham số vào PLC.", Cause = "Thay đổi số bắt đầu của các thanh ghi chỉ số được sử dụng trong cài đặt thiết bị của tham số PLC nhưng chỉ ghi tham số mà không ghi chương trình tương ứng vào bộ điều khiển.", Remedy = "Luôn ghi đồng thời cả tham số và chương trình vào bộ điều khiển lập trình.", Source = "17" };
            ErrorDb["CAN'T EXE. PRG. (2500)"] = e_203;
            var e_204 = new ErrorDetails { Code = "4101", Type = "Lỗi", Description = "Lỗi vượt quá phạm vi thiết bị.", Cause = "Kết quả của việc cài đặt chỉ số (index setting) áp dụng cho các thanh ghi file (ZR), thanh ghi dữ liệu mở rộng (D) hoặc thanh ghi liên kết mở rộng (W) vượt quá phạm vi của các tệp thanh ghi file.", Remedy = "Kiểm tra lại giá trị cài đặt chỉ số và đảm bảo dữ liệu sau khi sửa đổi không vượt quá phạm vi thiết bị được người dùng chỉ định.", Source = "17" };
            ErrorDb["4101"] = e_204;
            var e_205 = new ErrorDetails { Code = "1103", Type = "Lỗi", Description = "Lỗi vượt quá phạm vi thiết bị hệ thống.", Cause = "Kết quả của cài đặt chỉ số vượt quá phạm vi thiết bị do người dùng chỉ định và dữ liệu được ghi vào các thiết bị dành riêng cho hệ thống.", Remedy = "Điều chỉnh lại chương trình để đảm bảo các giá trị đếm và cài đặt chỉ số nằm trong phạm vi cho phép.", Source = "17" };
            ErrorDb["1103"] = e_205;
            var e_206 = new ErrorDetails { Code = "6706", Type = "Lỗi", Description = "Lỗi vận hành (đối với dòng FXCPU).", Cause = "Áp dụng cài đặt chỉ số (index setting) vượt quá phạm vi thiết bị quy định.", Remedy = "Kiểm tra lại cấu trúc chương trình và giới hạn thiết bị trong tài liệu hướng dẫn dòng FX tương ứng.", Source = "17" };
            ErrorDb["6706"] = e_206;
            var e_207 = new ErrorDetails { Code = "4100", Type = "Lỗi", Description = "Giá trị dữ liệu cài đặt không hợp lệ hoặc vượt dải cho phép.", Cause = "Dữ liệu điều khiển PID nằm ngoài phạm vi; Giới hạn trên MV nhỏ hơn giới hạn dưới; Số vòng lặp sử dụng nhỏ hơn số vòng lặp thực hiện trong một lần quét.", Remedy = "Kiểm tra và hiệu chỉnh lại các thông số cài đặt dữ liệu điều khiển PID trong dải cho phép.", Source = "18" };
            ErrorDb["4100"] = e_207;
            var e_208 = new ErrorDetails { Code = "4101", Type = "Lỗi", Description = "Vượt quá phạm vi thiết bị được chỉ định.", Cause = "Phạm vi thiết bị được phân bổ cho vùng dữ liệu điều khiển PID vượt quá số hiệu thiết bị cuối cùng của thiết bị tương ứng.", Remedy = "Đảm bảo dải thiết bị được chỉ định cho dữ liệu PID nằm trong phạm vi bộ nhớ của CPU.", Source = "18" };
            ErrorDb["4101"] = e_208;
            var e_209 = new ErrorDetails { Code = "4103", Type = "Lỗi", Description = "Thứ tự thực thi lệnh không đúng.", Cause = "Lệnh S(P).PIDCONT hoặc S(P).PIDSTOP/RUN được thực hiện trước khi thực hiện lệnh khởi tạo S(P).PIDINIT.", Remedy = "Đảm bảo lệnh S(P).PIDINIT được thực thi thành công trước khi gọi các lệnh điều khiển hoặc thay đổi thông số.", Source = "18" };
            ErrorDb["4103"] = e_209;
            var e_210 = new ErrorDetails { Code = "2110", Type = "Lỗi", Description = "Lỗi giám sát màn hình (chỉ dành cho QnACPU).", Cause = "Lệnh CMODE chưa được thực hiện cho module AD57(S1) trước khi gọi lệnh giám sát PID57.", Remedy = "Thực hiện lệnh CMODE để thiết lập chế độ hiển thị chuẩn cho AD57(S1).", Source = "18" };
            ErrorDb["2110"] = e_210;
            var e_211 = new ErrorDetails { Code = "Cảnh báo thay đổi MV (b1)", Type = "Cảnh báo", Description = "Tốc độ thay đổi giá trị MV vượt quá giới hạn.", Cause = "Biến thiên giữa giá trị MV hiện tại và trước đó lớn hơn giới hạn Delta MVL đã thiết lập.", Remedy = "Kiểm tra lại đặc tính tải hoặc nới lỏng giới hạn Delta MVL nếu cần thiết.", Source = "18" };
            ErrorDb["Cảnh báo thay đổi MV (b1)"] = e_211;
            var e_212 = new ErrorDetails { Code = "Cảnh báo thay đổi PV (b0)", Type = "Cảnh báo", Description = "Tốc độ thay đổi giá trị PV vượt quá giới hạn.", Cause = "Biến thiên giữa giá trị PV hiện tại và trước đó lớn hơn giới hạn Delta PVL đã thiết lập.", Remedy = "Kiểm tra cảm biến đầu vào hoặc điều chỉnh thông số Delta PVL.", Source = "18" };
            ErrorDb["Cảnh báo thay đổi PV (b0)"] = e_212;
        }

        public static ErrorDetails Lookup(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code == "0" || code == "--")
                return null;

            code = code.Trim();
            if (ErrorDb.TryGetValue(code, out var details))
                return details;

            // Try range lookup or hex lookup if the code is numeric
            if (int.TryParse(code, out int val))
            {
                foreach (var entry in Ranges)
                {
                    if (val >= entry.Min && val <= entry.Max)
                        return entry.Details;
                }

                // Try looking up as Hexadecimal representation, e.g. 28929 -> 7101H
                string hexUpper = val.ToString("X");
                if (ErrorDb.TryGetValue(hexUpper, out details))
                    return details;
                if (ErrorDb.TryGetValue(hexUpper + "H", out details))
                    return details;
                if (ErrorDb.TryGetValue("0x" + hexUpper, out details))
                    return details;
            }

            return null;
        }
    }
}
