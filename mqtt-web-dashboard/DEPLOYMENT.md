# 🚀 Deployment Guide - Chia sẻ Dashboard với các thiết bị khác

Hướng dẫn chi tiết để deploy MQTT Dashboard trên các nền tảng khác nhau.

## 📋 Tổng quan các cách

| Cách | Ưu điểm | Nhược điểm | Chi phí |
|------|---------|-----------|--------|
| **ngrok** | Nhanh, dễ | Tắt khi ngrok đóng | Free |
| **Cloudflare Tunnel** | Miễn phí, ổn định | Setup phức tạp | Free |
| **GitHub Pages + Standalone** | Miễn phí vĩnh viễn | Cần MQTT broker công cộng | Free |
| **Heroku/Railway** | Dễ, 1-click deploy | Có phí | $5-10/tháng |
| **VPS (DigitalOcean/Linode)** | Đầy đủ quyền kiểm soát | Phức tạp, tốn tiền | $5+/tháng |

---

## 🔥 Cách 1: Expose local server với **ngrok** (NHANH NHẤT)

### Bước 1: Cài ngrok

```bash
# Windows - Download tại https://ngrok.com/download
# Hoặc dùng Chocolatey:
choco install ngrok

# macOS:
brew install ngrok

# Linux:
wget https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-linux-amd64.zip
unzip ngrok-v3-stable-linux-amd64.zip
sudo mv ngrok /usr/local/bin/
```

### Bước 2: Lấy authtoken (free)

1. Đăng ký tại https://ngrok.com
2. Copy authtoken từ dashboard
3. Chạy:

```bash
ngrok config add-authtoken YOUR_AUTH_TOKEN
```

### Bước 3: Expose server

Khi đang chạy `npm start` (server chạy trên port 8080):

```bash
ngrok http 8080
```

Output:
```
ngrok by @inconshreveable                     (Ctrl+C to quit)
Session Status                 online
Account                        your@email.com (Plan: Free)
Version                        3.0.0
Region                         ap (Asia Pacific)
Forwarding                     https://abc123.ngrok.io -> http://localhost:8080
```

### Bước 4: Chia sẻ URL

```
https://abc123.ngrok.io
```

Người dùng khác truy cập URL này từ bất kỳ thiết bị nào!

**Lưu ý**: URL thay đổi mỗi lần ngrok restart (trừ khi dùng paid plan)

---

## 🌐 Cách 2: Cloudflare Tunnel (MIỄN PHÍ + ỔNĐỊNH)

### Bước 1: Cài Cloudflare Tunnel

```bash
# Windows
choco install cloudflared

# macOS
brew install cloudflare/cloudflare/cloudflared

# Linux
wget https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64
chmod +x cloudflared-linux-amd64
sudo mv cloudflared-linux-amd64 /usr/local/bin/cloudflared
```

### Bước 2: Login và setup

```bash
cloudflared tunnel login
# Chọn domain của bạn
```

### Bước 3: Tạo tunnel

```bash
cloudflared tunnel create mqtt-dashboard
```

### Bước 4: Cấu hình (tạo file `~/.cloudflared/config.yml`)

```yaml
tunnel: mqtt-dashboard
credentials-file: /path/to/.cloudflared/mqtt-dashboard.json

ingress:
  - hostname: mqtt.yourdomain.com
    service: http://localhost:8080
  - service: http_status:404
```

### Bước 5: Chạy tunnel

```bash
cloudflared tunnel run mqtt-dashboard
```

**Lợi ích**: URL ổn định, tên miền riêng

---

## 📦 Cách 3: Deploy toàn bộ lên **Railway.app** (1-click)

### Bước 1: Đẩy code lên GitHub

```bash
cd mqtt-web-dashboard
git init
git add .
git commit -m "Initial commit"
git remote add origin https://github.com/YOUR_USERNAME/mqtt-dashboard.git
git push -u origin main
```

### Bước 2: Connect với Railway

1. Truy cập https://railway.app
2. Đăng nhập bằng GitHub
3. Chọn "New Project" → "Deploy from GitHub repo"
4. Chọn repo `mqtt-dashboard`
5. Railway tự detect `package.json` và deploy

### Bước 3: Cấu hình environment

Railway dashboard → Variables:
```
PORT=8080
MQTT_BROKER=mqtts://beb7179d08fa43f79d440a9be9b95f24.s1.eu.hivemq.cloud:8883
MQTT_USERNAME=DACDT2026
MQTT_PASSWORD=trungaN123@
```

### Bước 4: Lấy URL

Sau deploy xong, Railway cấp URL công cộng:
```
https://mqtt-dashboard-prod-up.railway.app
```

**Chi phí**: $5/tháng (hoặc free credits)

---

## 🐳 Cách 4: Docker + VPS (DigitalOcean/Linode/Vultr)

### Bước 1: Tạo Dockerfile

```dockerfile
FROM node:18-alpine

WORKDIR /app

COPY package*.json ./
RUN npm install --production

COPY . .

EXPOSE 8080

CMD ["npm", "start"]
```

### Bước 2: Build image

```bash
docker build -t mqtt-dashboard .
```

### Bước 3: Push lên Docker Hub

```bash
docker login
docker tag mqtt-dashboard YOUR_USERNAME/mqtt-dashboard:latest
docker push YOUR_USERNAME/mqtt-dashboard:latest
```

### Bước 4: Deploy lên VPS

SSH vào VPS:

```bash
docker run -d \
  -p 80:8080 \
  -e MQTT_BROKER="mqtts://broker.example.com:8883" \
  -e MQTT_USERNAME="username" \
  -e MQTT_PASSWORD="password" \
  --restart always \
  YOUR_USERNAME/mqtt-dashboard:latest
```

**Chi phí**: $5-20/tháng tùy VPS

---

## 📱 Cách 5: GitHub Pages + Standalone HTML

Nếu muốn chạy pure frontend (không cần Node.js backend):

### Bước 1: Tạo standalone version

File: `mqtt-web-dashboard/standalone.html` (bao gồm tất cả trong 1 file)

### Bước 2: Push lên GitHub

```bash
git add standalone.html
git commit -m "Add standalone version"
git push
```

### Bước 3: Enable GitHub Pages

1. Repo settings → Pages
2. Source: main branch
3. Access tại: `https://USERNAME.github.io/mqtt-dashboard/`

**Lưu ý**: Cần MQTT broker hỗ trợ WebSocket và CORS

---

## 🔗 Cách 6: Local Network Access (LAN)

Cho các thiết bị trên cùng mạng:

### Tìm IP của máy chạy server

**Windows:**
```cmd
ipconfig
# Tìm IPv4 Address (ví dụ: 192.168.1.100)
```

**macOS/Linux:**
```bash
ifconfig | grep "inet "
# Tìm địa chỉ 192.168.x.x
```

### Truy cập từ thiết bị khác

```
http://192.168.1.100:8080
```

**Lưu ý**: Chỉ hoạt động trên LAN, không qua Internet

---

## 📊 So sánh chi tiết

### ngrok
```
✓ Setup: 5 phút
✓ Chi phí: Free
✗ URL thay đổi: Có
✗ Uptime: Tùy ngrok
✓ Tốc độ: Tốt
```

### Cloudflare Tunnel
```
✓ Setup: 10 phút
✓ Chi phí: Free
✓ URL cố định: Có (với custom domain)
✓ Uptime: Tuyệt vời (99.9%)
✓ Tốc độ: Rất tốt
```

### Railway
```
✓ Setup: 3 phút (1-click)
✓ Chi phí: $5/tháng
✓ URL cố định: Có
✓ Uptime: Tốt
✗ Công suất: Hạn chế (free tier)
```

### VPS
```
✓ Setup: 30 phút
✗ Chi phí: $5-20/tháng
✓ URL cố định: Có
✓ Uptime: Rất cao
✓ Tốc độ: Tuyệt vời
✓ Tùy chỉnh: Toàn quyền
```

---

## 🎯 Khuyến nghị cho từng trường hợp

| Nhu cầu | Cách tốt nhất |
|--------|---------------|
| Test nhanh, demo | **ngrok** |
| Sản xuất, ổn định | **Railway** hoặc **VPS** |
| Custom domain, free | **Cloudflare Tunnel** |
| GitHub Pages + DB | **Railway** (backend) + **GH Pages** (frontend) |
| Full control | **VPS + Docker** |

---

## 🔒 Bảo mật khi public

### 1. Bảo vệ MQTT credentials

```javascript
// ❌ KHÔNG làm (credentials lộ)
const MQTT_PASSWORD = 'trungaN123@';

// ✅ LÀM (dùng environment variables)
const MQTT_PASSWORD = process.env.MQTT_PASSWORD;
```

### 2. Thêm authentication

```javascript
// Ví dụ: Simple auth
const AUTH_TOKEN = process.env.AUTH_TOKEN || 'secret123';

app.use((req, res, next) => {
    const token = req.headers['x-auth-token'];
    if (token !== AUTH_TOKEN) {
        return res.status(401).json({ error: 'Unauthorized' });
    }
    next();
});
```

### 3. Dùng HTTPS

Tất cả cách (ngrok, Cloudflare, Railway, VPS) hỗ trợ HTTPS mặc định.

---

## 📞 Troubleshooting

### ngrok bị chặn
- Thử dùng region khác: `ngrok http --region eu 8080`
- Hoặc switch sang Cloudflare Tunnel

### Connection refused
- Kiểm tra server đang chạy: `npm start`
- Kiểm tra port 8080 mở: `netstat -an | grep 8080`

### MQTT connection failed
- Kiểm tra credentials
- Kiểm tra firewall/VPN
- Test kết nối: `mqtt sub -h broker.mqtt.cool -t test`

### WebSocket error trên production
- Bật CORS: `app.use(cors())`
- Kiểm tra secure websocket (WSS vs WS)

---

## 📚 Tài liệu thêm

- ngrok: https://ngrok.com/docs
- Cloudflare: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/
- Railway: https://docs.railway.app/
- Docker: https://docs.docker.com/

---

## ✅ Checklist Deploy

- [ ] Chọn cách deploy phù hợp
- [ ] Thiết lập credentials an toàn
- [ ] Test kết nối từ thiết bị khác
- [ ] Kiểm tra bảo mật
- [ ] Monitor uptime
- [ ] Backup config + credentials

Bây giờ bạn có thể chia sẻ dashboard với team/khách hàng! 🎉