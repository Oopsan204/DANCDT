# 📘 Hướng dẫn chi tiết Deploy lên GitHub Pages

Triển khai MQTT Dashboard lên GitHub Pages với standalone version - **MIỄN PHÍ VĨNH VIỄN**.

---

## 📋 Tổng quan

**GitHub Pages** cho phép host static website miễn phí. Chúng ta sẽ deploy file `standalone-github-pages.html` để có dashboard hoạt động 24/7 mà không cần server backend.

### ✅ Ưu điểm
- ✅ Hoàn toàn miễn phí
- ✅ URL công khai: `https://username.github.io/repo-name`
- ✅ HTTPS tự động
- ✅ Uptime 99.9%
- ✅ Custom domain (nếu có)
- ✅ Không cần quản lý server

### ⚠️ Yêu cầu
- MQTT broker phải hỗ trợ WebSocket (port 8883 hoặc 8884)
- MQTT broker phải cho phép kết nối từ browser (CORS)
- Tài khoản GitHub (free)

---

## 🎯 Kế hoạch triển khai

### Giai đoạn 1: Chuẩn bị Repository (10 phút)
1. Tạo GitHub repository
2. Clone về máy local
3. Chuẩn bị files

### Giai đoạn 2: Upload Code (5 phút)
1. Copy standalone file
2. Commit và push
3. Verify upload

### Giai đoạn 3: Kích hoạt GitHub Pages (5 phút)
1. Enable GitHub Pages
2. Chọn source branch
3. Lấy URL public

### Giai đoạn 4: Kiểm tra & Test (10 phút)
1. Truy cập dashboard
2. Test kết nối MQTT
3. Verify real-time data

### Giai đoạn 5: Tùy chỉnh (optional)
1. Custom domain
2. Chỉnh sửa UI
3. Update MQTT config

**Tổng thời gian: ~30-40 phút**

---

## 📝 Bước 1: Tạo GitHub Repository

### 1.1. Đăng nhập GitHub
Truy cập: https://github.com

### 1.2. Tạo Repository mới

1. Click nút **"New"** (góc trên bên phải)
2. Điền thông tin:
   ```
   Repository name: mqtt-dashboard
   Description: Real-time MQTT Dashboard
   Public ✓ (bắt buộc cho GitHub Pages free)
   ✓ Add a README file
   ```
3. Click **"Create repository"**

### 1.3. Lưu URL Repository

Bạn sẽ có URL dạng:
```
https://github.com/YOUR_USERNAME/mqtt-dashboard
```

Ví dụ: `https://github.com/johndoe/mqtt-dashboard`

---

## 📝 Bước 2: Clone Repository về máy

### 2.1. Mở Terminal/Command Prompt

**Windows:**
```cmd
cd C:\Projects
```

**macOS/Linux:**
```bash
cd ~/Projects
```

### 2.2. Clone repository

```bash
git clone https://github.com/YOUR_USERNAME/mqtt-dashboard.git
cd mqtt-dashboard
```

Thay `YOUR_USERNAME` bằng username GitHub của bạn.

### 2.3. Verify clone thành công

```bash
ls
# Kết quả: README.md
```

---

## 📝 Bước 3: Chuẩn bị Files

### 3.1. Copy standalone file

Từ thư mục `mqtt-web-dashboard`, copy file standalone:

**Windows:**
```cmd
copy ..\mqtt-web-dashboard\standalone-github-pages.html index.html
```

**macOS/Linux:**
```bash
cp ../mqtt-web-dashboard/standalone-github-pages.html index.html
```

**Hoặc:** Copy thủ công file `standalone-github-pages.html` và đổi tên thành `index.html`

### 3.2. Cấu hình MQTT (NẾU CẦN)

Mở file `index.html`, tìm dòng:

```javascript
// ⚠️ Cấu hình MQTT - CẬP NHẬT THEO NHU CẦU
this.MQTT_BROKER = 'beb7179d08fa43f79d440a9be9b95f24.s1.eu.hivemq.cloud';
this.MQTT_PORT = 8884; // WebSocket Secure
this.MQTT_USERNAME = 'DACDT2026';
this.MQTT_PASSWORD = 'trungaN123@';
this.MQTT_TOPIC = 'DACDT/machine/state';
```

**Cập nhật** với thông tin MQTT broker của bạn.

⚠️ **LƯU Ý BẢO MẬT:**
- File này sẽ public, mọi người đều thấy được
- Không để password quan trọng
- Hoặc dùng MQTT broker với authentication token

### 3.3. (Optional) Tạo README

Tạo file `README.md`:

```markdown
# MQTT Dashboard

Real-time dashboard hiển thị dữ liệu MQTT.

## Truy cập Dashboard

https://YOUR_USERNAME.github.io/mqtt-dashboard/

## Công nghệ

- HTML5 + CSS3 + JavaScript
- Paho MQTT JavaScript Client
- GitHub Pages hosting
```

---

## 📝 Bước 4: Commit và Push

### 4.1. Add files

```bash
git add index.html
git add README.md
```

### 4.2. Commit

```bash
git commit -m "Add MQTT Dashboard standalone version"
```

### 4.3. Push lên GitHub

```bash
git push origin main
```

Hoặc nếu branch là `master`:
```bash
git push origin master
```

### 4.4. Verify upload

Truy cập repository trên GitHub:
```
https://github.com/YOUR_USERNAME/mqtt-dashboard
```

Bạn sẽ thấy file `index.html` đã được upload.

---

## 📝 Bước 5: Kích hoạt GitHub Pages

### 5.1. Vào Settings

1. Trong repository, click tab **"Settings"**
2. Scroll xuống sidebar bên trái
3. Click **"Pages"**

### 5.2. Configure Source

Trong mục **"Build and deployment"**:

1. **Source:** Chọn **"Deploy from a branch"**
2. **Branch:** Chọn **"main"** (hoặc "master")
3. **Folder:** Chọn **"/ (root)"**
4. Click **"Save"**

### 5.3. Chờ deployment

GitHub sẽ build và deploy site. Quá trình này mất **1-2 phút**.

Bạn sẽ thấy thông báo:
```
✓ Your site is live at https://YOUR_USERNAME.github.io/mqtt-dashboard/
```

---

## 📝 Bước 6: Truy cập Dashboard

### 6.1. Mở Dashboard

Truy cập URL:
```
https://YOUR_USERNAME.github.io/mqtt-dashboard/
```

Ví dụ: `https://johndoe.github.io/mqtt-dashboard/`

### 6.2. Kiểm tra kết nối

Dashboard sẽ tự động:
1. Kết nối tới MQTT broker
2. Subscribe topic
3. Hiển thị dữ liệu real-time

**Status badge** ở header sẽ hiển thị:
- 🔴 **Disconnected**: Chưa kết nối
- 🟢 **Connected**: Đã kết nối thành công

### 6.3. Test với Simulator

Từ máy local, chạy simulator:

```bash
cd DACDT_2026
python mqtt_simulator.py
```

Dashboard trên GitHub Pages sẽ nhận và hiển thị dữ liệu!

---

## 🎨 Bước 7: Tùy chỉnh (Optional)

### 7.1. Đổi Title

Mở `index.html`, tìm:

```html
<title>MQTT Dashboard - Standalone</title>
```

Đổi thành:
```html
<title>Your Company - MQTT Dashboard</title>
```

### 7.2. Đổi màu sắc

Tìm `:root` CSS variables:

```css
:root {
    --bg-main: #0d1117;     /* Background chính */
    --bg-panel: #161b22;    /* Background panel */
    --cyan: #1f6feb;        /* Màu chính */
    --green: #3fb950;       /* Màu connected */
}
```

### 7.3. Commit changes

```bash
git add index.html
git commit -m "Customize dashboard"
git push
```

GitHub Pages sẽ tự động rebuild (1-2 phút).

---

## 🌐 Bước 8: Custom Domain (Optional)

Nếu bạn có domain riêng (ví dụ: `dashboard.example.com`):

### 8.1. Thêm CNAME record

Trong DNS provider của bạn, thêm:

```
Type: CNAME
Name: dashboard (hoặc subdomain bạn muốn)
Value: YOUR_USERNAME.github.io
```

### 8.2. Cấu hình trong GitHub

1. Vào **Settings → Pages**
2. Mục **"Custom domain"**
3. Nhập: `dashboard.example.com`
4. Click **"Save"**
5. ✓ **Enforce HTTPS** (khuyến nghị)

### 8.3. Chờ propagation

DNS propagation mất **10 phút - 24 giờ**.

Kiểm tra:
```bash
nslookup dashboard.example.com
```

---

## 🔧 Troubleshooting

### ❌ Lỗi: "404 Page Not Found"

**Nguyên nhân:** File chưa deploy hoặc đường dẫn sai

**Giải pháp:**
1. Verify file `index.html` có trong repo
2. Chờ 1-2 phút để GitHub Pages rebuild
3. Clear browser cache (Ctrl+Shift+R)
4. Check Settings → Pages có enable không

### ❌ Lỗi: "MQTT Connection Failed"

**Nguyên nhân:** Broker không hỗ trợ WebSocket hoặc CORS

**Giải pháp:**
1. Verify broker URL và port (phải là WebSocket: 8883 hoặc 8884)
2. Check credentials (username/password)
3. Test kết nối từ local trước
4. Xem browser console (F12) để xem lỗi chi tiết

**Test MQTT broker:**
```bash
# Từ local
mosquitto_sub -h YOUR_BROKER -p 8883 -t test -u USERNAME -P PASSWORD --capath /etc/ssl/certs/
```

### ❌ Lỗi: "Mixed Content" (HTTP/HTTPS)

**Nguyên nhân:** Dashboard HTTPS nhưng MQTT broker dùng WS (không SSL)

**Giải pháp:**
- Đổi sang WSS (WebSocket Secure)
- Hoặc dùng broker có hỗ trợ SSL

### ❌ Dashboard không cập nhật sau khi push

**Giải pháp:**
1. Chờ 1-2 phút
2. Vào **Actions** tab → Check deployment status
3. Clear browser cache
4. Thử Incognito mode

---

## 📊 So sánh: Standalone vs Node.js Backend

| Tính năng | Standalone (GitHub Pages) | Node.js Backend |
|-----------|---------------------------|-----------------|
| Chi phí | **Free vĩnh viễn** | Phụ thuộc hosting |
| Setup | Đơn giản (30 phút) | Phức tạp hơn |
| MQTT Connection | Browser → Broker | Server → Broker → Browser |
| Bảo mật | Password lộ trong code | Password trên server |
| Scalability | Unlimited | Phụ thuộc server |
| Offline support | ❌ | ✅ (có cache) |
| Custom logic | ❌ | ✅ |

**Khuyến nghị:**
- Dùng **Standalone** cho: Demo, monitoring, public dashboards
- Dùng **Node.js** cho: Production với bảo mật cao, custom logic

---

## 🔐 Bảo mật

### ⚠️ Lưu ý quan trọng

File `index.html` là **PUBLIC**, mọi người đều có thể xem source code và thấy:
- MQTT broker URL
- Username
- Password
- Topic names

### 🛡️ Cách bảo vệ

#### Option 1: Dùng MQTT broker với token authentication
```javascript
this.MQTT_USERNAME = 'public_readonly_token';
this.MQTT_PASSWORD = 'token_12345'; // Token có quyền hạn chế
```

#### Option 2: Giới hạn quyền của user
- Chỉ cho phép READ trên topic cụ thể
- Không cho WRITE
- Set ACL (Access Control List) trên broker

#### Option 3: Dùng API Gateway
Thay vì kết nối trực tiếp, kết nối qua API có authentication.

---

## ✅ Checklist Hoàn thành

- [ ] Tạo GitHub repository
- [ ] Clone về local
- [ ] Copy `standalone-github-pages.html` → `index.html`
- [ ] Cập nhật MQTT config (nếu cần)
- [ ] Commit và push
- [ ] Enable GitHub Pages
- [ ] Verify dashboard hoạt động
- [ ] Test MQTT connection
- [ ] Test với simulator
- [ ] (Optional) Custom domain
- [ ] (Optional) Tùy chỉnh UI

---

## 🎉 Xong!

Dashboard của bạn đã live tại:
```
https://YOUR_USERNAME.github.io/mqtt-dashboard/
```

Chia sẻ URL này với team hoặc khách hàng. Dashboard sẽ hoạt động 24/7, miễn phí, và tự động cập nhật khi bạn push code mới!

---

## 📚 Tài liệu tham khảo

- GitHub Pages: https://pages.github.com/
- Paho MQTT JS: https://www.eclipse.org/paho/index.php?page=clients/js/index.php
- MQTT WebSocket: https://www.hivemq.com/blog/mqtt-over-websockets-with-hivemq/

---

## 🆘 Cần trợ giúp?

- GitHub Pages documentation: https://docs.github.com/en/pages
- MQTT troubleshooting: Check broker documentation
- Browser console (F12): Xem error messages chi tiết