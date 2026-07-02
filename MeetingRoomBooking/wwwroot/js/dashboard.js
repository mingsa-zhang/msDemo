const API_BASE = window.location.origin;
let currentUser = null;

// 初始化
document.addEventListener('DOMContentLoaded', function() {
    checkAuth();
    loadUserInfo();
});

// 检查认证
function checkAuth() {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = '/';
    }
}

// 加载用户信息
function loadUserInfo() {
    const userStr = localStorage.getItem('user');
    if (userStr) {
        currentUser = JSON.parse(userStr);
        document.getElementById('userInfo').textContent = `${currentUser.username} (${getRoleName(currentUser.role)})`;

        // 非管理员隐藏管理员标签
        if (currentUser.role !== 1) {
            document.querySelectorAll('.admin-only').forEach(el => el.classList.add('hidden'));
        }

        loadRooms();
    }
}

function getRoleName(role) {
    return role === 1 ? '管理员' : '员工';
}

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/';
}

// API请求封装
async function apiRequest(url, options = {}) {
    const token = localStorage.getItem('token');
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    try {
        const response = await fetch(`${API_BASE}${url}`, {
            ...options,
            headers
        });

        if (response.status === 401) {
            logout();
            return;
        }

        return await response.json();
    } catch (error) {
        console.error('API请求失败:', error);
        alert('网络错误，请重试');
        return null;
    }
}

// 切换主标签
function switchMainTab(tab) {
    document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
    event.target.classList.add('active');

    document.querySelectorAll('.tab-content').forEach(content => content.classList.add('hidden'));

    switch(tab) {
        case 'rooms':
            document.getElementById('roomsTab').classList.remove('hidden');
            loadRooms();
            break;
        case 'my-bookings':
            document.getElementById('myBookingsTab').classList.remove('hidden');
            loadMyBookings();
            break;
        case 'pending-approvals':
            document.getElementById('pendingApprovalsTab').classList.remove('hidden');
            loadPendingApprovals();
            break;
        case 'all-bookings':
            document.getElementById('allBookingsTab').classList.remove('hidden');
            loadAllBookings();
            break;
        case 'statistics':
            document.getElementById('statisticsTab').classList.remove('hidden');
            loadStatistics();
            break;
    }
}

// 加载会议室列表
async function loadRooms() {
    const data = await apiRequest('/api/meetingrooms');
    if (!data) return;

    const html = `
        <table>
            <thead>
                <tr>
                    <th>名称</th>
                    <th>位置</th>
                    <th>容量</th>
                    <th>设备</th>
                    <th>可预约时间</th>
                    <th>操作</th>
                </tr>
            </thead>
            <tbody>
                ${data.map(room => `
                    <tr>
                        <td>${room.name}</td>
                        <td>${room.location || '-'}</td>
                        <td>${room.capacity}人</td>
                        <td>${room.equipment || '-'}</td>
                        <td>${room.availableFrom || '09:00'} - ${room.availableTo || '18:00'}</td>
                        <td>
                            <button class="btn btn-primary" onclick="showBookingModal(${room.id}, '${room.name}')">预约</button>
                            <button class="btn btn-danger admin-only" onclick="deleteRoom(${room.id})" style="${currentUser.role !== 1 ? 'display:none' : ''}">删除</button>
                        </td>
                    </tr>
                `).join('')}
            </tbody>
        </table>
    `;

    document.getElementById('roomsList').innerHTML = html;
}

// 加载我的预约
async function loadMyBookings() {
    const data = await apiRequest('/api/bookings/my');
    if (!data) return;

    const html = data.length === 0 ?
        '<p style="text-align:center;color:#999;padding:40px;">暂无预约记录</p>' :
        `<table>
            <thead>
                <tr>
                    <th>会议室</th>
                    <th>会议主题</th>
                    <th>开始时间</th>
                    <th>结束时间</th>
                    <th>状态</th>
                    <th>操作</th>
                </tr>
            </thead>
            <tbody>
                ${data.map(booking => `
                    <tr>
                        <td>${booking.roomName}</td>
                        <td>${booking.title}</td>
                        <td>${formatDateTime(booking.startTime)}</td>
                        <td>${formatDateTime(booking.endTime)}</td>
                        <td><span class="status ${getStatusClass(booking.status)}">${getStatusName(booking.status)}</span></td>
                        <td>
                            ${booking.status === 0 ? `<button class="btn btn-danger" onclick="cancelBooking(${booking.id})">取消预约</button>` : '-'}
                        </td>
                    </tr>
                `).join('')}
            </tbody>
        </table>`;

    document.getElementById('myBookingsList').innerHTML = html;
}

// 加载待审批预约
async function loadPendingApprovals() {
    const data = await apiRequest('/api/admin/bookings/pending');
    if (!data) return;

    const html = data.length === 0 ?
        '<p style="text-align:center;color:#999;padding:40px;">暂无待审批预约</p>' :
        `<table>
            <thead>
                <tr>
                    <th>申请人</th>
                    <th>会议室</th>
                    <th>会议主题</th>
                    <th>开始时间</th>
                    <th>结束时间</th>
                    <th>操作</th>
                </tr>
            </thead>
            <tbody>
                ${data.map(booking => `
                    <tr>
                        <td>${booking.userName}</td>
                        <td>${booking.roomName}</td>
                        <td>${booking.title}</td>
                        <td>${formatDateTime(booking.startTime)}</td>
                        <td>${formatDateTime(booking.endTime)}</td>
                        <td>
                            <button class="btn btn-success" onclick="approveBooking(${booking.id}, true)">批准</button>
                            <button class="btn btn-danger" onclick="approveBooking(${booking.id}, false)">拒绝</button>
                        </td>
                    </tr>
                `).join('')}
            </tbody>
        </table>`;

    document.getElementById('pendingList').innerHTML = html;
}

// 加载所有预约
async function loadAllBookings() {
    const data = await apiRequest('/api/admin/bookings');
    if (!data) return;

    const html = data.length === 0 ?
        '<p style="text-align:center;color:#999;padding:40px;">暂无预约记录</p>' :
        `<table>
            <thead>
                <tr>
                    <th>申请人</th>
                    <th>会议室</th>
                    <th>会议主题</th>
                    <th>开始时间</th>
                    <th>结束时间</th>
                    <th>状态</th>
                </tr>
            </thead>
            <tbody>
                ${data.map(booking => `
                    <tr>
                        <td>${booking.userName}</td>
                        <td>${booking.roomName}</td>
                        <td>${booking.title}</td>
                        <td>${formatDateTime(booking.startTime)}</td>
                        <td>${formatDateTime(booking.endTime)}</td>
                        <td><span class="status ${getStatusClass(booking.status)}">${getStatusName(booking.status)}</span></td>
                    </tr>
                `).join('')}
            </tbody>
        </table>`;

    document.getElementById('allBookingsList').innerHTML = html;
}

// 加载统计数据
async function loadStatistics() {
    const endDate = new Date().toISOString().split('T')[0];
    const startDate = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];

    const data = await apiRequest(`/api/admin/statistics?start=${startDate}&end=${endDate}`);
    if (!data) return;

    const html = `
        <div class="stats-grid">
            <div class="stat-card">
                <h4>总预约数</h4>
                <div class="value">${data.totalBookings}</div>
            </div>
            <div class="stat-card">
                <h4>已批准</h4>
                <div class="value">${data.approvedBookings}</div>
            </div>
            <div class="stat-card">
                <h4>总体使用率</h4>
                <div class="value">${data.overallUtilizationRate.toFixed(1)}%</div>
            </div>
        </div>

        <div class="card">
            <h3>会议室使用排行</h3>
            ${data.roomUsages.length > 0 ? `
                <table>
                    <thead>
                        <tr>
                            <th>会议室</th>
                            <th>预约次数</th>
                            <th>总时长</th>
                            <th>使用率</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${data.roomUsages.map(room => `
                            <tr>
                                <td>${room.roomName}</td>
                                <td>${room.bookingCount}</td>
                                <td>${room.totalHours.toFixed(1)}小时</td>
                                <td>${room.utilizationRate.toFixed(1)}%</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            ` : '<p style="text-align:center;color:#999;padding:20px;">暂无数据</p>'}
        </div>

        <div class="card">
            <h3>部门使用排行</h3>
            ${data.departmentUsages.length > 0 ? `
                <table>
                    <thead>
                        <tr>
                            <th>部门</th>
                            <th>预约次数</th>
                            <th>总时长</th>
                            <th>占比</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${data.departmentUsages.map(dept => `
                            <tr>
                                <td>${dept.department}</td>
                                <td>${dept.bookingCount}</td>
                                <td>${dept.totalHours.toFixed(1)}小时</td>
                                <td>${dept.percentage.toFixed(1)}%</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            ` : '<p style="text-align:center;color:#999;padding:20px;">暂无数据</p>'}
        </div>
    `;

    document.getElementById('statisticsContent').innerHTML = html;
}

// 显示预约模态框
function showBookingModal(roomId, roomName) {
    document.getElementById('bookingRoomId').value = roomId;
    document.getElementById('bookingRoomName').value = roomName;

    // 设置默认时间为当前时间的下一个整点
    const now = new Date();
    now.setMinutes(0);
    now.setSeconds(0);
    now.setMilliseconds(0);
    now.setHours(now.getHours() + 1);

    const end = new Date(now);
    end.setHours(end.getHours() + 1);

    document.getElementById('bookingStartTime').value = toLocalISOString(now);
    document.getElementById('bookingEndTime').value = toLocalISOString(end);

    document.getElementById('bookingModal').classList.add('active');
}

// 创建预约
async function createBooking(e) {
    e.preventDefault();

    const booking = {
        roomId: parseInt(document.getElementById('bookingRoomId').value),
        title: document.getElementById('bookingTitle').value,
        startTime: new Date(document.getElementById('bookingStartTime').value),
        endTime: new Date(document.getElementById('bookingEndTime').value),
        attendeeCount: document.getElementById('bookingAttendeeCount').value ? parseInt(document.getElementById('bookingAttendeeCount').value) : null,
        description: document.getElementById('bookingDescription').value
    };

    const result = await apiRequest('/api/bookings', {
        method: 'POST',
        body: JSON.stringify(booking)
    });

    if (result) {
        alert('预约申请已提交，等待管理员审批');
        closeModal('bookingModal');
        e.target.reset();
        loadMyBookings();
    }
}

// 取消预约
async function cancelBooking(id) {
    if (!confirm('确定要取消此预约吗？')) return;

    const result = await apiRequest(`/api/bookings/${id}/cancel`, {
        method: 'POST'
    });

    if (result) {
        alert('预约已取消');
        loadMyBookings();
    }
}

// 审批预约
async function approveBooking(id, approved) {
    if (approved) {
        if (!confirm('确定批准此预约吗？')) return;
    } else {
        const reason = prompt('请输入拒绝原因：');
        if (reason === null) return;
    }

    const result = await apiRequest(`/api/admin/bookings/${id}/approve`, {
        method: 'POST',
        body: JSON.stringify({
            approved,
            reason: !approved ? document.querySelector('input[placeholder="请输入拒绝原因"]')?.value : null
        })
    });

    if (result) {
        alert(approved ? '已批准' : '已拒绝');
        loadPendingApprovals();
    }
}

// 新增会议室
async function addRoom(e) {
    e.preventDefault();

    const room = {
        name: document.getElementById('roomName').value,
        location: document.getElementById('roomLocation').value,
        capacity: parseInt(document.getElementById('roomCapacity').value),
        equipment: document.getElementById('roomEquipment').value,
        availableFrom: document.getElementById('roomAvailableFrom').value,
        availableTo: document.getElementById('roomAvailableTo').value
    };

    const result = await apiRequest('/api/meetingrooms', {
        method: 'POST',
        body: JSON.stringify(room)
    });

    if (result) {
        alert('会议室添加成功');
        closeModal('addRoomModal');
        e.target.reset();
        loadRooms();
    }
}

// 删除会议室
async function deleteRoom(id) {
    if (!confirm('确定要删除此会议室吗？')) return;

    const result = await apiRequest(`/api/meetingrooms/${id}`, {
        method: 'DELETE'
    });

    if (result) {
        alert('会议室已删除');
        loadRooms();
    }
}

// 显示添加会议室模态框
function showAddRoomModal() {
    document.getElementById('addRoomModal').classList.add('active');
}

// 关闭模态框
function closeModal(modalId) {
    document.getElementById(modalId).classList.remove('active');
}

// 工具函数
function formatDateTime(dateStr) {
    const date = new Date(dateStr);
    return date.toLocaleString('zh-CN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function toLocalISOString(date) {
    const offset = date.getTimezoneOffset() * 60000;
    const localISOTime = (new Date(date - offset)).toISOString().slice(0, 16);
    return localISOTime;
}

function getStatusClass(status) {
    const classes = {
        0: 'status-pending',
        1: 'status-approved',
        2: 'status-rejected',
        3: 'status-cancelled'
    };
    return classes[status] || '';
}

function getStatusName(status) {
    const names = {
        0: '待审批',
        1: '已批准',
        2: '已拒绝',
        3: '已取消'
    };
    return names[status] || '未知';
}
