document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('visitorForm');
    const submitBtn = document.getElementById('submitBtn');

    // 表单提交处理
    form.addEventListener('submit', async function(e) {
        e.preventDefault();

        // 表单验证
        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        // 显示加载状态
        setLoading(true);

        // 收集表单数据
        const formData = {
            name: document.getElementById('name').value.trim(),
            phone: document.getElementById('phone').value.trim(),
            idCard: document.getElementById('idCard').value.trim().toUpperCase(),
            visitedPerson: document.getElementById('visitedPerson').value.trim(),
            visitReason: document.getElementById('visitReason').value.trim()
        };

        try {
            const response = await fetch('/api/visitor/register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {
                // 显示成功信息
                showSuccess(result);
                // 重置表单
                form.reset();
                form.classList.remove('was-validated');
            } else {
                // 显示错误信息
                showError(result.message);
            }
        } catch (error) {
            console.error('登记失败:', error);
            showError('网络错误，请检查连接后重试');
        } finally {
            setLoading(false);
        }
    });

    // 设置加载状态
    function setLoading(loading) {
        submitBtn.disabled = loading;
        const spinner = submitBtn.querySelector('.spinner-border');
        if (loading) {
            spinner.classList.remove('d-none');
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> 提交中...';
        } else {
            spinner.classList.add('d-none');
            submitBtn.innerHTML = '提交登记';
        }
    }

    // 显示成功信息
    function showSuccess(result) {
        document.getElementById('accessCodeDisplay').textContent = result.accessCode;
        document.getElementById('visitorName').textContent = result.name;
        document.getElementById('visitorPhone').textContent = result.phone;
        document.getElementById('visitorVisitedPerson').textContent = result.visitedPerson || '-';
        document.getElementById('visitorExpiryTime').textContent = new Date(result.expiryTime).toLocaleString('zh-CN');

        const modal = new bootstrap.Modal(document.getElementById('successModal'));
        modal.show();
    }

    // 显示错误信息
    function showError(message) {
        document.getElementById('errorMessage').textContent = message;
        const modal = new bootstrap.Modal(document.getElementById('errorModal'));
        modal.show();
    }
});

// 打印通行码
function printAccessCode() {
    const accessCode = document.getElementById('accessCodeDisplay').textContent;
    const visitorName = document.getElementById('visitorName').textContent;
    const visitorPhone = document.getElementById('visitorPhone').textContent;
    const visitorVisitedPerson = document.getElementById('visitorVisitedPerson').textContent;
    const visitorExpiryTime = document.getElementById('visitorExpiryTime').textContent;

    const printContent = `
        <div style="text-align: center; padding: 20px; font-family: Arial, sans-serif;">
            <h2 style="color: #0d6efd;">访客通行码</h2>
            <div style="font-size: 48px; font-weight: bold; margin: 20px 0; border: 3px solid #0d6efd; padding: 20px; display: inline-block;">
                ${accessCode}
            </div>
            <div style="margin: 20px 0; text-align: left; max-width: 400px; margin-left: auto; margin-right: auto;">
                <p><strong>访客姓名：</strong>${visitorName}</p>
                <p><strong>联系电话：</strong>${visitorPhone}</p>
                <p><strong>被访人员：</strong>${visitorVisitedPerson}</p>
                <p><strong>有效期限：</strong>${visitorExpiryTime}</p>
            </div>
            <div style="border-top: 1px solid #ccc; padding-top: 10px; margin-top: 20px; font-size: 14px;">
                <p>请妥善保管此通行码，凭此码可通过门禁系统</p>
                <p>生成时间：${new Date().toLocaleString('zh-CN')}</p>
            </div>
        </div>
    `;

    const printWindow = window.open('', '_blank');
    printWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>访客通行码</title>
            <style>
                @media print {
                    body { margin: 0; }
                }
            </style>
        </head>
        <body>
            ${printContent}
        </body>
        </html>
    `);
    printWindow.document.close();
    printWindow.focus();
    printWindow.print();
    printWindow.close();
}

// 输入格式化
document.getElementById('phone').addEventListener('input', function(e) {
    // 只允许输入数字
    this.value = this.value.replace(/\D/g, '');
});

document.getElementById('idCard').addEventListener('input', function(e) {
    // 自动转换为大写
    this.value = this.value.toUpperCase();
});