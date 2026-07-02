let currentRecords = [];
let currentPage = 1;
let pageSize = 20;

document.addEventListener('DOMContentLoaded', function() {
    initializePage();
    loadStatistics();
    bindEvents();
});

// 初始化页面
function initializePage() {
    // 设置默认查询时间为今天
    const today = new Date();
    const startDate = new Date(today.getFullYear(), today.getMonth(), today.getDate(), 0, 0, 0);
    const endDate = new Date(today.getFullYear(), today.getMonth(), today.getDate(), 23, 59, 59);

    document.getElementById('startDate').value = formatDateTimeLocal(startDate);
    document.getElementById('endDate').value = formatDateTimeLocal(endDate);

    // 自动执行今日查询
    searchRecords();
}

// 绑定事件
function bindEvents() {
    // 查询表单提交
    document.getElementById('searchForm').addEventListener('submit', function(e) {
        e.preventDefault();
        currentPage = 1;
        searchRecords();
    });

    // 重置按钮
    document.getElementById('resetBtn').addEventListener('click', resetForm);

    // 刷新按钮
    document.getElementById('refreshBtn').addEventListener('click', function() {
        loadStatistics();
        searchRecords();
    });

    // 导出按钮
    document.getElementById('exportBtn').addEventListener('click', exportToExcel);

    // 搜索框实时搜索
    let searchTimeout;
    document.getElementById('searchInput').addEventListener('input', function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            currentPage = 1;
            searchRecords();
        }, 500);
    });
}

// 加载统计数据
async function loadStatistics() {
    try {
        const response = await fetch('/api/visitor/statistics');
        if (response.ok) {
            const stats = await response.json();
            updateStatistics(stats);
        }
    } catch (error) {
        console.error('加载统计数据失败:', error);
    }
}

// 更新统计数据
function updateStatistics(stats) {
    document.getElementById('totalCount').textContent = stats.totalCount;
    document.getElementById('registeredCount').textContent = stats.registeredCount;
    document.getElementById('checkedInCount').textContent = stats.checkedInCount;
    document.getElementById('checkedOutCount').textContent = stats.checkedOutCount;
}

// 查询记录
async function searchRecords() {
    setSearching(true);

    try {
        const formData = new FormData(document.getElementById('searchForm'));
        const searchInput = document.getElementById('searchInput').value.trim();

        // 构建查询参数
        const params = new URLSearchParams();
        const startDate = formData.get('startDate');
        const endDate = formData.get('endDate');

        if (startDate) {
            params.append('startTime', new Date(startDate).toISOString());
        }
        if (endDate) {
            params.append('endTime', new Date(endDate).toISOString());
        }

        const url = `/api/visitor/records?${params.toString()}`;
        const response = await fetch(url);

        if (!response.ok) {
            throw new Error('查询失败');
        }

        const records = await response.json();
        currentRecords = records;

        // 过滤记录
        let filteredRecords = records;
        if (searchInput) {
            filteredRecords = records.filter(record =>
                record.name.toLowerCase().includes(searchInput.toLowerCase()) ||
                record.phone.includes(searchInput) ||
                record.idCard.includes(searchInput) ||
                record.visitedPerson.toLowerCase().includes(searchInput.toLowerCase())
            );
        }

        displayRecords(filteredRecords);
        document.getElementById('recordCount').textContent = filteredRecords.length;

    } catch (error) {
        console.error('查询记录失败:', error);
        showError('查询失败，请稍后重试');
    } finally {
        setSearching(false);
    }
}

// 显示记录
function displayRecords(records) {
    const tbody = document.getElementById('recordsTableBody');

    if (records.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="9" class="text-center text-muted py-4">
                    <i class="bi bi-inbox" style="font-size: 2rem;"></i>
                    <p class="mb-0 mt-2">没有找到符合条件的记录</p>
                </td>
            </tr>
        `;
        return;
    }

    const html = records.map(record => `
        <tr>
            <td>${record.name}</td>
            <td>${record.phone}</td>
            <td>${maskIdCard(record.idCard)}</td>
            <td>${record.visitedPerson}</td>
            <td>${record.visitReason}</td>
            <td><span class="code-input">${record.accessCode}</span></td>
            <td>${getStatusBadge(record.status)}</td>
            <td>${formatDateTime(record.createdTime)}</td>
            <td>
                <button class="btn btn-sm btn-outline-primary" onclick="showVisitorDetail(${record.id})">
                    <i class="bi bi-eye"></i>
                    详情
                </button>
            </td>
        </tr>
    `).join('');

    tbody.innerHTML = html;
}

// 显示访客详情
function showVisitorDetail(visitorId) {
    const record = currentRecords.find(r => r.id === visitorId);
    if (!record) return;

    const content = `
        <div class="row">
            <div class="col-md-6">
                <h6 class="text-primary">基本信息</h6>
                <table class="table table-borderless">
                    <tr>
                        <td><strong>姓名：</strong></td>
                        <td>${record.name}</td>
                    </tr>
                    <tr>
                        <td><strong>手机号：</strong></td>
                        <td>${record.phone}</td>
                    </tr>
                    <tr>
                        <td><strong>身份证号：</strong></td>
                        <td>${record.idCard}</td>
                    </tr>
                    <tr>
                        <td><strong>被访人：</strong></td>
                        <td>${record.visitedPerson}</td>
                    </tr>
                    <tr>
                        <td><strong>来访事由：</strong></td>
                        <td>${record.visitReason}</td>
                    </tr>
                </table>
            </div>
            <div class="col-md-6">
                <h6 class="text-primary">访问信息</h6>
                <table class="table table-borderless">
                    <tr>
                        <td><strong>通行码：</strong></td>
                        <td><span class="code-input">${record.accessCode}</span></td>
                    </tr>
                    <tr>
                        <td><strong>当前状态：</strong></td>
                        <td>${getStatusBadge(record.status)}</td>
                    </tr>
                    <tr>
                        <td><strong>登记时间：</strong></td>
                        <td>${formatDateTime(record.createdTime)}</td>
                    </tr>
                    <tr>
                        <td><strong>入场时间：</strong></td>
                        <td>${record.checkInTime ? formatDateTime(record.checkInTime) : '未入场'}</td>
                    </tr>
                    <tr>
                        <td><strong>离场时间：</strong></td>
                        <td>${record.checkOutTime ? formatDateTime(record.checkOutTime) : '未离场'}</td>
                    </tr>
                    <tr>
                        <td><strong>有效期至：</strong></td>
                        <td>${formatDateTime(record.expiryTime)}</td>
                    </tr>
                </table>
            </div>
        </div>
    `;

    document.getElementById('visitorDetailContent').innerHTML = content;
    const modal = new bootstrap.Modal(document.getElementById('visitorDetailModal'));
    modal.show();
}

// 获取状态徽章
function getStatusBadge(status) {
    const statusMap = {
        0: { class: 'status-registered', icon: 'clock', text: '已登记' },
        1: { class: 'status-checked-in', icon: 'check2', text: '已入场' },
        2: { class: 'status-checked-out', icon: 'box-arrow-right', text: '已离场' },
        3: { class: 'status-expired', icon: 'x-circle', text: '已过期' }
    };

    const statusInfo = statusMap[status] || { class: '', icon: '', text: '未知' };
    return `
        <span class="status-badge ${statusInfo.class}">
            <i class="bi bi-${statusInfo.icon}"></i>
            ${statusInfo.text}
        </span>
    `;
}

// 身份证号脱敏
function maskIdCard(idCard) {
    if (!idCard || idCard.length < 8) return idCard;
    return idCard.substring(0, 4) + '********' + idCard.substring(idCard.length - 4);
}

// 格式化日期时间
function formatDateTime(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleString('zh-CN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    });
}

// 格式化日期时间为本地格式（用于datetime-local输入框）
function formatDateTimeLocal(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
}

// 重置表单
function resetForm() {
    document.getElementById('searchForm').reset();
    initializePage();
}

// 设置搜索状态
function setSearching(searching) {
    const searchBtn = document.getElementById('searchBtn');
    const refreshBtn = document.getElementById('refreshBtn');
    const exportBtn = document.getElementById('exportBtn');

    searchBtn.disabled = searching;
    refreshBtn.disabled = searching;
    exportBtn.disabled = searching;

    if (searching) {
        searchBtn.innerHTML = `
            <span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
            查询中...
        `;
    } else {
        searchBtn.innerHTML = `
            <i class="bi bi-search"></i>
            查询
        `;
    }
}

// 导出Excel
function exportToExcel() {
    if (currentRecords.length === 0) {
        alert('没有数据可以导出');
        return;
    }

    // 准备CSV数据
    const headers = ['姓名', '手机号', '身份证号', '被访人', '来访事由', '通行码', '状态', '登记时间', '入场时间', '离场时间', '有效期'];
    const rows = currentRecords.map(record => [
        record.name,
        record.phone,
        maskIdCard(record.idCard),
        record.visitedPerson,
        record.visitReason,
        record.accessCode,
        getStatusText(record.status),
        formatDateTime(record.createdTime),
        record.checkInTime ? formatDateTime(record.checkInTime) : '',
        record.checkOutTime ? formatDateTime(record.checkOutTime) : '',
        formatDateTime(record.expiryTime)
    ]);

    // 生成CSV内容
    const csvContent = [headers, ...rows]
        .map(row => row.map(cell => `"${cell}"`).join(','))
        .join('\n');

    // 添加BOM以支持中文
    const BOM = '\uFEFF';
    const csvWithBOM = BOM + csvContent;

    // 创建下载链接
    const blob = new Blob([csvWithBOM], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', `访客记录_${new Date().toLocaleDateString('zh-CN')}.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// 获取状态文本
function getStatusText(status) {
    const statusMap = {
        0: '已登记',
        1: '已入场',
        2: '已离场',
        3: '已过期'
    };
    return statusMap[status] || '未知';
}

// 显示错误信息
function showError(message) {
    const alert = document.createElement('div');
    alert.className = 'alert alert-danger alert-dismissible fade show position-fixed';
    alert.style.cssText = 'top: 20px; right: 20px; z-index: 9999; max-width: 400px;';
    alert.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alert);

    // 3秒后自动消失
    setTimeout(() => {
        if (alert.parentNode) {
            alert.remove();
        }
    }, 3000);
}