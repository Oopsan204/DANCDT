# ⚡ Quick Deploy - GitHub Pages (5 phút)

Hướng dẫn nhanh deploy MQTT Dashboard lên GitHub Pages.

## 📋 Yêu cầu

- ✅ Tài khoản GitHub
- ✅ Git đã cài đặt
- ✅ File `standalone-github-pages.html`

## 🚀 5 bước nhanh

### 1️⃣ Tạo repo trên GitHub (2 phút)

Truy cập: https://github.com/new

```
Repository name: mqtt-dashboard
Description: MQTT Real-time Dashboard
☑️ Public
☑️ Add README
```

→ **Create repository**

### 2️⃣ Clone và chuẩn bị (1 phút)

```bash
# Clone repo
git clone https://github.com/YOUR_USERNAME/mqtt-dashboard.git
cd mqtt-dashboard

# Copy file standalone (đổi tên thành index.html)
# Windows:
copy path\to\standalone-github-pages.html index.html

# macOS/Linux:
cp path/to/standalone-github-pages.html index.html
```

### 3️⃣ Push code (1 phút)

```bash
git add index.html
git commit -m "Add MQTT dashboard"
git push origin main
```

### 4️⃣ Enable GitHub Pages (30 giây)

1. Vào repo → **Settings**
2. Sidebar → **Pages**
3. Source: **Deploy from a branch**
4. Branch: **main** + Folder: **/ (root)**
5. **Save**

### 5️⃣ Truy cập dashboard (30 giây)

Chờ 1-2 phút, sau đó mở:

```
https://YOUR_USERNAME.github.io/mqtt-dashboard/
```

## ✅ Done!

Dashboard đã live và hoạt động 24/7!

---

## 🔧 Tùy chỉnh MQTT config (optional)

Mở `index.html`, tìm và sửa:

```javascript
this.MQTT_BROKER = 'your-broker.hivemq.cloud';
this.MQTT_PORT = 8884;
this.MQTT_USERNAME = 'your_username';
this.MQTT_PASSWORD = 'your_password';
this.MQTT_TOPIC = 'your/topic';
```

Sau đó:

```bash
git add index.html
git commit -m "Update MQTT config"
git push
```

Chờ 1-2 phút để GitHub Pages rebuild.

---

## 📱 Chia sẻ với team

Gửi URL này cho mọi người:

```
https://YOUR_USERNAME.github.io/mqtt-dashboard/
```

Họ có thể xem dashboard real-time từ bất kỳ thiết bị nào!

---

## 🆘 Lỗi thường gặp

### Dashboard hiển thị 404
→ Chờ 2 phút rồi refresh (Ctrl+R)

### MQTT không kết nối
→ Mở Console (F12) xem lỗi
→ Kiểm tra broker URL, port, credentials

### Dashboard không update sau khi push
→ Clear cache (Ctrl+Shift+R)
→ Hoặc thử Incognito mode

---

## 📚 Đọc thêm

- **Chi tiết đầy đủ:** [GITHUB_PAGES_GUIDE.md](./GITHUB_PAGES_GUIDE.md)
- **Các cách deploy khác:** [DEPLOYMENT.md](./DEPLOYMENT.md)
- **Documentation:** [README.md](./README.md)