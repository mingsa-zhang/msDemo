let recentValidations = [];
let audioSuccess = null;
let audioError = null;

document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('validateForm');
    const accessCodeInput = document.getElementById('accessCode');
    const validateBtn = document.getElementById('validateBtn');

    // 创建音频对象（用于提示音）
    try {
        audioSuccess = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBix+zPLTgjMGHm7A7+OZURE');
        audioError = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBix+zPLTgjMGHm7A7+OZURE');
    } catch (e) {
        console.log('音频初始化失败:', e);
    }

    // 聚焦到通行码输入框
    accessCodeInput.focus();

    // 表单提交处理
    form.addEventListener('submit', async function(e) {
        e.preventDefault();

        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        const accessCode = accessCodeInput.value.trim();
        await validateAccessCode(accessCode);
    });

    // 通行码输入处理
    accessCodeInput.addEventListener('input', function(e) {
        // 只允许输入数字
        this.value = this.value.replace(/\D/g, '');

        // 自动提交当输入6位数字时
        if (this.value.length === 6) {
            form.dispatchEvent(new Event('submit'));
        }
    });

    // 键盘快捷键支持
    document.addEventListener('keydown', function(e) {
        // Ctrl+Enter 或 F5 触发验证
        if ((e.ctrlKey && e.key === 'Enter') || e.key === 'F5') {
            e.preventDefault();
            if (accessCodeInput.value.length === 6) {
                form.dispatchEvent(new Event('submit'));
            }
        }

        // Escape 清空输入
        if (e.key === 'Escape') {
            clearForm();
        }
    });
});

// 验证通行码
async function validateAccessCode(accessCode) {
    setValidating(true);

    try {
        const response = await fetch('/api/visitor/validate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ accessCode: accessCode })
        });

        const result = await response.json();
        displayValidationResult(result, accessCode);

        // 播放提示音
        playSound(result.isValid);

        // 添加到最近验证记录
        addToRecentValidations(result, accessCode);

    } catch (error) {
        console.error('验证失败:', error);
        displayError('网络错误，请检查连接后重试');
        playSound(false);
    } finally {
        setValidating(false);
    }
}

// 显示验证结果
function displayValidationResult(result, accessCode) {
    const resultArea = document.getElementById('resultArea');
    const resultContent = document.getElementById('resultContent');

    if (result.isValid) {
        // 验证成功
        resultContent.innerHTML = `
            <div class="alert alert-success alert-dismissible fade show" role="alert">
                <div class="d-flex align-items-center">
                    <i class="bi bi-check-circle-fill me-2" style="font-size: 1.5rem;"></i>
                    <div>
                        <h5 class="alert-heading mb-1">验证成功！</h5>
                        <p class="mb-0">${result.message}</p>
                    </div>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
            ${result.visitorInfo ? `
                <div class="card border-success">
                    <div class="card-body">
                        <h6 class="card-title text-success">
                            <i class="bi bi-person-check"></i>
                            访客信息
                        </h6>
                        <div class="row">
                            <div class="col-sm-6">
                                <strong>姓名：</strong>${result.visitorInfo.name}
                            </div>
                            <div class="col-sm-6">
                                <strong>状态：</strong>
                                <span class="status-badge status-checked-in">
                                    <i class="bi bi-check2"></i>
                                    ${result.visitorInfo.statusText}
                                </span>
                            </div>
                            <div class="col-sm-6 mt-2">
                                <strong>被访人：</strong>${result.visitorInfo.visitedPerson}
                            </div>
                            <div class="col-sm-6 mt-2">
                                <strong>来访事由：</strong>${result.visitorInfo.visitReason}
                            </div>
                        </div>
                    </div>
                </div>
            ` : ''}
        `;
    } else {
        // 验证失败
        resultContent.innerHTML = `
            <div class="alert alert-danger alert-dismissible fade show" role="alert">
                <div class="d-flex align-items-center">
                    <i class="bi bi-x-circle-fill me-2" style="font-size: 1.5rem;"></i>
                    <div>
                        <h5 class="alert-heading mb-1">验证失败</h5>
                        <p class="mb-0">${result.message}</p>
                    </div>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
            ${result.visitorInfo ? `
                <div class="card border-danger">
                    <div class="card-body">
                        <h6 class="card-title text-danger">
                            <i class="bi bi-person-x"></i>
                            访客信息
                        </h6>
                        <div class="row">
                            <div class="col-sm-6">
                                <strong>姓名：</strong>${result.visitorInfo.name}
                            </div>
                            <div class="col-sm-6">
                                <strong>状态：</strong>
                                <span class="status-badge ${getStatusClass(result.visitorInfo.status)}">
                                    ${getStatusIcon(result.visitorInfo.status)}
                                    ${result.visitorInfo.statusText}
                                </span>
                            </div>
                            <div class="col-sm-6 mt-2">
                                <strong>被访人：</strong>${result.visitorInfo.visitedPerson}
                            </div>
                            <div class="col-sm-6 mt-2">
                                <strong>来访事由：</strong>${result.visitorInfo.visitReason}
                            </div>
                        </div>
                    </div>
                </div>
            ` : ''}
        `;
    }

    resultArea.style.display = 'block';
}

// 显示错误信息
function displayError(message) {
    const resultArea = document.getElementById('resultArea');
    const resultContent = document.getElementById('resultContent');

    resultContent.innerHTML = `
        <div class="alert alert-warning alert-dismissible fade show" role="alert">
            <div class="d-flex align-items-center">
                <i class="bi bi-exclamation-triangle-fill me-2" style="font-size: 1.5rem;"></i>
                <div>
                    <h5 class="alert-heading mb-1">系统错误</h5>
                    <p class="mb-0">${message}</p>
                </div>
            </div>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;

    resultArea.style.display = 'block';
}

// 添加到最近验证记录
function addToRecentValidations(result, accessCode) {
    const record = {
        accessCode: accessCode,
        isValid: result.isValid,
        message: result.message,
        timestamp: new Date()
    };

    recentValidations.unshift(record);
    if (recentValidations.length > 10) {
        recentValidations.pop();
    }

    updateRecentValidationsDisplay();
}

// 更新最近验证记录显示
function updateRecentValidationsDisplay() {
    const container = document.getElementById('recentValidations');

    if (recentValidations.length === 0) {
        container.innerHTML = '<p class="text-muted text-center">暂无验证记录</p>';
        return;
    }

    const html = recentValidations.map(record => `
        <div class="d-flex justify-content-between align-items-center border-bottom py-2">
            <div>
                <strong class="code-input">${record.accessCode}</strong>
                <span class="ms-2 ${record.isValid ? 'text-success' : 'text-danger'}">
                    <i class="bi bi-${record.isValid ? 'check' : 'x'}-circle"></i>
                    ${record.isValid ? '成功' : '失败'}
                </span>
            </div>
            <small class="text-muted">${formatTime(record.timestamp)}</small>
        </div>
    `).join('');

    container.innerHTML = html;
}

// 格式化时间
function formatTime(date) {
    const now = new Date();
    const diff = Math.floor((now - date) / 1000);

    if (diff < 60) return '刚刚';
    if (diff < 3600) return `${Math.floor(diff / 60)}分钟前`;
    if (diff < 86400) return `${Math.floor(diff / 3600)}小时前`;

    return date.toLocaleTimeString('zh-CN');
}

// 设置验证状态
function setValidating(validating) {
    const validateBtn = document.getElementById('validateBtn');
    const accessCodeInput = document.getElementById('accessCode');

    validateBtn.disabled = validating;
    accessCodeInput.disabled = validating;

    if (validating) {
        validateBtn.innerHTML = `
            <span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
            验证中...
        `;
    } else {
        validateBtn.innerHTML = `
            <i class="bi bi-check-circle"></i>
            验证通行码
        `;
    }
}

// 清空表单
function clearForm() {
    const form = document.getElementById('validateForm');
    const accessCodeInput = document.getElementById('accessCode');
    const resultArea = document.getElementById('resultArea');

    form.reset();
    form.classList.remove('was-validated');
    resultArea.style.display = 'none';
    accessCodeInput.focus();
}

// 播放提示音
function playSound(success) {
    try {
        if (success && audioSuccess) {
            audioSuccess.play().catch(() => {});
        } else if (!success && audioError) {
            audioError.play().catch(() => {});
        }
    } catch (e) {
        console.log('播放音频失败:', e);
    }
}

// 获取状态样式类
function getStatusClass(status) {
    switch (status) {
        case 0: return 'status-registered';
        case 1: return 'status-checked-in';
        case 2: return 'status-checked-out';
        case 3: return 'status-expired';
        default: return '';
    }
}

// 获取状态图标
function getStatusIcon(status) {
    switch (status) {
        case 0: return '<i class="bi bi-clock"></i>';
        case 1: return '<i class="bi bi-check2"></i>';
        case 2: return '<i class="bi bi-box-arrow-right"></i>';
        case 3: return '<i class="bi bi-x-circle"></i>';
        default: return '';
    }
}