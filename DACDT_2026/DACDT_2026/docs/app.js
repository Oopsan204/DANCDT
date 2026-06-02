// DACDT 2026 Dashboard - JavaScript Logic
class DAcDTDashboard {
    constructor() {
        this.isConnected = false;
        this.isRunning = false;
        this.mqttConnected = false;
        this.mqttClient = null;
        this.currentJob = {
            fileName: 'Không có file',
            fileType: '-',
            progress: 0,
            elapsedTime: 0,
            speed: 0,
            temperature: 0
        };
        this.coordinates = {
            x: 0,
            y: 0,
            z: 0,
            maxX: 200,
            maxY: 200,
            maxZ: 100
        };
        this.systemStats = {
            errors: 0,
            warnings: 0,
            successfulJobs: 0,
            failedJobs: 0,
            totalTime: 0
        };
        this.logs = [];
        this.jobProgressInterval = null;
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.initMQTT();
        this.startSimulation();
        this.addLog('Ứng dụng khởi động thành công', 'info');
        this.updateDashboard();
    }

    initMQTT() {
        try {
            const clientId = 'DACDT_WebUI_' + Math.random().toString(36).substr(2, 9);
            // Kết nối tới HiveMQ Cloud qua WebSocket Secure (WSS)
            this.mqttClient = new Paho.MQTT.Client('beb7179d08fa43f79d440a9be9b95f24.s1.eu.hivemq.cloud', 8884, clientId);
            this.mqttClient.path = '/mqtt';
            
            this.mqttClient.onConnectionLost = (responseObject) => {
                this.mqttConnected = false;
                if (responseObject.errorCode !== 0) {
                    this.addLog('⚠ MQTT kết nối bị mất: ' + responseObject.errorMessage, 'warning');
                }
            };
            
            this.mqttClient.onMessageArrived = (message) => {
                this.handleMQTTMessage(message);
            };
            
            const options = {
                userName: 'DACDT2026',
                password: 'trungaN123@',
                useSSL: true,
                onSuccess: () => {
                    this.mqttConnected = true;
                    this.addLog('✓ MQTT kết nối thành công (HiveMQ Cloud)', 'info');
                    this.subscribeMQTTTopics();
                },
                onFailure: (invocationContext, errorCode, errorMessage) => {
                    this.addLog('✗ MQTT kết nối thất bại: ' + errorMessage, 'error');
                }
            };
            
            this.mqttClient.connect(options);
        } catch (error) {
            this.addLog('✗ Lỗi MQTT: ' + error.message, 'error');
        }
    }

    subscribeMQTTTopics() {
        const topics = [
            'dacdt/telemetry/position',
            'dacdt/telemetry/job',
            'dacdt/telemetry/system',
            'dacdt/state/current'
        ];
        
        topics.forEach(topic => {
            this.mqttClient.subscribe(topic, {
                onSuccess: () => {
                    this.addLog('✓ Đã subscribe: ' + topic, 'info');
                },
                onFailure: () => {
                    this.addLog('✗ Subscribe thất bại: ' + topic, 'warning');
                }
            });
        });
    }

    handleMQTTMessage(message) {
        try {
            const topic = message.destinationName;
            const payload = message.payloadString;
            const data = JSON.parse(payload);
            
            if (topic === 'dacdt/telemetry/position') {
                this.coordinates = {
                    x: data.x || this.coordinates.x,
                    y: data.y || this.coordinates.y,
                    z: data.z || this.coordinates.z,
                    maxX: this.coordinates.maxX,
                    maxY: this.coordinates.maxY,
                    maxZ: this.coordinates.maxZ
                };
            } else if (topic === 'dacdt/telemetry/job') {
                if (data.fileName) this.currentJob.fileName = data.fileName;
                if (data.fileType) this.currentJob.fileType = data.fileType;
                if (data.progress !== undefined) this.currentJob.progress = data.progress;
                if (data.speed !== undefined) this.currentJob.speed = data.speed;
                if (data.temperature !== undefined) this.currentJob.temperature = data.temperature;
            } else if (topic === 'dacdt/telemetry/system') {
                if (data.errors !== undefined) this.systemStats.errors = data.errors;
                if (data.warnings !== undefined) this.systemStats.warnings = data.warnings;
                if (data.successfulJobs !== undefined) this.systemStats.successfulJobs = data.successfulJobs;
                if (data.failedJobs !== undefined) this.systemStats.failedJobs = data.failedJobs;
            } else if (topic === 'dacdt/state/current') {
                if (data.isRunning !== undefined) this.isRunning = data.isRunning;
            }
        } catch (error) {
            console.error('Lỗi xử lý MQTT message:', error);
        }
    }

    setupEventListeners() {
        const btnStart = document.getElementById('btn-start');
        const btnPause = document.getElementById('btn-pause');
        const btnStop = document.getElementById('btn-stop');
        const btnReset = document.getElementById('btn-reset');

        if (btnStart) btnStart.addEventListener('click', () => this.startJob());
        if (btnPause) btnPause.addEventListener('click', () => this.pauseJob());
        if (btnStop) btnStop.addEventListener('click', () => this.stopJob());
        if (btnReset) btnReset.addEventListener('click', () => this.resetSystem());
    }

    startSimulation() {
        // Giả lập kết nối PLC sau 500ms
        setTimeout(() => {
            this.connectPLC();
        }, 500);

        // Cập nhật dashboard mỗi 500ms
        setInterval(() => {
            if (this.isConnected) {
                this.updateDashboard();
            }
        }, 500);
    }

    connectPLC() {
        this.isConnected = true;
        const statusDot = document.getElementById('plc-status')?.querySelector('.status-dot');
        if (statusDot) {
            statusDot.classList.add('online');
            statusDot.classList.remove('offline');
        }
        document.getElementById('status-text').textContent = 'Đã kết nối PLC';
        document.getElementById('plc-state').textContent = 'Online';
        document.getElementById('connection-status').textContent = 'Kết nối';
        this.addLog('✓ Kết nối PLC Mitsubishi QD75 thành công', 'info');
    }

    updateDashboard() {
        this.renderCoordinates();
        this.renderJobInfo();
        this.renderLogs();
    }

    renderCoordinates() {
        document.getElementById('coord-x').textContent = this.coordinates.x.toFixed(2) + ' mm';
        document.getElementById('coord-y').textContent = this.coordinates.y.toFixed(2) + ' mm';
        document.getElementById('coord-z').textContent = this.coordinates.z.toFixed(2) + ' mm';

        // Update progress bars
        document.getElementById('progress-x').style.width = (this.coordinates.x / this.coordinates.maxX * 100) + '%';
        document.getElementById('progress-y').style.width = (this.coordinates.y / this.coordinates.maxY * 100) + '%';
        document.getElementById('progress-z').style.width = (this.coordinates.z / this.coordinates.maxZ * 100) + '%';
    }

    renderJobInfo() {
        document.getElementById('file-name').textContent = this.currentJob.fileName;
        document.getElementById('file-type').textContent = this.currentJob.fileType;
        
        const progressFill = document.getElementById('job-progress');
        progressFill.style.width = this.currentJob.progress + '%';
        progressFill.textContent = this.currentJob.progress + '%';
        
        document.getElementById('speed-value').textContent = this.currentJob.speed.toFixed(1) + ' mm/min';
        document.getElementById('temperature').textContent = this.currentJob.temperature.toFixed(1) + '°C';
        
        const time = this.formatTime(this.currentJob.elapsedTime);
        document.getElementById('elapsed-time').textContent = time;

        document.getElementById('error-count').textContent = this.systemStats.errors;
        document.getElementById('warning-count').textContent = this.systemStats.warnings;
        document.getElementById('success-count').textContent = this.systemStats.successfulJobs;
        document.getElementById('failed-count').textContent = this.systemStats.failedJobs;
    }

    renderLogs() {
        const logContainer = document.getElementById('log-container');
        if (!logContainer) return;

        // Giữ lại 20 logs mới nhất
        const recentLogs = this.logs.slice(-20);
        logContainer.innerHTML = recentLogs.map(log => `
            <div class="log-entry ${log.type}">
                <span class="log-time">[${log.time}]</span>
                <span class="log-message">${log.message}</span>
            </div>
        `).join('');

        // Scroll xuống dưới cùng
        logContainer.scrollTop = logContainer.scrollHeight;
    }

    addLog(message, type = 'info') {
        const now = new Date();
        const timeStr = now.toLocaleTimeString('vi-VN');
        this.logs.push({
            message: message,
            type: type,
            time: timeStr
        });
        this.renderLogs();
    }

    startJob() {
        if (this.currentJob.progress === 0 && this.isConnected) {
            this.isRunning = true;
            this.currentJob.fileName = 'DXF_Run_' + new Date().toISOString().slice(0, 19).replace(/:/g, '-') + '.dxf';
            this.currentJob.fileType = 'DXF';
            this.currentJob.progress = 1;
            this.currentJob.elapsedTime = 0;
            this.currentJob.temperature = 25;
            this.addLog('▶ Bắt đầu công việc: ' + this.currentJob.fileName, 'info');
            
            // Giả lập tiến độ công việc
            this.jobProgressInterval = setInterval(() => {
                if (this.isRunning && this.currentJob.progress < 100) {
                    this.currentJob.progress += Math.random() * 3 + 1;
                    this.currentJob.speed = 500 + Math.random() * 1500;
                    this.currentJob.elapsedTime++;
                    this.currentJob.temperature = 25 + Math.random() * 20;
                    
                    // Simulate coordinate movement
                    this.coordinates.x += (Math.random() - 0.5) * 10;
                    this.coordinates.y += (Math.random() - 0.5) * 10;
                    this.coordinates.z += (Math.random() - 0.5) * 5;
                    
                    // Constraints
                    this.coordinates.x = Math.max(0, Math.min(this.coordinates.maxX, this.coordinates.x));
                    this.coordinates.y = Math.max(0, Math.min(this.coordinates.maxY, this.coordinates.y));
                    this.coordinates.z = Math.max(0, Math.min(this.coordinates.maxZ, this.coordinates.z));
                    
                    if (this.currentJob.progress >= 100) {
                        this.completeJob();
                    }
                }
            }, 1000);
        }
    }

    pauseJob() {
        if (this.isRunning && this.currentJob.progress > 0 && this.currentJob.progress < 100) {
            this.isRunning = false;
            clearInterval(this.jobProgressInterval);
            this.addLog('⏸ Tạm dừng công việc', 'warning');
        }
    }

    stopJob() {
        if (this.currentJob.progress > 0) {
            this.isRunning = false;
            clearInterval(this.jobProgressInterval);
            this.currentJob.progress = 0;
            this.currentJob.elapsedTime = 0;
            this.coordinates = { x: 0, y: 0, z: 0, maxX: 200, maxY: 200, maxZ: 100 };
            this.addLog('⏹ Dừng công việc', 'warning');
        }
    }

    resetSystem() {
        clearInterval(this.jobProgressInterval);
        this.isRunning = false;
        this.currentJob = {
            fileName: 'Không có file',
            fileType: '-',
            progress: 0,
            elapsedTime: 0,
            speed: 0,
            temperature: 0
        };
        this.coordinates = { x: 0, y: 0, z: 0, maxX: 200, maxY: 200, maxZ: 100 };
        this.addLog('🔄 Hệ thống đã reset', 'info');
        this.renderCoordinates();
        this.renderJobInfo();
    }

    completeJob() {
        clearInterval(this.jobProgressInterval);
        this.isRunning = false;
        this.currentJob.progress = 100;
        this.systemStats.successfulJobs++;
        this.systemStats.totalTime += this.currentJob.elapsedTime;
        this.addLog('✓ Công việc hoàn thành: ' + this.currentJob.fileName, 'info');
    }

    formatTime(seconds) {
        const hrs = Math.floor(seconds / 3600);
        const mins = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;
        return `${String(hrs).padStart(2, '0')}:${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
    }
}

// Khởi tạo ứng dụng khi DOM loaded
document.addEventListener('DOMContentLoaded', () => {
    window.dashboard = new DAcDTDashboard();
});